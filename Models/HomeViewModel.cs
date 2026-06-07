using System;
using System.Collections.Generic;

namespace LanguageCenter.Models
{
    public class HomeViewModel
    {
        public List<FeaturedProgramViewModel> FeaturedPrograms { get; set; }
        public List<NewClassViewModel> NewClasses { get; set; }
        public List<TeacherHomeViewModel> Teachers { get; set; }

        public HomeViewModel()
        {
            FeaturedPrograms = new List<FeaturedProgramViewModel>();
            NewClasses = new List<NewClassViewModel>();
            Teachers = new List<TeacherHomeViewModel>();
        }
    }

    public class FeaturedProgramViewModel
    {
        public int ProgramID { get; set; }
        public string ProgramName { get; set; }
        public string Level { get; set; }
        public string Duration { get; set; }
        public decimal Fee { get; set; }
        public string ImageURL { get; set; }
    }

    public class NewClassViewModel
    {
        public string ClassName { get; set; }
        public string ProgramName { get; set; }
        public string TeacherName { get; set; }
        public string StatusName { get; set; }
        public DateTime? StartDate { get; set; }
    }

    public class TeacherHomeViewModel
    {
        public string FullName { get; set; }
        public string Expertise { get; set; }
        public string Avatar { get; set; }
    }
}
