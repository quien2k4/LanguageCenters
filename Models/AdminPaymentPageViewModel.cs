using System.Collections.Generic;

namespace LanguageCenter.Models
{
    public class AdminPaymentPageViewModel
    {
        public List<AdminPaymentViewModel> Payments { get; set; }
        public string Search { get; set; }
        public string PaymentStatus { get; set; }
        public string Method { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalPayments { get; set; }
        public int PaidPayments { get; set; }
        public int UnpaidPayments { get; set; }
        public decimal TotalRevenue { get; set; }

        public AdminPaymentPageViewModel()
        {
            Payments = new List<AdminPaymentViewModel>();
            PaymentStatus = "All";
            Method = "All";
            CurrentPage = 1;
            TotalPages = 1;
        }
    }
}
