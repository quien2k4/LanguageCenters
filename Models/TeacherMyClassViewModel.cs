using System;
using System.Collections.Generic;

namespace LanguageCenter.Models
{
    public class TeacherMyClassesViewModel
    {
        public List<TeacherMyClassViewModel> Classes { get; set; }

        public TeacherMyClassesViewModel()
        {
            Classes = new List<TeacherMyClassViewModel>();
        }
    }

    public class TeacherMyClassViewModel
    {
        public int ClassID { get; set; }
        public string ClassName { get; set; }
        public string ProgramName { get; set; }
        public DateTime? StartDate { get; set; }
        public string ClassStatus { get; set; }
        public string Schedule { get; set; }
        public string Room { get; set; }
        public int StudentCount { get; set; }
    }
}
