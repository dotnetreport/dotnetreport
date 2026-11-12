using System;
using System.Net;
using ReportBuilder.Web.Models;

namespace ReportBuilder.WebForms.DotNetReport
{
    public partial class Users : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Admins, or Manage Users for their own Client Id
            var service = new ReportService();
            var settings = service.GetSettings();
            if (!settings.CanUseAdminMode && !service.GetUserPermissions(settings).Contains(DotNetReportPermissions.ManageUsers))
            {
                Response.StatusCode = (int)HttpStatusCode.Forbidden;
                Response.Write("You are not authorized to view this page");
                Response.End();
            }
        }
    }
}
