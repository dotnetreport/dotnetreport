using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportBuilder.Web.Models;

namespace ReportBuilder.Web.Controllers
{
    [Authorize(Roles = DotNetReportRoles.DotNetReportAdmin)]
    public class DotNetSetupController : Controller
    {
        public async Task<IActionResult> Index(string databaseApiKey = "")
        {
            return View();
        }

        public async Task<IActionResult> UsersAndRoles(string databaseApiKey = "")
        {
            return View();
        }

    }
}