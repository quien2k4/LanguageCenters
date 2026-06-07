using System;
using System.Configuration;
using System.Linq;
using System.Web.Mvc;
using LanguageCenter.Models;

namespace LanguageCenter.Controllers
{
    public class ProgramController : Controller
    {
        private const int PageSize = 6;

        private readonly string connectionString =
            ConfigurationManager.ConnectionStrings["LanguageCenterConnectionString"].ConnectionString;

        public ActionResult Index(string search, string level, int page = 1)
        {
            if (page < 1)
            {
                page = 1;
            }

            search = (search ?? string.Empty).Trim();
            level = (level ?? string.Empty).Trim();

            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var query = db.PROGRAMs.Where(p => p.IsActive == true);

                var levels = query
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

                var totalItems = query.Count();
                var totalPages = (int)Math.Ceiling(totalItems / (double)PageSize);

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
                    .Skip((page - 1) * PageSize)
                    .Take(PageSize)
                    .Select(p => new ProgramItemViewModel
                    {
                        ProgramID = p.ProgramID,
                        ProgramName = p.ProgramName,
                        Level = p.Level,
                        Duration = p.Duration,
                        Fee = p.Fee,
                        ImageURL = p.ImageURL
                    })
                    .ToList();

                var model = new ProgramListViewModel
                {
                    Programs = programs,
                    Levels = levels,
                    Search = search,
                    Level = level,
                    CurrentPage = page,
                    TotalPages = totalPages
                };

                return View(model);
            }
        }

        public ActionResult Detail(int id)
        {
            using (var db = new LanguageCenterDataContext(connectionString))
            {
                var program = db.PROGRAMs.FirstOrDefault(p => p.ProgramID == id && p.IsActive == true);

                if (program == null)
                {
                    TempData["Error"] = "Program not found.";
                    return RedirectToAction("Index");
                }

                var model = new ProgramDetailViewModel
                {
                    ProgramID = program.ProgramID,
                    ProgramName = program.ProgramName,
                    Description = program.Description,
                    OutputStandard = program.OutputStandard,
                    Level = program.Level,
                    Duration = program.Duration,
                    Fee = program.Fee,
                    ImageURL = program.ImageURL,
                    CurrentRole = Session["Role"] != null ? Session["Role"].ToString() : string.Empty
                };

                model.RelatedClasses = (
                    from c in db.CLASSes
                    join t in db.TEACHERs on c.TeacherID equals t.TeacherID into teacherJoin
                    from t in teacherJoin.DefaultIfEmpty()
                    join s in db.CLASS_STATUS on c.StatusID equals s.StatusID into statusJoin
                    from s in statusJoin.DefaultIfEmpty()
                    where c.ProgramID == id
                    orderby c.StartDate descending
                    select new RelatedClassViewModel
                    {
                        ClassID = c.ClassID,
                        ClassName = c.ClassName,
                        TeacherName = t != null ? t.FullName : string.Empty,
                        StatusName = s != null ? s.StatusName : string.Empty,
                        StartDate = c.StartDate
                    })
                    .ToList();

                return View(model);
            }
        }
    }
}
