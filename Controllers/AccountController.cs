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
                TempData["Error"] = "Vui long nhap day du thong tin dang nhap.";
                return View(model);
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var email = (model.Email ?? string.Empty).Trim();
                var account = db.USER_ACCOUNTs.FirstOrDefault(x => x.Email == email);

                if (account == null)
                {
                    TempData["Error"] = "Email khong ton tai.";
                    return View(model);
                }

                if (account.IsActive != true)
                {
                    TempData["Error"] = "Tai khoan chua duoc kich hoat.";
                    return View(model);
                }

                if (account.IsLockedOut == true)
                {
                    TempData["Error"] = "Tai khoan da bi khoa.";
                    return View(model);
                }

                if (account.PasswordHash != model.Password)
                {
                    TempData["Error"] = "Mat khau khong dung.";
                    return View(model);
                }

                Session["AccountID"] = account.AccountID;
                Session["Role"] = account.Role;
                Session["FullName"] = GetFullName(db, account);
                Session["Avatar"] = account.Avatar;

                TempData["Success"] = "Dang nhap thanh cong.";

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
                        TempData["Error"] = "Role tai khoan khong hop le.";
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
                TempData["Error"] = "Vui long nhap day du thong tin dang ky.";
                return View(model);
            }

            if (model.Password != model.ConfirmPassword)
            {
                TempData["Error"] = "Password va ConfirmPassword phai giong nhau.";
                return View(model);
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var email = (model.Email ?? string.Empty).Trim();
                var isEmailExists = db.USER_ACCOUNTs.Any(x => x.Email == email);

                if (isEmailExists)
                {
                    TempData["Error"] = "Email da ton tai.";
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

            TempData["Success"] = "Dang ky thanh cong. Vui long dang nhap.";
            return RedirectToAction("Login");
        }

        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Index", "Home");
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
        public string Password { get; set; }

        [Display(Name = "Remember Me")]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; }

        [Required]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }
    }
}
