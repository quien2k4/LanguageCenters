using System;
using System.ComponentModel.DataAnnotations;

namespace LanguageCenter.Models
{
    public class AdminStudentFormViewModel
    {
        public int StudentID { get; set; }
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

        [DataType(DataType.Date)]
        [Display(Name = "Ngày sinh")]
        public DateTime? DateOfBirth { get; set; }

        [Required]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; }

        public string Avatar { get; set; }

        [Display(Name = "Đang hoạt động")]
        public bool IsActive { get; set; }
    }
}
