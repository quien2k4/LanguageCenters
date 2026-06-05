using System;
using System.ComponentModel.DataAnnotations;

namespace LanguageCenter.Models
{
    public class AdminPlacementTestFormViewModel
    {
        public int TestID { get; set; }
        public string StudentName { get; set; }
        public string StudentEmail { get; set; }
        public string PhoneNumber { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime? TestDate { get; set; }

        [Required]
        public string TestTime { get; set; }

        [Required]
        public string Level { get; set; }

        [Display(Name = "Result Score")]
        public string ResultScore { get; set; }

        [Required]
        public string Status { get; set; }
    }
}
