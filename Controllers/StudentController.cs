using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using LanguageCenter.Models;

namespace LanguageCenter.Controllers
{
    public class StudentController : Controller
    {
        private readonly string connectionString =
            ConfigurationManager.ConnectionStrings["LanguageCenterConnectionString"].ConnectionString;

        public new ActionResult Profile()
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

            ViewBag.FullName = Session["FullName"] != null ? Session["FullName"].ToString() : string.Empty;
            ViewBag.Role = Session["Role"].ToString();
            ViewBag.AccountID = Session["AccountID"].ToString();

            return View();
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

            TempData["Success"] = "Register class successfully. Payment status is Unpaid.";
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
                    Classes = classes
                };

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
                    .OrderByDescending(x => x.ConsultationID)
                    .Select(x => new StudentConsultationViewModel
                    {
                        ConsultationID = x.ConsultationID,
                        GuestName = x.GuestName,
                        ContactInformation = x.ContactInformation,
                        QuestionContent = x.QuestionContent,
                        RequestStatus = x.RequestStatus
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
        public ActionResult CreateConsultation()
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

                var model = new CreateConsultationViewModel
                {
                    ContactInformation = defaultContact
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

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please enter contact information and question content.";
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

                var consultation = new CONSULTATION
                {
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

        private USER_ACCOUNT GetCurrentAccount(LanguageCenterDataContext db)
        {
            int accountId;
            if (Session["AccountID"] == null || !int.TryParse(Session["AccountID"].ToString(), out accountId))
            {
                return null;
            }

            return db.USER_ACCOUNTs.FirstOrDefault(a => a.AccountID == accountId);
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
                return "No schedule";
            }

            var items = schedules[classId].Select(x =>
                string.Format("{0} {1:hh\\:mm} - {2:hh\\:mm}", x.DayOfWeek, x.StartTime, x.EndTime));

            return string.Join(", ", items);
        }

        private static string BuildRoomText(Dictionary<int, List<CLASS_SCHEDULE>> schedules, int classId)
        {
            if (!schedules.ContainsKey(classId) || !schedules[classId].Any())
            {
                return "No room";
            }

            var rooms = schedules[classId]
                .Where(x => !string.IsNullOrWhiteSpace(x.Room))
                .Select(x => x.Room)
                .Distinct()
                .ToList();

            return rooms.Any() ? string.Join(", ", rooms) : "No room";
        }
    }
}
