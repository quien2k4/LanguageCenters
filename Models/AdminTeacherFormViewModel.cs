using System.ComponentModel.DataAnnotations;

namespace LanguageCenter.Models
{
    public class AdminTeacherFormViewModel
    {
        public int TeacherID { get; set; }
        public int AccountID { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string Password { get; set; }

        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; }

        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required]
        public string Expertise { get; set; }

        public string Avatar { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; }
    }
}
