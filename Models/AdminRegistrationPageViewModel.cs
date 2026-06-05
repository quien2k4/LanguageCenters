using System.Collections.Generic;

namespace LanguageCenter.Models
{
    public class AdminRegistrationPageViewModel
    {
        public List<AdminRegistrationViewModel> Registrations { get; set; }
        public string Search { get; set; }
        public string RegStatus { get; set; }
        public string PaymentStatus { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        public AdminRegistrationPageViewModel()
        {
            Registrations = new List<AdminRegistrationViewModel>();
            RegStatus = "All";
            PaymentStatus = "All";
            CurrentPage = 1;
            TotalPages = 1;
        }
    }
}
