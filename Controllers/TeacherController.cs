using System;
using System.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LanguageCenter.Models;

namespace LanguageCenter.Controllers
{
    public class TeacherController : Controller
    {
        private readonly string connectionString =
            ConfigurationManager.ConnectionStrings["LanguageCenterConnectionString"].ConnectionString;

        public ActionResult Dashboard()
        {
            if (Session["AccountID"] == null || Session["Role"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (Session["Role"].ToString() != "Teacher")
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
                var teacher = db.TEACHERs.FirstOrDefault(t => t.AccountID == accountId);
                if (teacher == null)
                {
                    TempData["Error"] = "Teacher profile not found.";
                    return RedirectToAction("Index", "Home");
                }

                var teacherClassIds = db.CLASSes
                    .Where(c => c.TeacherID == teacher.TeacherID)
                    .Select(c => c.ClassID)
                    .ToList();

                var totalClasses = teacherClassIds.Count;
                var totalStudents = db.REGISTRATIONs.Count(r => teacherClassIds.Contains(r.ClassID));
                var totalSchedules = db.CLASS_SCHEDULEs.Count(s => teacherClassIds.Contains(s.ClassID));
                var recentRegistrations = db.REGISTRATIONs.Count(r => teacherClassIds.Contains(r.ClassID));

                var schedules = (
                    from c in db.CLASSes
                    join p in db.PROGRAMs on c.ProgramID equals p.ProgramID into programJoin
                    from p in programJoin.DefaultIfEmpty()
                    join s in db.CLASS_SCHEDULEs on c.ClassID equals s.ClassID
                    where c.TeacherID == teacher.TeacherID
                    orderby s.DayOfWeek, s.StartTime
                    select new TeacherScheduleViewModel
                    {
                        ClassName = c.ClassName,
                        ProgramName = p != null ? p.ProgramName : string.Empty,
                        DayOfWeek = s.DayOfWeek,
                        StartTime = s.StartTime,
                        EndTime = s.EndTime,
                        Room = s.Room
                    })
                    .ToList();

                var activities = (
                    from r in db.REGISTRATIONs
                    join c in db.CLASSes on r.ClassID equals c.ClassID
                    join st in db.STUDENTs on r.StudentID equals st.StudentID into studentJoin
                    from st in studentJoin.DefaultIfEmpty()
                    where c.TeacherID == teacher.TeacherID
                    orderby r.RegistrationDate descending
                    select new TeacherRecentActivityViewModel
                    {
                        StudentName = st != null ? st.FullName : string.Empty,
                        ClassName = c.ClassName,
                        RegistrationDate = r.RegistrationDate,
                        RegStatus = r.RegStatus
                    })
                    .Take(5)
                    .ToList();

                var recentClasses = (
                    from c in db.CLASSes
                    join p in db.PROGRAMs on c.ProgramID equals p.ProgramID into programJoin
                    from p in programJoin.DefaultIfEmpty()
                    join s in db.CLASS_STATUS on c.StatusID equals s.StatusID into statusJoin
                    from s in statusJoin.DefaultIfEmpty()
                    where c.TeacherID == teacher.TeacherID
                    orderby c.StartDate descending
                    select new TeacherRecentClassViewModel
                    {
                        ClassID = c.ClassID,
                        ClassName = c.ClassName,
                        ProgramName = p != null ? p.ProgramName : string.Empty,
                        StatusName = s != null ? s.StatusName : string.Empty,
                        StartDate = c.StartDate,
                        StudentCount = db.REGISTRATIONs.Count(r => r.ClassID == c.ClassID)
                    })
                    .Take(5)
                    .ToList();

                var model = new TeacherDashboardViewModel
                {
                    FullName = teacher.FullName,
                    Role = Session["Role"].ToString(),
                    AccountID = accountId,
                    TeacherID = teacher.TeacherID,
                    Expertise = teacher.Expertise,
                    TotalClasses = totalClasses,
                    TotalStudents = totalStudents,
                    TotalSchedules = totalSchedules,
                    RecentRegistrations = recentRegistrations,
                    TeachingSchedules = schedules,
                    RecentActivities = activities,
                    RecentClasses = recentClasses
                };

                return View(model);
            }
        }

        public ActionResult MyClasses()
        {
            var authResult = CheckTeacherPermission();
            if (authResult != null)
            {
                return authResult;
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var teacher = GetCurrentTeacher(db);
                if (teacher == null)
                {
                    TempData["Error"] = "Teacher profile not found.";
                    return RedirectToAction("Index", "Home");
                }

                var classes = (
                    from c in db.CLASSes
                    join p in db.PROGRAMs on c.ProgramID equals p.ProgramID into programJoin
                    from p in programJoin.DefaultIfEmpty()
                    join s in db.CLASS_STATUS on c.StatusID equals s.StatusID into statusJoin
                    from s in statusJoin.DefaultIfEmpty()
                    where c.TeacherID == teacher.TeacherID
                    orderby c.StartDate descending
                    select new TeacherMyClassViewModel
                    {
                        ClassID = c.ClassID,
                        ClassName = c.ClassName,
                        ProgramName = p != null ? p.ProgramName : string.Empty,
                        StartDate = c.StartDate,
                        ClassStatus = s != null ? s.StatusName : string.Empty,
                        Schedule = string.Empty,
                        Room = string.Empty,
                        StudentCount = db.REGISTRATIONs.Count(r => r.ClassID == c.ClassID)
                    })
                    .ToList();

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
                    item.Schedule = BuildClassScheduleText(schedules, item.ClassID);
                    item.Room = BuildClassRoomText(schedules, item.ClassID);
                }

                var model = new TeacherMyClassesViewModel
                {
                    Classes = classes
                };

                return View(model);
            }
        }

