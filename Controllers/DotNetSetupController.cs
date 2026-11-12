using ReportBuilder.Web.Models;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace ReportBuilder.Web.Controllers
{
    //[Authorize(Roles="Administrator")]
    public class DotNetSetupController : Controller
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);
            var api = new DotNetReportApiController();
            var settings = api.GetSettings();
            ViewBag.AllowAdminMode = settings.CanUseAdminMode;
            ViewBag.ShowUsersPage = CanOpenUsersPage(settings, api.GetUserPermissions(settings));
        }

        // Admins, or Manage Users for their own Client Id
        internal static bool CanOpenUsersPage(DotNetReportSettings settings, List<string> permissions)
        {
            return settings.CanUseAdminMode || permissions.Contains(DotNetReportPermissions.ManageUsers);
        }

        public async Task<ActionResult> Index(string databaseApiKey = "")
        {
            return View();
        }

        public ActionResult Users()
        {
            if (ViewBag.ShowUsersPage != true)
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden, "You are not authorized to view this page");
            }
            return View();
        }
    }
}
