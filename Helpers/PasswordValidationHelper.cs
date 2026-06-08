using System.Linq;

namespace LanguageCenter.Helpers
{
    public static class PasswordValidationHelper
    {
        public static bool IsValidPassword(string password, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(password))
            {
                errorMessage = "Mật khẩu không được để trống.";
                return false;
            }

            if (password.Length < 6)
            {
                errorMessage = "Mật khẩu phải có ít nhất 6 ký tự.";
                return false;
            }

            if (password.Length > 50)
            {
                errorMessage = "Mật khẩu không được vượt quá 50 ký tự.";
                return false;
            }

            if (!password.Any(char.IsLetter))
            {
                errorMessage = "Mật khẩu phải có ít nhất 1 chữ cái.";
                return false;
            }

            if (!password.Any(char.IsDigit))
            {
                errorMessage = "Mật khẩu phải có ít nhất 1 chữ số.";
                return false;
            }

            return true;
        }
    }
}

