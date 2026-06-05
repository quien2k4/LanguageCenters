using System.Collections.Generic;

namespace LanguageCenter.Models
{
    public class AdminPlacementTestPageViewModel
    {
        public List<AdminPlacementTestViewModel> PlacementTests { get; set; }
        public List<string> Levels { get; set; }
        public string Search { get; set; }
        public string Status { get; set; }
        public string Level { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalTests { get; set; }
        public int PendingTests { get; set; }
        public int CompletedTests { get; set; }
        public int CancelledTests { get; set; }

        public AdminPlacementTestPageViewModel()
        {
            PlacementTests = new List<AdminPlacementTestViewModel>();
            Levels = new List<string>();
            Status = "All";
            CurrentPage = 1;
            TotalPages = 1;
        }
    }
}
