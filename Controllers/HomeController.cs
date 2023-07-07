using Microsoft.AspNetCore.Mvc;

namespace ReportBuilder.Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
