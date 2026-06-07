using System;

namespace LanguageCenter.Models
{
    public class AdminPaymentViewModel
    {
        public int PaymentID { get; set; }
        public int RegistrationID { get; set; }
        public string StudentName { get; set; }
        public string StudentEmail { get; set; }
        public string ClassName { get; set; }
        public string ProgramName { get; set; }
        public decimal Amount { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string Method { get; set; }
        public string PaymentStatus { get; set; }
        public string RegStatus { get; set; }
    }
}
