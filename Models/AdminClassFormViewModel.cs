using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace LanguageCenter.Models
{
    public class AdminClassFormViewModel
    {
        public int ClassID { get; set; }

        [Required]
        [Display(Name = "Class Name")]
        public string ClassName { get; set; }

        [Required]
        [Display(Name = "Program")]
        public int? ProgramID { get; set; }

        [Required]
        [Display(Name = "Teacher")]
        public int? TeacherID { get; set; }

        [Required]
        [Display(Name = "Status")]
        public int? StatusID { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime? StartDate { get; set; }

        [Display(Name = "Day Of Week")]
        public string DayOfWeek { get; set; }

        [Display(Name = "Start Time")]
        public string StartTime { get; set; }

        [Display(Name = "End Time")]
        public string EndTime { get; set; }

        public string Room { get; set; }

        public List<SelectListItem> Programs { get; set; }
        public List<SelectListItem> Teachers { get; set; }
        public List<SelectListItem> Statuses { get; set; }

        public AdminClassFormViewModel()
        {
            Programs = new List<SelectListItem>();
            Teachers = new List<SelectListItem>();
            Statuses = new List<SelectListItem>();
        }
    }
}
