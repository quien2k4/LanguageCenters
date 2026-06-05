using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace LanguageCenter.Models
{
    public class CreatePlacementTestViewModel
    {
        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Test Date")]
        public DateTime? TestDate { get; set; }

        [Required]
        [Display(Name = "Test Time")]
        public string TestTime { get; set; }

        [Required]
        public string Level { get; set; }
    }
}
