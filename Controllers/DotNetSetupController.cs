using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReportBuilder.Web.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Net.Http;
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