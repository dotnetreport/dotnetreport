using Microsoft.AspNetCore.Mvc;

namespace ReportBuilder.Web.Controllers
{
    //[Authorize(Roles="Administrator")]
    public class DotNetSetupController : Controller
    {
        public async Task<IActionResult> Index(string databaseApiKey = "")
        {
            return View();
        }
    }
}