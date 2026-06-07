using System;
using System.Linq;
using System.Security.Cryptography;

namespace LanguageCenter.Helpers
{
    public static class PasswordHelper
    {
        private const int Iterations = 10000;
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const string Prefix = "PBKDF2";

        public static string HashPassword(string password)
        {
            if (password == null)
            {
                password = string.Empty;
            }

            var salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations))
            {
                var hash = pbkdf2.GetBytes(HashSize);
                return string.Format(
                    "{0}${1}${2}${3}",
                    Prefix,
                    Iterations,
                    Convert.ToBase64String(salt),
                    Convert.ToBase64String(hash));
            }
        }

        public static bool VerifyPassword(string password, string storedHash)
        {
            if (storedHash == null)
            {
                return false;
            }

            if (!storedHash.StartsWith(Prefix + "$", StringComparison.Ordinal))
            {
                return storedHash == password;
            }

            var parts = storedHash.Split('$');
            if (parts.Length != 4)
            {
                return false;
            }

            int iterations;
            if (!int.TryParse(parts[1], out iterations))
            {
                return false;
            }

            try
            {
                var salt = Convert.FromBase64String(parts[2]);
                var expectedHash = Convert.FromBase64String(parts[3]);

                using (var pbkdf2 = new Rfc2898DeriveBytes(password ?? string.Empty, salt, iterations))
                {
                    var actualHash = pbkdf2.GetBytes(expectedHash.Length);
                    return actualHash.SequenceEqual(expectedHash);
                }
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public static bool IsHashedPassword(string storedHash)
        {
            return !string.IsNullOrWhiteSpace(storedHash)
                && storedHash.StartsWith(Prefix + "$", StringComparison.Ordinal);
        }
    }
}

