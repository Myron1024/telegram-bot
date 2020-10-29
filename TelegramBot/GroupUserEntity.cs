using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBot
{
    public class GroupUserEntity
    {
        public long GroupID { get; set; }
        public User[] Users { get; set; }
    }

    public class User
    {
        public long UserID { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}
