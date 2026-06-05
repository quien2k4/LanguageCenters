using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LanguageCenter.Models;

namespace LanguageCenter.Controllers
{
    public class AdminController : Controller
    {
        private readonly string connectionString =
            ConfigurationManager.ConnectionStrings["LanguageCenterConnectionString"].ConnectionString;

        public ActionResult Dashboard()
        {
            if (Session["AccountID"] == null || Session["Role"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (Session["Role"].ToString() != "Admin")
            {
                TempData["Error"] = "You do not have permission to access this page.";
                return RedirectToAction("Index", "Home");
            }

            int accountId;
            if (!int.TryParse(Session["AccountID"].ToString(), out accountId))
            {
                Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var paidPayments = db.PAYMENTs.Where(p => p.PaymentStatus == "Paid");

                var recentRegistrations = (
                    from r in db.REGISTRATIONs
                    join st in db.STUDENTs on r.StudentID equals st.StudentID
                    join c in db.CLASSes on r.ClassID equals c.ClassID
                    join p in db.PROGRAMs on c.ProgramID equals p.ProgramID
                    orderby r.RegistrationDate descending
                    select new AdminRecentRegistrationViewModel
                    {
                        StudentName = st.FullName,
                        ClassName = c.ClassName,
                        ProgramName = p.ProgramName,
                        RegistrationDate = r.RegistrationDate,
                        RegStatus = r.RegStatus
                    })
                    .Take(5)
                    .ToList();

                var recentPayments = (
                    from pay in db.PAYMENTs
                    join r in db.REGISTRATIONs on pay.RegistrationID equals r.RegistrationID
                    join st in db.STUDENTs on r.StudentID equals st.StudentID
                    join c in db.CLASSes on r.ClassID equals c.ClassID
                    orderby pay.PaymentDate descending, pay.PaymentID descending
                    select new AdminRecentPaymentViewModel
                    {
                        StudentName = st.FullName,
                        ClassName = c.ClassName,
                        Amount = pay.Amount,
                        PaymentStatus = pay.PaymentStatus,
                        PaymentDate = pay.PaymentDate
                    })
                    .Take(5)
                    .ToList();

                var recentConsultations = db.CONSULTATIONs
                    .OrderByDescending(c => c.ConsultationID)
                    .Take(5)
                    .Select(c => new AdminRecentConsultationViewModel
                    {
                        GuestName = c.GuestName,
                        ContactInformation = c.ContactInformation,
                        QuestionContent = c.QuestionContent,
                        RequestStatus = c.RequestStatus
                    })
                    .ToList();

                var model = new AdminDashboardViewModel
                {
                    FullName = Session["FullName"] != null ? Session["FullName"].ToString() : string.Empty,
                    Role = Session["Role"].ToString(),
                    AccountID = accountId,

                    TotalPrograms = db.PROGRAMs.Count(),
                    TotalClasses = db.CLASSes.Count(),
                    TotalStudents = db.STUDENTs.Count(),
                    TotalTeachers = db.TEACHERs.Count(),
                    TotalRegistrations = db.REGISTRATIONs.Count(),
                    TotalPayments = db.PAYMENTs.Count(),
                    TotalPlacementTests = db.PLACEMENT_TESTs.Count(),
                    TotalConsultations = db.CONSULTATIONs.Count(),

                    TotalRevenue = paidPayments.Sum(p => (decimal?)p.Amount) ?? 0,
                    UnpaidPayments = db.PAYMENTs.Count(p => p.PaymentStatus == "Unpaid"),
                    PaidPayments = db.PAYMENTs.Count(p => p.PaymentStatus == "Paid"),

                    RecentRegistrations = recentRegistrations,
                    RecentPayments = recentPayments,
                    RecentConsultations = recentConsultations
                };

                return View(model);
            }
        }

        public ActionResult Programs(string search, string level, string status, int page = 1)
        {
            const int pageSize = 10;

            var authResult = CheckAdminPermission();
            if (authResult != null)
            {
                return authResult;
            }

            if (page < 1)
            {
                page = 1;
            }

            search = (search ?? string.Empty).Trim();
            level = (level ?? string.Empty).Trim();
            status = string.IsNullOrWhiteSpace(status) ? "All" : status.Trim();

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var query = db.PROGRAMs.AsQueryable();

                var levels = db.PROGRAMs
                    .Where(p => p.Level != null && p.Level != string.Empty)
                    .Select(p => p.Level)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(p => p.ProgramName.Contains(search));
                }

                if (!string.IsNullOrWhiteSpace(level))
                {
                    query = query.Where(p => p.Level == level);
                }

                if (status == "Active")
                {
                    query = query.Where(p => p.IsActive == true);
                }
                else if (status == "Inactive")
                {
                    query = query.Where(p => p.IsActive != true);
                }

                var totalItems = query.Count();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
                if (totalPages < 1)
                {
                    totalPages = 1;
                }

                if (page > totalPages)
                {
                    page = totalPages;
                }

                var programs = query
                    .OrderBy(p => p.ProgramName)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new AdminProgramViewModel
                    {
                        ProgramID = p.ProgramID,
                        ProgramName = p.ProgramName,
                        Description = p.Description,
                        OutputStandard = p.OutputStandard,
                        Level = p.Level,
                        Duration = p.Duration,
                        Fee = p.Fee,
                        ImageURL = p.ImageURL,
                        IsActive = p.IsActive == true
                    })
                    .ToList();

                var model = new AdminProgramPageViewModel
                {
                    Programs = programs,
                    Levels = levels,
                    Search = search,
                    Level = level,
                    Status = status,
                    CurrentPage = page,
                    TotalPages = totalPages
                };

                return View(model);
            }
        }

