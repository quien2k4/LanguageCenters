using System.Web;
using System.Web.Mvc;

namespace LanguageCenter.Helpers
{
    public static class AuthHelper
    {
        public const string PermissionErrorMessage = "Bạn không có quyền truy cập chức năng này.";

        public static bool IsLoggedIn(HttpSessionStateBase session)
        {
            return session != null
                && session["AccountID"] != null
                && session["Role"] != null;
        }

        public static bool IsInRole(HttpSessionStateBase session, string role)
        {
            return IsLoggedIn(session)
                && session["Role"].ToString() == role;
        }

        public static int? GetCurrentAccountId(HttpSessionStateBase session)
        {
            int accountId;
            if (session == null || session["AccountID"] == null || !int.TryParse(session["AccountID"].ToString(), out accountId))
            {
                return null;
            }

            return accountId;
        }

        public static ActionResult RequireRole(Controller controller, string role)
        {
            if (!IsLoggedIn(controller.Session))
            {
                return new RedirectToRouteResult(new System.Web.Routing.RouteValueDictionary(
                    new { controller = "Account", action = "Login" }));
            }

            if (!IsInRole(controller.Session, role))
            {
                controller.TempData["Error"] = PermissionErrorMessage;
                return new RedirectToRouteResult(new System.Web.Routing.RouteValueDictionary(
                    new { controller = "Home", action = "Index" }));
            }

            return null;
        }
    }
}

