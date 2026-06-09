using System;
using System.Configuration;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Mvc;
using LanguageCenter.Helpers;
using LanguageCenter.Models;

namespace LanguageCenter.Controllers
{
    public class AccountController : Controller
    {
        private readonly string connectionString =
            ConfigurationManager.ConnectionStrings["LanguageCenterConnectionString"].ConnectionString;

        [HttpGet]
        public ActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ thông tin đăng nhập.";
                return View(model);
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var email = (model.Email ?? string.Empty).Trim();
                var account = db.USER_ACCOUNTs.FirstOrDefault(x => x.Email == email);

                if (account == null)
                {
                    TempData["Error"] = "Email hoặc mật khẩu không đúng.";
                    return View(model);
                }

                if (account.IsActive != true)
                {
                    TempData["Error"] = "Tài khoản chưa được kích hoạt.";
                    return View(model);
                }

                if (account.IsLockedOut == true)
                {
                    TempData["Error"] = "Tài khoản đã bị khóa.";
                    return View(model);
                }

                if (!PasswordHelper.VerifyPassword(model.Password, account.PasswordHash))
                {
                    var failedAttempts = (account.FailedLoginAttempts ?? 0) + 1;
                    account.FailedLoginAttempts = failedAttempts;

                    if (failedAttempts >= 5)
                    {
                        account.IsLockedOut = true;
                        db.SubmitChanges();
                        TempData["Error"] = "Tài khoản đã bị khóa do nhập sai mật khẩu quá nhiều lần.";
                        return View(model);
                    }

                    db.SubmitChanges();
                    TempData["Error"] = "Email hoặc mật khẩu không đúng. Số lần thử còn lại: " + (5 - failedAttempts);
                    return View(model);
                }

                var role = (account.Role ?? string.Empty).Trim();
                if (role != "Admin" && role != "Teacher" && role != "Student")
                {
                    Session.Clear();
                    TempData["Error"] = "Vai trò tài khoản không hợp lệ.";
                    return RedirectToAction("Login");
                }

                account.FailedLoginAttempts = 0;
                if (!PasswordHelper.IsHashedPassword(account.PasswordHash))
                {
                    account.PasswordHash = PasswordHelper.HashPassword(model.Password);
                }
                db.SubmitChanges();

                if (role == "Admin")
                {
                    SetLoginSession(db, account);
                    TempData["Success"] = "Đăng nhập thành công.";
                    return RedirectToAction("Index", "Home");
                }

                var otpCode = OtpHelper.GenerateOtpCode();
                var emailBody = "Xin chào " + account.Email + ",\n"
                    + "Mã xác minh đăng nhập của bạn là: " + otpCode + "\n"
                    + "Mã này có hiệu lực trong 5 phút.\n"
                    + "Nếu bạn không thực hiện đăng nhập, vui lòng bỏ qua email.";

                string emailError;
                if (!EmailHelper.SendOtpEmail(account.Email, "[LanguageCenter] Mã xác minh đăng nhập", emailBody, out emailError))
                {
                    TempData["Error"] = "Không gửi được mã xác minh. Vui lòng thử lại sau.";
                    return View(model);
                }

                ClearPendingLoginOtp();
                Session["PendingLoginAccountID"] = account.AccountID;
                Session["PendingLoginEmail"] = account.Email;
                Session["PendingLoginRole"] = role;
                Session["PendingLoginAvatar"] = account.Avatar;
                Session["PendingLoginFullName"] = GetFullName(db, account);
                Session["LoginOtpCode"] = otpCode;
                Session["LoginOtpExpireAt"] = OtpHelper.GetExpireTime();
                Session["LoginOtpLastSentAt"] = DateTime.Now;

