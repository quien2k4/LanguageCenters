using System;
using System.Collections.Generic;

namespace LanguageCenter.Models
{
    public class ProgramDetailViewModel
    {
        public int ProgramID { get; set; }
        public string ProgramName { get; set; }
        public string Description { get; set; }
        public string OutputStandard { get; set; }
        public string Level { get; set; }
        public string Duration { get; set; }
        public decimal Fee { get; set; }
        public string ImageURL { get; set; }
        public string CurrentRole { get; set; }
        public int OpenClassCount { get; set; }
        public List<RelatedClassViewModel> RelatedClasses { get; set; }

        public ProgramDetailViewModel()
        {
            RelatedClasses = new List<RelatedClassViewModel>();
        }
    }

    public class RelatedClassViewModel
    {
        public int ClassID { get; set; }
        public string ClassName { get; set; }
        public string TeacherName { get; set; }
        public string StatusName { get; set; }
        public DateTime? StartDate { get; set; }
        public string Schedule { get; set; }
        public string Room { get; set; }
    }
}

