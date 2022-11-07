using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ReportBuilder.Web.Models;
using System.Web;

namespace ReportBuilder.Web.Controllers
{
    public class DotNetReportController : Controller
    {

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Report(int reportId, string reportName, string reportDescription, bool includeSubTotal, bool showUniqueRecords,
            bool aggregateReport, bool showDataWithGraph, string reportSql, string connectKey, string reportFilter, string reportType, int selectedFolder, string reportSeries)
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

        public async Task<ActionResult> ReportLink(int reportId, int? filterId = null, string filterValue = "", bool adminMode = false)
        {
            var model = new DotNetReportModel();
            var settings = new DotNetReportSettings(); //GetSettings();

            using (var client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("account", settings.AccountApiToken),
                    new KeyValuePair<string, string>("dataConnect", settings.DataConnectApiToken),
                    new KeyValuePair<string, string>("clientId", settings.ClientId),
                    new KeyValuePair<string, string>("userId", settings.UserId),
                    new KeyValuePair<string, string>("userRole", String.Join(",", settings.CurrentUserRole)),
                    new KeyValuePair<string, string>("reportId", reportId.ToString()),
                    new KeyValuePair<string, string>("filterId", filterId.HasValue ? filterId.ToString() : ""),
                    new KeyValuePair<string, string>("filterValue", filterValue.ToString()),
                    new KeyValuePair<string, string>("adminMode", adminMode.ToString()),
                    new KeyValuePair<string, string>("dataFilters", JsonConvert.SerializeObject(settings.DataFilters))
                });

                var response = await client.PostAsync(new Uri(settings.ApiUrl + $"/ReportApi/RunLinkedReport"), content);
                var stringContent = await response.Content.ReadAsStringAsync();

                model = JsonConvert.DeserializeObject<DotNetReportModel>(stringContent);
                
            }

            return View("Report", model);
        }

        public IActionResult ReportPrint(int reportId, string reportName, string reportDescription, string reportSql, string connectKey, string reportFilter, string reportType,
            int selectedFolder = 0, bool includeSubTotal = true, bool showUniqueRecords = false, bool aggregateReport = false, bool showDataWithGraph = true,
            string userId = null, string clientId = null, string currentUserRole = null, string dataFilters = "",
            string reportSeries = "", bool expandAll = false)
        {
            TempData["reportPrint"] = "true";
            TempData["userId"] = userId;
            TempData["clientId"] = clientId;
            TempData["currentUserRole"] = currentUserRole;
            TempData["dataFilters"] = dataFilters;

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
                ReportFilter = reportFilter, // json data to setup filter correctly again
                ExpandAll = expandAll
            };

            return View(model);
        }

        public async Task<IActionResult> Dashboard(int? id = null, bool adminMode = false)
        {
            return View(new DotNetDashboardModel());
        }

        
        [HttpPost]
        public IActionResult DownloadExcel(string reportSql, string connectKey, string reportName, bool allExpanded, string expandSqls, string columnDetails = null, bool includeSubtotal = false)
        {
            reportSql = HttpUtility.HtmlDecode(reportSql);
            var columns = columnDetails == null ? new List<ReportHeaderColumn>() :  JsonConvert.DeserializeObject<List<ReportHeaderColumn>>(HttpUtility.UrlDecode(columnDetails));
            
            var excel = DotNetReportHelper.GetExcelFile(reportSql, connectKey, HttpUtility.UrlDecode(reportName), allExpanded, HttpUtility.UrlDecode(expandSqls)?.Split(',').ToList(), columns, includeSubtotal);
            Response.Headers.Add("content-disposition", "attachment; filename=" + reportName + ".xlsx");
            Response.ContentType = "application/vnd.ms-excel";

            return File(excel, "application/vnd.ms-excel", reportName + ".xlsx");
        }

        [HttpPost]
        public IActionResult DownloadXml(string reportSql, string connectKey, string reportName)
        {
            reportSql = HttpUtility.HtmlDecode(reportSql);
            var xml = DotNetReportHelper.GetXmlFile(reportSql, HttpUtility.UrlDecode(connectKey), HttpUtility.UrlDecode(reportName));
            Response.Headers.Add("content-disposition", "attachment; filename=" + HttpUtility.UrlDecode(reportName) + ".xml");
            Response.ContentType = "application/xml";

            return File(xml, "application/xml", reportName + ".xml");
        }

        [HttpPost]
        public async Task<IActionResult> DownloadPdf(string printUrl, int reportId, string reportSql, string connectKey, string reportName, bool expandAll)
        {
            reportSql = HttpUtility.HtmlDecode(reportSql);
            var settings = new DotNetReportSettings(); // GetSettings();
            var dataFilters = settings.DataFilters != null ? JsonConvert.SerializeObject(settings.DataFilters) : "";
            var pdf = await DotNetReportHelper.GetPdfFile(HttpUtility.UrlDecode(printUrl), reportId, reportSql, HttpUtility.UrlDecode(connectKey), HttpUtility.UrlDecode(reportName), settings.UserId, settings.ClientId, string.Join(",", settings.CurrentUserRole), dataFilters, expandAll);
            return File(pdf, "application/pdf", reportName + ".pdf");
        }
        
        [HttpPost]
        public IActionResult DownloadCsv(string reportSql, string connectKey, string reportName)
        {
            reportSql = HttpUtility.HtmlDecode(reportSql);
            var csv = DotNetReportHelper.GetCSVFile(reportSql, HttpUtility.UrlDecode(connectKey));

            Response.Headers.Add("content-disposition", "attachment; filename=" + HttpUtility.UrlDecode(reportName) + ".csv");
            Response.ContentType = "text/csv";

            return File(csv, "text/csv", reportName + ".csv");
        }

    }
}



