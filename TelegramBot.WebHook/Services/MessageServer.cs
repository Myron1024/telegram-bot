using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBot.WebHook.Models;
using TelegramBot.WebHook.Services;
using Newtonsoft.Json;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.Extensions.Logging;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.IO;

namespace TelegramBot.WebHook.Services
{
    public class MessageServer : IMessageServer
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<MessageServer> _logger;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly int AdminID = 740967577;
        private int defaultID = 0;  //835928787,

        public MessageServer(ILogger<MessageServer> logger, IHostingEnvironment hostingEnvironment, IConfiguration configuration)
        {
            _logger = logger;
            _hostingEnvironment = hostingEnvironment;
            _configuration = configuration;

            defaultID = _configuration.GetValue<int>("DefaultID");
        }


        public async Task OnMessageReceived(TelegramBotClient Bot, Message message)
        {
            string botName = Bot.GetMeAsync().Result.Username;

            var userName = message.From.Username ?? "";     // 发消息人的 UserName
            var firstName = message.From.FirstName ?? "";   // FirstName
            var lastName = message.From.LastName ?? "";     // LastName
            var userID = message.From.Id;                   // 发消息人的ID
            var chatID = message.Chat.Id;                   // 聊天会话ID 个人/chanel/群组
            var msgID = message.MessageId;                  // 聊天记录ID

            _logger.LogInformation(JsonConvert.SerializeObject(message));

            if (message == null || message.Type == MessageType.ChatMembersAdded || message.Type == MessageType.ChatMemberLeft)
            {
                if (message.Type == MessageType.ChatMembersAdded)
                {
                    User[] users = message.NewChatMembers;

                    StringBuilder sb = new StringBuilder();
                    foreach (var u in users)
                    {
                        if (!string.IsNullOrEmpty(u.Username))
                        {
                            sb.Append("<a href=\"tg://user?id=" + u.Id + "\">@" + u.Username + "</a>  ");
                        }
                        else
                        {
                            sb.Append("<a href=\"tg://user?id=" + u.Id + "\">" + u.FirstName + " " + u.LastName + "</a>  ");
                        }
                    }
                    await Bot.SendTextMessageAsync(chatID, "欢迎新成员入群！🎉🎉🎉 " + sb + "\n\n新人请点这里 → /join@All", ParseMode.Html);
                    //await Bot.DeleteMessageAsync(chatID, msgID);
                }
                if (message.Type == MessageType.ChatMemberLeft)
                {
                    User users = message.LeftChatMember;
                    await Bot.SendTextMessageAsync(chatID, users.FirstName + " " + users.LastName + " 已离开此群！", ParseMode.Html);
                    //await Bot.DeleteMessageAsync(chatID, msgID);
                }
                return;
            }

            if (message == null || message.Type != MessageType.Text) return;

            //_logger.LogInformation($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} " + "[From:" + firstName + "(" + userName + ")][chat:" + message.Chat.Username + "][text:" + message.Text + "][msgid:" + message.MessageId + "]");

            var ConfigPath = _hostingEnvironment.ContentRootPath + "/App_Data";

            var noticeUsersFile = "noticeUsers.txt";    //接收通知的 用户ID配置
            var groupUsersFile = "GroupUsers.txt";      //群组加入@all的用户ID配置
            var MsgDataFile = "MsgData_" + DateTime.Now.ToString("yyyy_MM_dd") + ".txt";            //消息ID配置
            var dataFile = ConfigPath + "/" + MsgDataFile;

            var messageText = message.Text;
            var msgTime = message.Date;
            var command = messageText.Trim().Split(' ').First().Replace("@" + botName, "");
            var parameters = messageText.Trim().Replace(command, "").Replace("@" + botName, "");

            // 如果是私聊
            if (message.Chat.Type == ChatType.Private)
            {
                //如果是管理员 或者想要接收的用户ID，则记录消息
                if (userID == defaultID)
                {
                    MsgDataEntity model = new MsgDataEntity();
                    model.userID = userID;
                    model.userName = firstName;
                    model.chatID = (int)chatID;
                    model.msgID = msgID;
                    model.msgText = messageText;
                    model.msgTime = msgTime;
                    model.isDelete = 0;

                    if (System.IO.File.Exists(dataFile))
                    {
                        //如果文件存在.则附加
                        var MsgData = System.IO.File.ReadAllText(dataFile);

                        MsgData = Md5Helper.De_DES(MsgData);    //解密

                        List<MsgDataEntity> list = JsonConvert.DeserializeObject<List<MsgDataEntity>>(MsgData);
                        if (list == null)
                        {
                            list = new List<MsgDataEntity>();
                        }
                        list.Add(model);
                        string content = JsonConvert.SerializeObject(list);
                        System.IO.File.WriteAllText(dataFile, Md5Helper.En_DES(content));
                    }
                    else
                    {
                        //文件不存在
                        using (StreamWriter sw = System.IO.File.CreateText(dataFile))  //创建文件
                        {
                            List<MsgDataEntity> list = new List<MsgDataEntity>();
                            list.Add(model);
                            string content = JsonConvert.SerializeObject(list);
                            sw.Write(Md5Helper.En_DES(content));
                            sw.Close();
                        }
                    }
                }


                switch (command)
                {
                    case "/All":
                    case "/all":
                    case "/join@All":
                        await Bot.SendTextMessageAsync(message.Chat.Id, "请在群组里发送 /join@All 指令即可加入。\n1、把我 @" + botName + " 拉入群组里\n2、在群里组发送 /join@All 即可使用此命令", ParseMode.Default);
                        break;
                    case "broadcast":
                    case "/broadcast":
                        if (userID != AdminID)
                        {
                            await Bot.SendTextMessageAsync(message.Chat.Id, "您不是管理员，没有权限操作！请<a href=\"tg://user?id=" + AdminID + "\">联系管理员</a>添加", ParseMode.Html);
                        }
                        else
                        {
                            var chats = System.IO.File.ReadAllText(ConfigPath + "\\" + noticeUsersFile);
                            if (!string.IsNullOrEmpty(chats))
                            {
                                List<string> userArr = chats.TrimEnd(',').Split(',').Distinct().ToList();
                                userArr.ForEach((x) =>
                                {
                                    Bot.SendTextMessageAsync(x, "[手动] " + parameters, ParseMode.Html);
                                });

                                _logger.LogInformation(userName + " 发布消息：" + parameters + "； 接收人：" + string.Join(",", userArr.ToArray()));
                            }
                        }
                        break;
                    case "/addNoticeMember":
                        if (userID != AdminID)
                        {
                            await Bot.SendTextMessageAsync(message.Chat.Id, "您不是管理员，没有权限操作！请<a href=\"tg://user?id=" + AdminID + "\">联系管理员</a>添加", ParseMode.Html);
                        }
                        else
                        {
                            try
                            {
                                if (string.IsNullOrEmpty(parameters.Trim()))
                                {
                                    await Bot.SendTextMessageAsync(message.Chat.Id, "用户ID不能为空，请在<code>/addNoticeMember</code>后面加上要添加的用户ID，并用空格隔开，一次只能添加一个。\n<code>例:/addNoticeMember 123456789</code>", ParseMode.Html, replyToMessageId: msgID);
                                }
                                else
                                {
                                    var chats2 = System.IO.File.ReadAllText(ConfigPath + "/" + noticeUsersFile);
                                    if (!string.IsNullOrEmpty(chats2))
                                    {
                                        List<string> userArr = chats2.TrimEnd(',').Split(',').Distinct().ToList();
                                        if (!userArr.Contains(parameters.Trim()))
                                        {
                                            userArr.Add(parameters.Trim());
                                            System.IO.File.WriteAllText(ConfigPath + "/" + noticeUsersFile, string.Join(",", userArr.ToArray()));
                                            await Bot.SendTextMessageAsync(message.Chat.Id, "添加成功，该用户后续将收到推送提醒！", ParseMode.Default, replyToMessageId: msgID);
                                        }
                                        else
                                        {
                                            await Bot.SendTextMessageAsync(message.Chat.Id, "该用户已经在通知人群里啦！无需重复添加！", ParseMode.Default, replyToMessageId: msgID);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "添加用户出错！" + ex.Message);
                                await Bot.SendTextMessageAsync(message.Chat.Id, "Opps！出错了！" + ex.Message, ParseMode.Default);
                            }
                        }
                        break;
                    case "/removeNoticeMember":
                        if (userID != AdminID)
                        {
                            await Bot.SendTextMessageAsync(message.Chat.Id, "您不是管理员，没有权限操作！请<a href=\"tg://user?id=" + AdminID + "\">联系管理员</a>添加", ParseMode.Html);
                        }
                        else
                        {
                            try
                            {
                                if (string.IsNullOrEmpty(parameters.Trim()))
                                {
                                    await Bot.SendTextMessageAsync(message.Chat.Id, "用户ID不能为空，请在<code>/removeNoticeMember</code>后面加上要删除的用户ID，并用空格隔开，一次只能删除一个。\n<code>例:/removeNoticeMember 123456789</code>", ParseMode.Html, replyToMessageId: msgID);
                                }
                                else
                                {
                                    var chats2 = System.IO.File.ReadAllText(ConfigPath + "/" + noticeUsersFile);
                                    if (!string.IsNullOrEmpty(chats2))
                                    {
                                        List<string> userArr = chats2.TrimEnd(',').Split(',').Distinct().ToList();
                                        if (userArr.Contains(parameters.Trim()))
                                        {
                                            userArr.Remove(parameters.Trim());
                                            System.IO.File.WriteAllText(ConfigPath + "/" + noticeUsersFile, string.Join(",", userArr.ToArray()));
                                            await Bot.SendTextMessageAsync(message.Chat.Id, "删除成功！", ParseMode.Default, replyToMessageId: msgID);
                                        }
                                        else
                                        {
                                            await Bot.SendTextMessageAsync(message.Chat.Id, "该用户不在通知人群里！", ParseMode.Default, replyToMessageId: msgID);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "删除用户出错！" + ex.Message);
                                await Bot.SendTextMessageAsync(message.Chat.Id, "Opps！出错了！" + ex.Message, ParseMode.Default);
                            }
                        }
                        break;
                    case "/getMyID":
                        await Bot.SendTextMessageAsync(message.Chat.Id, "你的UserID为：" + userID, ParseMode.Default, replyToMessageId: msgID);
                        break;
                    case "/showAllNoticeMember":
                        var chats3 = System.IO.File.ReadAllText(ConfigPath + "\\" + noticeUsersFile);
                        if (!string.IsNullOrEmpty(chats3))
                        {
                            StringBuilder sb = new StringBuilder();
                            List<string> userArr = chats3.TrimEnd(',').Split(',').Distinct().ToList();
                            userArr.ForEach((x) =>
                            {
                                sb.Append("<a href=\"tg://user?id=" + x + "\">@" + x + "</a>  ");
                            });
                            await Bot.SendTextMessageAsync(chatID, "当前配置的接收通知的人有： " + sb, ParseMode.Html);
                        }
                        else
                        {
                            await Bot.SendTextMessageAsync(chatID, "当前配置的接收通知的人为空，请先 /addNoticeMember 添加接收人", ParseMode.Default);
                        }
                        break;
                    case "/getGoogleAuthCode":
                        var code = "";
                        try
                        {
                            string key = _configuration.GetValue<string>("AdminKey");
                            var strUrl = "http://localhost:44385/Service/CommonHandler.ashx?act=GetCurrentPIN&key=" + key;
                            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(strUrl);
                            httpWebRequest.Method = "GET";
                            httpWebRequest.Timeout = 1000 * 10;
                            httpWebRequest.ContentType = "application/x-www-form-urlencoded";
                            HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                            Stream responseStream = httpWebResponse.GetResponseStream();
                            StreamReader streamReader = new StreamReader(responseStream, Encoding.UTF8);
                            string strResult = streamReader.ReadToEnd();
                            code = strResult;
                        }
                        catch (Exception ex)
                        {
                            code = "获取失败，" + ex.Message + "\n请联系管理员 @Zoom2 ";
                        }
                        await Bot.SendTextMessageAsync(message.Chat.Id, "谷歌验证码为： <code>" + code + "</code>", ParseMode.Html, replyToMessageId: msgID);
                        break;
                    case "/chat":
                        if (userID == AdminID)
                        {
                            var idx = parameters.IndexOf("$");
                            if (idx > 0)
                            {
                                var chatid = parameters.Split("$")[0];
                                var chatMsg = parameters.Split("$")[1];

                                int chID = 0;
                                bool r = int.TryParse(chatid, out chID);
                                if (string.IsNullOrEmpty(chatid) || r == false)
                                { }
                                else
                                {
                                    if (!string.IsNullOrEmpty(chatMsg))
                                    {
                                        await Bot.SendTextMessageAsync(chatid, chatMsg, ParseMode.Html);
                                    }
                                }
                            }
                            else
                            {
                                // 不带ID 则直接给默认用户发信息
                                if (!string.IsNullOrEmpty(parameters))
                                {
                                    int defaultID = _configuration.GetValue<int>("DefaultID");
                                    await Bot.SendTextMessageAsync(defaultID, parameters, ParseMode.Html);
                                }
                            }
                        }
                        break;
                    case "/start":
                    case "/help":
                        string usage = "[Private Chat] 目前只有以下功能测试:" +
                            "\n/join@All    - 加入@All以便在群组里一键@All" +
                            "\n/All    - 一键@（仅在群组里且加入过@All的用户可被提及到）" +
                            (userID == AdminID ? "\n/addNoticeMember    - 添加接收推送提醒的用户（管理员）" : "") +
                            (userID == AdminID ? "\n/removeNoticeMember    - 移除接收推送提醒的用户（管理员）" : "") +
                            (userID == AdminID ? "\n/showAllNoticeMember    - 查看所有接收推送提醒的用户" : "") +
                            (userID == AdminID ? "\n/broadcast    - 模拟给后台配置接收提醒的用户推送消息" : "") +
                            "\n/getMyID    - 查看我的UserID" +
                            "\n/getGoogleAuthCode    - 获取谷歌验证码";
                        await Bot.SendTextMessageAsync(chatID, usage, parseMode: ParseMode.Default, replyMarkup: new ReplyKeyboardRemove());
                        break;
                    case "/clear":
                        if (userID == AdminID)
                        {
                            try
                            {
                                if (System.IO.File.Exists(dataFile))
                                {
                                    var MsgData = System.IO.File.ReadAllText(dataFile);

                                    MsgData = Md5Helper.De_DES(MsgData);    //解密

                                    List<MsgDataEntity> list = Newtonsoft.Json.JsonConvert.DeserializeObject<List<MsgDataEntity>>(MsgData);
                                    var deleteList = list.Where(x => x.msgTime >= DateTime.Now.AddHours(-48) && x.isDelete == 0).ToList();
                                    
                                    foreach (var item in deleteList)
                                    {
                                        list.Remove(item);
                                        await Bot.DeleteMessageAsync(item.chatID, item.msgID);
                                        item.isDelete = 1;
                                        list.Add(item);
                                    }

                                    string content = JsonConvert.SerializeObject(list);
                                    System.IO.File.WriteAllText(dataFile, Md5Helper.En_DES(content));

                                    await Bot.SendTextMessageAsync(chatID, "SUCCESS", parseMode: ParseMode.Default, replyMarkup: new ReplyKeyboardRemove());
                                }
                                else
                                {
                                    await Bot.SendTextMessageAsync(chatID, "文件不存在", parseMode: ParseMode.Default, replyMarkup: new ReplyKeyboardRemove());
                                }
                            }
                            catch (Exception ex)
                            {
                                await Bot.SendTextMessageAsync(chatID, "发生异常。" + ex.Message, parseMode: ParseMode.Default, replyMarkup: new ReplyKeyboardRemove());
                            }
                        }
                        break;
                    default:
                        int forId = message.ForwardFrom?.Id ?? 0;
                        string forName = message.ForwardFrom?.Username ?? "";
                        if (forId != 0)
                        {
                            await Bot.SendTextMessageAsync(message.Chat.Id, "该消息转自用户ID：<a href=\"tg://user?id=" + forId + "\">" + forId + "</a>，username: @" + forName, ParseMode.Html, replyToMessageId: msgID);
                        }
                        else
                        {
                            string usage22 = "未能识别的指令，请输入 /help 查看所有指令";
                            await Bot.SendTextMessageAsync(chatID, usage22, parseMode: ParseMode.Default, replyMarkup: new ReplyKeyboardRemove());
                        }
                        break;
                }

                //不是管理员发的私聊，转发到管理员
                if (userID != AdminID)
                {
                    //转发消息给管理员
                    await Bot.ForwardMessageAsync(AdminID, chatID, msgID);
                }
                else
                {
                    //回复转发的消息 则用bot回复别人
                    try
                    {
                        if (message.ReplyToMessage != null)
                        {
                            var originalSendUser = message.ReplyToMessage.ForwardFrom.Id;     //转发的原始发送人
                            if (originalSendUser != 0)
                            {
                                Message botMsg = await Bot.SendTextMessageAsync(originalSendUser, messageText, ParseMode.Html);
                                if (originalSendUser == defaultID)
                                {
                                    //记录Bot发送的消息ID
                                    var MsgData = System.IO.File.ReadAllText(dataFile);

                                    MsgData = Md5Helper.De_DES(MsgData);    //解密

                                    List<MsgDataEntity> list = JsonConvert.DeserializeObject<List<MsgDataEntity>>(MsgData);
                                    if (list == null)
                                    {
                                        list = new List<MsgDataEntity>();
                                    }

                                    MsgDataEntity model = new MsgDataEntity();
                                    model.userID = botMsg.From.Id;
                                    model.userName = botMsg.From.Username;
                                    model.chatID = originalSendUser;
                                    model.msgID = botMsg.MessageId;
                                    model.msgText = messageText;
                                    model.msgTime = msgTime;
                                    model.isDelete = 0;
                                    list.Add(model);

                                    string content = JsonConvert.SerializeObject(list);
                                    System.IO.File.WriteAllText(dataFile, Md5Helper.En_DES(content));
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    { }
                }
            }
            else
            {
                // 群聊
                switch (command)
                {
                    //加入@All
                    case "/join@All":
                        if (message.Chat.Type == ChatType.Private || message.Chat.Type == ChatType.Channel)
                        {
                            await Bot.SendTextMessageAsync(message.Chat.Id, "请在群组里发送 /join@All 指令即可加入。\n1、把我 @" + botName + " 拉入群组里\n2、在群里组发送 /join@All 即可", ParseMode.Default);
                        }
                        else
                        {
                            //读取现在保存的 群组成员信息配置
                            var usersInGroup = System.IO.File.ReadAllText(ConfigPath + "/" + groupUsersFile);

                            // 如果配置不为空，序列化 循环组信息
                            if (!string.IsNullOrEmpty(usersInGroup))
                            {
                                List<GroupUserEntity> list = JsonConvert.DeserializeObject<List<GroupUserEntity>>(usersInGroup);

                                // 根据组ID 获取配置中的信息
                                GroupUserEntity model = list.FirstOrDefault(a => a.GroupID == chatID);
                                if (model != null)
                                {
                                    var groupID = model.GroupID;
                                    UserInfo[] users = model.Users;

                                    // 如果配置里有该组信息，则判断有无当前人员信息
                                    UserInfo us = users.FirstOrDefault(u => u.UserID == userID);
                                    if (us == null)
                                    {
                                        // 人员在该组中不存在，则保存
                                        UserInfo newU = new UserInfo() { UserID = userID, FirstName = firstName, LastName = lastName, UserName = userName };
                                        List<UserInfo> ulist = users.ToList();
                                        ulist.Add(newU);

                                        list.Remove(model);
                                        model.Users = ulist.ToArray();
                                        list.Add(model);

                                        System.IO.File.WriteAllText(ConfigPath + "/" + groupUsersFile, JsonConvert.SerializeObject(list));
                                    }
                                    else
                                    {
                                        // 若人员在该组中已存在（配置信息）
                                        await Bot.SendTextMessageAsync(chatID, "您已加入成功。无需重复加入", replyToMessageId: msgID);
                                        break;
                                    }
                                }
                                else
                                {
                                    //如果配置里 不包含当前分组信息，则直接添加保存
                                    UserInfo u = new UserInfo() { UserID = userID, FirstName = firstName, LastName = lastName, UserName = userName };
                                    UserInfo[] ulist = { u };
                                    GroupUserEntity groupInfo = new GroupUserEntity() { GroupID = chatID, Users = ulist };
                                    list.Add(groupInfo);

                                    System.IO.File.WriteAllText(ConfigPath + "/" + groupUsersFile, JsonConvert.SerializeObject(list));
                                }
                            }
                            else
                            {
                                // 如果为空 直接加入保存
                                List<GroupUserEntity> list = new List<GroupUserEntity>();
                                UserInfo u = new UserInfo() { UserID = userID, FirstName = firstName, LastName = lastName, UserName = userName };
                                UserInfo[] ulist = { u };
                                GroupUserEntity groupInfo = new GroupUserEntity() { GroupID = chatID, Users = ulist };
                                list.Add(groupInfo);

                                System.IO.File.WriteAllText(ConfigPath + "/" + groupUsersFile, JsonConvert.SerializeObject(list));
                            }

                            await Bot.SendTextMessageAsync(chatID, "加入成功！", replyToMessageId: msgID);
                        }
                        break;
                    case "/All":
                    case "/all":
                        if (message.Chat.Type == ChatType.Private || message.Chat.Type == ChatType.Channel)
                        {
                            await Bot.SendTextMessageAsync(message.Chat.Id, "请在群组里发送 /join@All 指令即可加入。\n1、把我 @" + botName + " 拉入群组里\n2、在群里组发送 /join@All 即可使用此命令", ParseMode.Default);
                        }
                        else
                        {
                            var usersInGroup = System.IO.File.ReadAllText(ConfigPath + "/" + groupUsersFile);
                            if (!string.IsNullOrEmpty(usersInGroup))
                            {
                                List<GroupUserEntity> list = JsonConvert.DeserializeObject<List<GroupUserEntity>>(usersInGroup);
                                // 根据组ID 获取配置中的信息
                                GroupUserEntity model = list.FirstOrDefault(a => a.GroupID == chatID);
                                if (model != null)
                                {
                                    UserInfo[] users = model.Users;
                                    StringBuilder sb = new StringBuilder();
                                    foreach (var u in users)
                                    {
                                        if (!string.IsNullOrEmpty(u.UserName))
                                        {
                                            sb.Append("<a href=\"tg://user?id=" + u.UserID + "\">@" + u.UserName + "</a>  ");
                                        }
                                        else
                                        {
                                            sb.Append("<a href=\"tg://user?id=" + u.UserID + "\">" + u.FirstName + " " + u.LastName + "</a>  ");
                                        }
                                    }
                                    await Bot.SendTextMessageAsync(chatID, sb.ToString(), ParseMode.Html);
                                }
                                else
                                {
                                    await Bot.SendTextMessageAsync(chatID, "请在群组里发送 /join@All 指令即可加入。\n1、把我 @" + botName + " 拉入群组里\n2、在群里组发送 /join@All 即可使用此命令", ParseMode.Default);
                                }
                            }
                            else
                            {
                                await Bot.SendTextMessageAsync(chatID, "请在群组里发送 /join@All 指令即可加入。\n1、把我 @" + botName + " 拉入群组里\n2、在群里组发送 /join@All 即可使用此命令", ParseMode.Default);
                            }
                        }
                        break;
                    case "/getMyID":
                        await Bot.SendTextMessageAsync(chatID, "你的UserID为：" + userID, ParseMode.Default, replyToMessageId: msgID);
                        break;
                    case "/showAllNoticeMember":
                        var chats = System.IO.File.ReadAllText(ConfigPath + "\\" + noticeUsersFile);
                        if (!string.IsNullOrEmpty(chats))
                        {
                            StringBuilder sb = new StringBuilder();
                            List<string> userArr = chats.TrimEnd(',').Split(',').Distinct().ToList();
                            userArr.ForEach((x) =>
                            {
                                sb.Append("<a href=\"tg://user?id=" + x + "\">@" + x + "</a>  ");
                            });
                            await Bot.SendTextMessageAsync(chatID, "当前配置的接收通知的人有： " + sb, ParseMode.Html);
                        }
                        else
                        {
                            await Bot.SendTextMessageAsync(chatID, "当前配置的接收通知的人为空，请先 /addNoticeMember 添加接收人", ParseMode.Default);
                        }
                        break;
                    default:
                        if (command.StartsWith('/'))
                        {
                            string usage = "[Group Chat] 目前只有以下功能测试:" +
                                "\n/join@All    - 加入@All以便在群组里一键@All" +
                                "\n/All    - 一键@（仅在群组里且加入过@All的用户可被提及到）" +
                                //"\n/showAllNoticeMember    - 查看所有接收推送提醒的用户" +
                                "\n/getMyID    - 查看我的UserID";
                            await Bot.SendTextMessageAsync(chatID, usage, parseMode: ParseMode.Default, replyMarkup: new ReplyKeyboardRemove());
                        }
                        break;
                }

            }

        }
    }
}
