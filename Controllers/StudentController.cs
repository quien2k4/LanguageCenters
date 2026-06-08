using System;
using System.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LanguageCenter.Helpers;
using LanguageCenter.Models;

namespace LanguageCenter.Controllers
{
    public class StudentController : Controller
    {
        private readonly string connectionString =
            ConfigurationManager.ConnectionStrings["LanguageCenterConnectionString"].ConnectionString;

        public new ActionResult Profile()
        {
            var authResult = CheckStudentPermission();
            if (authResult != null)
            {
                return authResult;
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var student = GetCurrentStudent(db);
                var account = GetCurrentAccount(db);

                if (student == null || account == null)
                {
                    TempData["Error"] = "Student profile not found.";
                    return RedirectToAction("Index", "Home");
                }

                var model = new StudentProfileViewModel
                {
                    AccountID = account.AccountID,
                    StudentID = student.StudentID,
                    Email = account.Email,
                    FullName = student.FullName,
                    DateOfBirth = student.DateOfBirth,
                    PhoneNumber = student.PhoneNumber,
                    Avatar = account.Avatar
                };

                return View(model);
            }
        }

        [HttpGet]
        public ActionResult EditProfile()
        {
            var authResult = CheckStudentPermission();
            if (authResult != null)
            {
                return authResult;
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var student = GetCurrentStudent(db);
                var account = GetCurrentAccount(db);

                if (student == null || account == null)
                {
                    TempData["Error"] = "Student profile not found.";
                    return RedirectToAction("Index", "Home");
                }

                var model = new EditStudentProfileViewModel
                {
                    StudentID = student.StudentID,
                    Email = account.Email,
                    FullName = student.FullName,
                    DateOfBirth = student.DateOfBirth,
                    PhoneNumber = student.PhoneNumber,
                    Avatar = account.Avatar
                };

                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditProfile(EditStudentProfileViewModel model, HttpPostedFileBase avatarFile)
        {
            var authResult = CheckStudentPermission();
            if (authResult != null)
            {
                return authResult;
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var student = GetCurrentStudent(db);
                var account = GetCurrentAccount(db);

                if (student == null || account == null)
                {
                    TempData["Error"] = "Student profile not found.";
                    return RedirectToAction("Index", "Home");
                }

                model.StudentID = student.StudentID;
                model.Email = account.Email;
                model.Avatar = account.Avatar;

                if (!ModelState.IsValid)
                {
                    TempData["Error"] = "Vui lòng kiểm tra thông tin hồ sơ.";
                    return View(model);
                }

                var avatarUrl = SaveAvatarImage(avatarFile);
                if (avatarFile != null && avatarFile.ContentLength > 0 && avatarUrl == null)
                {
                    TempData["Error"] = "Ảnh đại diện không hợp lệ. Vui lòng chọn file jpg, jpeg, png, gif hoặc webp.";
                    return View(model);
                }

                student.FullName = (model.FullName ?? string.Empty).Trim();
                student.DateOfBirth = model.DateOfBirth;
                student.PhoneNumber = (model.PhoneNumber ?? string.Empty).Trim();

                if (!string.IsNullOrWhiteSpace(avatarUrl))
                {
                    account.Avatar = avatarUrl;
                }

                db.SubmitChanges();

                Session["FullName"] = student.FullName;
                Session["Avatar"] = account.Avatar;
            }

            TempData["Success"] = "Cập nhật hồ sơ thành công.";
            return RedirectToAction("Profile", "Student");
        }

        [HttpGet]
        public ActionResult ChangePassword()
        {
            var authResult = CheckStudentPermission();
            if (authResult != null)
            {
                return authResult;
            }

            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ChangePasswordViewModel model)
        {
            var authResult = CheckStudentPermission();
            if (authResult != null)
            {
                return authResult;
            }

            string passwordError;
            if (!string.IsNullOrEmpty(model.NewPassword)
                && !PasswordValidationHelper.IsValidPassword(model.NewPassword, out passwordError))
            {
                ModelState.AddModelError("NewPassword", passwordError);
            }

            if (!string.IsNullOrWhiteSpace(model.CurrentPassword)
                && model.NewPassword == model.CurrentPassword)
            {
                ModelState.AddModelError("NewPassword", "Mật khẩu mới không được trùng với mật khẩu hiện tại.");
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Vui lòng kiểm tra thông tin đổi mật khẩu.";
                return View(model);
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var account = GetCurrentAccount(db);
                if (account == null)
                {
                    TempData["Error"] = "Student account not found.";
                    return RedirectToAction("Index", "Home");
                }

                if (!PasswordHelper.VerifyPassword(model.CurrentPassword, account.PasswordHash))
                {
                    TempData["Error"] = "Mật khẩu hiện tại không đúng.";
                    return View(model);
                }

                account.PasswordHash = PasswordHelper.HashPassword(model.NewPassword);
                db.SubmitChanges();
            }

            TempData["Success"] = "Đổi mật khẩu thành công.";
            return RedirectToAction("Profile", "Student");
        }

        [HttpGet]
        public ActionResult RegisterClass(int classId)
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

                var model = GetClassInfo(db, classId);
                if (model == null)
                {
                    TempData["Error"] = "Class not found.";
                    return RedirectToAction("Index", "Program");
                }

                model.IsAlreadyRegistered = db.REGISTRATIONs.Any(r =>
                    r.StudentID == student.StudentID && r.ClassID == classId);

                if (model.IsAlreadyRegistered)
                {
                    TempData["Error"] = "You already registered this class.";
                }

                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RegisterClassConfirm(int classId)
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

                var classInfo = (
                    from c in db.CLASSes
                    join p in db.PROGRAMs on c.ProgramID equals p.ProgramID
                    where c.ClassID == classId
                    select new
                    {
                        c.ClassID,
                        ProgramFee = p.Fee
                    })
                    .FirstOrDefault();

                if (classInfo == null)
                {
                    TempData["Error"] = "Class not found.";
                    return RedirectToAction("Index", "Program");
                }

                var isAlreadyRegistered = db.REGISTRATIONs.Any(r =>
                    r.StudentID == student.StudentID && r.ClassID == classId);

                if (isAlreadyRegistered)
                {
                    TempData["Error"] = "You already registered this class.";
                    return RedirectToAction("MyClasses", "Student");
                }

                var registration = new REGISTRATION
                {
                    StudentID = student.StudentID,
                    ClassID = classId,
                    RegistrationDate = DateTime.Now,
                    RegStatus = "Pending"
                };

                var payment = new PAYMENT
                {
                    REGISTRATION = registration,
                    Amount = classInfo.ProgramFee,
                    PaymentDate = DateTime.Now,
                    Method = null,
                    PaymentStatus = "Unpaid"
                };

                db.REGISTRATIONs.InsertOnSubmit(registration);
                db.PAYMENTs.InsertOnSubmit(payment);
                db.SubmitChanges();
            }

            TempData["Success"] = "Đăng ký lớp thành công. Trạng thái đăng ký là Chờ mở lớp và thanh toán là Chưa thanh toán.";
            return RedirectToAction("MyClasses", "Student");
        }

