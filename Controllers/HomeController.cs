using System.Configuration;
using System.Linq;
using System.Web.Mvc;
using LanguageCenter.Models;

namespace LanguageCenter.Controllers
{
    public class HomeController : Controller
    {
        private readonly string connectionString =
            ConfigurationManager.ConnectionStrings["LanguageCenterConnectionString"].ConnectionString;

        public ActionResult Index()
        {
            var model = new HomeViewModel();

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                model.FeaturedPrograms = db.PROGRAMs
                    .Where(p => p.IsActive == true)
                    .OrderBy(p => p.ProgramID)
                    .Take(4)
                    .Select(p => new FeaturedProgramViewModel
                    {
                        ProgramID = p.ProgramID,
                        ProgramName = p.ProgramName,
                        Level = p.Level,
                        Duration = p.Duration,
                        Fee = p.Fee,
                        ImageURL = p.ImageURL
                    })
                    .ToList();

                model.NewClasses = (
                    from c in db.CLASSes
                    join p in db.PROGRAMs on c.ProgramID equals p.ProgramID into programJoin
                    from p in programJoin.DefaultIfEmpty()
                    join t in db.TEACHERs on c.TeacherID equals t.TeacherID into teacherJoin
                    from t in teacherJoin.DefaultIfEmpty()
                    join s in db.CLASS_STATUS on c.StatusID equals s.StatusID into statusJoin
                    from s in statusJoin.DefaultIfEmpty()
                    orderby c.StartDate descending
                    select new NewClassViewModel
                    {
                        ClassName = c.ClassName,
                        ProgramName = p != null ? p.ProgramName : string.Empty,
                        TeacherName = t != null ? t.FullName : string.Empty,
                        StatusName = s != null ? s.StatusName : string.Empty,
                        StartDate = c.StartDate
                    })
                    .Take(4)
                    .ToList();

                model.Teachers = (
                    from t in db.TEACHERs
                    join a in db.USER_ACCOUNTs on t.AccountID equals a.AccountID into accountJoin
                    from a in accountJoin.DefaultIfEmpty()
                    orderby t.TeacherID
                    select new TeacherHomeViewModel
                    {
                        FullName = t.FullName,
                        Expertise = t.Expertise,
                        Avatar = a != null ? a.Avatar : string.Empty
                    })
                    .Take(3)
                    .ToList();
            }

            return View(model);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}
