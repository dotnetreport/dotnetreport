using Newtonsoft.Json;
using ReportBuilder.Web.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace ReportBuilder.Web.Controllers
{
    public class DotNetReportController : Controller
    {

        public ActionResult Index()
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

        public ActionResult ReportPrint(int reportId, string reportName, string reportDescription, string reportSql, string connectKey, string reportFilter, string reportType,
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

        public async Task<ActionResult> Dashboard(int? id = null, bool adminMode = false)
        {
            return View(new DotNetDashboardModel());
        }

        
        [HttpPost]
        public ActionResult DownloadExcel(string reportSql, string connectKey, string reportName, bool allExpanded, string expandSqls, string columnDetails = null, bool includeSubtotal = false, bool pivot = false)
        {
            reportSql = HttpUtility.HtmlDecode(reportSql);
            var columns = columnDetails == null ? new List<ReportHeaderColumn>() :  JsonConvert.DeserializeObject<List<ReportHeaderColumn>>(HttpUtility.UrlDecode(columnDetails));
            
            var excel = DotNetReportHelper.GetExcelFile(reportSql, connectKey, HttpUtility.UrlDecode(reportName), allExpanded, expandSqls?.Split(',').ToList(), columns, includeSubtotal, pivot);
            Response.ClearContent();

            Response.AddHeader("content-disposition", "attachment; filename=" + HttpUtility.UrlDecode(reportName) + ".xlsx");
            Response.ContentType = "application/vnd.ms-excel";
            Response.BinaryWrite(excel);
            Response.End();
            
            return View();
        }

        [HttpPost]
        public ActionResult DownloadXml(string reportSql, string connectKey, string reportName)
        {
            reportSql = HttpUtility.HtmlDecode(reportSql);
            var xml = DotNetReportHelper.GetXmlFile(reportSql, HttpUtility.UrlDecode(connectKey), HttpUtility.UrlDecode(reportName));
            Response.ClearContent();

            Response.AddHeader("content-disposition", "attachment; filename=" + HttpUtility.UrlDecode(reportName) + ".xml");
            Response.ContentType = "application/xml";
            Response.Write(xml);
            Response.End();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> DownloadPdf(string printUrl, int reportId, string reportSql, string connectKey, string reportName, bool expandAll,
                                                                    string clientId = null, string userId = null, string userRoles = null, string dataFilters = "")
        {
            reportSql = HttpUtility.HtmlDecode(reportSql);
            var pdf = await DotNetReportHelper.GetPdfFile(HttpUtility.UrlDecode(printUrl), reportId, reportSql, HttpUtility.UrlDecode(connectKey), HttpUtility.UrlDecode(reportName),
                                                userId, clientId, userRoles, dataFilters, expandAll);

            return File(pdf, "application/pdf", reportName + ".pdf");
        }


        [HttpPost]
        public async Task<ActionResult> DownloadPdfAlt(string reportSql, string connectKey, string reportName, string chartData = null, string columnDetails = null, bool includeSubtotal = false, bool pivot = false)
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
        public ActionResult DownloadCsv(string reportSql, string connectKey, string reportName, string columnDetails = null, bool includeSubtotal = false)
        {
            var columns = columnDetails == null ? new List<ReportHeaderColumn>() : JsonConvert.DeserializeObject<List<ReportHeaderColumn>>(HttpUtility.UrlDecode(columnDetails));
            reportSql = HttpUtility.HtmlDecode(reportSql);
            var csv = DotNetReportHelper.GetCSVFile(reportSql, HttpUtility.UrlDecode(connectKey), columns, includeSubtotal);

            Response.ClearContent();
            Response.AddHeader("content-disposition", "attachment; filename=" + HttpUtility.UrlDecode(reportName) + ".csv");
            Response.ContentType = "text/csv";
            Response.BinaryWrite(csv);
            Response.End();

            return View();
        }

    }
}

namespace ReportBuilder.Web
{
    public static class ReportUtil
    {
        /// <summary>
        /// Get script file name with available version
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static string GetScriptFile(string expression)
        {
            var path = HttpRuntime.AppDomainAppPath;
            var files = Directory.GetFiles(path + "Scripts").Select(x => Path.GetFileName(x)).ToList();
            string script = string.Empty;
            expression = expression.Replace(".", @"\.").Replace("{0}", "(\\d+\\.?)+");
            var r = new System.Text.RegularExpressions.Regex(@expression, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            foreach (var f in files)
            {
                var m = r.Match(f);
                while (m.Success)
                {
                    script = m.Captures[0].ToString();

                    m = m.NextMatch();
                }
            }

            return script;
        }
    }

    public static class HtmlExtensions
    {

        private class ScriptBlock : IDisposable
        {
            private const string scriptsKey = "scripts";
            public static List<string> pageScripts
            {
                get
                {
                    if (HttpContext.Current.Items[scriptsKey] == null)
                        HttpContext.Current.Items[scriptsKey] = new List<string>();
                    return (List<string>)HttpContext.Current.Items[scriptsKey];
                }
            }

            WebViewPage webPageBase;

            public ScriptBlock(WebViewPage webPageBase)
            {
                this.webPageBase = webPageBase;
                this.webPageBase.OutputStack.Push(new StringWriter());
            }

            public void Dispose()
            {
                pageScripts.Add(((StringWriter)this.webPageBase.OutputStack.Pop()).ToString());
            }
        }

        public static IDisposable BeginScripts(this HtmlHelper helper)
        {
            return new ScriptBlock((WebViewPage)helper.ViewDataContainer);
        }

        public static MvcHtmlString PageScripts(this HtmlHelper helper)
        {
            return MvcHtmlString.Create(string.Join(Environment.NewLine, ScriptBlock.pageScripts.Select(s => s.ToString())));
        }

    }
}


