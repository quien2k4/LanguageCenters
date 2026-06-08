using System.Collections.Generic;

namespace LanguageCenter.Models
{
    public class TeacherConsultationsViewModel
    {
        public List<TeacherConsultationViewModel> Consultations { get; set; }

        public TeacherConsultationsViewModel()
        {
            Consultations = new List<TeacherConsultationViewModel>();
        }
    }

    public class TeacherConsultationViewModel
    {
        public int ConsultationID { get; set; }
        public int ClassID { get; set; }
        public string ClassName { get; set; }
        public string ProgramName { get; set; }
        public string GuestName { get; set; }
        public string ContactInformation { get; set; }
        public string QuestionContent { get; set; }
        public string RequestStatus { get; set; }
    }
}
