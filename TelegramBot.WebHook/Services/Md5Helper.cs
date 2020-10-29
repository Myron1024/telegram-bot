using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace TelegramBot.WebHook.Services
{
    public static class Md5Helper
    {
        public static string Md5Encrypt(string strToBeEncrypt)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            Byte[] FromData = System.Text.Encoding.GetEncoding("utf-8").GetBytes(strToBeEncrypt);
            Byte[] TargetData = md5.ComputeHash(FromData);
            string Byte2String = "";
            for (int i = 0; i < TargetData.Length; i++)
            {
                Byte2String += TargetData[i].ToString("x2");
            }
            return Byte2String.ToUpper();
        }

        public static string ToMD5Hash(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return null;

            return Encoding.ASCII.GetBytes(str).ToMD5Hash();
        }

        public static string ToMD5Hash(this byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return null;

            using (var md5 = MD5.Create())
            {
                return string.Join("", md5.ComputeHash(bytes).Select(x => x.ToString("X2")));
            }
        }


        private static string EncryptKey = "XpjPjPJ1";
        private static byte[] Keys = new byte[] { 0x12, 0x34, 0x56, 120, 0x90, 0xab, 0xcd, 0xef };

        public static string De_DES(string decryptString)
        {
            string s = To_md5(EncryptKey + DateTime.Now.ToString("yyyyMMdd"), false).Substring(0, 8);
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(s);
                byte[] keys = Keys;
                byte[] buffer = Convert.FromBase64String(decryptString);
                DESCryptoServiceProvider provider = new DESCryptoServiceProvider();
                MemoryStream stream = new MemoryStream();
                CryptoStream stream2 = new CryptoStream(stream, provider.CreateDecryptor(bytes, keys), CryptoStreamMode.Write);
                stream2.Write(buffer, 0, buffer.Length);
                stream2.FlushFinalBlock();
                return Encoding.UTF8.GetString(stream.ToArray());
            }
            catch
            {
                return decryptString;
            }
        }

        public static string En_DES(string encryptString)
        {
            string s = To_md5(EncryptKey + DateTime.Now.ToString("yyyyMMdd"), false).Substring(0, 8);
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(s);
                byte[] keys = Keys;
                byte[] buffer = Encoding.UTF8.GetBytes(encryptString);
                DESCryptoServiceProvider provider = new DESCryptoServiceProvider();
                MemoryStream stream = new MemoryStream();
                CryptoStream stream2 = new CryptoStream(stream, provider.CreateEncryptor(bytes, keys), CryptoStreamMode.Write);
                stream2.Write(buffer, 0, buffer.Length);
                stream2.FlushFinalBlock();
                return Convert.ToBase64String(stream.ToArray());
            }
            catch
            {
                return encryptString;
            }
        }

        public static string To_md5(string str, bool up = false)
        {
            MD5CryptoServiceProvider provider = new MD5CryptoServiceProvider();
            string str2 = BitConverter.ToString(provider.ComputeHash(Encoding.Default.GetBytes(str))).Replace("-", "");
            if (!up)
            {
                return str2.ToLower();
            }
            return str2.ToUpper();
        }
    }
}
