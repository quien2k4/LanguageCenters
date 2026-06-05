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

        public ActionResult Students(string search, string status, int page = 1)
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
                    from student in db.STUDENTs
                    join account in db.USER_ACCOUNTs on student.AccountID equals account.AccountID
                    select new AdminStudentViewModel
                    {
                        StudentID = student.StudentID,
                        AccountID = student.AccountID,
                        Avatar = account.Avatar,
                        FullName = student.FullName,
                        Email = account.Email,
                        DateOfBirth = student.DateOfBirth,
                        PhoneNumber = student.PhoneNumber,
                        IsActive = account.IsActive == true,
                        RegistrationCount = db.REGISTRATIONs.Count(r => r.StudentID == student.StudentID),
                        PaymentCount = (
                            from payment in db.PAYMENTs
                            join registration in db.REGISTRATIONs on payment.RegistrationID equals registration.RegistrationID
                            where registration.StudentID == student.StudentID
                            select payment.PaymentID).Count(),
                        PlacementTestCount = db.PLACEMENT_TESTs.Count(t => t.StudentID == student.StudentID)
                    };

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(s =>
                        s.FullName.Contains(search)
                        || s.Email.Contains(search)
                        || (s.PhoneNumber != null && s.PhoneNumber.Contains(search)));
                }

                if (status == "Active")
                {
                    query = query.Where(s => s.IsActive);
                }
                else if (status == "Inactive")
                {
                    query = query.Where(s => !s.IsActive);
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

                var students = query
                    .OrderBy(s => s.FullName)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var model = new AdminStudentPageViewModel
                {
                    Students = students,
                    Search = search,
                    Status = status,
                    CurrentPage = page,
                    TotalPages = totalPages
                };

                return View(model);
            }
        }

        [HttpGet]
        public ActionResult CreateStudent()
        {
            var authResult = CheckAdminPermission();
            if (authResult != null)
            {
                return authResult;
            }

            return View(new AdminStudentFormViewModel { IsActive = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateStudent(AdminStudentFormViewModel model, HttpPostedFileBase avatarFile)
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
                    TempData["Error"] = "Please check student information.";
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
                    Role = "Student",
                    Avatar = avatarUrl,
                    IsActive = model.IsActive,
                    FailedLoginAttempts = 0,
                    IsLockedOut = false
                };

                db.USER_ACCOUNTs.InsertOnSubmit(account);
                db.SubmitChanges();

                var student = new STUDENT
                {
                    AccountID = account.AccountID,
                    FullName = (model.FullName ?? string.Empty).Trim(),
                    DateOfBirth = model.DateOfBirth,
                    PhoneNumber = (model.PhoneNumber ?? string.Empty).Trim()
                };

                db.STUDENTs.InsertOnSubmit(student);
                db.SubmitChanges();
            }

            TempData["Success"] = "Student created successfully.";
            return RedirectToAction("Students", "Admin");
        }

        [HttpGet]
        public ActionResult EditStudent(int id)
        {
            var authResult = CheckAdminPermission();
            if (authResult != null)
            {
                return authResult;
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var student = db.STUDENTs.FirstOrDefault(s => s.StudentID == id);
                if (student == null)
                {
                    TempData["Error"] = "Student not found.";
                    return RedirectToAction("Students", "Admin");
                }

                var account = db.USER_ACCOUNTs.FirstOrDefault(a => a.AccountID == student.AccountID);
                if (account == null)
                {
                    TempData["Error"] = "Student account not found.";
                    return RedirectToAction("Students", "Admin");
                }

                var model = new AdminStudentFormViewModel
                {
                    StudentID = student.StudentID,
                    AccountID = student.AccountID,
                    Email = account.Email,
                    FullName = student.FullName,
                    DateOfBirth = student.DateOfBirth,
                    PhoneNumber = student.PhoneNumber,
                    Avatar = account.Avatar,
                    IsActive = account.IsActive == true
                };

                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditStudent(AdminStudentFormViewModel model, HttpPostedFileBase avatarFile)
        {
            var authResult = CheckAdminPermission();
            if (authResult != null)
            {
                return authResult;
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var student = db.STUDENTs.FirstOrDefault(s => s.StudentID == model.StudentID);
                if (student == null)
                {
                    TempData["Error"] = "Student not found.";
                    return RedirectToAction("Students", "Admin");
                }

                var account = db.USER_ACCOUNTs.FirstOrDefault(a => a.AccountID == student.AccountID);
                if (account == null)
                {
                    TempData["Error"] = "Student account not found.";
                    return RedirectToAction("Students", "Admin");
                }

                var email = (model.Email ?? string.Empty).Trim();
                if (db.USER_ACCOUNTs.Any(a => a.Email == email && a.AccountID != account.AccountID))
                {
                    ModelState.AddModelError("Email", "Email already exists.");
                }

                if (!ModelState.IsValid)
                {
                    TempData["Error"] = "Please check student information.";
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

                student.FullName = (model.FullName ?? string.Empty).Trim();
                student.DateOfBirth = model.DateOfBirth;
                student.PhoneNumber = (model.PhoneNumber ?? string.Empty).Trim();

                db.SubmitChanges();
            }

            TempData["Success"] = "Student updated successfully.";
            return RedirectToAction("Students", "Admin");
        }

        [HttpGet]
        public ActionResult DeleteStudent(int id)
        {
            var authResult = CheckAdminPermission();
            if (authResult != null)
            {
                return authResult;
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var student = db.STUDENTs.FirstOrDefault(s => s.StudentID == id);
                if (student == null)
                {
                    TempData["Error"] = "Student not found.";
                    return RedirectToAction("Students", "Admin");
                }

                var account = db.USER_ACCOUNTs.FirstOrDefault(a => a.AccountID == student.AccountID);
                var registrationIds = db.REGISTRATIONs
                    .Where(r => r.StudentID == student.StudentID)
                    .Select(r => r.RegistrationID)
                    .ToList();

                var model = new AdminStudentViewModel
                {
                    StudentID = student.StudentID,
                    AccountID = student.AccountID,
                    Avatar = account != null ? account.Avatar : string.Empty,
                    FullName = student.FullName,
                    Email = account != null ? account.Email : string.Empty,
                    DateOfBirth = student.DateOfBirth,
                    PhoneNumber = student.PhoneNumber,
                    IsActive = account != null && account.IsActive == true,
                    RegistrationCount = registrationIds.Count,
                    PaymentCount = db.PAYMENTs.Count(p => registrationIds.Contains(p.RegistrationID)),
                    PlacementTestCount = db.PLACEMENT_TESTs.Count(t => t.StudentID == student.StudentID)
                };

                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmDeleteStudent(int id)
        {
            var authResult = CheckAdminPermission();
            if (authResult != null)
            {
                return authResult;
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var student = db.STUDENTs.FirstOrDefault(s => s.StudentID == id);
                if (student == null)
                {
                    TempData["Error"] = "Student not found.";
                    return RedirectToAction("Students", "Admin");
                }

                var account = db.USER_ACCOUNTs.FirstOrDefault(a => a.AccountID == student.AccountID);
                if (account == null)
                {
                    TempData["Error"] = "Student account not found.";
                    return RedirectToAction("Students", "Admin");
                }

                account.IsActive = false;
                db.SubmitChanges();
            }

            TempData["Success"] = "Student deactivated successfully.";
            return RedirectToAction("Students", "Admin");
        }

        public ActionResult Registrations(string search, string regStatus, string paymentStatus, int page = 1)
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
            regStatus = string.IsNullOrWhiteSpace(regStatus) ? "All" : regStatus.Trim();
            paymentStatus = string.IsNullOrWhiteSpace(paymentStatus) ? "All" : paymentStatus.Trim();

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var query =
                    from r in db.REGISTRATIONs
                    join st in db.STUDENTs on r.StudentID equals st.StudentID into studentJoin
                    from st in studentJoin.DefaultIfEmpty()
                    join account in db.USER_ACCOUNTs on st.AccountID equals account.AccountID into accountJoin
                    from account in accountJoin.DefaultIfEmpty()
                    join c in db.CLASSes on r.ClassID equals c.ClassID into classJoin
                    from c in classJoin.DefaultIfEmpty()
                    join p in db.PROGRAMs on c.ProgramID equals p.ProgramID into programJoin
                    from p in programJoin.DefaultIfEmpty()
                    join t in db.TEACHERs on c.TeacherID equals t.TeacherID into teacherJoin
                    from t in teacherJoin.DefaultIfEmpty()
                    join pay in db.PAYMENTs on r.RegistrationID equals pay.RegistrationID into paymentJoin
                    from pay in paymentJoin.DefaultIfEmpty()
                    select new AdminRegistrationViewModel
                    {
                        RegistrationID = r.RegistrationID,
                        StudentName = st != null ? st.FullName : string.Empty,
                        StudentEmail = account != null ? account.Email : string.Empty,
                        PhoneNumber = st != null ? st.PhoneNumber : string.Empty,
                        ClassName = c != null ? c.ClassName : string.Empty,
                        ProgramName = p != null ? p.ProgramName : string.Empty,
                        TeacherName = t != null ? t.FullName : string.Empty,
                        RegistrationDate = r.RegistrationDate,
                        RegStatus = r.RegStatus,
                        PaymentStatus = pay != null ? pay.PaymentStatus : "No payment",
                        Amount = pay != null ? (decimal?)pay.Amount : null
                    };

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(r =>
                        r.StudentName.Contains(search)
                        || r.StudentEmail.Contains(search)
                        || r.ClassName.Contains(search)
                        || r.ProgramName.Contains(search));
                }

                if (regStatus != "All")
                {
                    query = query.Where(r => r.RegStatus == regStatus);
                }

                if (paymentStatus != "All")
                {
                    query = query.Where(r => r.PaymentStatus == paymentStatus);
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

                var registrations = query
                    .OrderByDescending(r => r.RegistrationDate)
                    .ThenByDescending(r => r.RegistrationID)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var model = new AdminRegistrationPageViewModel
                {
                    Registrations = registrations,
                    Search = search,
                    RegStatus = regStatus,
                    PaymentStatus = paymentStatus,
                    CurrentPage = page,
                    TotalPages = totalPages
                };

                return View(model);
            }
        }

        [HttpGet]
        public ActionResult EditRegistration(int id)
        {
            var authResult = CheckAdminPermission();
            if (authResult != null)
            {
                return authResult;
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var model = (
                    from r in db.REGISTRATIONs
                    join st in db.STUDENTs on r.StudentID equals st.StudentID into studentJoin
                    from st in studentJoin.DefaultIfEmpty()
                    join c in db.CLASSes on r.ClassID equals c.ClassID into classJoin
                    from c in classJoin.DefaultIfEmpty()
                    join p in db.PROGRAMs on c.ProgramID equals p.ProgramID into programJoin
                    from p in programJoin.DefaultIfEmpty()
                    join pay in db.PAYMENTs on r.RegistrationID equals pay.RegistrationID into paymentJoin
                    from pay in paymentJoin.DefaultIfEmpty()
                    where r.RegistrationID == id
                    select new AdminRegistrationFormViewModel
                    {
                        RegistrationID = r.RegistrationID,
                        StudentName = st != null ? st.FullName : string.Empty,
                        ClassName = c != null ? c.ClassName : string.Empty,
                        ProgramName = p != null ? p.ProgramName : string.Empty,
                        RegistrationDate = r.RegistrationDate,
                        RegStatus = r.RegStatus,
                        PaymentStatus = pay != null ? pay.PaymentStatus : "No payment"
                    })
                    .FirstOrDefault();

                if (model == null)
                {
                    TempData["Error"] = "Registration not found.";
                    return RedirectToAction("Registrations", "Admin");
                }

                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditRegistration(AdminRegistrationFormViewModel model)
        {
            var authResult = CheckAdminPermission();
            if (authResult != null)
            {
                return authResult;
            }

            var allowedStatuses = new[] { "Pending", "Approved", "Cancelled" };
            if (string.IsNullOrWhiteSpace(model.RegStatus) || !allowedStatuses.Contains(model.RegStatus))
            {
                ModelState.AddModelError("RegStatus", "Registration status is invalid.");
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var registration = db.REGISTRATIONs.FirstOrDefault(r => r.RegistrationID == model.RegistrationID);
                if (registration == null)
                {
                    TempData["Error"] = "Registration not found.";
                    return RedirectToAction("Registrations", "Admin");
                }

                if (!ModelState.IsValid)
                {
                    var detail = (
                        from r in db.REGISTRATIONs
                        join st in db.STUDENTs on r.StudentID equals st.StudentID into studentJoin
                        from st in studentJoin.DefaultIfEmpty()
                        join c in db.CLASSes on r.ClassID equals c.ClassID into classJoin
                        from c in classJoin.DefaultIfEmpty()
                        join p in db.PROGRAMs on c.ProgramID equals p.ProgramID into programJoin
                        from p in programJoin.DefaultIfEmpty()
                        join pay in db.PAYMENTs on r.RegistrationID equals pay.RegistrationID into paymentJoin
                        from pay in paymentJoin.DefaultIfEmpty()
                        where r.RegistrationID == registration.RegistrationID
                        select new
                        {
                            StudentName = st != null ? st.FullName : string.Empty,
                            ClassName = c != null ? c.ClassName : string.Empty,
                            ProgramName = p != null ? p.ProgramName : string.Empty,
                            RegistrationDate = r.RegistrationDate,
                            PaymentStatus = pay != null ? pay.PaymentStatus : "No payment"
                        })
                        .FirstOrDefault();

                    if (detail != null)
                    {
                        model.StudentName = detail.StudentName;
                        model.ClassName = detail.ClassName;
                        model.ProgramName = detail.ProgramName;
                        model.RegistrationDate = detail.RegistrationDate;
                        model.PaymentStatus = detail.PaymentStatus;
                    }

                    TempData["Error"] = "Please check registration status.";
                    return View(model);
                }

                registration.RegStatus = model.RegStatus;
                db.SubmitChanges();
            }

            TempData["Success"] = "Registration status updated successfully.";
            return RedirectToAction("Registrations", "Admin");
        }

        public ActionResult Payments(string search, string paymentStatus, string method, int page = 1)
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
            paymentStatus = string.IsNullOrWhiteSpace(paymentStatus) ? "All" : paymentStatus.Trim();
            method = string.IsNullOrWhiteSpace(method) ? "All" : method.Trim();

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var allPayments = db.PAYMENTs;

                var query =
                    from pay in db.PAYMENTs
                    join r in db.REGISTRATIONs on pay.RegistrationID equals r.RegistrationID into registrationJoin
                    from r in registrationJoin.DefaultIfEmpty()
                    join st in db.STUDENTs on r.StudentID equals st.StudentID into studentJoin
                    from st in studentJoin.DefaultIfEmpty()
                    join account in db.USER_ACCOUNTs on st.AccountID equals account.AccountID into accountJoin
                    from account in accountJoin.DefaultIfEmpty()
                    join c in db.CLASSes on r.ClassID equals c.ClassID into classJoin
                    from c in classJoin.DefaultIfEmpty()
                    join p in db.PROGRAMs on c.ProgramID equals p.ProgramID into programJoin
                    from p in programJoin.DefaultIfEmpty()
                    select new AdminPaymentViewModel
                    {
                        PaymentID = pay.PaymentID,
                        RegistrationID = pay.RegistrationID,
                        StudentName = st != null ? st.FullName : string.Empty,
                        StudentEmail = account != null ? account.Email : string.Empty,
                        ClassName = c != null ? c.ClassName : string.Empty,
                        ProgramName = p != null ? p.ProgramName : string.Empty,
                        Amount = pay.Amount,
                        PaymentDate = pay.PaymentDate,
                        Method = pay.Method,
                        PaymentStatus = pay.PaymentStatus,
                        RegStatus = r != null ? r.RegStatus : string.Empty
                    };

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(p =>
                        p.StudentName.Contains(search)
                        || p.StudentEmail.Contains(search)
                        || p.ClassName.Contains(search)
                        || p.ProgramName.Contains(search));
                }

                if (paymentStatus != "All")
                {
                    query = query.Where(p => p.PaymentStatus == paymentStatus);
                }

                if (method != "All")
                {
                    query = query.Where(p => p.Method == method);
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

                var payments = query
                    .OrderByDescending(p => p.PaymentDate)
                    .ThenByDescending(p => p.PaymentID)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var model = new AdminPaymentPageViewModel
                {
                    Payments = payments,
                    Search = search,
                    PaymentStatus = paymentStatus,
                    Method = method,
                    CurrentPage = page,
                    TotalPages = totalPages,
                    TotalPayments = allPayments.Count(),
                    PaidPayments = allPayments.Count(p => p.PaymentStatus == "Paid"),
                    UnpaidPayments = allPayments.Count(p => p.PaymentStatus == "Unpaid"),
                    TotalRevenue = allPayments.Where(p => p.PaymentStatus == "Paid").Sum(p => (decimal?)p.Amount) ?? 0
                };

                return View(model);
            }
        }

        [HttpGet]
        public ActionResult EditPayment(int id)
        {
            var authResult = CheckAdminPermission();
            if (authResult != null)
            {
                return authResult;
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var model = (
                    from pay in db.PAYMENTs
                    join r in db.REGISTRATIONs on pay.RegistrationID equals r.RegistrationID into registrationJoin
                    from r in registrationJoin.DefaultIfEmpty()
                    join st in db.STUDENTs on r.StudentID equals st.StudentID into studentJoin
                    from st in studentJoin.DefaultIfEmpty()
                    join c in db.CLASSes on r.ClassID equals c.ClassID into classJoin
                    from c in classJoin.DefaultIfEmpty()
                    join p in db.PROGRAMs on c.ProgramID equals p.ProgramID into programJoin
                    from p in programJoin.DefaultIfEmpty()
                    where pay.PaymentID == id
                    select new AdminPaymentFormViewModel
                    {
                        PaymentID = pay.PaymentID,
                        RegistrationID = pay.RegistrationID,
                        StudentName = st != null ? st.FullName : string.Empty,
                        ClassName = c != null ? c.ClassName : string.Empty,
                        ProgramName = p != null ? p.ProgramName : string.Empty,
                        Amount = pay.Amount,
                        PaymentDate = pay.PaymentDate,
                        Method = pay.Method,
                        PaymentStatus = pay.PaymentStatus
                    })
                    .FirstOrDefault();

                if (model == null)
                {
                    TempData["Error"] = "Payment not found.";
                    return RedirectToAction("Payments", "Admin");
                }

                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditPayment(AdminPaymentFormViewModel model)
        {
            var authResult = CheckAdminPermission();
            if (authResult != null)
            {
                return authResult;
            }

            var allowedStatuses = new[] { "Unpaid", "Paid", "Failed", "Refunded" };
            var allowedMethods = new[] { "Demo", "Cash", "Bank Transfer", "Online" };

            if (string.IsNullOrWhiteSpace(model.PaymentStatus) || !allowedStatuses.Contains(model.PaymentStatus))
            {
                ModelState.AddModelError("PaymentStatus", "Payment status is invalid.");
            }

            if (model.PaymentStatus == "Paid" && string.IsNullOrWhiteSpace(model.Method))
            {
                ModelState.AddModelError("Method", "Method is required when payment status is Paid.");
            }

            if (!string.IsNullOrWhiteSpace(model.Method) && !allowedMethods.Contains(model.Method))
            {
                ModelState.AddModelError("Method", "Payment method is invalid.");
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var payment = db.PAYMENTs.FirstOrDefault(p => p.PaymentID == model.PaymentID);
                if (payment == null)
                {
                    TempData["Error"] = "Payment not found.";
                    return RedirectToAction("Payments", "Admin");
                }

                if (!ModelState.IsValid)
                {
                    FillPaymentEditInfo(db, model);
                    TempData["Error"] = "Please check payment information.";
                    return View(model);
                }

                payment.Amount = model.Amount;
                payment.Method = string.IsNullOrWhiteSpace(model.Method) ? null : model.Method.Trim();
                payment.PaymentStatus = model.PaymentStatus;

                if (payment.PaymentStatus == "Paid" && !payment.PaymentDate.HasValue)
                {
                    payment.PaymentDate = DateTime.Now;
                }

                db.SubmitChanges();
            }

            TempData["Success"] = "Payment updated successfully.";
            return RedirectToAction("Payments", "Admin");
        }

        private static void FillPaymentEditInfo(LanguageCenterDataContext db, AdminPaymentFormViewModel model)
        {
            var detail = (
                from pay in db.PAYMENTs
                join r in db.REGISTRATIONs on pay.RegistrationID equals r.RegistrationID into registrationJoin
                from r in registrationJoin.DefaultIfEmpty()
                join st in db.STUDENTs on r.StudentID equals st.StudentID into studentJoin
                from st in studentJoin.DefaultIfEmpty()
                join c in db.CLASSes on r.ClassID equals c.ClassID into classJoin
                from c in classJoin.DefaultIfEmpty()
                join p in db.PROGRAMs on c.ProgramID equals p.ProgramID into programJoin
                from p in programJoin.DefaultIfEmpty()
                where pay.PaymentID == model.PaymentID
                select new
                {
                    pay.RegistrationID,
                    StudentName = st != null ? st.FullName : string.Empty,
                    ClassName = c != null ? c.ClassName : string.Empty,
                    ProgramName = p != null ? p.ProgramName : string.Empty,
                    pay.PaymentDate
                })
                .FirstOrDefault();

            if (detail == null)
            {
                return;
            }

            model.RegistrationID = detail.RegistrationID;
            model.StudentName = detail.StudentName;
            model.ClassName = detail.ClassName;
            model.ProgramName = detail.ProgramName;
            model.PaymentDate = detail.PaymentDate;
        }

        public ActionResult PlacementTests(string search, string status, string level, int page = 1)
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
            level = (level ?? string.Empty).Trim();

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var query =
                    from test in db.PLACEMENT_TESTs
                    join st in db.STUDENTs on test.StudentID equals st.StudentID into studentJoin
                    from st in studentJoin.DefaultIfEmpty()
                    join account in db.USER_ACCOUNTs on st.AccountID equals account.AccountID into accountJoin
                    from account in accountJoin.DefaultIfEmpty()
                    select new AdminPlacementTestViewModel
                    {
                        TestID = test.TestID,
                        StudentName = st != null ? st.FullName : string.Empty,
                        StudentEmail = account != null ? account.Email : string.Empty,
                        PhoneNumber = st != null ? st.PhoneNumber : string.Empty,
                        TestDate = test.TestDate,
                        TestTime = test.TestTime,
                        Level = test.Level,
                        ResultScore = test.ResultScore,
                        Status = test.Status
                    };

                var levels = db.PLACEMENT_TESTs
                    .Where(t => t.Level != null && t.Level != string.Empty)
                    .Select(t => t.Level)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(t =>
                        t.StudentName.Contains(search)
                        || t.StudentEmail.Contains(search)
                        || t.PhoneNumber.Contains(search)
                        || t.Level.Contains(search));
                }

                if (status != "All")
                {
                    query = query.Where(t => t.Status == status);
                }

                if (!string.IsNullOrWhiteSpace(level))
                {
                    query = query.Where(t => t.Level == level);
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

                var placementTests = query
                    .OrderByDescending(t => t.TestDate)
                    .ThenByDescending(t => t.TestID)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var model = new AdminPlacementTestPageViewModel
                {
                    PlacementTests = placementTests,
                    Levels = levels,
                    Search = search,
                    Status = status,
                    Level = level,
                    CurrentPage = page,
                    TotalPages = totalPages,
                    TotalTests = db.PLACEMENT_TESTs.Count(),
                    PendingTests = db.PLACEMENT_TESTs.Count(t => t.Status == "Pending"),
                    CompletedTests = db.PLACEMENT_TESTs.Count(t => t.Status == "Completed"),
                    CancelledTests = db.PLACEMENT_TESTs.Count(t => t.Status == "Cancelled")
                };

                return View(model);
            }
        }

        [HttpGet]
        public ActionResult EditPlacementTest(int id)
        {
            var authResult = CheckAdminPermission();
            if (authResult != null)
            {
                return authResult;
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var detail = (
                    from test in db.PLACEMENT_TESTs
                    join st in db.STUDENTs on test.StudentID equals st.StudentID into studentJoin
                    from st in studentJoin.DefaultIfEmpty()
                    join account in db.USER_ACCOUNTs on st.AccountID equals account.AccountID into accountJoin
                    from account in accountJoin.DefaultIfEmpty()
                    where test.TestID == id
                    select new
                    {
                        TestID = test.TestID,
                        StudentName = st != null ? st.FullName : string.Empty,
                        StudentEmail = account != null ? account.Email : string.Empty,
                        PhoneNumber = st != null ? st.PhoneNumber : string.Empty,
                        TestDate = test.TestDate,
                        TestTime = test.TestTime,
                        Level = test.Level,
                        ResultScore = test.ResultScore,
                        Status = test.Status
                    })
                    .FirstOrDefault();

                if (detail == null)
                {
                    TempData["Error"] = "Placement test not found.";
                    return RedirectToAction("PlacementTests", "Admin");
                }

                var model = new AdminPlacementTestFormViewModel
                {
                    TestID = detail.TestID,
                    StudentName = detail.StudentName,
                    StudentEmail = detail.StudentEmail,
                    PhoneNumber = detail.PhoneNumber,
                    TestDate = detail.TestDate,
                    TestTime = detail.TestTime.ToString(@"hh\:mm"),
                    Level = detail.Level,
                    ResultScore = detail.ResultScore,
                    Status = detail.Status
                };

                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditPlacementTest(AdminPlacementTestFormViewModel model)
        {
            var authResult = CheckAdminPermission();
            if (authResult != null)
            {
                return authResult;
            }

            var allowedStatuses = new[] { "Pending", "Completed", "Cancelled" };
            TimeSpan testTime = TimeSpan.Zero;
            decimal score;

            if (string.IsNullOrWhiteSpace(model.Status) || !allowedStatuses.Contains(model.Status))
            {
                ModelState.AddModelError("Status", "Placement test status is invalid.");
            }

            if (string.IsNullOrWhiteSpace(model.TestTime) || !TimeSpan.TryParse(model.TestTime, out testTime))
            {
                ModelState.AddModelError("TestTime", "Test time is invalid.");
            }

            if (!string.IsNullOrWhiteSpace(model.ResultScore)
                && (!decimal.TryParse(model.ResultScore, out score) || score < 0 || score > 100))
            {
                ModelState.AddModelError("ResultScore", "Result score must be from 0 to 100.");
            }

            if (model.Status == "Completed" && string.IsNullOrWhiteSpace(model.ResultScore))
            {
                ModelState.AddModelError("ResultScore", "Result score is required when status is Completed.");
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var placementTest = db.PLACEMENT_TESTs.FirstOrDefault(t => t.TestID == model.TestID);
                if (placementTest == null)
                {
                    TempData["Error"] = "Placement test not found.";
                    return RedirectToAction("PlacementTests", "Admin");
                }

                if (!ModelState.IsValid)
                {
                    FillPlacementTestEditInfo(db, model);
                    TempData["Error"] = "Please check placement test information.";
                    return View(model);
                }

                placementTest.TestDate = model.TestDate.Value.Date;
                placementTest.TestTime = testTime;
                placementTest.Level = (model.Level ?? string.Empty).Trim();
                placementTest.ResultScore = string.IsNullOrWhiteSpace(model.ResultScore) ? null : model.ResultScore.Trim();
                placementTest.Status = model.Status;

                db.SubmitChanges();
            }

            TempData["Success"] = "Placement test updated successfully.";
            return RedirectToAction("PlacementTests", "Admin");
        }

        private static void FillPlacementTestEditInfo(LanguageCenterDataContext db, AdminPlacementTestFormViewModel model)
        {
            var detail = (
                from test in db.PLACEMENT_TESTs
                join st in db.STUDENTs on test.StudentID equals st.StudentID into studentJoin
                from st in studentJoin.DefaultIfEmpty()
                join account in db.USER_ACCOUNTs on st.AccountID equals account.AccountID into accountJoin
                from account in accountJoin.DefaultIfEmpty()
                where test.TestID == model.TestID
                select new
                {
                    StudentName = st != null ? st.FullName : string.Empty,
                    StudentEmail = account != null ? account.Email : string.Empty,
                    PhoneNumber = st != null ? st.PhoneNumber : string.Empty
                })
                .FirstOrDefault();

            if (detail == null)
            {
                return;
            }

            model.StudentName = detail.StudentName;
            model.StudentEmail = detail.StudentEmail;
            model.PhoneNumber = detail.PhoneNumber;
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
