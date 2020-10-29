using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBot.WebHook.Models
{
    public class GroupUserEntity
    {
        public long GroupID { get; set; }
        public UserInfo[] Users { get; set; }
    }

    public class UserInfo
    {
        public long UserID { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}
