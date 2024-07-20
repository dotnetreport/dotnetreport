using Newtonsoft.Json;
using ReportBuilder.Web.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using System.Web.Services;

namespace ReportBuilder.WebForms.DotNetReport
{
    /// <summary>
    /// Summary description for ReportService
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    [System.Web.Script.Services.ScriptService]
    public class ReportService : System.Web.Services.WebService
    {
        public DotNetReportSettings GetSettings()
        {
            var settings = new DotNetReportSettings
            {
                ApiUrl = ConfigurationManager.AppSettings["dotNetReport.apiUrl"],
                AccountApiToken = ConfigurationManager.AppSettings["dotNetReport.accountApiToken"], // Your Account Api Token from your http://dotnetreport.com Account
                DataConnectApiToken = ConfigurationManager.AppSettings["dotNetReport.dataconnectApiToken"] // Your Data Connect Api Token from your http://dotnetreport.com Account
            };

            // Populate the values below using your Application Roles/Claims if applicable
            settings.ClientId = "";  // You can pass your multi-tenant client id here to track their reports and folders
            settings.UserId = ""; // You can pass your current authenticated user id here to track their reports and folders            
            settings.UserName = "";
            settings.CurrentUserRole = new List<string>(); // Populate your current authenticated user's roles

            settings.Users = new List<dynamic>(); // Populate all your application's user, ex  { "Jane", "John" } or { new { id="1", text="Jane" }, new { id="2", text="John" }}
            settings.UserRoles = new List<string>(); // Populate all your application's user roles, ex  { "Admin", "Normal" }       
            settings.CanUseAdminMode = true; // Set to true only if current user can use Admin mode to setup reports and dashboard
            settings.DataFilters = new { }; // add global data filters to apply as needed https://dotnetreport.com/kb/docs/advance-topics/global-filters/

            return settings;
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public object GetLookupList(string lookupSql, string connectKey)
        {
            var sql = DotNetReportHelper.Decrypt(lookupSql);

            // Uncomment if you want to restrict max records returned
            sql = sql.Replace("SELECT ", "SELECT TOP 500 ");

            var json = new StringBuilder();
            var dt = new DataTable();
            using (var conn = new OleDbConnection(DotNetReportHelper.GetConnectionString(connectKey)))
            {
                conn.Open();
                var command = new OleDbCommand(sql, conn);
                var adapter = new OleDbDataAdapter(command);

                adapter.Fill(dt);
            }

            var data = new List<object>();
            foreach (DataRow dr in dt.Rows)
            {
                data.Add(new { id = dr[0], text = dr[1] });
            }

            return (data);
        }

        public class PostReportApiCallMode
        {
            public string method { get; set; }
            public string headerJson { get; set; }
            public bool useReportHeader { get; set; }

        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public object PostReportApi(string method, string headerJson, bool useReportHeader)
        {
            return CallReportApi(method, JsonConvert.SerializeObject(new PostReportApiCallMode
            {
                method = method,
                headerJson = headerJson,
                useReportHeader = useReportHeader
            }));
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public object RunReportApi(DotNetReportApiCall data)
        {
            return CallReportApi(data.Method, (new JavaScriptSerializer()).Serialize(data));
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public object RunReportApi(string method, bool SaveReport, string ReportJson, bool adminMode, bool SubTotalMode = false)
        {
            return CallReportApi(method, (new JavaScriptSerializer()).Serialize(new DotNetReportApiCall
            {
                Method = method,
                ReportJson = ReportJson,
                SaveReport = SaveReport,
                adminMode = adminMode,
                SubTotalMode = SubTotalMode
            }));
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public object CallReportApi(string method, string model)
        {
            using (var client = new HttpClient())
            {
                var settings = GetSettings();
                var keyvalues = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("account", settings.AccountApiToken),
                    new KeyValuePair<string, string>("dataConnect", settings.DataConnectApiToken),
                    new KeyValuePair<string, string>("clientId", settings.ClientId),
                    new KeyValuePair<string, string>("userId", settings.UserId),
                    new KeyValuePair<string, string>("userIdForSchedule", settings.UserIdForSchedule),
                    new KeyValuePair<string, string>("userRole", String.Join(",", settings.CurrentUserRole))
                };

                var data = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(model);
                foreach (var key in data.Keys)
                {
                    if ((key != "adminMode" || (key == "adminMode" && settings.CanUseAdminMode)) && data[key] != null)
                    {
                        keyvalues.Add(new KeyValuePair<string, string>(key, data[key].ToString()));
                    }
                }

                var content = new FormUrlEncodedContent(keyvalues);
                var response = client.PostAsync(new Uri(settings.ApiUrl + method), content).Result;
                var stringContent = response.Content.ReadAsStringAsync().Result;

                if (stringContent.Contains("\"sql\":"))
                {
                    var sqlqeuery = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(stringContent);
                    object value;
                    var keyValuePair = sqlqeuery.TryGetValue("sql", out value);
                    var sql = DotNetReportHelper.Decrypt(value.ToString());
                }
                Context.Response.StatusCode = (int)response.StatusCode;
                return (new JavaScriptSerializer()).Deserialize<dynamic>(stringContent);
            }

        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public DotNetReportResultModel RunReport(string reportSql, string connectKey, string reportType, int pageNumber = 1, int pageSize = 50, string sortBy = null, bool desc = false, string reportSeries = null, string pivotColumn = null, string pivotFunction = null, string reportData = null)
        {
            var sql = "";
            var sqlCount = "";
            int totalRecords = 0;

            try
            {
                if (string.IsNullOrEmpty(reportSql))
                {
                    throw new Exception("Query not found");
                }
                var allSqls = reportSql.Split(new string[] { "%2C", "," }, StringSplitOptions.RemoveEmptyEntries);
                var dtPaged = new DataTable();
                var dtCols = 0;

                List<string> fields = new List<string>();
                List<string> sqlFields = new List<string>();
                for (int i = 0; i < allSqls.Length; i++)
                {
                    sql = DotNetReportHelper.Decrypt(HttpUtility.HtmlDecode(allSqls[i]));
                    if (!sql.StartsWith("EXEC"))
                    {
                        var fromIndex = DotNetReportHelper.FindFromIndex(sql);
                        sqlFields = DotNetReportHelper.SplitSqlColumns(sql);

                        var sqlFrom = $"SELECT {sqlFields[0]} {sql.Substring(fromIndex)}";
                        sqlCount = $"SELECT COUNT(*) FROM ({(sqlFrom.Contains("ORDER BY") ? sqlFrom.Substring(0, sqlFrom.IndexOf("ORDER BY")) : sqlFrom)}) as countQry";

                        if (!String.IsNullOrEmpty(sortBy))
                        {
                            if (sortBy.StartsWith("DATENAME(MONTH, "))
                            {
                                sortBy = sortBy.Replace("DATENAME(MONTH, ", "MONTH(");
                            }
                            if (sortBy.StartsWith("MONTH(") && sortBy.Contains(")) +") && sql.Contains("Group By"))
                            {
                                sortBy = sortBy.Replace("MONTH(", "CONVERT(VARCHAR(3), DATENAME(MONTH, ");
                            }
                            if (!sql.Contains("ORDER BY"))
                            {
                                sql = sql + "ORDER BY " + sortBy + (desc ? " DESC" : "");
                            }
                            else
                            {
                                sql = sql.Substring(0, sql.IndexOf("ORDER BY")) + "ORDER BY " + sortBy + (desc ? " DESC" : "");
                            }
                        }

                        if (sql.Contains("ORDER BY") && !sql.Contains(" TOP "))
                            sql = sql + $" OFFSET {(pageNumber - 1) * pageSize} ROWS FETCH NEXT {pageSize} ROWS ONLY";

                        if (sql.Contains("__jsonc__"))
                            sql = sql.Replace("__jsonc__", "");
                    }
                    // Execute sql
                    var dtPagedRun = new DataTable();
                    using (var conn = new OleDbConnection(DotNetReportHelper.GetConnectionString(connectKey)))
                    {
                        conn.Open();
                        var command = new OleDbCommand(sqlCount, conn);
                        if (!sql.StartsWith("EXEC")) totalRecords = (int)command.ExecuteScalar();

                        command = new OleDbCommand(sql, conn);
                        var adapter = new OleDbDataAdapter(command);
                        adapter.Fill(dtPagedRun);
                        if (sql.StartsWith("EXEC"))
                        {
                            totalRecords = dtPagedRun.Rows.Count;
                            if (dtPagedRun.Rows.Count > 0)
                                dtPagedRun = dtPagedRun.AsEnumerable().Skip((pageNumber - 1) * pageSize).Take(pageSize).CopyToDataTable();
                        }
                        if (!sqlFields.Any())
                        {
                            foreach (DataColumn c in dtPagedRun.Columns) { sqlFields.Add($"{c.ColumnName} AS {c.ColumnName}"); }
                        }

                        string[] series = { };
                        if (i == 0)
                        {
                            fields.AddRange(sqlFields);

                            if (!string.IsNullOrEmpty(pivotColumn))
                            {
                                var ds = DotNetReportHelper.GetDrillDownData(conn, dtPagedRun, sqlFields, reportData);
                                dtPagedRun = DotNetReportHelper.PushDatasetIntoDataTable(dtPagedRun, ds, pivotColumn, pivotFunction);
                                fields.AddRange(dtPagedRun.Columns.Cast<DataColumn>().Skip(fields.Count).Select(x => $"__ AS {x.ColumnName}").ToList());
                            }

                            dtPaged = dtPagedRun;
                            dtCols = dtPagedRun.Columns.Count;
                        }
                        else if (i > 0)
                        {
                            // merge in to dt
                            if (!string.IsNullOrEmpty(reportSeries))
                                series = reportSeries.Split(new string[] { "%2C", "," }, StringSplitOptions.RemoveEmptyEntries);

                            var j = 1;
                            while (j < dtPagedRun.Columns.Count)
                            {
                                var col = dtPagedRun.Columns[j++];
                                dtPaged.Columns.Add($"{col.ColumnName} ({series[i - 1]})", col.DataType);
                                fields.Add(sqlFields[j - 1]);
                            }

                            foreach (DataRow dr in dtPagedRun.Rows)
                            {
                                DataRow match = dtPaged.AsEnumerable().FirstOrDefault(drun => Convert.ToString(drun[0]) == Convert.ToString(dr[0]));
                                if (fields[0].ToUpper().StartsWith("CONVERT(VARCHAR(10)")) // group by day
                                {
                                    match = dtPaged.AsEnumerable().Where(r => !string.IsNullOrEmpty(r.Field<string>(0)) && !string.IsNullOrEmpty((string)dr[0]) && Convert.ToDateTime(r.Field<string>(0)).Day == Convert.ToDateTime((string)dr[0]).Day).FirstOrDefault();
                                }
                                if (match != null)
                                {
                                    // If a matching row is found, merge the data
                                    j = 1;
                                    while (j < dtPagedRun.Columns.Count)
                                    {
                                        match[j + i + dtCols - 2] = dr[j];
                                        j++;
                                    }
                                }
                                else
                                {
                                    // If no matching row is found, add the entire row from dtPagedRun
                                    DataRow newRow = dtPaged.NewRow();
                                    newRow[0] = dr[0]; // Set the first column with the non-matching value

                                    // Set the values from dtPagedRun into the new row, offset by the correct index
                                    j = 1;
                                    while (j < dtPagedRun.Columns.Count)
                                    {
                                        newRow[j + i + dtCols - 2] = dr[j];
                                        j++;
                                    }

                                    // Set the rest of the values in newRow to DBNull.Value or some default value
                                    for (int k = 1; k < i + dtCols - 2; k++)
                                    {
                                        newRow[k] = DBNull.Value;
                                    }

                                    dtPaged.Rows.Add(newRow);
                                }
                            }
                        }
                    }
                }

                sql = DotNetReportHelper.Decrypt(HttpUtility.HtmlDecode(allSqls[0]));
                var model = new DotNetReportResultModel
                {
                    ReportData = DotNetReportHelper.DataTableToDotNetReportDataModel(dtPaged, fields),
                    Warnings = GetWarnings(sql),
                    ReportSql = sql,
                    ReportDebug = Context.Request.Url.Host.Contains("localhost"),
                    Pager = new DotNetReportPagerModel
                    {
                        CurrentPage = pageNumber,
                        PageSize = pageSize,
                        TotalRecords = totalRecords,
                        TotalPages = (int)(totalRecords == pageSize ? (totalRecords / pageSize) : (totalRecords / pageSize) + 1)
                    }
                };

                return model;

            }

            catch (Exception ex)
            {
                var model = new DotNetReportResultModel
                {
                    ReportData = new DotNetReportDataModel(),
                    ReportSql = sql,
                    HasError = true,
                    Exception = ex.Message,
                    ReportDebug = Context.Request.Url.Host.Contains("localhost"),
                };

                return model;
            }
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public object RunReportLink(int reportId, int? filterId = null, string filterValue = "", bool adminMode = false)
        {
            var model = new DotNetReportModel();
            var settings = GetSettings();

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

                var response = client.PostAsync(new Uri(settings.ApiUrl + $"/ReportApi/RunLinkedReport"), content).Result;
                var stringContent = response.Content.ReadAsStringAsync().Result;
                Context.Response.StatusCode = (int)response.StatusCode;
                return (new JavaScriptSerializer()).Deserialize<DotNetReportModel>(stringContent);

            }
        }


        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public object GetDashboards(bool adminMode = false)
        {
            var model = GetDashboardsData(adminMode);
            return model;
        }


        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public object LoadSavedDashboard(int? id = null, bool adminMode = false)
        {
            var settings = GetSettings();
            var model = new List<DotNetDasboardReportModel>();
            var dashboards = (GetDashboardsData(adminMode));
            if (!id.HasValue && dashboards.Count > 0)
            {
                id = dashboards.First().Id;
            }

            using (var client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("account", settings.AccountApiToken),
                    new KeyValuePair<string, string>("dataConnect", settings.DataConnectApiToken),
                    new KeyValuePair<string, string>("clientId", settings.ClientId),
                    new KeyValuePair<string, string>("userId", settings.UserId),
                    new KeyValuePair<string, string>("userRole", String.Join(",", settings.CurrentUserRole)),
                    new KeyValuePair<string, string>("id", id.HasValue ? id.Value.ToString() : "0"),
                    new KeyValuePair<string, string>("adminMode", adminMode.ToString()),
                });

                var response = client.PostAsync(new Uri(settings.ApiUrl + $"/ReportApi/LoadSavedDashboard"), content).Result;
                var stringContent = response.Content.ReadAsStringAsync().Result;

                model = JsonConvert.DeserializeObject<List<DotNetDasboardReportModel>>(stringContent);
            }

            return model;
        }

        private List<dynamic> GetDashboardsData(bool adminMode = false)
        {
            var settings = GetSettings();

            using (var client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("account", settings.AccountApiToken),
                    new KeyValuePair<string, string>("dataConnect", settings.DataConnectApiToken),
                    new KeyValuePair<string, string>("clientId", settings.ClientId),
                    new KeyValuePair<string, string>("userId", settings.UserId),
                    new KeyValuePair<string, string>("userRole", String.Join(",", settings.CurrentUserRole)),
                    new KeyValuePair<string, string>("adminMode", adminMode.ToString()),
                });

                var response = client.PostAsync(new Uri(settings.ApiUrl + $"/ReportApi/GetDashboards"), content).Result;
                var stringContent = response.Content.ReadAsStringAsync().Result;

                var model = (new JavaScriptSerializer()).Deserialize<List<dynamic>>(stringContent);
                return model;
            }
        }

        [WebMethod(EnableSession = true)]
        public async Task DownloadExcel(string reportSql, string connectKey, string reportName, bool allExpanded, string expandSqls, string columnDetails = null, bool includeSubtotal = false, string chartData = "")
        {
            var columns = columnDetails == null ? new List<ReportHeaderColumn>() : JsonConvert.DeserializeObject<List<ReportHeaderColumn>>(HttpUtility.UrlDecode(columnDetails));

            var excel = await DotNetReportHelper.GetExcelFile(reportSql, connectKey, HttpUtility.UrlDecode(reportName), chartData, allExpanded, HttpUtility.UrlDecode(expandSqls), columns, includeSubtotal);
            Context.Response.ClearContent();

            Context.Response.AddHeader("content-disposition", "attachment; filename=" + HttpUtility.UrlDecode(reportName) + ".xlsx");
            Context.Response.ContentType = "application/vnd.ms-excel";
            Context.Response.BinaryWrite(excel);
            Context.Response.End();

        }

        [WebMethod(EnableSession = true)]
        public async Task DownloadWord(string reportSql, string connectKey, string reportName, bool allExpanded, string expandSqls, string columnDetails = null, bool includeSubtotal = false, bool pivot = false, string chartData = "")
        {
            reportSql = HttpUtility.HtmlDecode(reportSql);
            var columns = columnDetails == null ? new List<ReportHeaderColumn>() : JsonConvert.DeserializeObject<List<ReportHeaderColumn>>(HttpUtility.UrlDecode(columnDetails));
            var word = await DotNetReportHelper.GetWordFile(reportSql, connectKey, HttpUtility.UrlDecode(reportName), chartData, allExpanded, HttpUtility.UrlDecode(expandSqls), columns, includeSubtotal, pivot);

            Context.Response.AddHeader("content-disposition", "attachment; filename=" + HttpUtility.UrlDecode(reportName) + ".docx");
            Context.Response.ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
            Context.Response.BinaryWrite(word);
            Context.Response.End();
        }

        [WebMethod(EnableSession = true)]
        public void DownloadXml(string reportSql, string connectKey, string reportName)
        {

            var xml = DotNetReportHelper.GetXmlFile(reportSql, HttpUtility.UrlDecode(connectKey), HttpUtility.UrlDecode(reportName));
            Context.Response.ClearContent();

            Context.Response.AddHeader("content-disposition", "attachment; filename=" + HttpUtility.UrlDecode(reportName) + ".xml");
            Context.Response.ContentType = "text/txt";
            Context.Response.Write(xml);
            Context.Response.End();

        }

        [WebMethod(EnableSession = true)]
        public void DownloadPdfAlt(string reportSql, string connectKey, string reportName, string chartData = null, string columnDetails = null, bool includeSubtotal = false)
        {
            reportSql = HttpUtility.HtmlDecode(reportSql);
            chartData = HttpUtility.UrlDecode(chartData);
            chartData = chartData.Replace(" ", " +");
            reportName = HttpUtility.UrlDecode(reportName);
            var columns = columnDetails == null ? new List<ReportHeaderColumn>() : JsonConvert.DeserializeObject<List<ReportHeaderColumn>>(HttpUtility.UrlDecode(columnDetails));

            var pdf = DotNetReportHelper.GetPdfFileAlt(reportSql, connectKey, reportName, chartData, columns, includeSubtotal);
            Context.Response.AddHeader("content-disposition", "attachment; filename=" + reportName + ".pdf");
            Context.Response.ContentType = "application/pdf";
            Context.Response.BinaryWrite(pdf);
            Context.Response.End();
        }

        [WebMethod(EnableSession = true)]
        public void DownloadCsv(string reportSql, string connectKey, string reportName, string columnDetails = null, bool includeSubtotal = false)
        {
            var columns = columnDetails == null ? new List<ReportHeaderColumn>() : JsonConvert.DeserializeObject<List<ReportHeaderColumn>>(HttpUtility.UrlDecode(columnDetails));
            var excel = DotNetReportHelper.GetCSVFile(reportSql, HttpUtility.UrlDecode(connectKey), columns, includeSubtotal);

            Context.Response.ClearContent();
            Context.Response.AddHeader("content-disposition", "attachment; filename=" + HttpUtility.UrlDecode(reportName) + ".csv");
            Context.Response.ContentType = "text/csv";
            Context.Response.BinaryWrite(excel);
            Context.Response.End();

        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public object GetUsersAndRoles()
        { 
            // These report permission settings will be applied by default to any new report user creates, leave black to allow access to all
            var newReportClientId = ""; // comma separated client ids to set report permission when new report is created
            var newReportEditUserId = ""; // comma separated user ids for report edit permission when new report is created
            var newReportViewUserId = ""; // comma separated user ids for report view permission when new report is created
            var newReportEditUserRoles = ""; // comma separated user roles for report edit permission when new report is created
            var newReportViewUserRoles = ""; // comma separated user roles for report view permission when new report is created

            var settings = GetSettings();
            return new
            {
                noAccount = string.IsNullOrEmpty(settings.AccountApiToken) || settings.AccountApiToken == "Your Public Account Api Token",
                users = settings.CanUseAdminMode ? settings.Users : new List<dynamic>(),
                userRoles = settings.CanUseAdminMode ? settings.UserRoles : new List<string>(),
                currentUserId = settings.UserId,
                currentUserRoles = settings.UserRoles,
                currentUserName = settings.UserName,
                allowAdminMode = settings.CanUseAdminMode,
                userIdForSchedule = settings.UserIdForSchedule,
                userIdForFilter = settings.UserIdForFilter,
                dataFilters = settings.DataFilters,
                clientId = settings.ClientId,

                newReportClientId,
                newReportEditUserId,
                newReportViewUserId,
                newReportEditUserRoles,
                newReportViewUserRoles
            };
        }

        private string GetWarnings(string sql)
        {
            var warning = "";
            if (sql.ToLower().Contains("cross join"))
            {
                warning += "Some data used in this report have relations that are not setup properly, so data might duplicate incorrectly.<br/>";
            }

            return warning;
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public async Task<object> LoadSetupSchema(string databaseApiKey = "", bool onlyApi = false)
        {
            try
            {
                var settings = GetSettings();
                if (!settings.CanUseAdminMode)
                {
                    throw new Exception("Not Authorized to access this Resource");
                }

                var connect = Setup.GetConnection(databaseApiKey);
                var tables = new List<TableViewModel>();
                var procedures = new List<TableViewModel>();
                if (onlyApi)
                {
                    tables.AddRange(await Setup.GetApiTables(connect.AccountApiKey, connect.DatabaseApiKey, true));
                }
                else
                {
                    tables.AddRange(await Setup.GetTables("TABLE", connect.AccountApiKey, connect.DatabaseApiKey));
                    tables.AddRange(await Setup.GetTables("VIEW", connect.AccountApiKey, connect.DatabaseApiKey));
                }
                procedures.AddRange(await Setup.GetApiProcs(connect.AccountApiKey, connect.DatabaseApiKey));

                var model = new ManageViewModel
                {
                    ApiUrl = connect.ApiUrl,
                    AccountApiKey = connect.AccountApiKey,
                    DatabaseApiKey = connect.DatabaseApiKey,
                    Tables = tables,
                    Procedures = procedures
                };

                return model;
            }
            catch (Exception ex)
            {
                Context.Response.StatusCode = 500;
                return new { ex.Message };
            }
        }


        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public object GetSchemaFromSql(string value, string accountKey, string dataConnectKey)
        {
            try
            {
                var table = new TableViewModel
                {
                    AllowedRoles = new List<string>(),
                    Columns = new List<ColumnViewModel>(),
                    CustomTable = true,
                    Selected = true
                };

                table.CustomTableSql = value;

                var connect = Setup.GetConnection(dataConnectKey);
                using (var client = new HttpClient())
                {
                    var response = client.GetAsync(String.Format("{0}/ReportApi/GetDataConnectKey?account={1}&dataConnect={2}", connect.ApiUrl, connect.AccountApiKey, connect.DatabaseApiKey)).Result;

                    response.EnsureSuccessStatusCode();

                    var content = response.Content.ReadAsStringAsync().Result;
                    dataConnectKey = content.Replace("\"", "");
                }

                var connString = DotNetReportHelper.GetConnectionString(dataConnectKey);
                using (OleDbConnection conn = new OleDbConnection(connString))
                {
                    // open the connection to the database 
                    conn.Open();
                    OleDbCommand cmd = new OleDbCommand(value, conn);
                    cmd.CommandType = CommandType.Text;
                    using (OleDbDataReader reader = cmd.ExecuteReader())
                    {
                        // Get the column metadata using schema.ini file
                        DataTable schemaTable = new DataTable();
                        schemaTable = reader.GetSchemaTable();
                        var idx = 0;

                        foreach (DataRow dr in schemaTable.Rows)
                        {
                            var column = new ColumnViewModel
                            {
                                ColumnName = dr["ColumnName"].ToString(),
                                DisplayName = dr["ColumnName"].ToString(),
                                PrimaryKey = dr["ColumnName"].ToString().ToLower().EndsWith("id") && idx == 0,
                                DisplayOrder = idx,
                                FieldType = Setup.ConvertToJetDataType((int)dr["ProviderType"]).ToString(),
                                AllowedRoles = new List<string>(),
                                Selected = true
                            };

                            idx++;
                            table.Columns.Add(column);
                        }
                        table.Columns = table.Columns.OrderBy(x => x.DisplayOrder).ToList();
                    }

                    return table;
                }
            }
            catch (Exception ex)
            {

                Context.Response.StatusCode = 500;
                return new { ex.Message };
            }
        }


        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public object SearchProcedure(string value = null, string accountKey = null, string dataConnectKey = null)
        {
            var connect = Setup.GetConnection(dataConnectKey);
            using (var client = new HttpClient())
            {
                var response = client.GetAsync(String.Format("{0}/ReportApi/GetDataConnectKey?account={1}&dataConnect={2}", connect.ApiUrl, connect.AccountApiKey, connect.DatabaseApiKey)).Result;

                response.EnsureSuccessStatusCode();

                var content = response.Content.ReadAsStringAsync().Result;
                dataConnectKey = content.Replace("\"", "");
            }

            return GetSearchProcedure(value, accountKey, dataConnectKey);
        }

        private SortedList<string, string> GetTimezones()
        {
            var timeZones = TimeZoneInfo.GetSystemTimeZones();
            SortedList<string, string> timeZoneList = new SortedList<string, string>();
            timeZoneList.Add("", "");

            foreach (TimeZoneInfo timezone in timeZones)
            {
                DateTime localTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.Now.ToUniversalTime(), timezone);
                TimeSpan localOffset = timezone.GetUtcOffset(localTime);

                string offset = localOffset.ToString();
                if (!offset.Contains("-"))
                {
                    offset = $"+{offset}";
                }

                string display = $"(GMT {offset}) {timezone.StandardName}";
                if (timezone.IsDaylightSavingTime(localTime))
                {
                    display = $"{display} (active daylight savings)";
                }

                timeZoneList.Add(display, timezone.Id); // Use timezone Id as value
            }

            return timeZoneList;
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(UseHttpGet = true, ResponseFormat = ResponseFormat.Json)]
        public object GetAllTimezones()
        {
            var timeZones = GetTimezones(); // Call your existing GetTimezones method
            return timeZones;
        }

        private List<TableViewModel> GetSearchProcedure(string value = null, string accountKey = null, string dataConnectKey = null)
        {
            var tables = new List<TableViewModel>();
            var connString = DotNetReportHelper.GetConnectionString(dataConnectKey);
            using (OleDbConnection conn = new OleDbConnection(connString))
            {
                // open the connection to the database 
                conn.Open();
                string spQuery = "SELECT ROUTINE_NAME, ROUTINE_DEFINITION, ROUTINE_SCHEMA FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_NAME LIKE '%" + value + "%' AND ROUTINE_TYPE = 'PROCEDURE'";
                OleDbCommand cmd = new OleDbCommand(spQuery, conn);
                cmd.CommandType = CommandType.Text;
                DataTable dtProcedures = new DataTable();
                dtProcedures.Load(cmd.ExecuteReader());
                int count = 1;
                foreach (DataRow dr in dtProcedures.Rows)
                {
                    var procName = dr["ROUTINE_NAME"].ToString();
                    var procSchema = dr["ROUTINE_SCHEMA"].ToString();
                    cmd = new OleDbCommand(procName, conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    // Get the parameters.
                    OleDbCommandBuilder.DeriveParameters(cmd);
                    List<ParameterViewModel> parameterViewModels = new List<ParameterViewModel>();
                    foreach (OleDbParameter param in cmd.Parameters)
                    {
                        if (param.Direction == ParameterDirection.Input)
                        {
                            var parameter = new ParameterViewModel
                            {
                                ParameterName = param.ParameterName,
                                DisplayName = param.ParameterName,
                                ParameterValue = param.Value != null ? param.Value.ToString() : "",
                                ParamterDataTypeOleDbTypeInteger = Convert.ToInt32(param.OleDbType),
                                ParamterDataTypeOleDbType = param.OleDbType,
                                ParameterDataTypeString = Setup.GetType(Setup.ConvertToJetDataType(Convert.ToInt32(param.OleDbType))).Name
                            };
                            if (parameter.ParameterDataTypeString.StartsWith("Int")) parameter.ParameterDataTypeString = "Int";
                            parameterViewModels.Add(parameter);
                        }
                    }
                    DataTable dt = new DataTable();
                    cmd = new OleDbCommand($"[{procSchema}].[{procName}]", conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    foreach (var data in parameterViewModels)
                    {
                        cmd.Parameters.Add(new OleDbParameter { Value = DBNull.Value, ParameterName = data.ParameterName, Direction = ParameterDirection.Input, IsNullable = true });
                    }
                    OleDbDataReader reader = cmd.ExecuteReader();
                    dt = reader.GetSchemaTable();

                    // Store the table names in the class scoped array list of table names
                    List<ColumnViewModel> columnViewModels = new List<ColumnViewModel>();
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        var column = new ColumnViewModel
                        {
                            ColumnName = dt.Rows[i].ItemArray[0].ToString(),
                            DisplayName = dt.Rows[i].ItemArray[0].ToString(),
                            FieldType = Setup.ConvertToJetDataType((int)dt.Rows[i]["ProviderType"]).ToString()
                        };
                        columnViewModels.Add(column);
                    }
                    tables.Add(new TableViewModel
                    {
                        TableName = procName,
                        SchemaName = dr["ROUTINE_SCHEMA"].ToString(),
                        DisplayName = procName,
                        Parameters = parameterViewModels,
                        Columns = columnViewModels
                    });
                    count++;
                }
                conn.Close();
                conn.Dispose();
            }
            return tables;
        }
    }
}