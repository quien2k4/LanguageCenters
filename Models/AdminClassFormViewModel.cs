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
        [Display(Name = "Tên lớp")]
        public string ClassName { get; set; }

        [Required]
        [Display(Name = "Chương trình")]
        public int? ProgramID { get; set; }

        [Required]
        [Display(Name = "Giáo viên")]
        public int? TeacherID { get; set; }

        [Required]
        [Display(Name = "Trạng thái")]
        public int? StatusID { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày bắt đầu")]
        public DateTime? StartDate { get; set; }

        [Display(Name = "Thứ trong tuần")]
        public string DayOfWeek { get; set; }

        [Display(Name = "Giờ bắt đầu")]
        public string StartTime { get; set; }

        [Display(Name = "Giờ kết thúc")]
        public string EndTime { get; set; }

        [Display(Name = "Phòng")]
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
