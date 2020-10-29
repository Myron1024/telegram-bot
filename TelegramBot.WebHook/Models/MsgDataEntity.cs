using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TelegramBot.WebHook.Models
{
    public class MsgDataEntity
    {
        public int userID { get; set; }
        public string userName { get; set; }
        public int chatID { get; set; }
        public int msgID { get; set; }
        public string msgText { get; set; }
        public DateTime msgTime { get; set; }
        public int isDelete { get; set; }
    }
}
