using Microsoft.AspNetCore.Authorization;
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

        [AllowAnonymous]
        public IActionResult ReportPrint(int reportId, string reportName, string reportDescription, string reportSql, string connectKey, string reportFilter, string reportType,
            int selectedFolder = 0, bool includeSubTotal = true, bool showUniqueRecords = false, bool aggregateReport = false, bool showDataWithGraph = true,
            string userId = null, string clientId = null, string currentUserRole = null, string dataFilters = "",
            string reportSeries = "", bool expandAll = false, string reportData = "")
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
                DataFilters = HttpUtility.UrlDecode(dataFilters),
                ReportData = HttpUtility.UrlDecode(reportData)
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
        public async Task<IActionResult> DownloadExcel(string reportSql, string connectKey, string reportName, bool allExpanded, string expandSqls, string chartData = null, string columnDetails = null, bool includeSubtotal = false, bool pivot = false,string pivotColumn=null, string pivotFunction = null,string onlyAndGroupInColumnDetail = null)
        {
            reportSql = HttpUtility.HtmlDecode(reportSql);
            chartData = HttpUtility.UrlDecode(chartData);
            chartData = chartData?.Replace(" ", " +");
            var columns = string.IsNullOrEmpty(columnDetails) ? new List<ReportHeaderColumn>() :  JsonConvert.DeserializeObject<List<ReportHeaderColumn>>(HttpUtility.UrlDecode(columnDetails));
            var onlyAndGroupInDetailColumns = string.IsNullOrEmpty(onlyAndGroupInColumnDetail) ? new List<ReportHeaderColumn>() :  JsonConvert.DeserializeObject<List<ReportHeaderColumn>>(HttpUtility.UrlDecode(onlyAndGroupInColumnDetail));
            
            var excel = await DotNetReportHelper.GetExcelFile(reportSql, connectKey, HttpUtility.UrlDecode(reportName), chartData, allExpanded, HttpUtility.UrlDecode(expandSqls), columns, includeSubtotal, pivot, pivotColumn, pivotFunction, onlyAndGroupInDetailColumns);
            Response.Headers.Add("content-disposition", "attachment; filename=" + reportName + ".xlsx");
            Response.ContentType = "application/vnd.ms-excel";

            return File(excel, "application/vnd.ms-excel", reportName + ".xlsx");
        }

        [HttpPost]
        public async Task<IActionResult> DownloadXml(string reportSql, string connectKey, string reportName, string expandSqls= null, string pivotColumn = null, string pivotFunction = null)
        {
            reportSql = HttpUtility.HtmlDecode(reportSql);
            string xml = await DotNetReportHelper.GetXmlFile(reportSql, HttpUtility.UrlDecode(connectKey), HttpUtility.UrlDecode(reportName),expandSqls,pivotColumn,pivotFunction);           
            var data = System.Text.Encoding.UTF8.GetBytes(xml);
            Response.ContentType = "text/txt";
            return File(data, "text/txt", reportName + ".xml");
        }

        [HttpPost]
        public async Task<IActionResult> DownloadPdf(string printUrl, int reportId, string reportSql, string connectKey, string reportName, bool expandAll,
                                                                    string clientId = null, string userId = null, string userRoles = null, string dataFilters = "", string expandSqls = null, string pivotColumn = null, string pivotFunction = null, bool debug = false)
        {
            reportSql = HttpUtility.HtmlDecode(reportSql);
            var pdf = await DotNetReportHelper.GetPdfFile(HttpUtility.UrlDecode(printUrl), reportId, reportSql, HttpUtility.UrlDecode(connectKey), HttpUtility.UrlDecode(reportName),
                                userId, clientId, userRoles, dataFilters, expandAll, expandSqls, pivotColumn, pivotFunction, false, debug);

            return File(pdf, "application/pdf", reportName + ".pdf");
        }

        [HttpPost]
        public async Task<IActionResult> DownloadAllPdf(string reportdata)
        {
            var pdfBytesList = new List<byte[]>();
            var ListofReports = reportdata != null ? JsonConvert.DeserializeObject<List<ExportReportModel>>(reportdata) :null;
            foreach (var report in ListofReports)
            {
                var pdf = await DotNetReportHelper.GetPdfFile(report.printUrl,report.reportId,HttpUtility.HtmlDecode(report.reportSql),HttpUtility.UrlDecode(report.connectKey),HttpUtility.UrlDecode(report.reportName),report.userId,
                    report.clientId,report.userRoles,report.dataFilters,report.expandAll,report.expandSqls,report.pivotColumn,report.pivotFunction);
                pdfBytesList.Add(pdf);
            }
            var combinedPdf = DotNetReportHelper.GetCombinePdfFile(pdfBytesList);
            return File(combinedPdf, "application/pdf", "CombinedReports.pdf");
        }
        [HttpPost]
        public async Task<IActionResult> DownloadAllExcel(string reportdata)
        {
            var excelbyteList = new List<byte[]>();
            var ListofReports = reportdata != null ? JsonConvert.DeserializeObject<List<ExportReportModel>>(reportdata) : null;
            foreach (var report in ListofReports)
            {
                report.reportSql = HttpUtility.HtmlDecode(report.reportSql);
                report.chartData= HttpUtility.UrlDecode(report.chartData)?.Replace(" ", " +");
                var columns = report.columnDetails == null ? new List<ReportHeaderColumn>() : JsonConvert.DeserializeObject<List<ReportHeaderColumn>>(HttpUtility.UrlDecode(report.columnDetails));
                var onlyAndGroupInDetailColumns = string.IsNullOrEmpty(report.onlyAndGroupInColumnDetail) ? new List<ReportHeaderColumn>() : JsonConvert.DeserializeObject<List<ReportHeaderColumn>>(HttpUtility.UrlDecode(report.onlyAndGroupInColumnDetail));
                var excelreport = await DotNetReportHelper.GetExcelFile(report.reportSql, report.connectKey, HttpUtility.UrlDecode(report.reportName), report.chartData, report.expandAll, HttpUtility.UrlDecode(report.expandSqls), columns, report.includeSubTotal, report.pivot, report.pivotColumn, report.pivotFunction, onlyAndGroupInDetailColumns);
                excelbyteList.Add(excelreport);
            }
            // Combine all Excel files into one workbook
            var combinedExcel = DotNetReportHelper.GetCombineExcelFile(excelbyteList, ListofReports.Select(r => r.reportName).ToList());
            Response.Headers.Add("content-disposition", "attachment; filename=CombinedReports.xlsx");
            Response.ContentType = "application/vnd.ms-excel";
            return File(combinedExcel, "application/vnd.ms-excel", "CombinedReports.xlsx");
        }
        [HttpPost]
        public async Task<IActionResult> DownloadAllWord(string reportdata)
        {
            var wordbyteList = new List<byte[]>();
            var ListofReports = reportdata != null ? JsonConvert.DeserializeObject<List<ExportReportModel>>(reportdata) : null;
            foreach (var report in ListofReports)
            {
                report.reportSql = HttpUtility.HtmlDecode(report.reportSql);
                report.chartData = HttpUtility.UrlDecode(report.chartData)?.Replace(" ", " +");
                var columns = report.columnDetails == null ? new List<ReportHeaderColumn>() : JsonConvert.DeserializeObject<List<ReportHeaderColumn>>(HttpUtility.UrlDecode(report.columnDetails));
                var wordreport = await DotNetReportHelper.GetWordFile(report.reportSql, report.connectKey, HttpUtility.UrlDecode(report.reportName), report.chartData, report.expandAll, HttpUtility.UrlDecode(report.expandSqls), columns, report.includeSubTotal, report.pivot, report.pivotColumn, report.pivotFunction);
                wordbyteList.Add(wordreport);
            }
            var combinedWord = DotNetReportHelper.GetCombineWordFile(wordbyteList);
            Response.Headers.Add("content-disposition", "attachment; filename=CombinedReports.docx");
            Response.ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
            return File(combinedWord, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "CombinedReports.docx");
        }
        [HttpPost]
        public async Task<IActionResult> DownloadWord(string reportSql, string connectKey, string reportName,  bool allExpanded, string expandSqls, string chartData = null, string columnDetails = null, bool includeSubtotal = false, bool pivot = false,string pivotColumn= null, string pivotFunction = null)
        {
            reportSql = HttpUtility.HtmlDecode(reportSql);
            chartData = HttpUtility.UrlDecode(chartData);
            chartData = chartData?.Replace(" ", " +");
            var columns = columnDetails == null ? new List<ReportHeaderColumn>() : JsonConvert.DeserializeObject<List<ReportHeaderColumn>>(HttpUtility.UrlDecode(columnDetails));
            var word = await DotNetReportHelper.GetWordFile(reportSql, connectKey, HttpUtility.UrlDecode(reportName),chartData, allExpanded, HttpUtility.UrlDecode(expandSqls), columns, includeSubtotal, pivot,pivotColumn,pivotFunction);
            Response.Headers.Add("content-disposition", "attachment; filename=" + reportName + ".docx");
            Response.ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
            return File(word, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", reportName + ".docx");
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
        public async Task<IActionResult> DownloadCsv(string reportSql, string connectKey, string reportName, string columnDetails = null, bool includeSubtotal = false,string expandSqls= null, string pivotColumn = null, string pivotFunction = null)
        {
            var columns = columnDetails == null ? new List<ReportHeaderColumn>() : JsonConvert.DeserializeObject<List<ReportHeaderColumn>>(HttpUtility.UrlDecode(columnDetails));
            reportSql = HttpUtility.HtmlDecode(reportSql);
            var csv = await DotNetReportHelper.GetCSVFile(reportSql, HttpUtility.UrlDecode(connectKey), columns, includeSubtotal,expandSqls,pivotColumn,pivotFunction);

            Response.Headers.Add("content-disposition", "attachment; filename=" + HttpUtility.UrlDecode(reportName) + ".csv");
            Response.ContentType = "text/csv";

            return File(csv, "text/csv", reportName + ".csv");
        }

    }
}



