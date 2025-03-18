using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportBuilder.Web.Helper;
using ReportBuilder.Web.Models;

namespace ReportBuilder.Web.Controllers
{
    public class DotNetSetupController : Controller
    {
        [CustomAuthorize(ClaimsStore.AllowSetupPageAccess)]
        public async Task<IActionResult> Index(string databaseApiKey = "")
        {
            return View();
        }
        [CustomAuthorize(ClaimsStore.AllowManageUsersAndRoles)]
        public async Task<IActionResult> UsersAndRoles(string databaseApiKey = "")
        {
            return View();
        }

    }
}