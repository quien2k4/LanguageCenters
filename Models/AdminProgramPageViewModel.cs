using System.Collections.Generic;

namespace LanguageCenter.Models
{
    public class AdminProgramPageViewModel
    {
        public List<AdminProgramViewModel> Programs { get; set; }
        public List<string> Levels { get; set; }
        public string Search { get; set; }
        public string Level { get; set; }
        public string Status { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        public AdminProgramPageViewModel()
        {
            Programs = new List<AdminProgramViewModel>();
            Levels = new List<string>();
            CurrentPage = 1;
            TotalPages = 1;
            Status = "All";
        }
    }
}
