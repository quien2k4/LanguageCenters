using System.Web.Mvc;

namespace LanguageCenter.Controllers
{
    public class TeacherController : Controller
    {
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

            ViewBag.FullName = Session["FullName"] != null ? Session["FullName"].ToString() : string.Empty;
            ViewBag.Role = Session["Role"].ToString();
            ViewBag.AccountID = Session["AccountID"].ToString();

            return View();
        }
    }
}
