using System.Threading.Tasks;
using System.Web.Mvc;

namespace ReportBuilder.Web.Controllers
{
    //[Authorize(Roles="Administrator")]
    public class DotNetSetupController : Controller
    {
        public async Task<ActionResult> Index(string databaseApiKey = "")
        {           
            return View();
        }
    }
}