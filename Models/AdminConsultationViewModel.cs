namespace LanguageCenter.Models
{
    public class AdminConsultationViewModel
    {
        public int ConsultationID { get; set; }
        public int? ClassID { get; set; }
        public string ClassName { get; set; }
        public string ProgramName { get; set; }
        public string GuestName { get; set; }
        public string ContactInformation { get; set; }
        public string QuestionContent { get; set; }
        public string RequestStatus { get; set; }
    }
}
