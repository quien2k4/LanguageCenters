using System.ComponentModel.DataAnnotations;

namespace LanguageCenter.Models
{
    public class CreateConsultationViewModel
    {
        [Required]
        [Display(Name = "Contact Information")]
        public string ContactInformation { get; set; }

        [Required]
        [Display(Name = "Question Content")]
        public string QuestionContent { get; set; }
    }
}
