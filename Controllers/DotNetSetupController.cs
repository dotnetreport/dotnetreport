using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ReportBuilder.Web.Models;

namespace ReportBuilder.Web.Controllers
{
    //[Authorize(Roles="Administrator")]
    public class DotNetSetupController : Controller
    {
        private readonly IConfiguration _configuration;
        public DotNetSetupController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);
            var api = new DotNetReportApiController(_configuration) { ControllerContext = ControllerContext };
            var settings = api.GetSettings();
            ViewBag.AllowAdminMode = settings.CanUseAdminMode;
            ViewBag.ShowUsersPage = CanOpenUsersPage(settings, api.GetUserPermissions(settings));
        }

        // Admins, or Manage Users for their own Client Id
        internal static bool CanOpenUsersPage(DotNetReportSettings settings, List<string> permissions)
        {
            return settings.CanUseAdminMode || permissions.Contains(DotNetReportPermissions.ManageUsers);
        }

        public async Task<IActionResult> Index(string databaseApiKey = "")
        {
            return View();
        }

        public IActionResult Users()
        {
            if (ViewBag.ShowUsersPage != true)
            {
                return StatusCode(403, "You are not authorized to view this page");
            }
            return View();
        }
    }
}
