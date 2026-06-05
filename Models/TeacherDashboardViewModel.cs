using System;
using System.Collections.Generic;

namespace LanguageCenter.Models
{
    public class TeacherDashboardViewModel
    {
        public string FullName { get; set; }
        public string Role { get; set; }
        public int AccountID { get; set; }
        public int TeacherID { get; set; }
        public string Expertise { get; set; }
        public int TotalClasses { get; set; }
        public int TotalStudents { get; set; }
        public int TotalSchedules { get; set; }
        public int RecentRegistrations { get; set; }
        public List<TeacherScheduleViewModel> TeachingSchedules { get; set; }
        public List<TeacherRecentActivityViewModel> RecentActivities { get; set; }
        public List<TeacherRecentClassViewModel> RecentClasses { get; set; }

        public TeacherDashboardViewModel()
        {
            TeachingSchedules = new List<TeacherScheduleViewModel>();
            RecentActivities = new List<TeacherRecentActivityViewModel>();
            RecentClasses = new List<TeacherRecentClassViewModel>();
        }
    }

    public class TeacherScheduleViewModel
    {
        public string ClassName { get; set; }
        public string ProgramName { get; set; }
        public string DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Room { get; set; }
    }

    public class TeacherRecentActivityViewModel
    {
        public string StudentName { get; set; }
        public string ClassName { get; set; }
        public DateTime? RegistrationDate { get; set; }
        public string RegStatus { get; set; }
    }

    public class TeacherRecentClassViewModel
    {
        public int ClassID { get; set; }
        public string ClassName { get; set; }
        public string ProgramName { get; set; }
        public string StatusName { get; set; }
        public DateTime? StartDate { get; set; }
        public int StudentCount { get; set; }
    }
}
