using System;
using System.ComponentModel.DataAnnotations;

namespace LanguageCenter.Models
{
    public class AdminRegistrationFormViewModel
    {
        public int RegistrationID { get; set; }
        public string StudentName { get; set; }
        public string ClassName { get; set; }
        public string ProgramName { get; set; }
        public DateTime? RegistrationDate { get; set; }
        public string PaymentStatus { get; set; }

        [Required]
        [Display(Name = "Registration Status")]
        public string RegStatus { get; set; }
    }
}
