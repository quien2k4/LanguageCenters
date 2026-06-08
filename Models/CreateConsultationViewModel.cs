using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace LanguageCenter.Models
{
    public class CreateConsultationViewModel
    {
        [Required]
        [Display(Name = "Lớp liên quan")]
        public int? ClassID { get; set; }

        [Required]
        [Display(Name = "Contact Information")]
        public string ContactInformation { get; set; }

        [Required]
        [Display(Name = "Question Content")]
        public string QuestionContent { get; set; }

        public List<SelectListItem> Classes { get; set; }

        public CreateConsultationViewModel()
        {
            Classes = new List<SelectListItem>();
        }
    }
}