        public ActionResult ClassStudents(int classId)
        {
            var authResult = CheckTeacherPermission();
            if (authResult != null)
            {
                return authResult;
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var teacher = GetCurrentTeacher(db);
                if (teacher == null)
                {
                    TempData["Error"] = "Teacher profile not found.";
                    return RedirectToAction("Index", "Home");
                }

                var classInfo = (
                    from c in db.CLASSes
                    join p in db.PROGRAMs on c.ProgramID equals p.ProgramID into programJoin
                    from p in programJoin.DefaultIfEmpty()
                    join s in db.CLASS_STATUS on c.StatusID equals s.StatusID into statusJoin
                    from s in statusJoin.DefaultIfEmpty()
                    where c.ClassID == classId
                    select new
                    {
                        Class = c,
                        ProgramName = p != null ? p.ProgramName : string.Empty,
                        StatusName = s != null ? s.StatusName : string.Empty
                    })
                    .FirstOrDefault();

                if (classInfo == null || classInfo.Class.TeacherID != teacher.TeacherID)
                {
                    TempData["Error"] = "You do not have permission to view this class.";
                    return RedirectToAction("MyClasses", "Teacher");
                }

                var students = (
                    from r in db.REGISTRATIONs
                    join st in db.STUDENTs on r.StudentID equals st.StudentID into studentJoin
                    from st in studentJoin.DefaultIfEmpty()
                    join pay in db.PAYMENTs on r.RegistrationID equals pay.RegistrationID into paymentJoin
                    from pay in paymentJoin.DefaultIfEmpty()
                    where r.ClassID == classId
                    orderby st.FullName
                    select new TeacherClassStudentViewModel
                    {
                        StudentID = st != null ? st.StudentID : 0,
                        StudentName = st != null ? st.FullName : string.Empty,
                        PhoneNumber = st != null ? st.PhoneNumber : string.Empty,
                        RegistrationDate = r.RegistrationDate,
                        RegistrationStatus = r.RegStatus,
                        PaymentStatus = pay != null ? pay.PaymentStatus : "No payment",
                        LatestAttendanceStatus = "No attendance yet"
                    })
                    .ToList();

                var registrations = db.REGISTRATIONs
                    .Where(r => r.ClassID == classId)
                    .Select(r => new { r.RegistrationID, r.StudentID })
                    .ToList();

                foreach (var item in students)
                {
                    var registration = registrations.FirstOrDefault(r => r.StudentID == item.StudentID);
                    if (registration != null)
                    {
                        var latestAttendance = db.ATTENDANCEs
                            .Where(a => a.RegistrationID == registration.RegistrationID)
                            .OrderByDescending(a => a.ClassDate)
                            .FirstOrDefault();

                        item.LatestAttendanceStatus = latestAttendance != null && !string.IsNullOrWhiteSpace(latestAttendance.Status)
                            ? latestAttendance.Status
                            : "No attendance yet";
                    }
                }

                var model = new TeacherClassStudentsViewModel
                {
                    ClassID = classInfo.Class.ClassID,
                    ClassName = classInfo.Class.ClassName,
                    ProgramName = classInfo.ProgramName,
                    StartDate = classInfo.Class.StartDate,
                    ClassStatus = classInfo.StatusName,
                    Students = students
                };

                return View(model);
            }
        }

