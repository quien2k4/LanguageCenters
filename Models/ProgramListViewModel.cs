using System.Collections.Generic;

namespace LanguageCenter.Models
{
    public class ProgramListViewModel
    {
        public List<ProgramItemViewModel> Programs { get; set; }
        public List<string> Levels { get; set; }
        public string Search { get; set; }
        public string Level { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        public ProgramListViewModel()
        {
            Programs = new List<ProgramItemViewModel>();
            Levels = new List<string>();
            CurrentPage = 1;
            TotalPages = 1;
        }
    }

    public class ProgramItemViewModel
    {
        public int ProgramID { get; set; }
        public string ProgramName { get; set; }
        public string Level { get; set; }
        public string Duration { get; set; }
        public decimal Fee { get; set; }
        public string ImageURL { get; set; }
    }
}
