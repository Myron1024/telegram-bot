using System;
using System.IO;

namespace TelegramBot
{
    public class Log
    {
        public static void LogInfo(string info)
        {
            try
            {
                string path = System.AppDomain.CurrentDomain.BaseDirectory + "\\logs\\";
                string file = path + DateTime.Now.ToString("yyyy-MM-dd") + ".log";
                if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(path)))
                {
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
                }
                File.AppendAllText(file, "\r\n\r\n[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "] " + info);
            }
            catch (Exception ex)
            { }
        }
    }
}
