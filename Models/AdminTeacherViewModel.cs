namespace LanguageCenter.Models
{
    public class AdminTeacherViewModel
    {
        public int TeacherID { get; set; }
        public int AccountID { get; set; }
        public string Avatar { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Expertise { get; set; }
        public bool IsActive { get; set; }
        public int ClassCount { get; set; }
    }
}
