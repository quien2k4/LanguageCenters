namespace LanguageCenter.Models
{
    public static class DisplayText
    {
        public static string Status(string value)
        {
            switch ((value ?? string.Empty).Trim())
            {
                case "Pending":
                    return "Chờ xử lý";
                case "Approved":
                    return "Đã duyệt";
                case "Cancelled":
                    return "Đã hủy";
                case "Paid":
                    return "Đã thanh toán";
                case "Unpaid":
                    return "Chưa thanh toán";
                case "Completed":
                    return "Hoàn thành";
                case "Processing":
                    return "Đang xử lý";
                case "Active":
                    return "Đang hoạt động";
                case "Inactive":
                    return "Ngừng hoạt động";
                case "Failed":
                    return "Thất bại";
                case "Refunded":
                    return "Đã hoàn tiền";
                case "Confirmed":
                    return "Đã xác nhận";
                case "No payment":
                    return "Chưa có thanh toán";
                default:
                    return string.IsNullOrWhiteSpace(value) ? "Đang cập nhật" : value;
            }
        }
    }
}
