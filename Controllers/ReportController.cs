using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ReportBuilder.Web.Controllers
{
    public class ReportController : Controller
    {
        // GET: Report
        public ActionResult Index()
        {
            return RedirectToAction("Index", "DotNetReport");
        }

        public ActionResult Dashboard()
        {
            return RedirectToAction("Dashboard", "DotNetReport");
        }
    }
}