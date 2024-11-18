using ReportBuilder.Web.Models;
using System;
using System.Web;

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
            string dataFilters = Request.Form["dataFilters"] ?? "";
            string reportData = Request.Form["reportData"] ?? "";

            Session["reportPrint"] = "true";
            Session["userId"] = userId;
            Session["clientId"] = clientId;
            Session["currentUserRole"] = currentUserRole;

            Model = new DotNetReportPrintModel
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

                UserId = userId,
                ClientId = clientId,
                CurrentUserRoles = currentUserRole,
                DataFilters = HttpUtility.UrlDecode(dataFilters),
                ReportData = HttpUtility.UrlDecode(reportData)
            };

        }
    }
}