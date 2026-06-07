using System.ComponentModel.DataAnnotations;

namespace LanguageCenter.Models
{
    public class AdminProgramViewModel
    {
        public int ProgramID { get; set; }

        [Required]
        [Display(Name = "Tên chương trình")]
        public string ProgramName { get; set; }

        [Display(Name = "Mô tả")]
        public string Description { get; set; }

        [Display(Name = "Chuẩn đầu ra")]
        public string OutputStandard { get; set; }

        [Required]
        [Display(Name = "Trình độ")]
        public string Level { get; set; }

        [Required]
        [Display(Name = "Thời lượng")]
        public string Duration { get; set; }

        [Range(0, double.MaxValue)]
        [Display(Name = "Học phí")]
        public decimal Fee { get; set; }

        public string ImageURL { get; set; }

        [Display(Name = "Đang hoạt động")]
        public bool IsActive { get; set; }
    }
}
