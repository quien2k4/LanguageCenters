using System;
using System.Collections.Generic;

namespace LanguageCenter.Models
{
    public class TeacherMaterialsViewModel
    {
        public int ClassID { get; set; }
        public string ClassName { get; set; }
        public string ProgramName { get; set; }
        public List<TeacherMaterialViewModel> Materials { get; set; }

        public TeacherMaterialsViewModel()
        {
            Materials = new List<TeacherMaterialViewModel>();
        }
    }

    public class TeacherMaterialViewModel
    {
        public int MaterialID { get; set; }
        public string Title { get; set; }
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public DateTime? UploadDate { get; set; }
    }
}
