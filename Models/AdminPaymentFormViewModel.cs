using System;
using System.ComponentModel.DataAnnotations;

namespace LanguageCenter.Models
{
    public class AdminPaymentFormViewModel
    {
        public int PaymentID { get; set; }
        public int RegistrationID { get; set; }
        public string StudentName { get; set; }
        public string ClassName { get; set; }
        public string ProgramName { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Số tiền phải lớn hơn hoặc bằng 0.")]
        [Display(Name = "Số tiền")]
        public decimal Amount { get; set; }

        public DateTime? PaymentDate { get; set; }
        [Display(Name = "Phương thức")]
        public string Method { get; set; }

        [Required]
        [Display(Name = "Trạng thái thanh toán")]
        public string PaymentStatus { get; set; }
    }
}
