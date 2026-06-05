using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LanguageCenter.Models
{
    public class StudentPlacementTestsViewModel
    {
        public List<StudentPlacementTestViewModel> PlacementTests { get; set; }

        public StudentPlacementTestsViewModel()
        {
            PlacementTests = new List<StudentPlacementTestViewModel>();
        }
    }

    public class StudentPlacementTestViewModel
    {
        public int TestID { get; set; }
        public DateTime TestDate { get; set; }
        public TimeSpan TestTime { get; set; }
        public string Level { get; set; }
        public string ResultScore { get; set; }
        public string Status { get; set; }
    }
}