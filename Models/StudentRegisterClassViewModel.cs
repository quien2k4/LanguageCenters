using System;

namespace LanguageCenter.Models
{
    public class StudentRegisterClassViewModel
    {
        public int ClassID { get; set; }
        public int ProgramID { get; set; }
        public string ClassName { get; set; }
        public string ProgramName { get; set; }
        public string TeacherName { get; set; }
        public string StatusName { get; set; }
        public DateTime? StartDate { get; set; }
        public decimal Fee { get; set; }
        public bool IsAlreadyRegistered { get; set; }
    }
}
