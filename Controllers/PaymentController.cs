using System;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using LanguageCenter.Models;
using LanguageCenter.VNPAY;

namespace LanguageCenter.Controllers
{
    public class PaymentController : Controller
    {
        private readonly string connectionString =
            ConfigurationManager.ConnectionStrings["LanguageCenterConnectionString"].ConnectionString;

        [HttpGet]
        public ActionResult VnPayCheckout(int paymentId)
        {
            var authResult = CheckStudentPermission();
            if (authResult != null)
            {
                return authResult;
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var student = GetCurrentStudent(db);
                if (student == null)
                {
                    TempData["Error"] = "Student profile not found.";
                    return RedirectToAction("Index", "Home");
                }

                var paymentInfo = (
                    from pay in db.PAYMENTs
                    join registration in db.REGISTRATIONs on pay.RegistrationID equals registration.RegistrationID
                    where pay.PaymentID == paymentId
                    select new
                    {
                        Payment = pay,
                        registration.StudentID
                    })
                    .FirstOrDefault();

                if (paymentInfo == null)
                {
                    TempData["Error"] = "Payment not found.";
                    return RedirectToAction("Payments", "Student");
                }

                if (paymentInfo.StudentID != student.StudentID)
                {
                    TempData["Error"] = "You do not have permission to pay this payment.";
                    return RedirectToAction("Payments", "Student");
                }

                if (paymentInfo.Payment.PaymentStatus == "Paid")
                {
                    TempData["Error"] = "This payment is already paid.";
                    return RedirectToAction("Payments", "Student");
                }

                var config = GetVnPayConfig();
                if (!config.IsValid)
                {
                    TempData["Error"] = "VNPAY configuration is missing or still uses placeholder values. Please update vnp_TmnCode and vnp_HashSecret in web.config appSettings.";
                    return RedirectToAction("Payments", "Student");
                }

                var library = new VnPayLibrary();
                var createDate = DateTime.Now;
                var returnUrl = string.IsNullOrWhiteSpace(config.ReturnUrl)
                    ? Url.Action("VnPayReturn", "Payment", null, Request.Url.Scheme)
                    : config.ReturnUrl;

                library.AddRequestData("vnp_Version", "2.1.0");
                library.AddRequestData("vnp_Command", "pay");
                library.AddRequestData("vnp_TmnCode", config.TmnCode);
                library.AddRequestData("vnp_Amount", VnPayLibrary.FormatAmount(paymentInfo.Payment.Amount));
                library.AddRequestData("vnp_CreateDate", createDate.ToString("yyyyMMddHHmmss"));
                library.AddRequestData("vnp_CurrCode", "VND");
                library.AddRequestData("vnp_IpAddr", GetIpAddress());
                library.AddRequestData("vnp_Locale", "vn");
                library.AddRequestData("vnp_OrderInfo", "LanguageCenter Payment " + paymentInfo.Payment.PaymentID);
                library.AddRequestData("vnp_OrderType", "other");
                library.AddRequestData("vnp_ReturnUrl", returnUrl);
                library.AddRequestData("vnp_TxnRef", paymentInfo.Payment.PaymentID.ToString(CultureInfo.InvariantCulture));

                var paymentUrl = library.CreateRequestUrl(config.Url, config.HashSecret);
                if (IsVnPayDebugEnabled())
                {
                    var debugText = "vnp_Url: " + config.Url + Environment.NewLine
                        + "vnp_TmnCode: " + config.TmnCode + Environment.NewLine
                        + "vnp_ReturnUrl: " + returnUrl + Environment.NewLine
                        + "paymentId: " + paymentInfo.Payment.PaymentID + Environment.NewLine
                        + "vnp_Amount: " + VnPayLibrary.FormatAmount(paymentInfo.Payment.Amount) + Environment.NewLine
                        + "vnp_RequestData: " + library.GetRequestDebugQuery() + Environment.NewLine
                        + "vnp_PaymentUrlMasked: " + MaskSecureHash(paymentUrl);

                    return Content(debugText, "text/plain");
                }

                return Redirect(paymentUrl);
            }
        }

