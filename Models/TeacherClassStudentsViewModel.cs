using System;
using System.Collections.Generic;

namespace LanguageCenter.Models
{
    public class TeacherClassStudentsViewModel
    {
        public int ClassID { get; set; }
        public string ClassName { get; set; }
        public string ProgramName { get; set; }
        public DateTime? StartDate { get; set; }
        public string ClassStatus { get; set; }
        public List<TeacherClassStudentViewModel> Students { get; set; }

        public TeacherClassStudentsViewModel()
        {
            Students = new List<TeacherClassStudentViewModel>();
        }
    }

    public class TeacherClassStudentViewModel
    {
        public int StudentID { get; set; }
        public string StudentName { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime? RegistrationDate { get; set; }
        public string RegistrationStatus { get; set; }
        public string PaymentStatus { get; set; }
        public string LatestAttendanceStatus { get; set; }
    }
}
