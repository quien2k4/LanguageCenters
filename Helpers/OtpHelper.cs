using System;
using System.Security.Cryptography;

namespace LanguageCenter.Helpers
{
    public static class OtpHelper
    {
        public const int ExpireMinutes = 5;

        public static string GenerateOtpCode()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                var bytes = new byte[4];
                rng.GetBytes(bytes);
                var value = BitConverter.ToUInt32(bytes, 0) % 10000;
                return value.ToString("D4");
            }
        }

        public static DateTime GetExpireTime()
        {
            return DateTime.Now.AddMinutes(ExpireMinutes);
        }

        public static bool IsOtpExpired(DateTime expireTime)
        {
            return DateTime.Now > expireTime;
        }
    }
}
