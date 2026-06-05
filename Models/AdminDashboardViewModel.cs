using System;
using System.Collections.Generic;

namespace LanguageCenter.Models
{
    public class AdminDashboardViewModel
    {
        public string FullName { get; set; }
        public string Role { get; set; }
        public int AccountID { get; set; }

        public int TotalPrograms { get; set; }
        public int TotalClasses { get; set; }
        public int TotalStudents { get; set; }
        public int TotalTeachers { get; set; }
        public int TotalRegistrations { get; set; }
        public int TotalPayments { get; set; }
        public int TotalPlacementTests { get; set; }
        public int TotalConsultations { get; set; }

        public decimal TotalRevenue { get; set; }
        public int UnpaidPayments { get; set; }
        public int PaidPayments { get; set; }

        public List<AdminRecentRegistrationViewModel> RecentRegistrations { get; set; }
        public List<AdminRecentPaymentViewModel> RecentPayments { get; set; }
        public List<AdminRecentConsultationViewModel> RecentConsultations { get; set; }

        public AdminDashboardViewModel()
        {
            RecentRegistrations = new List<AdminRecentRegistrationViewModel>();
            RecentPayments = new List<AdminRecentPaymentViewModel>();
            RecentConsultations = new List<AdminRecentConsultationViewModel>();
        }
    }

    public class AdminRecentRegistrationViewModel
    {
        public string StudentName { get; set; }
        public string ClassName { get; set; }
        public string ProgramName { get; set; }
        public DateTime? RegistrationDate { get; set; }
        public string RegStatus { get; set; }
    }

    public class AdminRecentPaymentViewModel
    {
        public string StudentName { get; set; }
        public string ClassName { get; set; }
        public decimal Amount { get; set; }
        public string PaymentStatus { get; set; }
        public DateTime? PaymentDate { get; set; }
    }

    public class AdminRecentConsultationViewModel
    {
        public string GuestName { get; set; }
        public string ContactInformation { get; set; }
        public string QuestionContent { get; set; }
        public string RequestStatus { get; set; }
    }
}