                TempData["Success"] = "Mã xác minh đã được gửi đến email của bạn.";
                return RedirectToAction("VerifyLoginOtp", "Account");
            }
        }

        [HttpGet]
        public ActionResult VerifyLoginOtp()
        {
            if (Session["PendingLoginAccountID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Email = Session["PendingLoginEmail"];
            return View(new VerifyOtpViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult VerifyLoginOtp(VerifyOtpViewModel model)
        {
            if (Session["PendingLoginAccountID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Email = Session["PendingLoginEmail"];

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var fixedTestEnabled = IsFixedTestOtpEnabled();
            var fixedTestCode = GetFixedTestOtpCode();
            var enteredOtp = (model.OtpCode ?? string.Empty).Trim();

            if (fixedTestEnabled && !string.IsNullOrWhiteSpace(fixedTestCode) && enteredOtp == fixedTestCode)
            {
                SetPendingLoginSessionAsReal();
                ClearPendingLoginOtp();
                TempData["Success"] = "Đăng nhập thành công.";
                return RedirectToAction("Index", "Home");
            }

            var expireAt = Session["LoginOtpExpireAt"] as DateTime?;
            if (!expireAt.HasValue || OtpHelper.IsOtpExpired(expireAt.Value))
            {
                ModelState.AddModelError("OtpCode", "Mã xác minh đã hết hạn.");
                return View(model);
            }

            var otpCode = Session["LoginOtpCode"] == null ? string.Empty : Session["LoginOtpCode"].ToString();
            if (enteredOtp != otpCode)
            {
                ModelState.AddModelError("OtpCode", "Mã xác minh không đúng.");
                return View(model);
            }

            SetPendingLoginSessionAsReal();
            ClearPendingLoginOtp();
            TempData["Success"] = "Đăng nhập thành công.";
            return RedirectToAction("Index", "Home");
        }

        public ActionResult ResendLoginOtp()
        {
            if (Session["PendingLoginAccountID"] == null || Session["PendingLoginEmail"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!CanResendOtp("LoginOtpLastSentAt"))
            {
                TempData["Error"] = "Vui lòng chờ 60 giây trước khi gửi lại mã.";
                return RedirectToAction("VerifyLoginOtp", "Account");
            }

            var email = Session["PendingLoginEmail"].ToString();
            var otpCode = OtpHelper.GenerateOtpCode();
            var emailBody = "Xin chào " + email + ",\n"
                + "Mã xác minh đăng nhập của bạn là: " + otpCode + "\n"
                + "Mã này có hiệu lực trong 5 phút.\n"
                + "Nếu bạn không thực hiện đăng nhập, vui lòng bỏ qua email.";

            string emailError;
            if (!EmailHelper.SendOtpEmail(email, "[LanguageCenter] Mã xác minh đăng nhập", emailBody, out emailError))
            {
                TempData["Error"] = "Không gửi được mã xác minh. Vui lòng thử lại sau.";
                return RedirectToAction("VerifyLoginOtp", "Account");
            }

            Session["LoginOtpCode"] = otpCode;
            Session["LoginOtpExpireAt"] = OtpHelper.GetExpireTime();
            Session["LoginOtpLastSentAt"] = DateTime.Now;
            TempData["Success"] = "Mã xác minh mới đã được gửi.";
            return RedirectToAction("VerifyLoginOtp", "Account");
        }

        [HttpGet]
        public ActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            string passwordError;
            if (!string.IsNullOrEmpty(model.Password)
                && !PasswordValidationHelper.IsValidPassword(model.Password, out passwordError))
            {
                ModelState.AddModelError("Password", passwordError);
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ thông tin đăng ký.";
                return View(model);
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var email = (model.Email ?? string.Empty).Trim();
                var isEmailExists = db.USER_ACCOUNTs.Any(x => x.Email == email);

                if (isEmailExists)
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng.");
                    TempData["Error"] = "Email này đã được sử dụng.";
                    return View(model);
                }
            }

            var otpCode = OtpHelper.GenerateOtpCode();
            var registerEmailBody = "Xin chào,\n"
                + "Mã xác minh đăng ký tài khoản của bạn là: " + otpCode + "\n"
                + "Mã này có hiệu lực trong 5 phút.\n"
                + "Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email.";

            string emailError;
            if (!EmailHelper.SendOtpEmail(
                (model.Email ?? string.Empty).Trim(),
                "[LanguageCenter] Mã xác minh đăng ký tài khoản",
                registerEmailBody,
                out emailError))
            {
                TempData["Error"] = "Không gửi được mã xác minh. Vui lòng thử lại sau.";
                return View(model);
            }

            ClearPendingRegisterOtp();
            Session["PendingRegisterModel"] = model;
            Session["RegisterOtpCode"] = otpCode;
            Session["RegisterOtpExpireAt"] = OtpHelper.GetExpireTime();
            Session["RegisterOtpLastSentAt"] = DateTime.Now;

            TempData["Success"] = "Mã xác minh đã được gửi đến email của bạn.";
            return RedirectToAction("VerifyRegisterOtp", "Account");
        }

        [HttpGet]
        public ActionResult VerifyRegisterOtp()
        {
            var pendingModel = Session["PendingRegisterModel"] as RegisterViewModel;
            if (pendingModel == null)
            {
                return RedirectToAction("Register", "Account");
            }

            ViewBag.Email = pendingModel.Email;
            return View(new VerifyOtpViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult VerifyRegisterOtp(VerifyOtpViewModel model)
        {
            var pendingModel = Session["PendingRegisterModel"] as RegisterViewModel;
            if (pendingModel == null)
            {
                return RedirectToAction("Register", "Account");
            }

            ViewBag.Email = pendingModel.Email;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var expireAt = Session["RegisterOtpExpireAt"] as DateTime?;
            if (!expireAt.HasValue || OtpHelper.IsOtpExpired(expireAt.Value))
            {
                ModelState.AddModelError("OtpCode", "Mã xác minh đã hết hạn.");
                return View(model);
            }

            var otpCode = Session["RegisterOtpCode"] == null ? string.Empty : Session["RegisterOtpCode"].ToString();
            if (model.OtpCode != otpCode)
            {
                ModelState.AddModelError("OtpCode", "Mã xác minh không đúng.");
                return View(model);
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var email = (pendingModel.Email ?? string.Empty).Trim();
                if (db.USER_ACCOUNTs.Any(x => x.Email == email))
                {
                    ClearPendingRegisterOtp();
                    TempData["Error"] = "Email này đã được sử dụng.";
                    return RedirectToAction("Register", "Account");
                }

                var account = new USER_ACCOUNT
                {
                    Email = email,
                    PasswordHash = PasswordHelper.HashPassword(pendingModel.Password),
                    Role = "Student",
                    Avatar = null,
                    IsActive = true,
                    FailedLoginAttempts = 0,
                    IsLockedOut = false
                };

                var student = new STUDENT
                {
                    FullName = (pendingModel.FullName ?? string.Empty).Trim(),
                    DateOfBirth = null,
                    PhoneNumber = (pendingModel.PhoneNumber ?? string.Empty).Trim(),
                    USER_ACCOUNT = account
                };

                db.USER_ACCOUNTs.InsertOnSubmit(account);
                db.STUDENTs.InsertOnSubmit(student);
                db.SubmitChanges();
            }

            ClearPendingRegisterOtp();
            TempData["Success"] = "Đăng ký thành công. Vui lòng đăng nhập.";
            return RedirectToAction("Login", "Account");
        }

        public ActionResult ResendRegisterOtp()
        {
            var pendingModel = Session["PendingRegisterModel"] as RegisterViewModel;
            if (pendingModel == null)
            {
                return RedirectToAction("Register", "Account");
            }

            if (!CanResendOtp("RegisterOtpLastSentAt"))
            {
                TempData["Error"] = "Vui lòng chờ 60 giây trước khi gửi lại mã.";
                return RedirectToAction("VerifyRegisterOtp", "Account");
            }

            var otpCode = OtpHelper.GenerateOtpCode();
            var registerEmailBody = "Xin chào,\n"
                + "Mã xác minh đăng ký tài khoản của bạn là: " + otpCode + "\n"
                + "Mã này có hiệu lực trong 5 phút.\n"
                + "Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email.";

            string emailError;
            if (!EmailHelper.SendOtpEmail(
                (pendingModel.Email ?? string.Empty).Trim(),
                "[LanguageCenter] Mã xác minh đăng ký tài khoản",
                registerEmailBody,
                out emailError))
            {
                TempData["Error"] = "Không gửi được mã xác minh. Vui lòng thử lại sau.";
                return RedirectToAction("VerifyRegisterOtp", "Account");
            }

            Session["RegisterOtpCode"] = otpCode;
            Session["RegisterOtpExpireAt"] = OtpHelper.GetExpireTime();
            Session["RegisterOtpLastSentAt"] = DateTime.Now;
            TempData["Success"] = "Mã xác minh mới đã được gửi.";
            return RedirectToAction("VerifyRegisterOtp", "Account");
        }

        [HttpGet]
        public ActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Vui lòng nhập email tài khoản.";
                return View(model);
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                string errorMessage;
                var email = (model.Email ?? string.Empty).Trim();
                var account = GetPasswordResetAccount(db, email, out errorMessage);

                if (account == null)
                {
                    TempData["Error"] = errorMessage;
                    return View(model);
                }

                return RedirectToAction("ResetPassword", "Account", new { email = account.Email });
            }
        }

        [HttpGet]
        public ActionResult ResetPassword(string email)
        {
            using (var db = new LanguageCenterDataContext(connectionString))
            {
                string errorMessage;
                var account = GetPasswordResetAccount(db, email, out errorMessage);

                if (account == null)
                {
                    TempData["Error"] = errorMessage;
                    return RedirectToAction("ForgotPassword", "Account");
                }

                return View(new ResetPasswordViewModel { Email = account.Email });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(ResetPasswordViewModel model)
        {
            string passwordError;
            if (!string.IsNullOrEmpty(model.NewPassword)
                && !PasswordValidationHelper.IsValidPassword(model.NewPassword, out passwordError))
            {
                ModelState.AddModelError("NewPassword", passwordError);
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                string errorMessage;
                var account = GetPasswordResetAccount(db, model.Email, out errorMessage);

                if (account == null)
                {
                    TempData["Error"] = errorMessage;
                    return RedirectToAction("ForgotPassword", "Account");
                }

                if (!ModelState.IsValid)
                {
                    TempData["Error"] = "Vui lòng kiểm tra thông tin đổi mật khẩu.";
                    model.Email = account.Email;
                    return View(model);
                }

                account.PasswordHash = PasswordHelper.HashPassword(model.NewPassword);
                db.SubmitChanges();
            }

            TempData["Success"] = "Password reset successfully. Please login again.";
            return RedirectToAction("Login", "Account");
        }

        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        private static void SetLoginSession(LanguageCenterDataContext db, USER_ACCOUNT account)
        {
            System.Web.HttpContext.Current.Session["AccountID"] = account.AccountID;
            System.Web.HttpContext.Current.Session["Email"] = account.Email;
            System.Web.HttpContext.Current.Session["Role"] = account.Role;
            System.Web.HttpContext.Current.Session["FullName"] = GetFullName(db, account);
            System.Web.HttpContext.Current.Session["Avatar"] = account.Avatar;
        }

        private void ClearPendingLoginOtp()
        {
            Session.Remove("PendingLoginAccountID");
            Session.Remove("PendingLoginEmail");
            Session.Remove("PendingLoginRole");
            Session.Remove("PendingLoginAvatar");
            Session.Remove("PendingLoginFullName");
            Session.Remove("LoginOtpCode");
            Session.Remove("LoginOtpExpireAt");
            Session.Remove("LoginOtpLastSentAt");
        }

        private void ClearPendingRegisterOtp()
        {
            Session.Remove("PendingRegisterModel");
            Session.Remove("RegisterOtpCode");
            Session.Remove("RegisterOtpExpireAt");
            Session.Remove("RegisterOtpLastSentAt");
        }

        private bool CanResendOtp(string sessionKey)
        {
            var lastSentAt = Session[sessionKey] as DateTime?;
            return !lastSentAt.HasValue || DateTime.Now.Subtract(lastSentAt.Value).TotalSeconds >= 60;
        }

        private void SetPendingLoginSessionAsReal()
        {
            Session["AccountID"] = Session["PendingLoginAccountID"];
            Session["Email"] = Session["PendingLoginEmail"];
            Session["Role"] = Session["PendingLoginRole"];
            Session["Avatar"] = Session["PendingLoginAvatar"];
            Session["FullName"] = Session["PendingLoginFullName"];
        }

        private static bool IsFixedTestOtpEnabled()
        {
            var value = (ConfigurationManager.AppSettings["otp_EnableFixedTestCode"] ?? string.Empty).Trim();
            bool enabled;
            return bool.TryParse(value, out enabled) && enabled;
        }

        private static string GetFixedTestOtpCode()
        {
            return (ConfigurationManager.AppSettings["otp_FixedTestCode"] ?? string.Empty).Trim();
        }

        private void GenerateRegisterCaptcha()
        {
            var random = new Random();
            var captcha = random.Next(1000, 10000).ToString();
            Session["RegisterCaptcha"] = captcha;
            ViewBag.RegisterCaptcha = captcha;
        }

        private static USER_ACCOUNT GetPasswordResetAccount(LanguageCenterDataContext db, string email, out string errorMessage)
        {
            errorMessage = string.Empty;
            email = (email ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(email))
            {
                errorMessage = "Vui lòng nhập email tài khoản.";
                return null;
            }

            var account = db.USER_ACCOUNTs.FirstOrDefault(x => x.Email == email);
            if (account == null)
            {
                errorMessage = "Email không tồn tại.";
                return null;
            }

            var role = (account.Role ?? string.Empty).Trim();
            if (role == "Admin")
            {
                errorMessage = "Admin account cannot reset password here. Please contact system owner.";
                return null;
            }

            if (role != "Student" && role != "Teacher")
            {
                errorMessage = "Tài khoản này không được phép đặt lại mật khẩu tại đây.";
                return null;
            }

            if (account.IsActive != true)
            {
                errorMessage = "Tài khoản chưa được kích hoạt.";
                return null;
            }

            if (account.IsLockedOut == true)
            {
                errorMessage = "Tài khoản đã bị khóa.";
                return null;
            }

            return account;
        }

        private static string GetFullName(LanguageCenterDataContext db, USER_ACCOUNT account)
        {
            if (account == null)
            {
                return string.Empty;
            }

            var role = (account.Role ?? string.Empty).Trim();

            if (role == "Student")
            {
                var student = db.STUDENTs.FirstOrDefault(x => x.AccountID == account.AccountID);
                return student != null ? student.FullName : account.Email;
            }

            if (role == "Teacher")
            {
                var teacher = db.TEACHERs.FirstOrDefault(x => x.AccountID == account.AccountID);
                return teacher != null ? teacher.FullName : account.Email;
            }

            return account.Email;
        }
    }

    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; }

        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Họ tên không được để trống.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên phải từ 2 đến 100 ký tự.")]
        [RegularExpression(@"^(?!\s*$).+", ErrorMessage = "Họ tên không được để trống.")]
        [Display(Name = "Họ tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống.")]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Xác nhận mật khẩu không được để trống.")]
        [System.ComponentModel.DataAnnotations.Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        [Display(Name = "Xác nhận mật khẩu")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được để trống.")]
        [RegularExpression(@"^(0|\+84)(3|5|7|8|9)\d{8}$", ErrorMessage = "Số điện thoại không hợp lệ.")]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Mã xác nhận")]
        public string CaptchaCode { get; set; }
    }
}





