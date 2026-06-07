using System.ComponentModel.DataAnnotations;

namespace LanguageCenter.Models
{
    public class AdminTeacherFormViewModel
    {
        public int TeacherID { get; set; }
        public int AccountID { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Display(Name = "Mật khẩu")]
        public string Password { get; set; }

        [Display(Name = "Xác nhận mật khẩu")]
        public string ConfirmPassword { get; set; }

        [Required]
        [Display(Name = "Họ tên")]
        public string FullName { get; set; }

        [Required]
        [Display(Name = "Chuyên môn")]
        public string Expertise { get; set; }

        public string Avatar { get; set; }

        [Display(Name = "Đang hoạt động")]
        public bool IsActive { get; set; }

        [Display(Name = "Đang bị khóa")]
        public bool IsLockedOut { get; set; }

        public int FailedLoginAttempts { get; set; }

        [Display(Name = "Mở khóa tài khoản")]
        public bool UnlockAccount { get; set; }
    }
}

