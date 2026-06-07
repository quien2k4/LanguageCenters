using System;

namespace LanguageCenter.Models
{
    public class TeacherPlacementResultViewModel
    {
        public int TestID { get; set; }
        public string StudentName { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime TestDate { get; set; }
        public TimeSpan TestTime { get; set; }
        public string Level { get; set; }
        public string ResultScore { get; set; }
        public string Status { get; set; }
    }
}
