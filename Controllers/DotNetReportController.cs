using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportBuilder.Web.Models;
using System.Web;

namespace ReportBuilder.Web.Controllers
{
    //[Authorize]
    public class DotNetReportController : Controller
    {

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Report(int reportId, string reportName, string reportDescription, bool includeSubTotal = false, bool showUniqueRecords = false,
            bool aggregateReport = false, bool showDataWithGraph = false, string reportSql = "", string connectKey = "", string reportFilter = "", string reportType = "", int selectedFolder = 0, string reportSeries = "")
        {            
            var model = new DotNetReportModel
            {
                ReportId = reportId,
                ReportType = reportType,
                ReportName = HttpUtility.UrlDecode(reportName),
                ReportDescription = HttpUtility.UrlDecode(reportDescription),
                ReportSql = reportSql,
                ConnectKey = connectKey,
                IncludeSubTotals = includeSubTotal,
                ShowUniqueRecords = showUniqueRecords,
                ShowDataWithGraph = showDataWithGraph,
                SelectedFolder = selectedFolder,
                ReportSeries = !string.IsNullOrEmpty(reportSeries) ? reportSeries.Replace("%20", " ") : string.Empty,
                ReportFilter = reportFilter // json data to setup filter correctly again
            };

            return View(model);
        }

        [AllowAnonymous]
        public IActionResult ReportPrint(string reportName, string reportDescription, string reportFilter, string reportType,
            int selectedFolder = 0, bool includeSubTotal = true, bool showUniqueRecords = false, bool aggregateReport = false, bool showDataWithGraph = true,
            string reportSeries = "", bool expandAll = false, string reportData = "", string exportId = "")
        {
            var session = ExportSessionStore.Get(exportId);
            if (session == null)
            {
                return Unauthorized();
            }

            var settings = session.Settings;
            var reportId = session.ReportId;
            var reportSql = session.ReportSql;
            var connectKey = session.ConnectKey;
            var dataFilters = System.Text.Json.JsonSerializer.Serialize(settings.DataFilters ?? new { });

            ViewBag.ExportId = exportId;

            var sanitizer = new Ganss.Xss.HtmlSanitizer
            {
                AllowedSchemes = { "data" }, // allow base64 images
                AllowedTags = { "b", "i", "u", "p", "span", "div", "img", "table", "tr", "td" },
                AllowedAttributes = { "style", "class", "src", "alt", "width", "height" }
            };

            var model = new DotNetReportPrintModel
            {
                ReportId = reportId,
                ReportType = reportType,
                ReportName = HttpUtility.UrlDecode(reportName),
                ReportDescription = HttpUtility.UrlDecode(reportDescription),
                ReportSql = reportSql,
                ConnectKey = connectKey,
                IncludeSubTotals = includeSubTotal,
                ShowUniqueRecords = showUniqueRecords,
                ShowDataWithGraph = showDataWithGraph,
                SelectedFolder = selectedFolder,
                ReportFilter = reportFilter, // json data to setup filter correctly again
                ExpandAll = expandAll,
                
                ClientId = settings.ClientId,
                UserId = settings.UserId,
                CurrentUserRoles = string.Join(",", settings.CurrentUserRole ?? new List<string>()),
                DataFilters = dataFilters,
                ReportData = sanitizer.Sanitize(HttpUtility.UrlDecode(reportData))
            };

            return View(model);
        }

        public async Task<IActionResult> Dashboard(int? id = null, bool adminMode = false)
        {
            return View();
        }

        public IActionResult Query()
        {
            return View();
        }
    }
}



