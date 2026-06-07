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
        [Display(Name = "Ngày kiểm tra")]
        public DateTime? TestDate { get; set; }

        [Required]
        [Display(Name = "Giờ kiểm tra")]
        public string TestTime { get; set; }

        [Required]
        [Display(Name = "Trình độ")]
        public string Level { get; set; }

        [Display(Name = "Điểm kết quả")]
        public string ResultScore { get; set; }

        [Required]
        [Display(Name = "Trạng thái")]
        public string Status { get; set; }
    }
}
