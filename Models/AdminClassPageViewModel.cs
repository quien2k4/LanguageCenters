using System.Collections.Generic;
using System.Web.Mvc;

namespace LanguageCenter.Models
{
    public class AdminClassPageViewModel
    {
        public List<AdminClassViewModel> Classes { get; set; }
        public List<SelectListItem> Programs { get; set; }
        public List<SelectListItem> Teachers { get; set; }
        public List<SelectListItem> Statuses { get; set; }
        public string Search { get; set; }
        public int? ProgramID { get; set; }
        public int? TeacherID { get; set; }
        public int? StatusID { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        public AdminClassPageViewModel()
        {
            Classes = new List<AdminClassViewModel>();
            Programs = new List<SelectListItem>();
            Teachers = new List<SelectListItem>();
            Statuses = new List<SelectListItem>();
            CurrentPage = 1;
            TotalPages = 1;
        }
    }
}
