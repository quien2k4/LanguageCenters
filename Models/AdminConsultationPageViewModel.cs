using System.Collections.Generic;

namespace LanguageCenter.Models
{
    public class AdminConsultationPageViewModel
    {
        public List<AdminConsultationViewModel> Consultations { get; set; }
        public string Search { get; set; }
        public string Status { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalConsultations { get; set; }
        public int PendingConsultations { get; set; }
        public int ProcessingConsultations { get; set; }
        public int CompletedConsultations { get; set; }
        public int CancelledConsultations { get; set; }

        public AdminConsultationPageViewModel()
        {
            Consultations = new List<AdminConsultationViewModel>();
            Status = "All";
            CurrentPage = 1;
            TotalPages = 1;
        }
    }
}
