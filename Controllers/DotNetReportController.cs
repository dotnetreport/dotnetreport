using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
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

        public IActionResult ReportPrint(int reportId, string reportName, string reportDescription, string reportSql, string connectKey, string reportFilter, string reportType,
            int selectedFolder = 0, bool includeSubTotal = true, bool showUniqueRecords = false, bool aggregateReport = false, bool showDataWithGraph = true,
            string userId = null, string clientId = null, string currentUserRole = null, string dataFilters = "",
            string reportSeries = "", bool expandAll = false)
        {
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
                
                ClientId = clientId,    
                UserId = userId,
                CurrentUserRoles = currentUserRole,
                DataFilters = dataFilters
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


        [HttpPost]
        public IActionResult DownloadExcel(string reportSql, string connectKey, string reportName, bool allExpanded, string expandSqls, string columnDetails = null, bool includeSubtotal = false, bool pivot = false)
        {
            reportSql = HttpUtility.HtmlDecode(reportSql);
            var columns = columnDetails == null ? new List<ReportHeaderColumn>() :  JsonConvert.DeserializeObject<List<ReportHeaderColumn>>(HttpUtility.UrlDecode(columnDetails));
            
            var excel = DotNetReportHelper.GetExcelFile(reportSql, connectKey, HttpUtility.UrlDecode(reportName), allExpanded, HttpUtility.UrlDecode(expandSqls), columns, includeSubtotal, pivot);
            Response.Headers.Add("content-disposition", "attachment; filename=" + reportName + ".xlsx");
            Response.ContentType = "application/vnd.ms-excel";

            return File(excel, "application/vnd.ms-excel", reportName + ".xlsx");
        }

        [HttpPost]
        public IActionResult DownloadXml(string reportSql, string connectKey, string reportName)
        {
            reportSql = HttpUtility.HtmlDecode(reportSql);
            string xml = DotNetReportHelper.GetXmlFile(reportSql, HttpUtility.UrlDecode(connectKey), HttpUtility.UrlDecode(reportName));           
            var data = System.Text.Encoding.UTF8.GetBytes(xml);
            Response.ContentType = "text/txt";
            return File(data, "text/txt", reportName + ".xml");
        }

        [HttpPost]
        public async Task<IActionResult> DownloadPdf(string printUrl, int reportId, string reportSql, string connectKey, string reportName, bool expandAll,
                                                        string clientId = null, string userId = null, string userRoles = null, string dataFilters = "")
        {
            reportSql = HttpUtility.HtmlDecode(reportSql);
            var pdf = await DotNetReportHelper.GetPdfFile(HttpUtility.UrlDecode(printUrl), reportId, reportSql, HttpUtility.UrlDecode(connectKey), HttpUtility.UrlDecode(reportName), 
                                userId, clientId, userRoles, dataFilters, expandAll);

            return File(pdf, "application/pdf", reportName + ".pdf");
        }


        [HttpPost]
        public async Task<IActionResult> DownloadPdfAlt(string reportSql, string connectKey, string reportName, string chartData = null, string columnDetails = null, bool includeSubtotal = false, bool pivot = false)
        {
            reportSql = HttpUtility.HtmlDecode(reportSql);
            chartData = HttpUtility.UrlDecode(chartData);
            chartData = chartData?.Replace(" ", " +");
            reportName = HttpUtility.UrlDecode(reportName);
            var columns = columnDetails == null ? new List<ReportHeaderColumn>() : JsonConvert.DeserializeObject<List<ReportHeaderColumn>>(HttpUtility.UrlDecode(columnDetails));

            var pdf = DotNetReportHelper.GetPdfFileAlt(reportSql, connectKey, reportName, chartData, columns, includeSubtotal, pivot);

            return File(pdf, "application/pdf", reportName + ".pdf");
        }

        [HttpPost]
        public IActionResult DownloadCsv(string reportSql, string connectKey, string reportName, string columnDetails = null, bool includeSubtotal = false)
        {
            var columns = columnDetails == null ? new List<ReportHeaderColumn>() : JsonConvert.DeserializeObject<List<ReportHeaderColumn>>(HttpUtility.UrlDecode(columnDetails));
            reportSql = HttpUtility.HtmlDecode(reportSql);
            var csv = DotNetReportHelper.GetCSVFile(reportSql, HttpUtility.UrlDecode(connectKey), columns, includeSubtotal);

            Response.Headers.Add("content-disposition", "attachment; filename=" + HttpUtility.UrlDecode(reportName) + ".csv");
            Response.ContentType = "text/csv";

            return File(csv, "text/csv", reportName + ".csv");
        }

    }
}



