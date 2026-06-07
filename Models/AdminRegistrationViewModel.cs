using System;

namespace LanguageCenter.Models
{
    public class AdminRegistrationViewModel
    {
        public int RegistrationID { get; set; }
        public string StudentName { get; set; }
        public string StudentEmail { get; set; }
        public string PhoneNumber { get; set; }
        public string ClassName { get; set; }
        public string ProgramName { get; set; }
        public string TeacherName { get; set; }
        public DateTime? RegistrationDate { get; set; }
        public string RegStatus { get; set; }
        public string PaymentStatus { get; set; }
        public decimal? Amount { get; set; }
    }
}