        [HttpGet]
        public ActionResult VnPayReturn()
        {
            var config = GetVnPayConfig();
            var library = new VnPayLibrary();

            foreach (string key in Request.QueryString.AllKeys)
            {
                library.AddResponseData(key, Request.QueryString[key]);
            }

            var secureHash = library.GetResponseData("vnp_SecureHash");
            var responseCode = library.GetResponseData("vnp_ResponseCode");
            var transactionNo = library.GetResponseData("vnp_TransactionNo");
            var txnRef = library.GetResponseData("vnp_TxnRef");
            var amountText = library.GetResponseData("vnp_Amount");

            int paymentId;
            if (!int.TryParse(txnRef, out paymentId))
            {
                return StoreAndRedirectResult(false, "Invalid payment", "VNPAY did not return a valid payment reference.", null, amountText, transactionNo, responseCode);
            }

            if (!config.IsValid || string.IsNullOrWhiteSpace(secureHash) || !library.ValidateSignature(secureHash, config.HashSecret))
            {
                return StoreAndRedirectResult(false, "Invalid signature", "Payment signature is invalid. The payment was not updated.", paymentId, amountText, transactionNo, responseCode);
            }

            if (responseCode != "00")
            {
                return StoreAndRedirectResult(false, "Payment failed", "VNPAY returned response code " + responseCode + ". The payment remains unpaid.", paymentId, amountText, transactionNo, responseCode);
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var payment = db.PAYMENTs.FirstOrDefault(pay => pay.PaymentID == paymentId);

                if (payment == null)
                {
                    return StoreAndRedirectResult(false, "Payment not found", "The returned payment reference was not found.", paymentId, amountText, transactionNo, responseCode);
                }

                if (payment.PaymentStatus == "Paid")
                {
                    return StoreAndRedirectResult(true, "Payment already paid", "This payment was already marked as paid.", paymentId, amountText, transactionNo, responseCode);
                }

                if (amountText != VnPayLibrary.FormatAmount(payment.Amount))
                {
                    return StoreAndRedirectResult(false, "Invalid amount", "Returned amount does not match the payment amount.", paymentId, amountText, transactionNo, responseCode);
                }

                payment.PaymentStatus = "Paid";
                payment.Method = "VNPAY";
                payment.PaymentDate = DateTime.Now;
                db.SubmitChanges();
            }

            return StoreAndRedirectResult(true, "Payment successful", "Your VNPAY payment was completed successfully.", paymentId, amountText, transactionNo, responseCode);
        }

        [HttpGet]
        public ActionResult Result()
        {
            var model = TempData["PaymentResult"] as PaymentResultViewModel
                ?? new PaymentResultViewModel
                {
                    IsSuccess = false,
                    Title = "Payment result",
                    Message = "No payment result is available."
                };

            return View(model);
        }

        private ActionResult StoreAndRedirectResult(bool isSuccess, string title, string message, int? paymentId, string amount, string transactionNo, string responseCode)
        {
            TempData["PaymentResult"] = new PaymentResultViewModel
            {
                IsSuccess = isSuccess,
                Title = title,
                Message = message,
                PaymentID = paymentId,
                Amount = amount,
                TransactionNo = transactionNo,
                ResponseCode = responseCode
            };

            TempData[isSuccess ? "Success" : "Error"] = message;
            return RedirectToAction("Result", "Payment");
        }

        private ActionResult CheckStudentPermission()
        {
            if (Session["AccountID"] == null || Session["Role"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (Session["Role"].ToString() != "Student")
            {
                TempData["Error"] = "You do not have permission to access this page.";
                return RedirectToAction("Index", "Home");
            }

            return null;
        }

        private STUDENT GetCurrentStudent(LanguageCenterDataContext db)
        {
            int accountId;
            if (Session["AccountID"] == null || !int.TryParse(Session["AccountID"].ToString(), out accountId))
            {
                return null;
            }

            return db.STUDENTs.FirstOrDefault(s => s.AccountID == accountId);
        }

        private string GetIpAddress()
        {
            var ipAddress = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (!string.IsNullOrWhiteSpace(ipAddress))
            {
                ipAddress = ipAddress.Split(',')[0].Trim();
                return ipAddress == "::1" ? "127.0.0.1" : ipAddress;
            }

            ipAddress = Request.UserHostAddress;
            return ipAddress == "::1" ? "127.0.0.1" : ipAddress;
        }

        private static VnPayConfig GetVnPayConfig()
        {
            return new VnPayConfig
            {
                Url = (ConfigurationManager.AppSettings["vnp_Url"] ?? string.Empty).Trim(),
                TmnCode = (ConfigurationManager.AppSettings["vnp_TmnCode"] ?? string.Empty).Trim(),
                HashSecret = (ConfigurationManager.AppSettings["vnp_HashSecret"] ?? string.Empty).Trim(),
                ReturnUrl = (ConfigurationManager.AppSettings["vnp_ReturnUrl"] ?? string.Empty).Trim()
            };
        }

        private class VnPayConfig
        {
            public string Url { get; set; }
            public string TmnCode { get; set; }
            public string HashSecret { get; set; }
            public string ReturnUrl { get; set; }

            public bool IsValid
            {
                get
                {
                    return !string.IsNullOrWhiteSpace(Url)
                        && !string.IsNullOrWhiteSpace(TmnCode)
                        && !string.IsNullOrWhiteSpace(HashSecret)
                        && TmnCode != "YOUR_SANDBOX_TMN_CODE"
                        && HashSecret != "YOUR_SANDBOX_HASH_SECRET";
                }
            }
        }

        private static bool IsVnPayDebugEnabled()
        {
            bool enabled;
            return bool.TryParse(ConfigurationManager.AppSettings["vnp_EnableDebug"], out enabled) && enabled;
        }

        private static string MaskSecureHash(string paymentUrl)
        {
            if (string.IsNullOrWhiteSpace(paymentUrl))
            {
                return string.Empty;
            }

            var marker = "vnp_SecureHash=";
            var index = paymentUrl.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                return paymentUrl;
            }

            return paymentUrl.Substring(0, index + marker.Length) + "***";
        }
    }
}
