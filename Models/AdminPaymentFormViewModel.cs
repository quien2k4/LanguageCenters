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

        [Range(0, double.MaxValue, ErrorMessage = "Amount must be greater than or equal to 0.")]
        public decimal Amount { get; set; }

        public DateTime? PaymentDate { get; set; }
        public string Method { get; set; }

        [Required]
        [Display(Name = "Payment Status")]
        public string PaymentStatus { get; set; }
    }
}