        public ActionResult MyClasses()
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

                var classes = (
                    from r in db.REGISTRATIONs
                    join c in db.CLASSes on r.ClassID equals c.ClassID
                    join p in db.PROGRAMs on c.ProgramID equals p.ProgramID into programJoin
                    from p in programJoin.DefaultIfEmpty()
                    join t in db.TEACHERs on c.TeacherID equals t.TeacherID into teacherJoin
                    from t in teacherJoin.DefaultIfEmpty()
                    join s in db.CLASS_STATUS on c.StatusID equals s.StatusID into statusJoin
                    from s in statusJoin.DefaultIfEmpty()
                    join pay in db.PAYMENTs on r.RegistrationID equals pay.RegistrationID into paymentJoin
                    from pay in paymentJoin.DefaultIfEmpty()
                    where r.StudentID == student.StudentID
                    orderby r.RegistrationDate descending
                    select new StudentMyClassViewModel
                    {
                        RegistrationID = r.RegistrationID,
                        ClassID = c.ClassID,
                        ClassName = c.ClassName,
                        ProgramName = p != null ? p.ProgramName : string.Empty,
                        TeacherName = t != null ? t.FullName : string.Empty,
                        ClassStatus = s != null ? s.StatusName : string.Empty,
                        StartDate = c.StartDate,
                        RegistrationDate = r.RegistrationDate,
                        RegStatus = r.RegStatus,
                        Amount = pay != null ? pay.Amount : 0,
                        PaymentStatus = pay != null ? pay.PaymentStatus : string.Empty,
                        Schedule = string.Empty,
                        Room = string.Empty
                    })
                    .ToList();

