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

                account.FailedLoginAttempts = 0;
                if (!PasswordHelper.IsHashedPassword(account.PasswordHash))
                {
                    account.PasswordHash = PasswordHelper.HashPassword(model.Password);
                }
                db.SubmitChanges();

                Session["AccountID"] = account.AccountID;
                Session["Email"] = account.Email;
                Session["Role"] = account.Role;
                Session["FullName"] = GetFullName(db, account);
                Session["Avatar"] = account.Avatar;

                TempData["Success"] = "Đăng nhập thành công.";

                var role = (account.Role ?? string.Empty).Trim();
                if (role != "Admin" && role != "Teacher" && role != "Student")
                {
                    Session.Clear();
                    TempData["Error"] = "Vai trò tài khoản không hợp lệ.";
                    return RedirectToAction("Login");
                }

                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public ActionResult Register()
        {
            GenerateRegisterCaptcha();
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

            var currentCaptcha = Session["RegisterCaptcha"] == null
                ? string.Empty
                : Session["RegisterCaptcha"].ToString();

            if (string.IsNullOrWhiteSpace(model.CaptchaCode))
            {
                ModelState.AddModelError("CaptchaCode", "Vui lòng nhập mã xác nhận.");
            }
            else if (model.CaptchaCode.Trim() != currentCaptcha)
            {
                ModelState.AddModelError("CaptchaCode", "Mã xác nhận không đúng.");
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ thông tin đăng ký.";
                GenerateRegisterCaptcha();
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
                    GenerateRegisterCaptcha();
                    return View(model);
                }

                var account = new USER_ACCOUNT
                {
                    Email = email,
                    PasswordHash = PasswordHelper.HashPassword(model.Password),
                    Role = "Student",
                    Avatar = null,
                    IsActive = true,
                    FailedLoginAttempts = 0,
                    IsLockedOut = false
                };

                var student = new STUDENT
                {
                    FullName = (model.FullName ?? string.Empty).Trim(),
                    DateOfBirth = null,
                    PhoneNumber = (model.PhoneNumber ?? string.Empty).Trim(),
                    USER_ACCOUNT = account
                };

                db.USER_ACCOUNTs.InsertOnSubmit(account);
                db.STUDENTs.InsertOnSubmit(student);
                db.SubmitChanges();
            }

            Session.Remove("RegisterCaptcha");
            TempData["Success"] = "Đăng ký thành công. Vui lòng đăng nhập.";
            return RedirectToAction("Login");
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





