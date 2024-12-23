using ReportBuilder.Web.Models;
using System;
using System.Collections.Generic;

namespace ReportBuilder.WebForms.DotNetReport
{
    public partial class Setup : System.Web.UI.Page
    {
        private ManageViewModel _model;
        public ManageViewModel Model
        {
            get
            {
                return _model ?? new ManageViewModel();
            }
            set
            {
                _model = value;
            }
        }

        protected async void Page_Load(object sender, EventArgs e)
        {
            string databaseApiKey = Request.QueryString["databaseApiKey"];
            var connect = DotNetReportHelper.GetConnection(databaseApiKey);
            var tables = new List<TableViewModel>();
            var procedures = new List<TableViewModel>();
            Model = new ManageViewModel
            {
                ApiUrl = connect.ApiUrl,
                AccountApiKey = connect.AccountApiKey,
                DatabaseApiKey = connect.DatabaseApiKey,
                Tables = tables,
                Procedures = procedures
            };

        }
    }
}