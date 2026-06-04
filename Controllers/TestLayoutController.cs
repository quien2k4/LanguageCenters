using System.Web.Mvc;

namespace LanguageCenterWeb.Controllers
{
    public class TestLayoutController : Controller
    {
        public ActionResult Public()
        {
            ViewBag.Title = "Public Layout";
            return View();
        }

        public ActionResult Student()
        {
            Session["FullName"] = "Nguyễn Tấn Quyền";
            Session["Avatar"] = null;
            ViewBag.Title = "Student Layout";
            return View();
        }

        public ActionResult Teacher()
        {
            Session["FullName"] = "Trịnh Công Nhật";
            Session["Avatar"] = null;
            ViewBag.Title = "Teacher Layout";
            return View();
        }

        public ActionResult Admin()
        {
            Session["FullName"] = "Admin";
            Session["Avatar"] = null;
            ViewBag.Title = "Admin Layout";
            return View();
        }
    }
}