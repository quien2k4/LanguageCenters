using System;
using System.Collections.Generic;

namespace LanguageCenter.Models
{
    public class TeacherClassAttendanceViewModel
    {
        public int ClassID { get; set; }
        public string ClassName { get; set; }
        public string ProgramName { get; set; }
        public string TeacherName { get; set; }
        public string ScheduleText { get; set; }
        public string RoomText { get; set; }
        public string ProgressText { get; set; }
        public int ProgressPercent { get; set; }
        public DateTime SelectedDate { get; set; }
        public List<TeacherAttendanceStudentViewModel> Students { get; set; }

        public TeacherClassAttendanceViewModel()
        {
            Students = new List<TeacherAttendanceStudentViewModel>();
        }
    }

    public class TeacherAttendanceStudentViewModel
    {
        public int RegistrationID { get; set; }
        public int StudentID { get; set; }
        public string StudentName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string RegStatus { get; set; }
        public string AttendanceStatus { get; set; }
    }

    public class StudentAttendanceViewModel
    {
        public DateTime ClassDate { get; set; }
        public string Status { get; set; }
        public string StatusText { get; set; }
        public string BadgeClass { get; set; }
    }
}
