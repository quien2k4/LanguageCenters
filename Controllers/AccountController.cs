using System;
using System.Configuration;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Mvc;
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
                    TempData["Error"] = "Email không tồn tại.";
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

                if (account.PasswordHash != model.Password)
                {
                    TempData["Error"] = "Mật khẩu không đúng.";
                    return View(model);
                }

                Session["AccountID"] = account.AccountID;
                Session["Role"] = account.Role;
                Session["FullName"] = GetFullName(db, account);
                Session["Avatar"] = account.Avatar;

                TempData["Success"] = "Đăng nhập thành công.";

                switch ((account.Role ?? string.Empty).Trim())
                {
                    case "Admin":
                        return RedirectToAction("Dashboard", "Admin");
                    case "Teacher":
                        return RedirectToAction("Dashboard", "Teacher");
                    case "Student":
                        return RedirectToAction("Profile", "Student");
                    default:
                        Session.Clear();
                        TempData["Error"] = "Vai trò tài khoản không hợp lệ.";
                        return RedirectToAction("Login");
                }
            }
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
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ thông tin đăng ký.";
                return View(model);
            }

            if (model.Password != model.ConfirmPassword)
            {
                TempData["Error"] = "Mật khẩu và xác nhận mật khẩu phải giống nhau.";
                return View(model);
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var email = (model.Email ?? string.Empty).Trim();
                var isEmailExists = db.USER_ACCOUNTs.Any(x => x.Email == email);

                if (isEmailExists)
                {
                    TempData["Error"] = "Email đã tồn tại.";
                    return View(model);
                }

                var account = new USER_ACCOUNT
                {
                    Email = email,
                    PasswordHash = model.Password,
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
            if (model.NewPassword != model.ConfirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "Xác nhận mật khẩu không khớp.");
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

                account.PasswordHash = model.NewPassword;
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
        [Required]
        [Display(Name = "Họ tên")]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; }

        [Required]
        [Display(Name = "Xác nhận mật khẩu")]
        public string ConfirmPassword { get; set; }

        [Required]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; }
    }
}