                var classIds = (
                    from r in db.REGISTRATIONs
                    where r.StudentID == student.StudentID
                    select r.ClassID)
                    .Distinct()
                    .ToList();

                var schedules = db.CLASS_SCHEDULEs
                    .Where(x => classIds.Contains(x.ClassID))
                    .OrderBy(x => x.ClassID)
                    .ThenBy(x => x.ScheduleID)
                    .ToList()
                    .GroupBy(x => x.ClassID)
                    .ToDictionary(x => x.Key, x => x.ToList());

                foreach (var item in classes)
                {
                    item.Schedule = BuildScheduleText(schedules, item.ClassID);
                    item.Room = BuildRoomText(schedules, item.ClassID);
                }

                var model = new StudentMyClassesViewModel
                {
                    Classes = classes,
                    TotalClasses = classes.Count,
                    OngoingClasses = classes.Count(c => (c.ClassStatus ?? string.Empty).Trim() == "Ongoing"),
                    UpcomingClasses = classes.Count(c => (c.ClassStatus ?? string.Empty).Trim() == "Upcoming"),
                    UnpaidPayments = classes.Count(c => (c.PaymentStatus ?? string.Empty).Trim() == "Unpaid")
                };

                return View(model);
            }
        }

        public ActionResult ClassDetail(int registrationId)
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

                var model = (
                    from r in db.REGISTRATIONs
                    join c in db.CLASSes on r.ClassID equals c.ClassID
                    join p in db.PROGRAMs on c.ProgramID equals p.ProgramID into programJoin
                    from p in programJoin.DefaultIfEmpty()
                    join t in db.TEACHERs on c.TeacherID equals t.TeacherID into teacherJoin
                    from t in teacherJoin.DefaultIfEmpty()
                    join s in db.CLASS_STATUS on c.StatusID equals s.StatusID into statusJoin
                    from s in statusJoin.DefaultIfEmpty()
                    join pay in db.PAYMENTs on r.RegistrationID equals pay.RegistrationID into paymentJoin
                    from pay in paymentJoin.DefaultIfEmpty()
                    where r.RegistrationID == registrationId && r.StudentID == student.StudentID
                    select new StudentClassDetailViewModel
                    {
                        RegistrationID = r.RegistrationID,
                        ClassID = c.ClassID,
                        ClassName = c.ClassName,
                        ProgramName = p != null ? p.ProgramName : string.Empty,
                        TeacherName = t != null ? t.FullName : string.Empty,
                        ClassStatus = s != null ? s.StatusName : string.Empty,
                        StartDate = c.StartDate,
                        RegistrationDate = r.RegistrationDate,
                        RegStatus = r.RegStatus,
                        HasPayment = pay != null,
                        Amount = pay != null ? pay.Amount : 0,
                        PaymentStatus = pay != null ? pay.PaymentStatus : string.Empty,
                        Method = pay != null ? pay.Method : string.Empty,
                        PaymentDate = pay != null ? pay.PaymentDate : null,
                        Schedule = string.Empty,
                        Room = string.Empty
                    })
                    .FirstOrDefault();

                if (model == null)
                {
                    TempData["Error"] = "Bạn không có quyền xem thông tin đăng ký này.";
                    return RedirectToAction("MyClasses", "Student");
                }

                var classIds = new List<int> { model.ClassID };
                var schedules = db.CLASS_SCHEDULEs
                    .Where(x => classIds.Contains(x.ClassID))
                    .OrderBy(x => x.ClassID)
                    .ThenBy(x => x.ScheduleID)
                    .ToList()
                    .GroupBy(x => x.ClassID)
                    .ToDictionary(x => x.Key, x => x.ToList());

                model.Schedule = BuildScheduleText(schedules, model.ClassID);
                model.Room = BuildRoomText(schedules, model.ClassID);

                return View(model);
            }
        }

        public ActionResult Payments()
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

                var payments = (
                    from pay in db.PAYMENTs
                    join r in db.REGISTRATIONs on pay.RegistrationID equals r.RegistrationID
                    join c in db.CLASSes on r.ClassID equals c.ClassID
                    join p in db.PROGRAMs on c.ProgramID equals p.ProgramID into programJoin
                    from p in programJoin.DefaultIfEmpty()
                    where r.StudentID == student.StudentID
                    orderby pay.PaymentDate descending, pay.PaymentID descending
                    select new StudentPaymentViewModel
                    {
                        PaymentID = pay.PaymentID,
                        RegistrationID = pay.RegistrationID,
                        ClassName = c.ClassName,
                        ProgramName = p != null ? p.ProgramName : string.Empty,
                        Amount = pay.Amount,
                        PaymentDate = pay.PaymentDate,
                        Method = pay.Method,
                        PaymentStatus = pay.PaymentStatus,
                        RegistrationStatus = r.RegStatus
                    })
                    .ToList();

                var model = new StudentPaymentsViewModel
                {
                    Payments = payments
                };

                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PayDemo(int paymentId)
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
                    join r in db.REGISTRATIONs on pay.RegistrationID equals r.RegistrationID
                    where pay.PaymentID == paymentId
                    select new
                    {
                        Payment = pay,
                        r.StudentID
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

                paymentInfo.Payment.PaymentStatus = "Paid";
                paymentInfo.Payment.Method = "Demo";
                paymentInfo.Payment.PaymentDate = DateTime.Now;

                db.SubmitChanges();
            }

            TempData["Success"] = "Payment completed successfully.";
            return RedirectToAction("Payments", "Student");
        }

        public ActionResult PlacementTests()
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

                var tests = db.PLACEMENT_TESTs
                    .Where(x => x.StudentID == student.StudentID)
                    .OrderByDescending(x => x.TestDate)
                    .ThenByDescending(x => x.TestTime)
                    .Select(x => new StudentPlacementTestViewModel
                    {
                        TestID = x.TestID,
                        TestDate = x.TestDate,
                        TestTime = x.TestTime,
                        Level = x.Level,
                        ResultScore = x.ResultScore,
                        Status = x.Status
                    })
                    .ToList();

                var model = new StudentPlacementTestsViewModel
                {
                    PlacementTests = tests
                };

                return View(model);
            }
        }

        [HttpGet]
        public ActionResult CreatePlacementTest()
        {
            var authResult = CheckStudentPermission();
            if (authResult != null)
            {
                return authResult;
            }

            return View(new CreatePlacementTestViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreatePlacementTest(CreatePlacementTestViewModel model)
        {
            var authResult = CheckStudentPermission();
            if (authResult != null)
            {
                return authResult;
            }

            TimeSpan testTime;
            if (!TimeSpan.TryParse(model.TestTime, out testTime))
            {
                ModelState.AddModelError("TestTime", "TestTime is invalid.");
            }

            if (model.TestDate.HasValue && model.TestDate.Value.Date < DateTime.Today)
            {
                ModelState.AddModelError("TestDate", "TestDate cannot be earlier than today.");
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please check placement test information.";
                return View(model);
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var student = GetCurrentStudent(db);
                if (student == null)
                {
                    TempData["Error"] = "Student profile not found.";
                    return RedirectToAction("Index", "Home");
                }

                var placementTest = new PLACEMENT_TEST
                {
                    StudentID = student.StudentID,
                    TestDate = model.TestDate.Value.Date,
                    TestTime = testTime,
                    Level = model.Level,
                    ResultScore = null,
                    Status = "Pending"
                };

                db.PLACEMENT_TESTs.InsertOnSubmit(placementTest);
                db.SubmitChanges();
            }

            TempData["Success"] = "Placement test registered successfully.";
            return RedirectToAction("PlacementTests", "Student");
        }

        public ActionResult Consultation()
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

                var account = GetCurrentAccount(db);
                var fullName = student.FullName ?? string.Empty;
                var phoneNumber = student.PhoneNumber ?? string.Empty;
                var email = account != null ? account.Email ?? string.Empty : string.Empty;

                var consultations = db.CONSULTATIONs
                    .Where(x =>
                        x.GuestName == fullName ||
                        x.ContactInformation == phoneNumber ||
                        x.ContactInformation == email)
                    .Select(x => new
                    {
                        Consultation = x,
                        ClassName = x.ClassID.HasValue
                            ? db.CLASSes.Where(c => c.ClassID == x.ClassID.Value).Select(c => c.ClassName).FirstOrDefault()
                            : null,
                        ProgramName = x.ClassID.HasValue
                            ? (from c in db.CLASSes
                               join p in db.PROGRAMs on c.ProgramID equals p.ProgramID
                               where c.ClassID == x.ClassID.Value
                               select p.ProgramName).FirstOrDefault()
                            : null
                    })
                    .OrderByDescending(x => x.Consultation.ConsultationID)
                    .Select(x => new StudentConsultationViewModel
                    {
                        ConsultationID = x.Consultation.ConsultationID,
                        ClassID = x.Consultation.ClassID,
                        ClassName = x.ClassName,
                        ProgramName = x.ProgramName,
                        GuestName = x.Consultation.GuestName,
                        ContactInformation = x.Consultation.ContactInformation,
                        QuestionContent = x.Consultation.QuestionContent,
                        RequestStatus = x.Consultation.RequestStatus
                    })
                    .ToList();

                var model = new StudentConsultationsViewModel
                {
                    Consultations = consultations
                };

                return View(model);
            }
        }

        [HttpGet]
        public ActionResult CreateConsultation(int? classId)
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

                var account = GetCurrentAccount(db);
                var defaultContact = !string.IsNullOrWhiteSpace(student.PhoneNumber)
                    ? student.PhoneNumber
                    : account != null ? account.Email : string.Empty;

                var registeredClasses = GetRegisteredClassSelectList(db, student.StudentID, classId);
                if (!registeredClasses.Any())
                {
                    TempData["Error"] = "Bạn cần đăng ký lớp trước khi gửi yêu cầu tư vấn theo lớp.";
                    return RedirectToAction("Consultation", "Student");
                }

                if (classId.HasValue && !registeredClasses.Any(x => x.Value == classId.Value.ToString()))
                {
                    TempData["Error"] = "Bạn chỉ được gửi tư vấn cho lớp mình đã đăng ký.";
                    return RedirectToAction("MyClasses", "Student");
                }

                var model = new CreateConsultationViewModel
                {
                    ClassID = classId,
                    ContactInformation = defaultContact,
                    Classes = registeredClasses
                };

                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateConsultation(CreateConsultationViewModel model)
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

                model.Classes = GetRegisteredClassSelectList(db, student.StudentID, model.ClassID);

                if (!model.ClassID.HasValue || !IsStudentRegisteredInClass(db, student.StudentID, model.ClassID.Value))
                {
                    ModelState.AddModelError("ClassID", "Bạn chỉ được chọn lớp mình đã đăng ký.");
                }

                if (!ModelState.IsValid)
                {
                    TempData["Error"] = "Please enter class, contact information and question content.";
                    return View(model);
                }

                var consultation = new CONSULTATION
                {
                    ClassID = model.ClassID,
                    GuestName = student.FullName ?? string.Empty,
                    ContactInformation = (model.ContactInformation ?? string.Empty).Trim(),
                    QuestionContent = (model.QuestionContent ?? string.Empty).Trim(),
                    RequestStatus = "Pending"
                };

                db.CONSULTATIONs.InsertOnSubmit(consultation);
                db.SubmitChanges();
            }

            TempData["Success"] = "Consultation request sent successfully.";
            return RedirectToAction("Consultation", "Student");
        }

        private ActionResult CheckStudentPermission()
        {
            return AuthHelper.RequireRole(this, "Student");
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

        private USER_ACCOUNT GetCurrentAccount(LanguageCenterDataContext db)
        {
            int accountId;
            if (Session["AccountID"] == null || !int.TryParse(Session["AccountID"].ToString(), out accountId))
            {
                return null;
            }

            return db.USER_ACCOUNTs.FirstOrDefault(a => a.AccountID == accountId);
        }

        private string SaveAvatarImage(HttpPostedFileBase avatarFile)
        {
            if (avatarFile == null || avatarFile.ContentLength <= 0)
            {
                return null;
            }

            var extension = Path.GetExtension(avatarFile.FileName);
            if (!IsAllowedAvatarExtension(extension))
            {
                return null;
            }

            var uploadFolder = Server.MapPath("~/Content/Uploads/Avatars");
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            var fileName = string.Format("{0}_{1}{2}", DateTime.Now.Ticks, Guid.NewGuid().ToString("N"), extension);
            var physicalPath = Path.Combine(uploadFolder, fileName);
            avatarFile.SaveAs(physicalPath);

            return "/Content/Uploads/Avatars/" + fileName;
        }

        private static bool IsAllowedAvatarExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return false;
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            return allowedExtensions.Contains(extension.ToLower());
        }

        private StudentRegisterClassViewModel GetClassInfo(LanguageCenterDataContext db, int classId)
        {
            return (
                from c in db.CLASSes
                join p in db.PROGRAMs on c.ProgramID equals p.ProgramID into programJoin
                from p in programJoin.DefaultIfEmpty()
                join t in db.TEACHERs on c.TeacherID equals t.TeacherID into teacherJoin
                from t in teacherJoin.DefaultIfEmpty()
                join s in db.CLASS_STATUS on c.StatusID equals s.StatusID into statusJoin
                from s in statusJoin.DefaultIfEmpty()
                where c.ClassID == classId
                select new StudentRegisterClassViewModel
                {
                    ClassID = c.ClassID,
                    ProgramID = p != null ? p.ProgramID : 0,
                    ClassName = c.ClassName,
                    ProgramName = p != null ? p.ProgramName : string.Empty,
                    TeacherName = t != null ? t.FullName : string.Empty,
                    StatusName = s != null ? s.StatusName : string.Empty,
                    StartDate = c.StartDate,
                    Fee = p != null ? p.Fee : 0
                })
                .FirstOrDefault();
        }

        private static string BuildScheduleText(Dictionary<int, List<CLASS_SCHEDULE>> schedules, int classId)
        {
            if (!schedules.ContainsKey(classId) || !schedules[classId].Any())
            {
                return "Chưa có lịch học.";
            }

            var items = schedules[classId].Select(x =>
                string.Format("{0} {1:hh\\:mm} - {2:hh\\:mm}", x.DayOfWeek, x.StartTime, x.EndTime));

            return string.Join(", ", items);
        }

        private static string BuildRoomText(Dictionary<int, List<CLASS_SCHEDULE>> schedules, int classId)
        {
            if (!schedules.ContainsKey(classId) || !schedules[classId].Any())
            {
                return "Chưa có phòng.";
            }

            var rooms = schedules[classId]
                .Where(x => !string.IsNullOrWhiteSpace(x.Room))
                .Select(x => x.Room)
                .Distinct()
                .ToList();

            return rooms.Any() ? string.Join(", ", rooms) : "Chưa có phòng.";
        }

        private static List<SelectListItem> GetRegisteredClassSelectList(LanguageCenterDataContext db, int studentId, int? selectedClassId)
        {
            return (
                from r in db.REGISTRATIONs
                join c in db.CLASSes on r.ClassID equals c.ClassID
                join p in db.PROGRAMs on c.ProgramID equals p.ProgramID into programJoin
                from p in programJoin.DefaultIfEmpty()
                where r.StudentID == studentId
                orderby c.StartDate descending, c.ClassName
                select new
                {
                    c.ClassID,
                    c.ClassName,
                    ProgramName = p != null ? p.ProgramName : string.Empty
                })
                .ToList()
                .Select(x => new SelectListItem
                {
                    Value = x.ClassID.ToString(),
                    Text = string.IsNullOrWhiteSpace(x.ProgramName)
                        ? x.ClassName
                        : x.ClassName + " - " + x.ProgramName,
                    Selected = selectedClassId.HasValue && selectedClassId.Value == x.ClassID
                })
                .ToList();
        }

        private static bool IsStudentRegisteredInClass(LanguageCenterDataContext db, int studentId, int classId)
        {
            return db.REGISTRATIONs.Any(r => r.StudentID == studentId && r.ClassID == classId);
        }
    }
}