        public ActionResult Materials(int? classId)
        {
            var authResult = CheckTeacherPermission();
            if (authResult != null)
            {
                return authResult;
            }

            if (!classId.HasValue)
            {
                TempData["Error"] = "Please choose a class to manage materials.";
                return RedirectToAction("MyClasses", "Teacher");
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var teacher = GetCurrentTeacher(db);
                if (teacher == null)
                {
                    TempData["Error"] = "Teacher profile not found.";
                    return RedirectToAction("Index", "Home");
                }

                var classInfo = GetTeacherClassInfo(db, classId.Value);
                if (classInfo == null || classInfo.Class.TeacherID != teacher.TeacherID)
                {
                    TempData["Error"] = "You do not have permission to manage this class.";
                    return RedirectToAction("MyClasses", "Teacher");
                }

                var materials = db.CLASS_MATERIALs
                    .Where(m => m.ClassID == classId.Value)
                    .OrderByDescending(m => m.UploadDate)
                    .Select(m => new TeacherMaterialViewModel
                    {
                        MaterialID = m.MaterialID,
                        Title = m.FileName,
                        FileName = m.FileName,
                        FilePath = m.FilePath,
                        UploadDate = m.UploadDate
                    })
                    .ToList();

                var model = new TeacherMaterialsViewModel
                {
                    ClassID = classInfo.Class.ClassID,
                    ClassName = classInfo.Class.ClassName,
                    ProgramName = classInfo.ProgramName,
                    Materials = materials
                };

                return View(model);
            }
        }

