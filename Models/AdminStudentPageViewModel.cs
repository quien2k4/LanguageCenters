using System.Collections.Generic;

namespace LanguageCenter.Models
{
    public class AdminStudentPageViewModel
    {
        public List<AdminStudentViewModel> Students { get; set; }
        public string Search { get; set; }
        public string Status { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        public AdminStudentPageViewModel()
        {
            Students = new List<AdminStudentViewModel>();
            Status = "All";
            CurrentPage = 1;
            TotalPages = 1;
        }
    }
}
