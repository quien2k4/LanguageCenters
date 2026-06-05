using System.Collections.Generic;

namespace LanguageCenter.Models
{
    public class StudentConsultationsViewModel
    {
        public List<StudentConsultationViewModel> Consultations { get; set; }

        public StudentConsultationsViewModel()
        {
            Consultations = new List<StudentConsultationViewModel>();
        }
    }

    public class StudentConsultationViewModel
    {
        public int ConsultationID { get; set; }
        public string GuestName { get; set; }
        public string ContactInformation { get; set; }
        public string QuestionContent { get; set; }
        public string RequestStatus { get; set; }
    }
}