        [HttpGet]
        public ActionResult UploadMaterial(int classId)
        {
            return RedirectToAction("Materials", "Teacher", new { classId = classId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UploadMaterial(int classId, string title, HttpPostedFileBase file)
        {
            var authResult = CheckTeacherPermission();
            if (authResult != null)
            {
                return authResult;
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var teacher = GetCurrentTeacher(db);
                if (teacher == null)
                {
                    TempData["Error"] = "Teacher profile not found.";
                    return RedirectToAction("Index", "Home");
                }

                var classInfo = GetTeacherClassInfo(db, classId);
                if (classInfo == null || classInfo.Class.TeacherID != teacher.TeacherID)
                {
                    TempData["Error"] = "You do not have permission to manage this class.";
                    return RedirectToAction("MyClasses", "Teacher");
                }

                if (string.IsNullOrWhiteSpace(title))
                {
                    TempData["Error"] = "Title is required.";
                    return RedirectToAction("Materials", "Teacher", new { classId = classId });
                }

                if (file == null || file.ContentLength <= 0)
                {
                    TempData["Error"] = "Please choose a file to upload.";
                    return RedirectToAction("Materials", "Teacher", new { classId = classId });
                }

                var extension = Path.GetExtension(file.FileName);
                if (!IsAllowedMaterialExtension(extension))
                {
                    TempData["Error"] = "Invalid file type. Please upload pdf, doc, docx, ppt, pptx, or txt.";
                    return RedirectToAction("Materials", "Teacher", new { classId = classId });
                }

                var uploadFolder = Server.MapPath("~/Content/Uploads/Materials");
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                var originalName = Path.GetFileNameWithoutExtension(file.FileName);
                var safeOriginalName = MakeSafeFileName(originalName);
                var storedFileName = string.Format("{0}_{1}{2}", DateTime.Now.Ticks, safeOriginalName, extension);
                var physicalPath = Path.Combine(uploadFolder, storedFileName);
                file.SaveAs(physicalPath);

                var material = new CLASS_MATERIAL
                {
                    ClassID = classId,
                    FileName = title.Trim(),
                    FilePath = "/Content/Uploads/Materials/" + storedFileName,
                    UploadDate = DateTime.Now
                };

                db.CLASS_MATERIALs.InsertOnSubmit(material);
                db.SubmitChanges();
            }

            TempData["Success"] = "Material uploaded successfully.";
            return RedirectToAction("Materials", "Teacher", new { classId = classId });
        }

        public ActionResult DownloadMaterial(int materialId)
        {
            var authResult = CheckTeacherPermission();
            if (authResult != null)
            {
                return authResult;
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var teacher = GetCurrentTeacher(db);
                if (teacher == null)
                {
                    TempData["Error"] = "Teacher profile not found.";
                    return RedirectToAction("Index", "Home");
                }

                var material = db.CLASS_MATERIALs.FirstOrDefault(m => m.MaterialID == materialId);
                if (material == null)
                {
                    TempData["Error"] = "Material not found.";
                    return RedirectToAction("MyClasses", "Teacher");
                }

                var classInfo = db.CLASSes.FirstOrDefault(c => c.ClassID == material.ClassID);
                if (classInfo == null || classInfo.TeacherID != teacher.TeacherID)
                {
                    TempData["Error"] = "You do not have permission to manage this class.";
                    return RedirectToAction("MyClasses", "Teacher");
                }

                var physicalPath = Server.MapPath(GetServerRelativePath(material.FilePath));
                if (!System.IO.File.Exists(physicalPath))
                {
                    TempData["Error"] = "File not found.";
                    return RedirectToAction("Materials", "Teacher", new { classId = material.ClassID });
                }

                var downloadName = material.FileName + Path.GetExtension(physicalPath);
                return File(physicalPath, "application/octet-stream", downloadName);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteMaterial(int materialId)
        {
            var authResult = CheckTeacherPermission();
            if (authResult != null)
            {
                return authResult;
            }

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var teacher = GetCurrentTeacher(db);
                if (teacher == null)
                {
                    TempData["Error"] = "Teacher profile not found.";
                    return RedirectToAction("Index", "Home");
                }

                var material = db.CLASS_MATERIALs.FirstOrDefault(m => m.MaterialID == materialId);
                if (material == null)
                {
                    TempData["Error"] = "Material not found.";
                    return RedirectToAction("MyClasses", "Teacher");
                }

                var classId = material.ClassID;
                var classInfo = db.CLASSes.FirstOrDefault(c => c.ClassID == classId);
                if (classInfo == null || classInfo.TeacherID != teacher.TeacherID)
                {
                    TempData["Error"] = "You do not have permission to manage this class.";
                    return RedirectToAction("MyClasses", "Teacher");
                }

                var physicalPath = Server.MapPath(GetServerRelativePath(material.FilePath));
                if (System.IO.File.Exists(physicalPath))
                {
                    System.IO.File.Delete(physicalPath);
                }

                db.CLASS_MATERIALs.DeleteOnSubmit(material);
                db.SubmitChanges();

                TempData["Success"] = "Material deleted successfully.";
                return RedirectToAction("Materials", "Teacher", new { classId = classId });
            }
        }

        public ActionResult PlacementResults(string search, string status, int page = 1)
        {
            const int pageSize = 10;

            var authResult = CheckTeacherPermission();
            if (authResult != null)
            {
                return authResult;
            }

            if (page < 1)
            {
                page = 1;
            }

            search = (search ?? string.Empty).Trim();
            status = (status ?? string.Empty).Trim();

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var teacher = GetCurrentTeacher(db);
                if (teacher == null)
                {
                    TempData["Error"] = "Teacher profile not found.";
                    return RedirectToAction("Index", "Home");
                }

                var query =
                    from test in db.PLACEMENT_TESTs
                    join student in db.STUDENTs on test.StudentID equals student.StudentID into studentJoin
                    from student in studentJoin.DefaultIfEmpty()
                    select new TeacherPlacementResultViewModel
                    {
                        TestID = test.TestID,
                        StudentName = student != null ? student.FullName : string.Empty,
                        PhoneNumber = student != null ? student.PhoneNumber : string.Empty,
                        TestDate = test.TestDate,
                        TestTime = test.TestTime,
                        Level = test.Level,
                        ResultScore = test.ResultScore,
                        Status = test.Status
                    };

                var statuses = db.PLACEMENT_TESTs
                    .Where(x => x.Status != null && x.Status != string.Empty)
                    .Select(x => x.Status)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(x =>
                        x.StudentName.Contains(search) ||
                        x.Level.Contains(search));
                }

                if (!string.IsNullOrWhiteSpace(status))
                {
                    query = query.Where(x => x.Status == status);
                }

                var totalItems = query.Count();
                var totalPages = (int)System.Math.Ceiling(totalItems / (double)pageSize);
                if (totalPages < 1)
                {
                    totalPages = 1;
                }

                if (page > totalPages)
                {
                    page = totalPages;
                }

                var results = query
                    .OrderByDescending(x => x.TestDate)
                    .ThenByDescending(x => x.TestTime)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var model = new TeacherPlacementResultsPageViewModel
                {
                    Results = results,
                    Statuses = statuses,
                    Search = search,
                    Status = status,
                    CurrentPage = page,
                    TotalPages = totalPages
                };

                return View(model);
            }
        }

        public ActionResult StudentFeedback(string search, int? rating, int page = 1)
        {
            const int pageSize = 10;

            var authResult = CheckTeacherPermission();
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
                var teacher = GetCurrentTeacher(db);
                if (teacher == null)
                {
                    TempData["Error"] = "Teacher profile not found.";
                    return RedirectToAction("Index", "Home");
                }

                var teacherFeedbackQuery =
                    from feedback in db.STUDENT_FEEDBACKs
                    join c in db.CLASSes on feedback.ClassID equals c.ClassID
                    join student in db.STUDENTs on feedback.StudentID equals student.StudentID into studentJoin
                    from student in studentJoin.DefaultIfEmpty()
                    where c.TeacherID == teacher.TeacherID
                    select new TeacherStudentFeedbackViewModel
                    {
                        FeedbackID = feedback.FeedbackID,
                        StudentName = student != null ? student.FullName : string.Empty,
                        Rating = feedback.Rating,
                        Comment = feedback.FeedbackContent,
                        FeedbackDate = feedback.FeedbackDate
                    };

                var totalFeedback = teacherFeedbackQuery.Count();
                var averageRating = totalFeedback > 0
                    ? teacherFeedbackQuery.Where(x => x.Rating.HasValue).Average(x => (double?)x.Rating.Value) ?? 0
                    : 0;

                var ratings = teacherFeedbackQuery
                    .Where(x => x.Rating.HasValue)
                    .Select(x => x.Rating.Value)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                var query = teacherFeedbackQuery;

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(x =>
                        x.StudentName.Contains(search) ||
                        x.Comment.Contains(search));
                }

                if (rating.HasValue)
                {
                    query = query.Where(x => x.Rating == rating.Value);
                }

                var totalItems = query.Count();
                var totalPages = (int)System.Math.Ceiling(totalItems / (double)pageSize);
                if (totalPages < 1)
                {
                    totalPages = 1;
                }

                if (page > totalPages)
                {
                    page = totalPages;
                }

                var feedbacks = query
                    .OrderByDescending(x => x.FeedbackDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var model = new TeacherStudentFeedbackPageViewModel
                {
                    Feedbacks = feedbacks,
                    Ratings = ratings,
                    Search = search,
                    Rating = rating,
                    CurrentPage = page,
                    TotalPages = totalPages,
                    TotalFeedback = totalFeedback,
                    AverageRating = averageRating
                };

                return View(model);
            }
        }

        private ActionResult CheckTeacherPermission()
        {
            if (Session["AccountID"] == null || Session["Role"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (Session["Role"].ToString() != "Teacher")
            {
                TempData["Error"] = "You do not have permission to access this page.";
                return RedirectToAction("Index", "Home");
            }

            return null;
        }

        private TEACHER GetCurrentTeacher(LanguageCenterDataContext db)
        {
            int accountId;
            if (Session["AccountID"] == null || !int.TryParse(Session["AccountID"].ToString(), out accountId))
            {
                return null;
            }

            return db.TEACHERs.FirstOrDefault(t => t.AccountID == accountId);
        }

        private TeacherClassInfo GetTeacherClassInfo(LanguageCenterDataContext db, int classId)
        {
            return (
                from c in db.CLASSes
                join p in db.PROGRAMs on c.ProgramID equals p.ProgramID into programJoin
                from p in programJoin.DefaultIfEmpty()
                where c.ClassID == classId
                select new TeacherClassInfo
                {
                    Class = c,
                    ProgramName = p != null ? p.ProgramName : string.Empty
                })
                .FirstOrDefault();
        }

        private static bool IsAllowedMaterialExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return false;
            }

            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".txt" };
            return allowedExtensions.Contains(extension.ToLower());
        }

