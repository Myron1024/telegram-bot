using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBot.WebHook.Models;
using TelegramBot.WebHook.Services;
using Newtonsoft.Json;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace TelegramBot.WebHook.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger _logger;

        private readonly IBotService _botService;

        private readonly IHostingEnvironment _hostingEnvironment;

        private readonly IMessageServer _messageServer;
        private readonly IConfiguration _configuration;

        private int defaultID = 0;

        private static string botName = "";

        public HomeController(ILogger<HomeController> logger, IBotService botService, IHostingEnvironment hostingEnvironment, IMessageServer messageServer, IConfiguration configuration)
        {
            _logger = logger;
            _botService = botService;
            _hostingEnvironment = hostingEnvironment;
            _messageServer = messageServer;
            _configuration = configuration;

            defaultID = _configuration.GetValue<int>("DefaultID");

            botName = _botService.Client.GetMeAsync().Result.Username;
        }

        public IActionResult Index()
        {
            var me = _botService.Client.GetMeAsync().Result;
            bool isReceiving =_botService.Client.IsReceiving;
            ViewBag.Title = "获取Bot：@" + me.Username;
            ViewBag.IsReceiving = isReceiving;
            WebhookInfo hook = _botService.Client.GetWebhookInfoAsync().Result;
            ViewBag.WebHookInfo = JsonConvert.SerializeObject(hook);
            return View();
        }

        [HttpPost]
        public IActionResult SetWebhook(string webhook)
        {
            _logger.LogInformation("SetWebhook:" + webhook);
            _botService.Client.SetWebhookAsync(webhook).Wait();
            return Redirect("/");
        }

        [HttpPost]
        public IActionResult DeleteWebhook()
        {
            _botService.Client.DeleteWebhookAsync().Wait();
            return Redirect("/");
        }

        [HttpPost]
        public IActionResult getUpdates()
        {
            _botService.Client.OnMessage += BotOnMessageReceived;
            _botService.Client.StartReceiving(Array.Empty<UpdateType>());
            ViewBag.Msg = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} 开启监听TG机器人： @{botName}";
            return View();
        }

        [HttpPost]
        public IActionResult StopGetUpdates()
        {
            try
            {
                bool isReceiving = _botService.Client.IsReceiving;
                if (isReceiving)
                {
                    _botService.Client.StopReceiving();
                }
                return Redirect("/");
            }
            catch (Exception ex)
            {
                _logger.LogInformation("StopGetUpdates异常: " + ex.Message);
                return Redirect("/");
            }
        }
        
        private async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            var Bot = _botService.Client;
            var message = messageEventArgs.Message;         // 获取消息实体
            try
            {
                await _messageServer.OnMessageReceived(Bot, message);
            }
            catch (Exception ex)
            {
                _logger.LogInformation("OnMessageReceived 异常: " + ex.Message);
            }
        }

        /// <summary>
        /// 程序请求 推送通知接口
        /// </summary>
        /// <param name="platType">平台类型 1、第三方回调支付中心处理失败；2、支付中心回调乐淘失败；3、乐淘回调处理失败</param>
        /// <param name="orderID">充值订单号</param>
        /// <param name="amount">订单金额</param>
        /// <param name="remark">备注信息，如填写失败原因</param>
        /// <param name="token">orderID + amount 的MD5Hash值</param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult PushNotice(string platType, string orderID, string amount, string remark, string token)
        {
            try
            {
                var t = Md5Helper.ToMD5Hash(orderID + amount);
                if (string.IsNullOrEmpty(token) || t != token)
                {
                    _logger.LogInformation("token不正确.[t:" + t + "][token:" + token + "]");
                    return Content("token不正确");
                }
                else
                {
                    var ConfigPath = _hostingEnvironment.ContentRootPath + "/App_Data";
                    var noticeUsersFile = "noticeUsers.txt";    //接收通知的 用户ID配置
                    var chats = System.IO.File.ReadAllText(ConfigPath + "\\" + noticeUsersFile);
                    if (!string.IsNullOrEmpty(chats))
                    {
                        var platMemo = platType == "1" ? "第三方回调支付中心处理失败" : (platType == "2" ? "支付中心回调乐淘失败" : "乐淘回调处理失败");

                        List<string> userArr = chats.TrimEnd(',').Split(',').Distinct().ToList();
                        userArr.ForEach((x) =>
                        {
                            _botService.Client.SendTextMessageAsync(x, $"[{platMemo}]\n订单号 [{orderID}]，金额 [{amount}]，失败原因：{remark}", ParseMode.Html);
                        });
                    }
                    return Content("ok");
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation("PushNotice异常. " + ex.Message);
                return Content("error");
            }
        }

        [HttpPost]
        public IActionResult MD5Hash(string txtStr)
        {
            return Content(Md5Helper.ToMD5Hash(txtStr));
        }



        #region Web Use

        public IActionResult Test(string id)
        {
            if (string.IsNullOrEmpty(id) || id != ("My" + DateTime.Now.ToString("yyyyMMdd")))
            {
                return Content("Hi");
            }
            else
            {
                var ConfigPath = _hostingEnvironment.ContentRootPath + "/App_Data";
                List<MsgDataEntity> list = new List<MsgDataEntity>();
                DateTime now = DateTime.Now;
                for (int i = 0; i < 20; i++)
                {
                    var MsgDataFile = "MsgData_" + now.AddDays(-1 * i).ToString("yyyy_MM_dd") + ".txt";            //消息ID配置
                    var dataFile = ConfigPath + "/" + MsgDataFile;

                    if (System.IO.File.Exists(dataFile))
                    {
                        var MsgData = System.IO.File.ReadAllText(dataFile);

                        MsgData = Md5Helper.De_DES(MsgData);    //解密

                        var listDay = JsonConvert.DeserializeObject<List<MsgDataEntity>>(MsgData);
                        if (listDay != null && listDay.Count > 0)
                        {
                            list.AddRange(listDay);
                        }
                    }
                }
                if (list != null && list.Count > 0)
                {
                    list = list.OrderBy(x => x.msgTime).ToList();
                }
                //------------------
                //string content = JsonConvert.SerializeObject(list);
                //System.IO.File.WriteAllText(ConfigPath + "/" + "MsgData_" + DateTime.Now.ToString("yyyy_MM_dd") + ".txt", Md5Helper.En_DES(content));
                //------------------
                ViewBag.List = list;
                return View();
            }
        }

        [HttpPost]
        public async Task<IActionResult> TestPost(string txtMessage)
        {
            try
            {
                if (txtMessage == "/clear")
                {
                    var ConfigPath = _hostingEnvironment.ContentRootPath + "/App_Data";
                    var MsgDataFile = "MsgData_" + DateTime.Now.ToString("yyyy_MM_dd") + ".txt";            //消息ID配置
                    var dataFile = ConfigPath + "/" + MsgDataFile;

                    if (System.IO.File.Exists(dataFile))
                    {
                        var MsgData = System.IO.File.ReadAllText(dataFile);
                        List<MsgDataEntity> list = Newtonsoft.Json.JsonConvert.DeserializeObject<List<MsgDataEntity>>(MsgData);
                        var deleteList = list.Where(x => x.msgTime >= DateTime.Now.AddHours(-48) && x.isDelete == 0).ToList();

                        foreach (var item in deleteList)
                        {
                            list.Remove(item);
                            await _botService.Client.DeleteMessageAsync(item.chatID, item.msgID);
                            item.isDelete = 1;
                            list.Add(item);
                        }

                        string content = JsonConvert.SerializeObject(list);
                        System.IO.File.WriteAllText(dataFile, content);

                        return Content("0");
                    }
                    else
                    {
                        return Content("2");
                    }
                }
                else
                {
                    Message botMsg = await _botService.Client.SendTextMessageAsync(defaultID, txtMessage);

                    var ConfigPath = _hostingEnvironment.ContentRootPath + "/App_Data";
                    var MsgDataFile = "MsgData_" + DateTime.Now.ToString("yyyy_MM_dd") + ".txt";            //消息ID配置
                    var dataFile = ConfigPath + "/" + MsgDataFile;

                    List<MsgDataEntity> list;
                    if (System.IO.File.Exists(dataFile))
                    {
                        var MsgData = System.IO.File.ReadAllText(dataFile);
                        list = JsonConvert.DeserializeObject<List<MsgDataEntity>>(MsgData);
                        if (list == null)
                        {
                            list = new List<MsgDataEntity>();
                        }
                    }
                    else
                    {
                        list = new List<MsgDataEntity>();
                    }

                    MsgDataEntity model = new MsgDataEntity();
                    model.userID = botMsg.From.Id;
                    model.userName = botMsg.From.Username;
                    model.chatID = defaultID;
                    model.msgID = botMsg.MessageId;
                    model.msgText = txtMessage;
                    model.msgTime = botMsg.Date;
                    model.isDelete = 0;
                    list.Add(model);

                    string content = JsonConvert.SerializeObject(list);
                    System.IO.File.WriteAllText(dataFile, content);

                    return Content("0");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "web post msg error:" + ex.Message);
                return Content("1");
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMsg(string txtMessageID)
        {
            try
            {
                await _botService.Client.DeleteMessageAsync(defaultID, int.Parse(txtMessageID));


                var ConfigPath = _hostingEnvironment.ContentRootPath + "/App_Data";
                var MsgDataFile = "MsgData_" + DateTime.Now.ToString("yyyy_MM_dd") + ".txt";            //消息ID配置
                var dataFile = ConfigPath + "/" + MsgDataFile;

                if (System.IO.File.Exists(dataFile))
                {
                    var MsgData = System.IO.File.ReadAllText(dataFile);
                    List<MsgDataEntity> list = JsonConvert.DeserializeObject<List<MsgDataEntity>>(MsgData);
                    if (list != null)
                    {
                        list.ForEach(i => {
                            if (i.msgID == int.Parse(txtMessageID))
                            {
                                i.isDelete = 1;
                            }
                        });
                        string content = JsonConvert.SerializeObject(list);
                        System.IO.File.WriteAllText(dataFile, content);
                    }
                }

                return Content("0");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "web delete msg error:" + ex.Message);
                return Content("1");
            }
        }


        #endregion

    }
}