using System;
using System.Collections.Generic;

namespace LanguageCenter.Models
{
    public class StudentPaymentsViewModel
    {
        public List<StudentPaymentViewModel> Payments { get; set; }

        public StudentPaymentsViewModel()
        {
            Payments = new List<StudentPaymentViewModel>();
        }
    }

    public class StudentPaymentViewModel
    {
        public int PaymentID { get; set; }
        public int RegistrationID { get; set; }
        public string ClassName { get; set; }
        public string ProgramName { get; set; }
        public decimal Amount { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string Method { get; set; }
        public string PaymentStatus { get; set; }
        public string RegistrationStatus { get; set; }
    }
}
