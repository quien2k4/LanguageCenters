using System.ComponentModel.DataAnnotations;

namespace LanguageCenter.Models
{
    public class AdminConsultationFormViewModel
    {
        public int ConsultationID { get; set; }
        public int? ClassID { get; set; }
        public string ClassName { get; set; }
        public string ProgramName { get; set; }
        public string GuestName { get; set; }
        public string ContactInformation { get; set; }
        public string QuestionContent { get; set; }

        [Required]
        [Display(Name = "Trạng thái yêu cầu")]
        public string RequestStatus { get; set; }
    }
}
