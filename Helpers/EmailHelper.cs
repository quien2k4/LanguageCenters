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
        public string PhoneNumber { get; set; }
        public string ClassName { get; set; }
        public string ProgramName { get; set; }
        public decimal Amount { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string Method { get; set; }
        public string VnPayTransactionNo { get; set; }
        public string ResponseCode { get; set; }
    }

    public class PaymentEmailSendResult
    {
        public bool AdminSent { get; set; }
        public bool StudentSent { get; set; }
        public bool IsDuplicateRecipient { get; set; }
        public string AdminError { get; set; }
        public string StudentError { get; set; }
    }

    public static class EmailHelper
    {
        public static bool SendOtpEmail(string toEmail, string subject, string bodyText, out string errorMessage)
        {
            errorMessage = string.Empty;

            var config = GetSmtpConfig();
            if (!config.IsValid)
            {
                errorMessage = "SMTP is not configured.";
                return false;
            }

            var html = new StringBuilder();
            html.Append("<div style=\"font-family:Arial,sans-serif;color:#1f2937;line-height:1.6;\">");
            html.Append("<h2 style=\"color:#16a34a;margin-bottom:12px;\">LanguageCenter</h2>");
            html.Append("<p>");
            html.Append(Html(bodyText).Replace("\r\n", "<br />").Replace("\n", "<br />"));
            html.Append("</p>");
            html.Append("</div>");

            return SendEmail(config, toEmail, subject, html.ToString(), out errorMessage);
        }

        public static PaymentEmailSendResult SendPaymentSuccessEmails(PaymentSuccessEmailInfo info)
        {
            var result = new PaymentEmailSendResult();

            try
            {
                var config = GetSmtpConfig();
                if (!config.IsValid)
                {
                    result.AdminError = "SMTP is not configured.";
                    result.StudentError = "SMTP is not configured.";
                    return result;
                }

                var adminEmail = GetAppSetting("payment_AdminEmail");
                var studentEmail = (info.StudentEmail ?? string.Empty).Trim();

                if (!string.IsNullOrWhiteSpace(adminEmail))
                {
                    result.AdminSent = SendEmail(
                        config,
                        adminEmail,
                        "[LanguageCenter] Học viên vừa thanh toán thành công",
                        BuildAdminPaymentSuccessBody(info),
                        out var adminError);
                    result.AdminError = adminError;
                }
                else
                {
                    result.AdminError = "Admin email is not configured.";
                }

                if (string.IsNullOrWhiteSpace(studentEmail))
                {
                    result.StudentError = "Student email is empty.";
                    return result;
                }

                if (!string.IsNullOrWhiteSpace(adminEmail)
                    && string.Equals(adminEmail, studentEmail, StringComparison.OrdinalIgnoreCase))
                {
                    result.IsDuplicateRecipient = true;
                    result.StudentSent = result.AdminSent;
                    result.StudentError = result.AdminError;
                    return result;
                }

                result.StudentSent = SendEmail(
                    config,
                    studentEmail,
                    "[LanguageCenter] Xác nhận thanh toán học phí thành công",
                    BuildStudentPaymentSuccessBody(info),
                    out var studentError);
                result.StudentError = studentError;

                return result;
            }
            catch (Exception ex)
            {
                result.AdminError = string.IsNullOrWhiteSpace(result.AdminError) ? ex.Message : result.AdminError;
                result.StudentError = string.IsNullOrWhiteSpace(result.StudentError) ? ex.Message : result.StudentError;
                System.Diagnostics.Debug.WriteLine("Payment confirmation email failed: " + ex.Message);
                return result;
            }
        }

        public static bool SendPaymentSuccessEmail(PaymentSuccessEmailInfo info, out string errorMessage)
        {
            var result = SendPaymentSuccessEmails(info);
            errorMessage = result.AdminError;
            return result.AdminSent;
        }

        private static bool SendEmail(SmtpConfig config, string to, string subject, string body, out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(config.User, config.FromName);
                    message.To.Add(to);
                    message.Subject = subject;
                    message.SubjectEncoding = Encoding.UTF8;
                    message.BodyEncoding = Encoding.UTF8;
                    message.IsBodyHtml = true;
                    message.Body = body;

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
                System.Diagnostics.Debug.WriteLine("Email send failed: " + ex.Message);
                return false;
            }
        }

        private static string BuildStudentPaymentSuccessBody(PaymentSuccessEmailInfo info)
        {
            var html = new StringBuilder();
            html.Append("<div style=\"font-family:Arial,sans-serif;color:#1f2937;line-height:1.6;\">");
            html.Append("<h2 style=\"color:#16a34a;margin-bottom:12px;\">Thanh toán học phí thành công</h2>");
            html.Append("<p>Xin chào <strong>");
            html.Append(Html(info.StudentName));
            html.Append("</strong>,</p>");
            html.Append("<p>Hệ thống LanguageCenter xác nhận bạn đã thanh toán học phí thành công.</p>");
            html.Append("<table cellpadding=\"8\" cellspacing=\"0\" style=\"border-collapse:collapse;width:100%;max-width:680px;\">");
            AppendRow(html, "Mã thanh toán", info.PaymentID.ToString(CultureInfo.InvariantCulture));
            AppendRow(html, "Mã đăng ký", info.RegistrationID.ToString(CultureInfo.InvariantCulture));
            AppendRow(html, "Lớp", info.ClassName);
            AppendRow(html, "Chương trình", info.ProgramName);
            AppendRow(html, "Số tiền", FormatMoney(info.Amount));
            AppendRow(html, "Phương thức", string.IsNullOrWhiteSpace(info.Method) ? "VNPAY" : info.Method);
            AppendRow(html, "Ngày thanh toán", FormatDate(info.PaymentDate));
            AppendRow(html, "Mã giao dịch VNPAY", EmptyText(info.VnPayTransactionNo));
            html.Append("</table>");
            html.Append("<p style=\"margin-top:18px;\">Cảm ơn bạn đã sử dụng dịch vụ của LanguageCenter.</p>");
            html.Append("</div>");
            return html.ToString();
        }

        private static string BuildAdminPaymentSuccessBody(PaymentSuccessEmailInfo info)
        {
            var html = new StringBuilder();
            html.Append("<div style=\"font-family:Arial,sans-serif;color:#1f2937;line-height:1.6;\">");
            html.Append("<h2 style=\"color:#16a34a;margin-bottom:12px;\">Học viên vừa thanh toán thành công</h2>");
            html.Append("<table cellpadding=\"8\" cellspacing=\"0\" style=\"border-collapse:collapse;width:100%;max-width:680px;\">");
            AppendRow(html, "Học viên", info.StudentName);
            AppendRow(html, "Email học viên", info.StudentEmail);
            AppendRow(html, "Số điện thoại", info.PhoneNumber);
            AppendRow(html, "Mã thanh toán", info.PaymentID.ToString(CultureInfo.InvariantCulture));
            AppendRow(html, "Mã đăng ký", info.RegistrationID.ToString(CultureInfo.InvariantCulture));
            AppendRow(html, "Lớp", info.ClassName);
            AppendRow(html, "Chương trình", info.ProgramName);
            AppendRow(html, "Số tiền", FormatMoney(info.Amount));
            AppendRow(html, "Phương thức", string.IsNullOrWhiteSpace(info.Method) ? "VNPAY" : info.Method);
            AppendRow(html, "Ngày thanh toán", FormatDate(info.PaymentDate));
            AppendRow(html, "Mã giao dịch VNPAY", EmptyText(info.VnPayTransactionNo));
            AppendRow(html, "ResponseCode", EmptyText(info.ResponseCode));
            html.Append("</table>");
            html.Append("</div>");
            return html.ToString();
        }

        private static void AppendRow(StringBuilder html, string label, string value)
        {
            html.Append("<tr>");
            html.Append("<td style=\"border:1px solid #e5e7eb;background:#f9fafb;font-weight:bold;width:210px;\">");
            html.Append(Html(label));
            html.Append("</td>");
            html.Append("<td style=\"border:1px solid #e5e7eb;\">");
            html.Append(Html(EmptyText(value)));
            html.Append("</td>");
            html.Append("</tr>");
        }

        private static string FormatMoney(decimal amount)
        {
            return amount.ToString("N0", CultureInfo.InvariantCulture) + " VNĐ";
        }

        private static string FormatDate(DateTime? date)
        {
            return date.HasValue
                ? date.Value.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture)
                : "Đang cập nhật";
        }

        private static string EmptyText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "Không có" : value;
        }

        private static string Html(string value)
        {
            return HttpUtility.HtmlEncode(value ?? string.Empty);
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
