using System;

namespace LanguageCenter.Models
{
    public class AdminClassViewModel
    {
        public int ClassID { get; set; }
        public int ProgramID { get; set; }
        public int TeacherID { get; set; }
        public int StatusID { get; set; }
        public string ClassName { get; set; }
        public string ProgramName { get; set; }
        public string TeacherName { get; set; }
        public string StatusName { get; set; }
        public DateTime? StartDate { get; set; }
        public string Schedule { get; set; }
        public string Room { get; set; }
        public int StudentCount { get; set; }
    }
}
