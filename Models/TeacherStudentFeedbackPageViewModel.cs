using System.Collections.Generic;

namespace LanguageCenter.Models
{
    public class TeacherStudentFeedbackPageViewModel
    {
        public List<TeacherStudentFeedbackViewModel> Feedbacks { get; set; }
        public List<int> Ratings { get; set; }
        public string Search { get; set; }
        public int? Rating { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalFeedback { get; set; }
        public double AverageRating { get; set; }

        public TeacherStudentFeedbackPageViewModel()
        {
            Feedbacks = new List<TeacherStudentFeedbackViewModel>();
            Ratings = new List<int>();
            CurrentPage = 1;
            TotalPages = 1;
        }
    }
}
