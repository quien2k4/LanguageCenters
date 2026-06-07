using System;
using System.ComponentModel.DataAnnotations;

namespace LanguageCenter.Models
{
    public class EditStudentProfileViewModel
    {
        public int StudentID { get; set; }
        public string Email { get; set; }
        public string Avatar { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [Display(Name = "Họ tên")]
        public string FullName { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Ngày sinh")]
        public DateTime? DateOfBirth { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; }
    }
}

