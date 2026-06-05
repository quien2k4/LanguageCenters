using System.Configuration;
using System.Linq;
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
    }
}