        private static string MakeSafeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return "material";
            }

            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(invalidChar, '_');
            }

            return fileName;
        }

        private static string GetServerRelativePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return "~/";
            }

            return filePath.StartsWith("/")
                ? "~" + filePath
                : filePath;
        }

        private static string BuildClassScheduleText(Dictionary<int, List<CLASS_SCHEDULE>> schedules, int classId)
        {
            if (!schedules.ContainsKey(classId) || !schedules[classId].Any())
            {
                return "No schedule";
            }

            var items = schedules[classId].Select(s =>
                string.Format(
                    "{0} {1:hh\\:mm} - {2:hh\\:mm} {3}",
                    s.DayOfWeek,
                    s.StartTime,
                    s.EndTime,
                    string.IsNullOrWhiteSpace(s.Room) ? string.Empty : s.Room));

            return string.Join("\n", items);
        }

        private static string BuildClassRoomText(Dictionary<int, List<CLASS_SCHEDULE>> schedules, int classId)
        {
            if (!schedules.ContainsKey(classId) || !schedules[classId].Any())
            {
                return "No room";
            }

            var rooms = schedules[classId]
                .Where(s => !string.IsNullOrWhiteSpace(s.Room))
                .Select(s => s.Room)
                .Distinct()
                .ToList();

            return rooms.Any() ? string.Join(", ", rooms) : "No room";
        }

        private class TeacherClassInfo
        {
            public CLASS Class { get; set; }
            public string ProgramName { get; set; }
        }
    }
}
