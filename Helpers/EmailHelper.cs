using System;
using System.Configuration;
using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Web;

namespace LanguageCenter.Helpers
{
    public class PaymentSuccessEmailInfo
    {
        public int PaymentID { get; set; }
        public int RegistrationID { get; set; }
        public string StudentName { get; set; }
        public string StudentEmail { get; set; }
        public string ClassName { get; set; }
        public string ProgramName { get; set; }
        public decimal Amount { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string Method { get; set; }
        public string VnPayTransactionNo { get; set; }
        public string ResponseCode { get; set; }
    }

    public static class EmailHelper
    {
        public static bool SendPaymentSuccessEmail(PaymentSuccessEmailInfo info, out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                var config = GetSmtpConfig();
                if (!config.IsValid)
                {
                    errorMessage = "SMTP is not configured.";
                    return false;
                }

                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(config.User, config.FromName);
                    message.To.Add(config.User);
                    message.Subject = "[LanguageCenter] Xác nhận thanh toán thành công";
                    message.SubjectEncoding = Encoding.UTF8;
                    message.BodyEncoding = Encoding.UTF8;
                    message.IsBodyHtml = true;
                    message.Body = BuildPaymentSuccessBody(info);

                    using (var smtp = new SmtpClient(config.Host, config.Port))
                    {
                        smtp.EnableSsl = config.EnableSsl;
                        smtp.Credentials = new NetworkCredential(config.User, config.Password);
                        smtp.Send(message);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                System.Diagnostics.Debug.WriteLine("Payment confirmation email failed: " + ex.Message);
                return false;
            }
        }

        private static string BuildPaymentSuccessBody(PaymentSuccessEmailInfo info)
        {
            var paymentDate = info.PaymentDate.HasValue
                ? info.PaymentDate.Value.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture)
                : "Đang cập nhật";

            var amount = info.Amount.ToString("N0", CultureInfo.InvariantCulture) + " VNĐ";

            var html = new StringBuilder();
            html.Append("<div style=\"font-family:Arial,sans-serif;color:#1f2937;line-height:1.6;\">");
            html.Append("<h2 style=\"color:#16a34a;margin-bottom:12px;\">Thanh toán thành công</h2>");
            html.Append("<p>Cảm ơn bạn đã thanh toán học phí tại <strong>LanguageCenter</strong>.</p>");
            html.Append("<table cellpadding=\"8\" cellspacing=\"0\" style=\"border-collapse:collapse;width:100%;max-width:680px;\">");
            AppendRow(html, "PaymentID", info.PaymentID.ToString(CultureInfo.InvariantCulture));
            AppendRow(html, "RegistrationID", info.RegistrationID.ToString(CultureInfo.InvariantCulture));
            AppendRow(html, "Tên học viên", info.StudentName);
            AppendRow(html, "Email học viên", info.StudentEmail);
            AppendRow(html, "Tên lớp", info.ClassName);
            AppendRow(html, "Tên chương trình", info.ProgramName);
            AppendRow(html, "Số tiền", amount);
            AppendRow(html, "Phương thức thanh toán", string.IsNullOrWhiteSpace(info.Method) ? "VNPAY" : info.Method);
            AppendRow(html, "Ngày thanh toán", paymentDate);
            AppendRow(html, "Mã giao dịch VNPAY", string.IsNullOrWhiteSpace(info.VnPayTransactionNo) ? "Không có" : info.VnPayTransactionNo);
            AppendRow(html, "ResponseCode", string.IsNullOrWhiteSpace(info.ResponseCode) ? "Không có" : info.ResponseCode);
            html.Append("</table>");
            html.Append("<p style=\"margin-top:18px;\">LanguageCenter</p>");
            html.Append("</div>");

            return html.ToString();
        }

        private static void AppendRow(StringBuilder html, string label, string value)
        {
            html.Append("<tr>");
            html.Append("<td style=\"border:1px solid #e5e7eb;background:#f9fafb;font-weight:bold;width:210px;\">");
            html.Append(HttpUtility.HtmlEncode(label));
            html.Append("</td>");
            html.Append("<td style=\"border:1px solid #e5e7eb;\">");
            html.Append(HttpUtility.HtmlEncode(string.IsNullOrWhiteSpace(value) ? "Đang cập nhật" : value));
            html.Append("</td>");
            html.Append("</tr>");
        }

        private static SmtpConfig GetSmtpConfig()
        {
            int port;
            if (!int.TryParse(GetAppSetting("smtp_Port"), out port))
            {
                port = 587;
            }

            bool enableSsl;
            if (!bool.TryParse(GetAppSetting("smtp_EnableSsl"), out enableSsl))
            {
                enableSsl = true;
            }

            return new SmtpConfig
            {
                Host = GetAppSetting("smtp_Host"),
                Port = port,
                EnableSsl = enableSsl,
                User = GetAppSetting("smtp_User"),
                Password = GetAppSetting("smtp_Password"),
                FromName = GetAppSetting("smtp_FromName")
            };
        }

        private static string GetAppSetting(string key)
        {
            return (ConfigurationManager.AppSettings[key] ?? string.Empty).Trim();
        }

        private class SmtpConfig
        {
            public string Host { get; set; }
            public int Port { get; set; }
            public bool EnableSsl { get; set; }
            public string User { get; set; }
            public string Password { get; set; }
            public string FromName { get; set; }

            public bool IsValid
            {
                get
                {
                    return !string.IsNullOrWhiteSpace(Host)
                        && !string.IsNullOrWhiteSpace(User)
                        && !string.IsNullOrWhiteSpace(Password)
                        && Password != "APP_PASSWORD_GMAIL";
                }
            }
        }
    }
}
