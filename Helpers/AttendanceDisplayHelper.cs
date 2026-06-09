namespace LanguageCenter.Helpers
{
    public static class AttendanceDisplayHelper
    {
        public static string Text(string status)
        {
            switch ((status ?? string.Empty).Trim())
            {
                case "Present":
                    return "Có mặt";
                case "Absent":
                    return "Vắng";
                case "Excused":
                    return "Vắng có phép";
                default:
                    return "Chưa điểm danh";
            }
        }

        public static string BadgeClass(string status)
        {
            switch ((status ?? string.Empty).Trim())
            {
                case "Present":
                    return "bg-success";
                case "Absent":
                    return "bg-danger";
                case "Excused":
                    return "bg-warning text-dark";
                default:
                    return "bg-secondary";
            }
        }

        public static bool IsValidStatus(string status)
        {
            status = (status ?? string.Empty).Trim();
            return status == "Present" || status == "Absent" || status == "Excused";
        }
    }
}
