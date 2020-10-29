using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;
using System.Configuration;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace TelegramBot
{
    public class Program
    {
        private static readonly string token = ConfigurationSettings.AppSettings["token"].ToString();

        private static readonly TelegramBotClient Bot = new TelegramBotClient(token);

        private static string botName = Bot.GetMeAsync().Result.Username;
        private static int AdminID = 740967577;

        public static void Main(string[] args)
        {
            var me = Bot.GetMeAsync().Result;
            Console.Title = me.Username;

            Bot.OnMessage += BotOnMessageReceived;
            Bot.OnMessageEdited += BotOnMessageReceived;
            Bot.OnCallbackQuery += BotOnCallbackQueryReceived;
            Bot.OnReceiveError += BotOnReceiveError;
            

            Bot.StartReceiving(Array.Empty<UpdateType>());
            Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} 开启监听TG机器人： @{me.Username}");
            //Console.WriteLine("按回车键结束程序");
            Console.ReadLine();
            Bot.StopReceiving();
        }

        private static async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            var message = messageEventArgs.Message;         // 获取消息实体
            var userName = message.From.Username ?? "";     // 发消息人的 UserName
            var firstName = message.From.FirstName ?? "";   // FirstName
            var lastName = message.From.LastName ?? "";     // LastName
            var userID = message.From.Id;                   // 发消息人的ID
            var chatID = message.Chat.Id;                   // 聊天会话ID 个人/chanel/群组
            var msgID = message.MessageId;                  // 聊天记录ID

            // Log.LogInfo(JsonConvert.SerializeObject(message));

            if (message == null || message.Type != MessageType.Text) return;

            Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} " + "[From:" + firstName + "(" + userName + ")][chat:"
                + message.Chat.Username + "][text:" + message.Text + "][msgid:" + message.MessageId + "]");

            var ConfigPath = Environment.CurrentDirectory + "\\App_Data";
            if (!Directory.Exists(Path.GetDirectoryName(ConfigPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
            }
            var noticeUsersFile = "noticeUsers.txt";    //接收通知的 用户ID配置
            var groupUsersFile = "GroupUsers.txt";      //群组加入@all的用户ID配置

            var messageText = message.Text;
            var command = messageText.Trim().Split(' ').First().Replace("@" + botName, "");
            var parameters = messageText.Trim().Replace(command, "").Replace("@" + botName, "");

            if (message.Chat.Type == ChatType.Private)
            {
                // 私聊
                // await Bot.SendTextMessageAsync(message.Chat.Id, "I don't accept private chat.");

                switch (command)
                {
                    case "/join@All":
                        await Bot.SendTextMessageAsync(message.Chat.Id, "请在群组里发送 /join@All 指令即可加入。\n1、把我 @" + botName + " 拉入群组里\n2、在群里组发送 /join@All 即可", ParseMode.Default);
                        break;
                    //case "/getCallbackNotice":
                    //    await Bot.SendTextMessageAsync(message.Chat.Id, "功能未开放..正在开发中...暂未接入数据库，只是模拟流程，推送提醒. 此步骤相当于校验，后台配置接收提醒的用户ID, 需要用户", ParseMode.Default);

                    //    //1. 先获取聊天用户的 userID,判断配置里有没有该用户
                    //    //2. 如果有该用户，则提示成功。如果没有，则提示联系管理员添加
                    //    var users = File.ReadAllText(ConfigPath + "\\" + noticeUsersFile);
                    //    if (!string.IsNullOrEmpty(users))
                    //    {
                    //        List<string> userArr = users.TrimEnd(',').Split(',').Distinct().ToList();
                    //        if (userArr.Contains(userID.ToString()))
                    //        {
                    //            //File.AppendAllText(ConfigPath + "\\" + chatFile, chatID + ","); //校验通过，储存chatID   --此处不做存储配置
                    //            await Bot.SendTextMessageAsync(message.Chat.Id, "校验成功！后面将自动推送回调失败的订单给您，请注意查看！");
                    //        }
                    //        else
                    //        {
                    //            await Bot.SendTextMessageAsync(message.Chat.Id, "您不符合条件！");
                    //        }
                    //    }
                    //    else
                    //    {
                    //        await Bot.SendTextMessageAsync(message.Chat.Id, "您不符合条件！");
                    //    }

                    //    break;
                    case "broadcast":
                    case "/broadcast":
                        var chats = File.ReadAllText(ConfigPath + "\\" + noticeUsersFile);
                        if (!string.IsNullOrEmpty(chats))
                        {
                            List<string> userArr = chats.TrimEnd(',').Split(',').Distinct().ToList();
                            userArr.ForEach((x) => {
                                Bot.SendTextMessageAsync(x, "这是测试广播消息。" + parameters + "\n\n/broadcast 后面可跟参数，用空格隔开.参数为要发送的内容.\n\n<code>eg: /broadcast Hello World.</code>", ParseMode.Html);
                            });

                            Log.LogInfo(userName + " 发布消息：" + parameters + "； 接收人：" + string.Join(",", userArr.ToArray()));

                            //如果发消息的人 自身不在通知人员队列中，则提示是否要加入
                            if (!userArr.Contains(userID.ToString()))
                            {
                                await Task.Delay(1000);
                                await Bot.SendTextMessageAsync(message.Chat.Id, "没有收到通知？ /addNoticeMember 试一下吧.", ParseMode.Default);
                            }
                        }
                        break;
                    case "/addNoticeMember":
                        var chats2 = File.ReadAllText(ConfigPath + "\\" + noticeUsersFile);
                        if (!string.IsNullOrEmpty(chats2))
                        {
                            List<string> userArr = chats2.TrimEnd(',').Split(',').Distinct().ToList();
                            if (!userArr.Contains(userID.ToString()))
                            {
                                userArr.Add(userID.ToString());

                                File.WriteAllText(ConfigPath + "\\" + noticeUsersFile, string.Join(",", userArr.ToArray()));

                                await Bot.SendTextMessageAsync(message.Chat.Id, "添加完毕，你已经在通知人群里啦！再试一下推送消息吧！\n（模拟后台添加接收提醒用户配置）", ParseMode.Default);
                            }
                            else
                            {
                                await Bot.SendTextMessageAsync(message.Chat.Id, "你已经在通知人群里啦！", ParseMode.Default, replyToMessageId: msgID);
                            }
                        }
                        break;
                    case "/getMyID":
                        await Bot.SendTextMessageAsync(message.Chat.Id, "你的UserID为：" + userID, ParseMode.Default, replyToMessageId: msgID);
                        break;
                    default:
                        await Bot.SendTextMessageAsync(message.Chat.Id, message.Text, ParseMode.Default);
                        string usage = "目前只有以下功能测试:" +
                            //"\n/getCallbackNotice    - 获取订单回调失败提醒" +
                            "\n/join@All    - 加入@All以便在群组里一键@All" +
                            "\n/All    - 一键@（仅在群组里且加入过@All的用户可被提及到）" +
                            "\n/broadcast    - 模拟给后台配置接收提醒的用户推送消息" +
                            "\n/getMyID    - 查看我的UserID";
                        await Bot.SendTextMessageAsync(message.Chat.Id, usage, parseMode: ParseMode.Default, replyMarkup: new ReplyKeyboardRemove());
                        break;
                }
            }
            else
            {
                // 群聊
                // await Bot.SendTextMessageAsync(message.Chat.Id, "What do you mean the “" + message.Text + "”？");

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
                            var usersInGroup = File.ReadAllText(ConfigPath + "\\" + groupUsersFile);

                            // 如果配置不为空，序列化 循环组信息
                            if (!string.IsNullOrEmpty(usersInGroup))
                            {
                                List<GroupUserEntity> list = JsonConvert.DeserializeObject<List<GroupUserEntity>>(usersInGroup);

                                // 根据组ID 获取配置中的信息
                                GroupUserEntity model = list.FirstOrDefault(a => a.GroupID == chatID);
                                if (model != null)
                                {
                                    var groupID = model.GroupID;
                                    User[] users = model.Users;

                                    // 如果配置里有该组信息，则判断有无当前人员信息
                                    User us = users.FirstOrDefault(u => u.UserID == userID);
                                    if (us == null)
                                    {
                                        // 人员在该组中不存在，则保存
                                        User newU = new User() { UserID = userID, FirstName = firstName, LastName = lastName, UserName = userName };
                                        List<User> ulist = users.ToList();
                                        ulist.Add(newU);

                                        list.Remove(model);
                                        model.Users = ulist.ToArray();
                                        list.Add(model);

                                        File.WriteAllText(ConfigPath + "\\" + groupUsersFile, JsonConvert.SerializeObject(list));
                                    }
                                    else
                                    {
                                        // 若人员在该组中已存在（配置信息）
                                        await Bot.SendTextMessageAsync(chatID, "您已加入成功。无需重复加入");
                                        break;
                                    }
                                }
                                else
                                {
                                    //如果配置里 不包含当前分组信息，则直接添加保存
                                    User u = new User() { UserID = userID, FirstName = firstName, LastName = lastName, UserName = userName };
                                    User[] ulist = { u };
                                    GroupUserEntity groupInfo = new GroupUserEntity() { GroupID = chatID, Users = ulist };
                                    list.Add(groupInfo);

                                    File.WriteAllText(ConfigPath + "\\" + groupUsersFile, JsonConvert.SerializeObject(list));
                                }
                            }
                            else
                            {
                                // 如果为空 直接加入保存
                                List<GroupUserEntity> list = new List<GroupUserEntity>();
                                User u = new User() { UserID = userID, FirstName = firstName, LastName = lastName, UserName = userName };
                                User[] ulist = { u };
                                GroupUserEntity groupInfo = new GroupUserEntity() { GroupID = chatID, Users = ulist };
                                list.Add(groupInfo);

                                File.WriteAllText(ConfigPath + "\\" + groupUsersFile, JsonConvert.SerializeObject(list));
                            }

                            await Bot.SendTextMessageAsync(chatID, "加入成功。 测试一下吧！ /All");
                        }
                        break;
                    case "/All":
                    case "/all":
                        if (message.Chat.Type == ChatType.Private || message.Chat.Type == ChatType.Channel)
                        {
                            await Bot.SendTextMessageAsync(message.Chat.Id, "请在群组里发送 /join@All 指令即可加入。\n1、把我 @" + botName + " 拉入群组里\n2、在群里组发送 /join@All 即可", ParseMode.Default);
                        }
                        else
                        {
                            var usersInGroup = File.ReadAllText(ConfigPath + "\\" + groupUsersFile);
                            if (!string.IsNullOrEmpty(usersInGroup))
                            {
                                List<GroupUserEntity> list = JsonConvert.DeserializeObject<List<GroupUserEntity>>(usersInGroup);
                                // 根据组ID 获取配置中的信息
                                GroupUserEntity model = list.FirstOrDefault(a => a.GroupID == chatID);
                                if (model != null)
                                {
                                    User[] users = model.Users;
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
                                    await Bot.SendTextMessageAsync(message.Chat.Id, "请在群组里发送 /join@All 指令即可加入。\n1、把我 @" + botName + " 拉入群组里\n2、在群里组发送 /join@All 即可", ParseMode.Default);
                                }
                            }
                            else
                            {
                                await Bot.SendTextMessageAsync(message.Chat.Id, "请在群组里发送 /join@All 指令即可加入。\n1、把我 @" + botName + " 拉入群组里\n2、在群里组发送 /join@All 即可", ParseMode.Default);
                            }
                        }
                        break;
                    case "/getCallbackNotice":
                        await Bot.SendTextMessageAsync(message.Chat.Id, "请与我私发消息进行操作。", ParseMode.Default, replyToMessageId: msgID);
                        break;
                    case "/getMyID":
                        await Bot.SendTextMessageAsync(message.Chat.Id, "你的UserID为：" + userID, ParseMode.Default, replyToMessageId: msgID);
                        break;
                    case "/broadcast":
                        await Bot.SendTextMessageAsync(message.Chat.Id, "要使用该功能请与bot私信操作.", ParseMode.Default, replyToMessageId: msgID);
                        break;
                    default:
                        string usage = "目前只有以下功能测试:" +
                            //"\n/getCallbackNotice    - 获取订单回调失败提醒" +
                            "\n/join@All    - 加入@All以便在群组里一键@All" +
                            "\n/All    - 一键@（仅在群组里且加入过@All的用户可被提及到）" +
                            "\n/broadcast    - 模拟给后台配置接收提醒的用户推送消息" +
                            "\n/getMyID    - 查看我的UserID";
                        await Bot.SendTextMessageAsync(message.Chat.Id, usage, parseMode: ParseMode.Default, replyMarkup: new ReplyKeyboardRemove());
                        break;
                }

            }
        }

        private static async void BotOnCallbackQueryReceived(object sender, CallbackQueryEventArgs callbackQueryEventArgs)
        {
            var callbackQuery = callbackQueryEventArgs.CallbackQuery;
            await Bot.AnswerCallbackQueryAsync(callbackQuery.Id, $"Received {callbackQuery.Data}");
            await Bot.SendTextMessageAsync(callbackQuery.Message.Chat.Id, $"Received {callbackQuery.Data}");
        }

        private static void BotOnReceiveError(object sender, ReceiveErrorEventArgs receiveErrorEventArgs)
        {
            Console.WriteLine("Received error: {0} — {1}", receiveErrorEventArgs.ApiRequestException.ErrorCode, receiveErrorEventArgs.ApiRequestException.Message);
        }
    }
}
