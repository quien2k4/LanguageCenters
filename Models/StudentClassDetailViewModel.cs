using System;

namespace LanguageCenter.Models
{
    public class StudentClassDetailViewModel
    {
        public int RegistrationID { get; set; }
        public int ClassID { get; set; }
        public string ClassName { get; set; }
        public string ProgramName { get; set; }
        public string TeacherName { get; set; }
        public string ClassStatus { get; set; }
        public DateTime? StartDate { get; set; }
        public string Schedule { get; set; }
        public string Room { get; set; }
        public DateTime? RegistrationDate { get; set; }
        public string RegStatus { get; set; }
        public bool HasPayment { get; set; }
        public decimal Amount { get; set; }
        public string PaymentStatus { get; set; }
        public string Method { get; set; }
        public DateTime? PaymentDate { get; set; }
    }
}

