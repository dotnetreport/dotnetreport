using ReportBuilder.Web.Models;
using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ReportBuilder.WebForms.DotNetReport
{
    public partial class ReportPrint : System.Web.UI.Page
    {
        private DotNetReportPrintModel _model;
        public DotNetReportPrintModel Model
        {
            get
            {
                return _model ?? new DotNetReportPrintModel();
            }
            set
            {
                _model = value;
            }
        }
        protected void Page_Load(object sender, EventArgs e)
        {

            int reportId = Convert.ToInt32(Request.Form["reportId"]);
            string reportName = Request.Form["reportName"];
            string reportDescription = Request.Form["reportDescription"];
            bool includeSubTotal = Convert.ToBoolean(Request.Form["includeSubTotal"]);
            bool showUniqueRecords = Convert.ToBoolean(Request.Form["showUniqueRecords"]);
            bool aggregateReport = Convert.ToBoolean(Request.Form["aggregateReport"]);
            bool showDataWithGraph = Convert.ToBoolean(Request.Form["showDataWithGraph"]);
            string reportSql = Request.Form["reportSql"];
            string connectKey = Request.Form["connectKey"];
            string reportFilter = Request.Form["reportFilter"];
            string reportType = Request.Form["reportType"];
            int selectedFolder = Convert.ToInt32(Request.Form["selectedFolder"]);
            string reportSeries = Request.Form["reportSeries"];
            string userId = Request.Form["userId"];
            string clientId = Request.Form["clientId"];
            string currentUserRole = Request.Form["currentUserRole"];
            string dataFilters = HttpUtility.HtmlDecode(Request.Form["dataFilters"]) ?? "";
            string reportData = HttpUtility.HtmlDecode(Request.Unvalidated["reportData"]) ?? "";

            Session["reportPrint"] = "true";
            Session["userId"] = userId;
            Session["clientId"] = clientId;
            Session["currentUserRole"] = currentUserRole;

            var settings = new DotNetReportSettings
            {
                ClientId = clientId,
                UserId = userId,
                CurrentUserRole = (currentUserRole ?? "")
                    .Split(',')
                    .ToList(),
                DataFilters = string.IsNullOrEmpty(dataFilters) ?
                                    new { } :
                                    Newtonsoft.Json.JsonConvert.DeserializeObject<object>(dataFilters)
            };

            var exportId = ExportSessionStore.Save(settings);
            Session["ExportId"] = exportId;

            var sanitizer = new Ganss.Xss.HtmlSanitizer
            {
                AllowedSchemes = { "data" }, // allow base64 images
                AllowedTags = { "b", "i", "u", "p", "span", "div", "img", "table", "tr", "td" },
                AllowedAttributes = { "style", "class", "src", "alt", "width", "height" }
            };
            Model = new DotNetReportPrintModel
            {
                ReportId = reportId,
                ReportType = reportType,
                ReportName = HttpUtility.HtmlDecode(reportName),
                ReportDescription = HttpUtility.HtmlDecode(reportDescription),
                ReportSql = reportSql,
                ConnectKey = connectKey,
                IncludeSubTotals = includeSubTotal,
                ShowUniqueRecords = showUniqueRecords,
                ShowDataWithGraph = showDataWithGraph,
                SelectedFolder = selectedFolder,
                ReportFilter = reportFilter, // json data to setup filter correctly again

                UserId = userId,
                ClientId = clientId,
                CurrentUserRoles = currentUserRole,
                DataFilters = dataFilters,
                ReportData = sanitizer.Sanitize(HttpUtility.UrlDecode(reportData))
            };

        }
    }
}