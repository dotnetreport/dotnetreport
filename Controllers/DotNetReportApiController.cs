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
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace ReportBuilder.Web.Controllers
{
    public class DotNetReportApiController : Controller
    {
        public readonly static string dbtype = DbTypes.MS_SQL.ToString().Replace("_", " ");
     
        private DotNetReportSettings GetSettings()
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
            settings.CanUseAdminMode = true; // Set to true only if current user can use Admin mode to setup reports, dashboard and schema
            settings.DataFilters = new { }; // add global data filters to apply as needed https://dotnetreport.com/kb/docs/advance-topics/global-filters/

            return settings;
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public JsonResult GetLookupList(string lookupSql, string connectKey)
        {
            var qry = new SqlQuery();
            var sql = DotNetReportHelper.Decrypt(lookupSql);
            if (sql.StartsWith("{\"sql\""))
            {
                qry = JsonConvert.DeserializeObject<SqlQuery>(sql);
                sql = qry.sql;
            }

            // Uncomment if you want to restrict max records returned
            sql = sql.Replace("SELECT ", "SELECT TOP 500 ");

            var json = new StringBuilder();
            var dt = new DataTable();

            var connectionString = DotNetReportHelper.GetConnectionString(connectKey);
            IDatabaseConnection databaseConnection = DatabaseConnectionFactory.GetConnection(dbtype);

            dt = databaseConnection.ExecuteQuery(connectionString, sql, qry.parameters);

            var data = new List<object>();
            foreach (DataRow dr in dt.Rows)
            {
                data.Add(new { id = dr[0], text = dr[1] });
            }

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public class PostReportApiCallMode
        {
            public string method { get; set; }
            public string headerJson { get; set; }
            public bool useReportHeader { get; set; }

        }
        [AllowAnonymous]
        public async Task<JsonResult> CallReportApiUnAuth(string method, string model)
        {
            var settings = new DotNetReportSettings
            {
                ApiUrl = ConfigurationManager.AppSettings["dotNetReport.apiUrl"],
                AccountApiToken = ConfigurationManager.AppSettings["dotNetReport.accountApiToken"], // Your Account Api Token from your http://dotnetreport.com Account
                DataConnectApiToken = ConfigurationManager.AppSettings["dotNetReport.dataconnectApiToken"] // Your Data Connect Api Token from your http://dotnetreport.com Account
            };

            return await ExecuteCallReportApi(method, model, settings);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<JsonResult> PostReportApi(PostReportApiCallMode data)
        {
            string method = data.method;
            return await CallReportApi(method, JsonConvert.SerializeObject(data));
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<JsonResult> RunReportApi(DotNetReportApiCall data)
        {
            return await CallReportApi(data.Method, (new JavaScriptSerializer()).Serialize(data));
        }
        public class ReportApiCallModel
        {
            public string method { get; set; }
            public string model { get; set; }
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<JsonResult> CallPostReportApi(ReportApiCallModel data)
        {
            return await CallReportApi(data.method, data.model);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<JsonResult> CallReportApi(ReportApiCallModel data)
        {
            return await CallReportApi(data.method, data.model);
        }
         
        [HttpGet]
        public async Task<JsonResult> CallReportApi(string method, string model)
        {
            return string.IsNullOrEmpty(method) || string.IsNullOrEmpty(model) ? Json(new { }) : await ExecuteCallReportApi(method, model);
        }

        private async Task<JsonResult> ExecuteCallReportApi(string method, string model, DotNetReportSettings settings = null)
        { 
            using (var client = new HttpClient())
            {
                settings = settings ?? GetSettings();
                var keyvalues = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("account", settings.AccountApiToken),
                    new KeyValuePair<string, string>("dataConnect", settings.DataConnectApiToken),
                    new KeyValuePair<string, string>("clientId", settings.ClientId),
                    new KeyValuePair<string, string>("userId", settings.UserId),
                    new KeyValuePair<string, string>("userIdForSchedule", settings.UserIdForSchedule),
                    new KeyValuePair<string, string>("userRole", String.Join(",", settings.CurrentUserRole)),
                    new KeyValuePair<string, string>("useParameters", "false")
            };

                var data = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(model);
                foreach (var key in data.Keys)
                {
                    if (key == "dataConnect" && data[key] != null)
                    {
                        keyvalues.RemoveAt(keyvalues.FindIndex(kv => kv.Key == "dataConnect"));
                    }
                    if (key == "account" && data[key] != null)
                    {
                        keyvalues.RemoveAt(keyvalues.FindIndex(kv => kv.Key == "account"));
                    }
                    if ((key != "adminMode" || (key == "adminMode" && settings.CanUseAdminMode)) && data[key] != null)
                    {
                        keyvalues.Add(new KeyValuePair<string, string>(key, data[key].ToString()));
                    }
                }

                var content = new FormUrlEncodedContent(keyvalues);
                var response = await client.PostAsync(new Uri(settings.ApiUrl + method), content);
                var stringContent = await response.Content.ReadAsStringAsync();

                Response.StatusCode = (int)response.StatusCode;
                return Json((new JavaScriptSerializer()).Deserialize<dynamic>(stringContent), JsonRequestBehavior.AllowGet);
            }

        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<JsonResult> RunReportUnAuth(string reportSql, string connectKey, string reportType, int pageNumber = 1, int pageSize = 50, string sortBy = null, 
            bool desc = false, string reportSeries = null, string pivotColumn = null, string pivotFunction = null, string reportData = null)
        {
            return await RunReport(reportSql, connectKey, reportType, pageNumber, pageSize, sortBy, desc, reportSeries, pivotColumn, pivotFunction, reportData);
        }

        public class SqlQuery
        {
            public string sql { get; set; } = "";
            public List<KeyValuePair<string, string>> parameters { get; set; } = null;
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<JsonResult> RunReport(string reportSql, string connectKey, string reportType, int pageNumber = 1, int pageSize = 50, string sortBy = null, 
            bool desc = false, string reportSeries = null, string pivotColumn = null, string pivotFunction = null, string reportData = null)
        {
            var sql = "";
            var sqlCount = "";
            int totalRecords = 0;
            var qry = new SqlQuery();

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
                    if (sql.StartsWith("{\"sql\""))
                    {
                        qry = JsonConvert.DeserializeObject<SqlQuery>(sql);
                        sql = qry.sql;
                    }
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

                        if (sql.Contains("ORDER BY") && !sql.Contains(" TOP ") && string.IsNullOrEmpty(pivotColumn))
                            sql = sql + $" OFFSET {(pageNumber - 1) * pageSize} ROWS FETCH NEXT {pageSize} ROWS ONLY";

                        if (sql.Contains("__jsonc__"))
                            sql = sql.Replace("__jsonc__", "");
                    }
                    // Execute sql
                    var connectionString = DotNetReportHelper.GetConnectionString(connectKey);
                    IDatabaseConnection databaseConnection = DatabaseConnectionFactory.GetConnection(dbtype);

                    var dtPagedRun = new DataTable();

                    totalRecords = databaseConnection.GetTotalRecords(connectionString, sqlCount, sql, qry.parameters);

                    if (!string.IsNullOrEmpty(pivotColumn))
                    {
                        sql = sql.Remove(sql.IndexOf("SELECT "), "SELECT ".Length).Insert(sql.IndexOf("SELECT "), "SELECT TOP 1 ");
                    }

                    dtPagedRun = databaseConnection.ExecuteQuery(connectionString, sql, qry.parameters);
                    dtPagedRun = await DotNetReportHelper.ExecuteCustomFunction(dtPagedRun, sql);

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
                            var pd = await DotNetReportHelper.GetPivotTable(databaseConnection, connectionString, dtPagedRun, sql, sqlFields, reportData, pivotColumn, pivotFunction, pageNumber, pageSize, sortBy, desc);
                            dtPagedRun = pd.dt; 
                            if (!string.IsNullOrEmpty(pd.sql)) sql = pd.sql;

                            //var ds = await DotNetReportHelper.GetDrillDownData(databaseConnection, connectionString, dtPagedRun, sqlFields, reportData);
                            //dtPagedRun = DotNetReportHelper.PushDatasetIntoDataTable(dtPagedRun, ds, pivotColumn, pivotFunction, reportData);
                            var keywordsToExclude = new[] { "Count", "Sum", "Max", "Avg" };
                            fields = fields
                                .Where(field => !keywordsToExclude.Any(keyword => field.Contains(keyword)))  // Filter fields to exclude unwanted keywords
                                .ToList();
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

                if (string.IsNullOrEmpty(pivotColumn)) sql = DotNetReportHelper.Decrypt(HttpUtility.HtmlDecode(allSqls[0]));
                var model = new DotNetReportResultModel
                {
                    ReportData = DotNetReportHelper.DataTableToDotNetReportDataModel(dtPaged, fields),
                    //Warnings = GetWarnings(sql),
                    ReportSql = sql,
                    ReportDebug = Request.Url.Host.Contains("localhost"),
                    Pager = new DotNetReportPagerModel
                    {
                        CurrentPage = pageNumber,
                        PageSize = pageSize,
                        TotalRecords = totalRecords,
                        TotalPages = (int)(totalRecords == pageSize ? (totalRecords / pageSize) : (totalRecords / pageSize) + 1)
                    }
                };

                return new JsonResult()
                {
                    Data = model,
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                    MaxJsonLength = Int32.MaxValue
                };

            }

            catch (Exception ex)
            {
                var model = new DotNetReportResultModel
                {
                    ReportData = new DotNetReportDataModel(),
                    ReportSql = sql,
                    HasError = true,
                    Exception = ex.Message,
                    ReportDebug = Request.Url.Host.Contains("localhost"),
                };

                return Json(model, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public async Task<JsonResult> RunReportLink(int reportId, int? filterId = null, string filterValue = "", bool adminMode = false)
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
                    new KeyValuePair<string, string>("dataFilters", JsonConvert.SerializeObject(settings.DataFilters)),
                    new KeyValuePair<string, string>("useParameters", "false")
                });

                var response = await client.PostAsync(new Uri(settings.ApiUrl + $"/ReportApi/RunLinkedReport"), content);
                var stringContent = await response.Content.ReadAsStringAsync();

                model = JsonConvert.DeserializeObject<DotNetReportModel>(stringContent);

            }

            return Json(model, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public async Task<JsonResult> GetDashboards(bool adminMode = false)
        {
            var model = await GetDashboardsData(adminMode);
            return Json(model, JsonRequestBehavior.AllowGet);
        }


        [HttpPost]
        public async Task<JsonResult> LoadSavedDashboard(int? id = null, bool adminMode = false)
        {
            var settings = GetSettings();
            var model = new List<DotNetDasboardReportModel>();
            var dashboards = (await GetDashboardsData(adminMode));
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
                    new KeyValuePair<string, string>("dataFilters", JsonConvert.SerializeObject(settings.DataFilters))
                });

                var response = await client.PostAsync(new Uri(settings.ApiUrl + $"/ReportApi/LoadDashboardData"), content);
                var stringContent = await response.Content.ReadAsStringAsync();

                model = JsonConvert.DeserializeObject<List<DotNetDasboardReportModel>>(stringContent);
            }

            return Json(model);
        }

        private async Task<List<dynamic>> GetDashboardsData(bool adminMode = false)
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

                var response = await client.PostAsync(new Uri(settings.ApiUrl + $"/ReportApi/GetDashboards"), content);
                var stringContent = await response.Content.ReadAsStringAsync();

                var model = (new JavaScriptSerializer()).Deserialize<List<dynamic>>(stringContent);
                return model;
            }
        }

        public JsonResult GetUsersAndRoles()
        {
            // These report permission settings will be applied by default to any new report user creates, leave black to allow access to all
            var newReportClientId = ""; // comma separated client ids to set report permission when new report is created
            var newReportEditUserId = ""; // comma separated user ids for report edit permission when new report is created
            var newReportViewUserId = ""; // comma separated user ids for report view permission when new report is created
            var newReportEditUserRoles = ""; // comma separated user roles for report edit permission when new report is created
            var newReportViewUserRoles = ""; // comma separated user roles for report view permission when new report is created

            var settings = GetSettings();
            return Json(new
            {
                noAccount = string.IsNullOrEmpty(settings.AccountApiToken) || settings.AccountApiToken == "Your Public Account Api Token",
                users = settings.Users,
                userRoles = settings.UserRoles,
                currentUserId = settings.UserId,
                currentUserRoles = settings.CurrentUserRole,
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
            });
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<JsonResult> GetSchemaFromSql(SchemaFromSqlCall data)
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

                if (string.IsNullOrEmpty(data.value) || !data.value.StartsWith("SELECT ")) {
                    throw new Exception("Invalid SQL");
                }
                table.CustomTableSql = data.value;

                var connString = await DotNetReportHelper.GetConnectionString(DotNetReportHelper.GetConnection(data.dataConnectKey));
                using (OleDbConnection conn = new OleDbConnection(connString))
                {
                    // open the connection to the database 
                    conn.Open();
                    OleDbCommand cmd = new OleDbCommand(data.value, conn);
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
                                FieldType = DotNetSetupController.ConvertToJetDataType((int)dr["ProviderType"]).ToString(),
                                AllowedRoles = new List<string>(),
                                Selected = true
                            };

                            idx++;
                            table.Columns.Add(column);
                        }
                        table.Columns = table.Columns.OrderBy(x => x.DisplayOrder).ToList();
                    }

                    return Json(table, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { ex.Message }, JsonRequestBehavior.AllowGet);
            }
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

        [HttpGet]
        public async Task<ActionResult> GetAllTimezones()
        {
            try
            {
                var timeZones = GetTimezones(); // Call your existing GetTimezones method
                return Json(timeZones, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { ex.Message }, JsonRequestBehavior.AllowGet);
            }
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

        //[Authorize(Roles="Administrator")]
        [HttpPost]
        public async Task<JsonResult> LoadSetupSchema(string databaseApiKey = "", bool onlyApi = false)
        {
            try
            {
                var settings = GetSettings();
                if (!settings.CanUseAdminMode)
                {
                    throw new Exception("Not Authorized to access this Resource");
                }

            var connect = DotNetReportHelper.GetConnection(databaseApiKey);
            var tables = new List<TableViewModel>();
            var procedures = new List<TableViewModel>();
            var functions = new List<CustomFunctionModel>();
            if (onlyApi)
            {
                tables.AddRange(await DotNetSetupController.GetApiTables(connect.AccountApiKey, connect.DatabaseApiKey, true));
            }
            else
            {
                tables.AddRange(await DotNetSetupController.GetTables("TABLE", connect.AccountApiKey, connect.DatabaseApiKey));
                tables.AddRange(await DotNetSetupController.GetTables("VIEW", connect.AccountApiKey, connect.DatabaseApiKey));
            }
            procedures.AddRange(await DotNetSetupController.GetApiProcs(connect.AccountApiKey, connect.DatabaseApiKey));
            functions.AddRange(await DotNetReportHelper.GetApiFunctions(connect.AccountApiKey, connect.DatabaseApiKey));

            var model = new ManageViewModel
            {
                ApiUrl = connect.ApiUrl,
                AccountApiKey = connect.AccountApiKey,
                DatabaseApiKey = connect.DatabaseApiKey,
                Tables = tables,
                Procedures = procedures,
                Functions = functions
            };

                return new JsonResult { Data = model, MaxJsonLength = int.MaxValue };
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500; 
                return Json(new { ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public class SearchProcCall { 
            public string value { get; set; } 
            public string accountKey { get; set; } 
            public string dataConnectKey { get; set; } 
        }

        public class SchemaFromSqlCall : SearchProcCall
        {
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<JsonResult> SearchProcedure(SearchProcCall data)
        {
            try{
                string value = data.value; string accountKey = data.accountKey; string dataConnectKey = data.dataConnectKey;
                return Json(await GetSearchProcedure(value, accountKey, dataConnectKey), JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        private async Task<List<TableViewModel>> GetSearchProcedure(string value = null, string accountKey = null, string dataConnectKey = null)
        {
            var tables = new List<TableViewModel>();
            var connString = await DotNetReportHelper.GetConnectionString(DotNetReportHelper.GetConnection(dataConnectKey));
            using (OleDbConnection conn = new OleDbConnection(connString))
            {
                // open the connection to the database 
                conn.Open();
                string spQuery = "SELECT ROUTINE_NAME, ROUTINE_DEFINITION, ROUTINE_SCHEMA FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_NAME LIKE ? AND ROUTINE_TYPE = 'PROCEDURE'";
                OleDbCommand cmd = new OleDbCommand(spQuery, conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.Add(new OleDbParameter("?", $"%{value}%"));
                DataTable dtProcedures = new DataTable();
                dtProcedures.Load(cmd.ExecuteReader());
                int count = 1;

                if (dtProcedures.Rows.Count == 0)
                {
                    throw new Exception($"No stored procs found matching {value}");
                }
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
                                ParameterDataTypeString = DotNetSetupController.GetType(DotNetSetupController.ConvertToJetDataType(Convert.ToInt32(param.OleDbType))).Name
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

                    if (dt == null) continue;

                    // Store the table names in the class scoped array list of table names
                    List<ColumnViewModel> columnViewModels = new List<ColumnViewModel>();
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        var column = new ColumnViewModel
                        {
                            ColumnName = dt.Rows[i].ItemArray[0].ToString(),
                            DisplayName = dt.Rows[i].ItemArray[0].ToString(),
                            FieldType = DotNetSetupController.ConvertToJetDataType((int)dt.Rows[i]["ProviderType"]).ToString()
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