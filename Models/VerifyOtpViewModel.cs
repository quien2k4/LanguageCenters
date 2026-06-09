using System.ComponentModel.DataAnnotations;

namespace LanguageCenter.Models
{
    public class VerifyOtpViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mã xác minh.")]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "Mã xác minh phải gồm 4 chữ số.")]
        [RegularExpression(@"^\d{4}$", ErrorMessage = "Mã xác minh phải gồm 4 chữ số.")]
        [Display(Name = "Mã xác minh")]
        public string OtpCode { get; set; }
    }
}
