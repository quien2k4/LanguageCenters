using System.ComponentModel.DataAnnotations;

namespace LanguageCenter.Models
{
    public class AdminProgramViewModel
    {
        public int ProgramID { get; set; }

        [Required]
        [Display(Name = "Program Name")]
        public string ProgramName { get; set; }

        public string Description { get; set; }

        [Display(Name = "Output Standard")]
        public string OutputStandard { get; set; }

        [Required]
        public string Level { get; set; }

        [Required]
        public string Duration { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Fee { get; set; }

        public string ImageURL { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; }
    }
}
