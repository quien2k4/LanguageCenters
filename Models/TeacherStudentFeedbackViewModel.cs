using System;

namespace LanguageCenter.Models
{
    public class TeacherStudentFeedbackViewModel
    {
        public int FeedbackID { get; set; }
        public string StudentName { get; set; }
        public int? Rating { get; set; }
        public string Comment { get; set; }
        public DateTime? FeedbackDate { get; set; }
    }
}
