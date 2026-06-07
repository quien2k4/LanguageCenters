using System;

namespace LanguageCenter.Models
{
    public class StudentProfileViewModel
    {
        public int AccountID { get; set; }
        public int StudentID { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string PhoneNumber { get; set; }
        public string Avatar { get; set; }
    }
}
