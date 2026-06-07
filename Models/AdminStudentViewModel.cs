using System;

namespace LanguageCenter.Models
{
    public class AdminStudentViewModel
    {
        public int StudentID { get; set; }
        public int AccountID { get; set; }
        public string Avatar { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsActive { get; set; }
        public int RegistrationCount { get; set; }
        public int PaymentCount { get; set; }
        public int PlacementTestCount { get; set; }
    }
}
