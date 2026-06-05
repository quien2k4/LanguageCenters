using System.Collections.Generic;

namespace LanguageCenter.Models
{
    public class AdminTeacherPageViewModel
    {
        public List<AdminTeacherViewModel> Teachers { get; set; }
        public string Search { get; set; }
        public string Status { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        public AdminTeacherPageViewModel()
        {
            Teachers = new List<AdminTeacherViewModel>();
            Status = "All";
            CurrentPage = 1;
            TotalPages = 1;
        }
    }
}
