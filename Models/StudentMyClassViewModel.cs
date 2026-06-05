using System;
using System.Collections.Generic;

namespace LanguageCenter.Models
{
    public class StudentMyClassesViewModel
    {
        public List<StudentMyClassViewModel> Classes { get; set; }

        public StudentMyClassesViewModel()
        {
            Classes = new List<StudentMyClassViewModel>();
        }
    }

    public class StudentMyClassViewModel
    {
        public int RegistrationID { get; set; }
        public int ClassID { get; set; }
        public string ClassName { get; set; }
        public string ProgramName { get; set; }
        public string TeacherName { get; set; }
        public string ClassStatus { get; set; }
        public DateTime? StartDate { get; set; }
        public string Schedule { get; set; }
        public string Room { get; set; }
        public DateTime? RegistrationDate { get; set; }
        public string RegStatus { get; set; }
        public decimal Amount { get; set; }
        public string PaymentStatus { get; set; }
    }
}
