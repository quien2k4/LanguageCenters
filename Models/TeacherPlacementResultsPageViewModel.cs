using System.Collections.Generic;

namespace LanguageCenter.Models
{
    public class TeacherPlacementResultsPageViewModel
    {
        public List<TeacherPlacementResultViewModel> Results { get; set; }
        public List<string> Statuses { get; set; }
        public string Search { get; set; }
        public string Status { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        public TeacherPlacementResultsPageViewModel()
        {
            Results = new List<TeacherPlacementResultViewModel>();
            Statuses = new List<string>();
            CurrentPage = 1;
            TotalPages = 1;
        }
    }
}