        [HttpGet]
        public ActionResult CreateProgram()
        {
            var authResult = CheckAdminPermission();
            if (authResult != null)
            {
                return authResult;
            }

            return View(new AdminProgramViewModel { IsActive = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateProgram(AdminProgramViewModel model, HttpPostedFileBase imageFile)
        {
            var authResult = CheckAdminPermission();
            if (authResult != null)
            {
                return authResult;
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please check program information.";
                return View(model);
            }

            var imageUrl = SaveProgramImage(imageFile);
            if (imageFile != null && imageFile.ContentLength > 0 && imageUrl == null)
            {
                TempData["Error"] = "Invalid image type. Please upload jpg, jpeg, png, gif, or webp.";
                return View(model);
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var program = new PROGRAM
                {
                    ProgramName = (model.ProgramName ?? string.Empty).Trim(),
                    Description = model.Description,
                    OutputStandard = model.OutputStandard,
                    Level = (model.Level ?? string.Empty).Trim(),
                    Duration = (model.Duration ?? string.Empty).Trim(),
                    Fee = model.Fee,
                    ImageURL = imageUrl,
                    IsActive = model.IsActive
                };

                db.PROGRAMs.InsertOnSubmit(program);
                db.SubmitChanges();
            }

            TempData["Success"] = "Program created successfully.";
            return RedirectToAction("Programs", "Admin");
        }

        [HttpGet]
        public ActionResult EditProgram(int id)
        {
            var authResult = CheckAdminPermission();
            if (authResult != null)
            {
                return authResult;
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var program = db.PROGRAMs.FirstOrDefault(p => p.ProgramID == id);
                if (program == null)
                {
                    TempData["Error"] = "Program not found.";
                    return RedirectToAction("Programs", "Admin");
                }

                var model = new AdminProgramViewModel
                {
                    ProgramID = program.ProgramID,
                    ProgramName = program.ProgramName,
                    Description = program.Description,
                    OutputStandard = program.OutputStandard,
                    Level = program.Level,
                    Duration = program.Duration,
                    Fee = program.Fee,
                    ImageURL = program.ImageURL,
                    IsActive = program.IsActive == true
                };

                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditProgram(AdminProgramViewModel model, HttpPostedFileBase imageFile)
        {
            var authResult = CheckAdminPermission();
            if (authResult != null)
            {
                return authResult;
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please check program information.";
                return View(model);
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var program = db.PROGRAMs.FirstOrDefault(p => p.ProgramID == model.ProgramID);
                if (program == null)
                {
                    TempData["Error"] = "Program not found.";
                    return RedirectToAction("Programs", "Admin");
                }

                var imageUrl = SaveProgramImage(imageFile);
                if (imageFile != null && imageFile.ContentLength > 0 && imageUrl == null)
                {
                    TempData["Error"] = "Invalid image type. Please upload jpg, jpeg, png, gif, or webp.";
                    model.ImageURL = program.ImageURL;
                    return View(model);
                }

                program.ProgramName = (model.ProgramName ?? string.Empty).Trim();
                program.Description = model.Description;
                program.OutputStandard = model.OutputStandard;
                program.Level = (model.Level ?? string.Empty).Trim();
                program.Duration = (model.Duration ?? string.Empty).Trim();
                program.Fee = model.Fee;
                program.IsActive = model.IsActive;

                if (!string.IsNullOrWhiteSpace(imageUrl))
                {
                    program.ImageURL = imageUrl;
                }

                db.SubmitChanges();
            }

            TempData["Success"] = "Program updated successfully.";
            return RedirectToAction("Programs", "Admin");
        }

        [HttpGet]
        public ActionResult DeleteProgram(int id)
        {
            var authResult = CheckAdminPermission();
            if (authResult != null)
            {
                return authResult;
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var program = db.PROGRAMs.FirstOrDefault(p => p.ProgramID == id);
                if (program == null)
                {
                    TempData["Error"] = "Program not found.";
                    return RedirectToAction("Programs", "Admin");
                }

                var model = new AdminProgramViewModel
                {
                    ProgramID = program.ProgramID,
                    ProgramName = program.ProgramName,
                    Description = program.Description,
                    OutputStandard = program.OutputStandard,
                    Level = program.Level,
                    Duration = program.Duration,
                    Fee = program.Fee,
                    ImageURL = program.ImageURL,
                    IsActive = program.IsActive == true
                };

                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmDeleteProgram(int id)
        {
            var authResult = CheckAdminPermission();
            if (authResult != null)
            {
                return authResult;
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var program = db.PROGRAMs.FirstOrDefault(p => p.ProgramID == id);
                if (program == null)
                {
                    TempData["Error"] = "Program not found.";
                    return RedirectToAction("Programs", "Admin");
                }

                program.IsActive = false;
                db.SubmitChanges();
            }

            TempData["Success"] = "Program deactivated successfully.";
            return RedirectToAction("Programs", "Admin");
        }

        public ActionResult Classes(string search, int? programId, int? teacherId, int? statusId, int page = 1)
        {
            const int pageSize = 10;

            var authResult = CheckAdminPermission();
            if (authResult != null)
            {
                return authResult;
            }

            if (page < 1)
            {
                page = 1;
            }

            search = (search ?? string.Empty).Trim();

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var query =
                    from c in db.CLASSes
                    join p in db.PROGRAMs on c.ProgramID equals p.ProgramID into programJoin
                    from p in programJoin.DefaultIfEmpty()
                    join t in db.TEACHERs on c.TeacherID equals t.TeacherID into teacherJoin
                    from t in teacherJoin.DefaultIfEmpty()
                    join s in db.CLASS_STATUS on c.StatusID equals s.StatusID into statusJoin
                    from s in statusJoin.DefaultIfEmpty()
                    select new AdminClassViewModel
                    {
                        ClassID = c.ClassID,
                        ProgramID = c.ProgramID,
                        TeacherID = c.TeacherID,
                        StatusID = c.StatusID,
                        ClassName = c.ClassName,
                        ProgramName = p != null ? p.ProgramName : string.Empty,
                        TeacherName = t != null ? t.FullName : string.Empty,
                        StatusName = s != null ? s.StatusName : string.Empty,
                        StartDate = c.StartDate,
                        Schedule = string.Empty,
                        Room = string.Empty,
                        StudentCount = db.REGISTRATIONs.Count(r => r.ClassID == c.ClassID)
                    };

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(c => c.ClassName.Contains(search) || c.ProgramName.Contains(search));
                }

                if (programId.HasValue)
                {
                    query = query.Where(c => c.ProgramID == programId.Value);
                }

                if (teacherId.HasValue)
                {
                    query = query.Where(c => c.TeacherID == teacherId.Value);
                }

                if (statusId.HasValue)
                {
                    query = query.Where(c => c.StatusID == statusId.Value);
                }

                var totalItems = query.Count();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
                if (totalPages < 1)
                {
                    totalPages = 1;
                }

                if (page > totalPages)
                {
                    page = totalPages;
                }

                var classes = query
                    .OrderByDescending(c => c.StartDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                FillClassSchedules(db, classes);

                var model = new AdminClassPageViewModel
                {
                    Classes = classes,
                    Programs = GetProgramSelectList(db, programId, false),
                    Teachers = GetTeacherSelectList(db, teacherId),
                    Statuses = GetStatusSelectList(db, statusId),
                    Search = search,
                    ProgramID = programId,
                    TeacherID = teacherId,
                    StatusID = statusId,
                    CurrentPage = page,
                    TotalPages = totalPages
                };

                return View(model);
            }
        }

        [HttpGet]
        public ActionResult CreateClass()
        {
            var authResult = CheckAdminPermission();
            if (authResult != null)
            {
                return authResult;
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var model = new AdminClassFormViewModel
                {
                    Programs = GetProgramSelectList(db, null, true),
                    Teachers = GetTeacherSelectList(db, null),
                    Statuses = GetStatusSelectList(db, null)
                };

                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateClass(AdminClassFormViewModel model)
        {
            var authResult = CheckAdminPermission();
            if (authResult != null)
            {
                return authResult;
            }

            ValidateSchedule(model);

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                model.Programs = GetProgramSelectList(db, model.ProgramID, true);
                model.Teachers = GetTeacherSelectList(db, model.TeacherID);
                model.Statuses = GetStatusSelectList(db, model.StatusID);

                if (!ModelState.IsValid)
                {
                    TempData["Error"] = "Please check class information.";
                    return View(model);
                }

                var newClass = new CLASS
                {
                    ClassName = (model.ClassName ?? string.Empty).Trim(),
                    ProgramID = model.ProgramID.Value,
                    TeacherID = model.TeacherID.Value,
                    StatusID = model.StatusID.Value,
                    StartDate = model.StartDate
                };

                db.CLASSes.InsertOnSubmit(newClass);
                db.SubmitChanges();

                InsertScheduleIfAny(db, newClass.ClassID, model);
                db.SubmitChanges();
            }

            TempData["Success"] = "Class created successfully.";
            return RedirectToAction("Classes", "Admin");
        }

        [HttpGet]
        public ActionResult EditClass(int id)
        {
            var authResult = CheckAdminPermission();
            if (authResult != null)
            {
                return authResult;
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var classInfo = db.CLASSes.FirstOrDefault(c => c.ClassID == id);
                if (classInfo == null)
                {
                    TempData["Error"] = "Class not found.";
                    return RedirectToAction("Classes", "Admin");
                }

                var schedule = db.CLASS_SCHEDULEs
                    .Where(s => s.ClassID == id)
                    .OrderBy(s => s.ScheduleID)
                    .FirstOrDefault();

                var model = new AdminClassFormViewModel
                {
                    ClassID = classInfo.ClassID,
                    ClassName = classInfo.ClassName,
                    ProgramID = classInfo.ProgramID,
                    TeacherID = classInfo.TeacherID,
                    StatusID = classInfo.StatusID,
                    StartDate = classInfo.StartDate,
                    DayOfWeek = schedule != null ? schedule.DayOfWeek : string.Empty,
                    StartTime = schedule != null ? schedule.StartTime.ToString(@"hh\:mm") : string.Empty,
                    EndTime = schedule != null ? schedule.EndTime.ToString(@"hh\:mm") : string.Empty,
                    Room = schedule != null ? schedule.Room : string.Empty,
                    Programs = GetProgramSelectList(db, classInfo.ProgramID, false),
                    Teachers = GetTeacherSelectList(db, classInfo.TeacherID),
                    Statuses = GetStatusSelectList(db, classInfo.StatusID)
                };

                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditClass(AdminClassFormViewModel model)
        {
            var authResult = CheckAdminPermission();
            if (authResult != null)
            {
                return authResult;
            }

            ValidateSchedule(model);

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                model.Programs = GetProgramSelectList(db, model.ProgramID, false);
                model.Teachers = GetTeacherSelectList(db, model.TeacherID);
                model.Statuses = GetStatusSelectList(db, model.StatusID);

                if (!ModelState.IsValid)
                {
                    TempData["Error"] = "Please check class information.";
                    return View(model);
                }

                var classInfo = db.CLASSes.FirstOrDefault(c => c.ClassID == model.ClassID);
                if (classInfo == null)
                {
                    TempData["Error"] = "Class not found.";
                    return RedirectToAction("Classes", "Admin");
                }

                classInfo.ClassName = (model.ClassName ?? string.Empty).Trim();
                classInfo.ProgramID = model.ProgramID.Value;
                classInfo.TeacherID = model.TeacherID.Value;
                classInfo.StatusID = model.StatusID.Value;
                classInfo.StartDate = model.StartDate;

                var oldSchedules = db.CLASS_SCHEDULEs.Where(s => s.ClassID == model.ClassID).ToList();
                db.CLASS_SCHEDULEs.DeleteAllOnSubmit(oldSchedules);
                InsertScheduleIfAny(db, model.ClassID, model);

                db.SubmitChanges();
            }

            TempData["Success"] = "Class updated successfully.";
            return RedirectToAction("Classes", "Admin");
        }

        [HttpGet]
        public ActionResult DeleteClass(int id)
        {
            var authResult = CheckAdminPermission();
            if (authResult != null)
            {
                return authResult;
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var classInfo = (
                    from c in db.CLASSes
                    join p in db.PROGRAMs on c.ProgramID equals p.ProgramID into programJoin
                    from p in programJoin.DefaultIfEmpty()
                    join t in db.TEACHERs on c.TeacherID equals t.TeacherID into teacherJoin
                    from t in teacherJoin.DefaultIfEmpty()
                    join s in db.CLASS_STATUS on c.StatusID equals s.StatusID into statusJoin
                    from s in statusJoin.DefaultIfEmpty()
                    where c.ClassID == id
                    select new AdminClassViewModel
                    {
                        ClassID = c.ClassID,
                        ClassName = c.ClassName,
                        ProgramName = p != null ? p.ProgramName : string.Empty,
                        TeacherName = t != null ? t.FullName : string.Empty,
                        StatusName = s != null ? s.StatusName : string.Empty,
                        StartDate = c.StartDate,
                        StudentCount = db.REGISTRATIONs.Count(r => r.ClassID == c.ClassID)
                    })
                    .FirstOrDefault();

                if (classInfo == null)
                {
                    TempData["Error"] = "Class not found.";
                    return RedirectToAction("Classes", "Admin");
                }

                return View(classInfo);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmDeleteClass(int id)
        {
            var authResult = CheckAdminPermission();
            if (authResult != null)
            {
                return authResult;
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var classInfo = db.CLASSes.FirstOrDefault(c => c.ClassID == id);
                if (classInfo == null)
                {
                    TempData["Error"] = "Class not found.";
                    return RedirectToAction("Classes", "Admin");
                }

                var closedStatus = db.CLASS_STATUS.FirstOrDefault(s => s.StatusName == "Cancelled")
                    ?? db.CLASS_STATUS.FirstOrDefault(s => s.StatusName == "Closed");

                if (closedStatus == null)
                {
                    TempData["Error"] = "Please create Cancelled or Closed status first.";
                    return RedirectToAction("Classes", "Admin");
                }

                classInfo.StatusID = closedStatus.StatusID;
                db.SubmitChanges();
            }

            TempData["Success"] = "Class status changed successfully.";
            return RedirectToAction("Classes", "Admin");
        }

        public ActionResult Teachers(string search, string status, int page = 1)
        {
            const int pageSize = 10;

            var authResult = CheckAdminPermission();
            if (authResult != null)
            {
                return authResult;
            }

            if (page < 1)
            {
                page = 1;
            }

            search = (search ?? string.Empty).Trim();
            status = string.IsNullOrWhiteSpace(status) ? "All" : status.Trim();

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var query =
                    from teacher in db.TEACHERs
                    join account in db.USER_ACCOUNTs on teacher.AccountID equals account.AccountID
                    select new AdminTeacherViewModel
                    {
                        TeacherID = teacher.TeacherID,
                        AccountID = teacher.AccountID,
                        Avatar = account.Avatar,
                        FullName = teacher.FullName,
                        Email = account.Email,
                        Expertise = teacher.Expertise,
                        IsActive = account.IsActive == true,
                        ClassCount = db.CLASSes.Count(c => c.TeacherID == teacher.TeacherID)
                    };

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(t =>
                        t.FullName.Contains(search)
                        || t.Email.Contains(search)
                        || (t.Expertise != null && t.Expertise.Contains(search)));
                }

                if (status == "Active")
                {
                    query = query.Where(t => t.IsActive);
                }
                else if (status == "Inactive")
                {
                    query = query.Where(t => !t.IsActive);
                }

                var totalItems = query.Count();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
                if (totalPages < 1)
                {
                    totalPages = 1;
                }

                if (page > totalPages)
                {
                    page = totalPages;
                }

                var teachers = query
                    .OrderBy(t => t.FullName)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var model = new AdminTeacherPageViewModel
                {
                    Teachers = teachers,
                    Search = search,
                    Status = status,
                    CurrentPage = page,
                    TotalPages = totalPages
                };

                return View(model);
            }
        }

        [HttpGet]
        public ActionResult CreateTeacher()
        {
            var authResult = CheckAdminPermission();
            if (authResult != null)
            {
                return authResult;
            }

            return View(new AdminTeacherFormViewModel { IsActive = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateTeacher(AdminTeacherFormViewModel model, HttpPostedFileBase avatarFile)
        {
            var authResult = CheckAdminPermission();
            if (authResult != null)
            {
                return authResult;
            }

            if (string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError("Password", "Password is required.");
            }

            if (model.Password != model.ConfirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "ConfirmPassword must match Password.");
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var email = (model.Email ?? string.Empty).Trim();
                if (db.USER_ACCOUNTs.Any(a => a.Email == email))
                {
                    ModelState.AddModelError("Email", "Email already exists.");
                }

                if (!ModelState.IsValid)
                {
                    TempData["Error"] = "Please check teacher information.";
                    return View(model);
                }

                var avatarUrl = SaveAvatarImage(avatarFile);
                if (avatarFile != null && avatarFile.ContentLength > 0 && avatarUrl == null)
                {
                    TempData["Error"] = "Invalid avatar type. Please upload jpg, jpeg, png, gif, or webp.";
                    return View(model);
                }

                var account = new USER_ACCOUNT
                {
                    Email = email,
                    PasswordHash = model.Password,
                    Role = "Teacher",
                    Avatar = avatarUrl,
                    IsActive = model.IsActive,
                    FailedLoginAttempts = 0,
                    IsLockedOut = false
                };

                db.USER_ACCOUNTs.InsertOnSubmit(account);
                db.SubmitChanges();

                var teacher = new TEACHER
                {
                    AccountID = account.AccountID,
                    FullName = (model.FullName ?? string.Empty).Trim(),
                    Expertise = (model.Expertise ?? string.Empty).Trim()
                };

                db.TEACHERs.InsertOnSubmit(teacher);
                db.SubmitChanges();
            }

            TempData["Success"] = "Teacher created successfully.";
            return RedirectToAction("Teachers", "Admin");
        }

        [HttpGet]
        public ActionResult EditTeacher(int id)
        {
            var authResult = CheckAdminPermission();
            if (authResult != null)
            {
                return authResult;
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var teacher = db.TEACHERs.FirstOrDefault(t => t.TeacherID == id);
                if (teacher == null)
                {
                    TempData["Error"] = "Teacher not found.";
                    return RedirectToAction("Teachers", "Admin");
                }

                var account = db.USER_ACCOUNTs.FirstOrDefault(a => a.AccountID == teacher.AccountID);
                if (account == null)
                {
                    TempData["Error"] = "Teacher account not found.";
                    return RedirectToAction("Teachers", "Admin");
                }

                var model = new AdminTeacherFormViewModel
                {
                    TeacherID = teacher.TeacherID,
                    AccountID = teacher.AccountID,
                    Email = account.Email,
                    FullName = teacher.FullName,
                    Expertise = teacher.Expertise,
                    Avatar = account.Avatar,
                    IsActive = account.IsActive == true
                };

                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditTeacher(AdminTeacherFormViewModel model, HttpPostedFileBase avatarFile)
        {
            var authResult = CheckAdminPermission();
            if (authResult != null)
            {
                return authResult;
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var teacher = db.TEACHERs.FirstOrDefault(t => t.TeacherID == model.TeacherID);
                if (teacher == null)
                {
                    TempData["Error"] = "Teacher not found.";
                    return RedirectToAction("Teachers", "Admin");
                }

                var account = db.USER_ACCOUNTs.FirstOrDefault(a => a.AccountID == teacher.AccountID);
                if (account == null)
                {
                    TempData["Error"] = "Teacher account not found.";
                    return RedirectToAction("Teachers", "Admin");
                }

                var email = (model.Email ?? string.Empty).Trim();
                if (db.USER_ACCOUNTs.Any(a => a.Email == email && a.AccountID != account.AccountID))
                {
                    ModelState.AddModelError("Email", "Email already exists.");
                }

                if (!ModelState.IsValid)
                {
                    TempData["Error"] = "Please check teacher information.";
                    model.Avatar = account.Avatar;
                    return View(model);
                }

                var avatarUrl = SaveAvatarImage(avatarFile);
                if (avatarFile != null && avatarFile.ContentLength > 0 && avatarUrl == null)
                {
                    TempData["Error"] = "Invalid avatar type. Please upload jpg, jpeg, png, gif, or webp.";
                    model.Avatar = account.Avatar;
                    return View(model);
                }

                account.Email = email;
                account.IsActive = model.IsActive;
                if (!string.IsNullOrWhiteSpace(avatarUrl))
                {
                    account.Avatar = avatarUrl;
                }

                teacher.FullName = (model.FullName ?? string.Empty).Trim();
                teacher.Expertise = (model.Expertise ?? string.Empty).Trim();

                db.SubmitChanges();
            }

            TempData["Success"] = "Teacher updated successfully.";
            return RedirectToAction("Teachers", "Admin");
        }

        [HttpGet]
        public ActionResult DeleteTeacher(int id)
        {
            var authResult = CheckAdminPermission();
            if (authResult != null)
            {
                return authResult;
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var teacher = db.TEACHERs.FirstOrDefault(t => t.TeacherID == id);
                if (teacher == null)
                {
                    TempData["Error"] = "Teacher not found.";
                    return RedirectToAction("Teachers", "Admin");
                }

                var account = db.USER_ACCOUNTs.FirstOrDefault(a => a.AccountID == teacher.AccountID);
                var model = new AdminTeacherViewModel
                {
                    TeacherID = teacher.TeacherID,
                    AccountID = teacher.AccountID,
                    Avatar = account != null ? account.Avatar : string.Empty,
                    FullName = teacher.FullName,
                    Email = account != null ? account.Email : string.Empty,
                    Expertise = teacher.Expertise,
                    IsActive = account != null && account.IsActive == true,
                    ClassCount = db.CLASSes.Count(c => c.TeacherID == teacher.TeacherID)
                };

                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmDeleteTeacher(int id)
        {
            var authResult = CheckAdminPermission();
            if (authResult != null)
            {
                return authResult;
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var teacher = db.TEACHERs.FirstOrDefault(t => t.TeacherID == id);
                if (teacher == null)
                {
                    TempData["Error"] = "Teacher not found.";
                    return RedirectToAction("Teachers", "Admin");
                }

                var account = db.USER_ACCOUNTs.FirstOrDefault(a => a.AccountID == teacher.AccountID);
                if (account == null)
                {
                    TempData["Error"] = "Teacher account not found.";
                    return RedirectToAction("Teachers", "Admin");
                }

                account.IsActive = false;
                db.SubmitChanges();
            }

            TempData["Success"] = "Teacher deactivated successfully.";
            return RedirectToAction("Teachers", "Admin");
        }

        private ActionResult CheckAdminPermission()
        {
            if (Session["AccountID"] == null || Session["Role"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (Session["Role"].ToString() != "Admin")
            {
                TempData["Error"] = "You do not have permission to access this page.";
                return RedirectToAction("Index", "Home");
            }

            return null;
        }

        private string SaveProgramImage(HttpPostedFileBase imageFile)
        {
            if (imageFile == null || imageFile.ContentLength <= 0)
            {
                return null;
            }

            var extension = Path.GetExtension(imageFile.FileName);
            if (!IsAllowedProgramImageExtension(extension))
            {
                return null;
            }

            var uploadFolder = Server.MapPath("~/Content/Uploads/Programs");
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            var fileName = string.Format("{0}_{1}{2}", DateTime.Now.Ticks, Guid.NewGuid().ToString("N"), extension);
            var physicalPath = Path.Combine(uploadFolder, fileName);
            imageFile.SaveAs(physicalPath);

            return "/Content/Uploads/Programs/" + fileName;
        }

        private string SaveAvatarImage(HttpPostedFileBase avatarFile)
        {
            if (avatarFile == null || avatarFile.ContentLength <= 0)
            {
                return null;
            }

            var extension = Path.GetExtension(avatarFile.FileName);
            if (!IsAllowedProgramImageExtension(extension))
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

        private static bool IsAllowedProgramImageExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return false;
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            return allowedExtensions.Contains(extension.ToLower());
        }

        private static System.Collections.Generic.List<SelectListItem> GetProgramSelectList(LanguageCenterDataContext db, int? selectedId, bool activeOnly)
        {
            var query = db.PROGRAMs.AsQueryable();
            if (activeOnly)
            {
                query = query.Where(p => p.IsActive == true);
            }

            return query
                .OrderBy(p => p.ProgramName)
                .ToList()
                .Select(p => new SelectListItem
                {
                    Value = p.ProgramID.ToString(),
                    Text = p.ProgramName,
                    Selected = selectedId.HasValue && p.ProgramID == selectedId.GetValueOrDefault()
                })
                .ToList();
        }

        private static System.Collections.Generic.List<SelectListItem> GetTeacherSelectList(LanguageCenterDataContext db, int? selectedId)
        {
            return db.TEACHERs
                .OrderBy(t => t.FullName)
                .ToList()
                .Select(t => new SelectListItem
                {
                    Value = t.TeacherID.ToString(),
                    Text = t.FullName,
                    Selected = selectedId.HasValue && t.TeacherID == selectedId.GetValueOrDefault()
                })
                .ToList();
        }

        private static System.Collections.Generic.List<SelectListItem> GetStatusSelectList(LanguageCenterDataContext db, int? selectedId)
        {
            return db.CLASS_STATUS
                .OrderBy(s => s.StatusName)
                .ToList()
                .Select(s => new SelectListItem
                {
                    Value = s.StatusID.ToString(),
                    Text = s.StatusName,
                    Selected = selectedId.HasValue && s.StatusID == selectedId.GetValueOrDefault()
                })
                .ToList();
        }

        private void ValidateSchedule(AdminClassFormViewModel model)
        {
            var hasAnySchedule = !string.IsNullOrWhiteSpace(model.DayOfWeek)
                || !string.IsNullOrWhiteSpace(model.StartTime)
                || !string.IsNullOrWhiteSpace(model.EndTime)
                || !string.IsNullOrWhiteSpace(model.Room);

            if (!hasAnySchedule)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(model.DayOfWeek)
                || string.IsNullOrWhiteSpace(model.StartTime)
                || string.IsNullOrWhiteSpace(model.EndTime)
                || string.IsNullOrWhiteSpace(model.Room))
            {
                ModelState.AddModelError("DayOfWeek", "Please enter DayOfWeek, StartTime, EndTime, and Room, or leave schedule empty.");
                return;
            }

            TimeSpan startTime;
            TimeSpan endTime;
            if (!TimeSpan.TryParse(model.StartTime, out startTime) || !TimeSpan.TryParse(model.EndTime, out endTime))
            {
                ModelState.AddModelError("StartTime", "Schedule time is invalid.");
                return;
            }

            if (endTime <= startTime)
            {
                ModelState.AddModelError("EndTime", "EndTime must be later than StartTime.");
            }
        }

        private static void InsertScheduleIfAny(LanguageCenterDataContext db, int classId, AdminClassFormViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.DayOfWeek)
                || string.IsNullOrWhiteSpace(model.StartTime)
                || string.IsNullOrWhiteSpace(model.EndTime)
                || string.IsNullOrWhiteSpace(model.Room))
            {
                return;
            }

            TimeSpan startTime;
            TimeSpan endTime;
            if (!TimeSpan.TryParse(model.StartTime, out startTime) || !TimeSpan.TryParse(model.EndTime, out endTime))
            {
                return;
            }

            var schedule = new CLASS_SCHEDULE
            {
                ClassID = classId,
                DayOfWeek = model.DayOfWeek.Trim(),
                StartTime = startTime,
                EndTime = endTime,
                Room = model.Room.Trim()
            };

            db.CLASS_SCHEDULEs.InsertOnSubmit(schedule);
        }

        private static void FillClassSchedules(LanguageCenterDataContext db, System.Collections.Generic.List<AdminClassViewModel> classes)
        {
            var classIds = classes.Select(c => c.ClassID).ToList();
            var schedules = db.CLASS_SCHEDULEs
                .Where(s => classIds.Contains(s.ClassID))
                .OrderBy(s => s.ClassID)
                .ThenBy(s => s.ScheduleID)
                .ToList()
                .GroupBy(s => s.ClassID)
                .ToDictionary(s => s.Key, s => s.ToList());

            foreach (var item in classes)
            {
                if (!schedules.ContainsKey(item.ClassID) || !schedules[item.ClassID].Any())
                {
                    item.Schedule = "No schedule";
                    item.Room = "No room";
                    continue;
                }

                var rows = schedules[item.ClassID].Select(s =>
                    string.Format("{0} {1:hh\\:mm} - {2:hh\\:mm}", s.DayOfWeek, s.StartTime, s.EndTime));

                var rooms = schedules[item.ClassID]
                    .Where(s => !string.IsNullOrWhiteSpace(s.Room))
                    .Select(s => s.Room)
                    .Distinct()
                    .ToList();

                item.Schedule = string.Join("\n", rows);
                item.Room = rooms.Any() ? string.Join(", ", rooms) : "No room";
            }
        }
    }
}
