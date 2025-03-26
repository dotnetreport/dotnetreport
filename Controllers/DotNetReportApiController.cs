using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using ReportBuilder.Web.Helper;
using ReportBuilder.Web.Models;
using System.Data;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Web;

namespace ReportBuilder.Web.Controllers
{
    [Authorize]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class DotNetReportApiController : ControllerBase
    {
        private readonly IConfigurationRoot _configuration;
        public readonly static string _configFileName = "appsettings.dotnetreport.json";
        public readonly static string dbtype = DbTypes.MS_SQL.ToString().Replace("_", " ");
        public DotNetReportApiController()
        {
            var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            _configuration = builder.Build();
        }

        private DotNetReportSettings GetSettings()
        {
            var settings = new DotNetReportSettings
            {
                ApiUrl = _configuration.GetValue<string>("dotNetReport:apiUrl"),
                AccountApiToken = _configuration.GetValue<string>("dotNetReport:accountApiToken"), // Your Account Api Token from your http://dotnetreport.com Account
                DataConnectApiToken = _configuration.GetValue<string>("dotNetReport:dataconnectApiToken") // Your Data Connect Api Token from your http://dotnetreport.com Account            };
            };

            var appSettings = DotNetReportHelper.GetAppSettings();
            var userConfig = "";
            var dbConfig = DotNetReportHelper.GetDbConnectionSettings(settings.AccountApiToken, settings.DataConnectApiToken);
            if (dbConfig != null && dbConfig["UserConfig"] != null)
            {
                userConfig = dbConfig["UserConfig"].ToString();
            }

            if (!string.IsNullOrEmpty(appSettings.backendApiUrl))
            {
                settings.ApiUrl = appSettings.backendApiUrl;
            }

            // Populate the values below using your Application Roles/Claims if applicable
            settings.ClientId = "";  // You can pass your multi-tenant client id here to track their reports and folders
            settings.UserId = ""; // You can pass your current authenticated user id here to track their reports and folders            
            settings.UserName = "";
            settings.CurrentUserRole = new List<string>(); // Populate your current authenticated user's roles

            settings.Users = new List<dynamic>(); // Populate all your application's user, ex  { "Jane", "John" } or { new { id="1", text="Jane" }, new { id="2", text="John" }}
            settings.UserRoles = new List<string>(); // Populate all your application's user roles, ex  { "Admin", "Normal" }       
            settings.CanUseAdminMode = ClaimsHelper.HasAnyRequiredClaim(User as ClaimsPrincipal, ClaimsStore.AllowAdminMode); // Set to true only if current user can use Admin mode to setup reports, dashboard and schema
            settings.DataFilters = new { }; // add global data filters to apply as needed https://dotnetreport.com/kb/docs/advance-topics/global-filters/

            if (userConfig == "dnr-managed")
            {
               //Handle Dnr-Managed Code
            }

            return settings;
        }

        public class GetLookupListParameters
        {
            public string lookupSql { get; set; }
            public string connectKey { get; set; }
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public IActionResult GetLookupList(GetLookupListParameters model)
        {
            string lookupSql = model.lookupSql;
            string connectKey = model.connectKey;
            var qry = new SqlQuery();
            var sql = DotNetReportHelper.Decrypt(lookupSql);
            if (sql.StartsWith("{\"sql\""))
            {
                qry = JsonSerializer.Deserialize<SqlQuery>(sql);
                sql = qry.sql;
            }

            // Uncomment if you want to restrict max records returned
            sql = sql.Replace("SELECT ", "SELECT TOP 500 ");

            var json = new StringBuilder();
            var dt = new DataTable();

            var connect = DotNetReportHelper.GetConnection();
            var dbConfig = DotNetReportHelper.GetDbConnectionSettings(connect.AccountApiKey, connect.DatabaseApiKey);
            if (dbConfig == null)
            {
                throw new Exception("Data Connection settings not found");
            }

            var dbtype = dbConfig["DatabaseType"].ToString();
            string connectionString = dbConfig["ConnectionString"].ToString();
            IDatabaseConnection databaseConnection = DatabaseConnectionFactory.GetConnection(dbtype);
            dt = databaseConnection.ExecuteQuery(connectionString, sql, qry.parameters);

            var data = new List<object>();
            foreach (DataRow dr in dt.Rows)
            {
                data.Add(new { id = dr[0], text = dr[1] });
            }

            return Ok(data);
        }

        public class PostReportApiCallMode
        {
            public string method { get; set; }
            public string headerJson { get; set; }
            public bool useReportHeader { get; set; }

        }

        public class ReportApiCallModel
        {
            public string method { get; set; }
            public string model { get; set; }
        }

        [AllowAnonymous]
        public async Task<IActionResult> CallReportApiUnAuth(string method, string model)
        {
            var settings = new DotNetReportSettings
            {
                ApiUrl = _configuration.GetValue<string>("dotNetReport:apiUrl"),
                AccountApiToken = _configuration.GetValue<string>("dotNetReport:accountApiToken"), // Your Account Api Token from your http://dotnetreport.com Account
                DataConnectApiToken = _configuration.GetValue<string>("dotNetReport:dataconnectApiToken") // Your Data Connect Api Token from your http://dotnetreport.com Account            };
            };

            return await ExecuteCallReportApi(method, model, settings);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> PostReportApi(PostReportApiCallMode data)
        {
            string method = data.method;
            return await CallReportApi(method, JsonSerializer.Serialize(data));
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> RunReportApi(DotNetReportApiCall data)
        {
            return await CallReportApi(data.Method, JsonSerializer.Serialize(data));
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> CallPostReportApi(ReportApiCallModel data)
        {
            return await CallReportApi(data.method, data.model);
        }

        [HttpGet]
        public async Task<IActionResult> CallReportApi(string? method, string? model)
        {
            return string.IsNullOrEmpty(method) || string.IsNullOrEmpty(model) ? Ok() : await ExecuteCallReportApi(method, model);
        }

        private async Task<IActionResult> ExecuteCallReportApi(string method, string model, DotNetReportSettings settings = null)
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

                var data = JsonSerializer.Deserialize<Dictionary<string, dynamic>>(model);
                foreach (var key in data.Keys)
                {
                    if (key == "dataConnect" && data[key] is not null)
                    {
                        keyvalues.RemoveAt(keyvalues.FindIndex(kv => kv.Key == "dataConnect"));
                    }
                    if (key == "account" && data[key] is not null)
                    {
                        keyvalues.RemoveAt(keyvalues.FindIndex(kv => kv.Key == "account"));
                    }
                    if ((key != "adminMode" || (key == "adminMode" && settings.CanUseAdminMode)) && data[key] is not null)
                    {
                        keyvalues.Add(new KeyValuePair<string, string>(key, data[key].ToString()));
                    }
                }

                var content = new FormUrlEncodedContent(keyvalues);
                var response = await client.PostAsync(new Uri(settings.ApiUrl + method), content);
                var stringContent = await response.Content.ReadAsStringAsync();

                Response.StatusCode = (int)response.StatusCode;
                var result = JsonSerializer.Deserialize<dynamic>(stringContent);
                if (stringContent == "\"\"") result = new { };
                return Response.StatusCode == 200 ? Ok(result) : BadRequest(result);
            }

        }

        public class RunReportParameters
        {
            public string reportSql { get; set; }
            public string connectKey { get; set; }
            public string reportType { get; set; }
            public int pageNumber { get; set; }
            public int pageSize { get; set; }
            public string sortBy { get; set; }
            public bool desc { get; set; }
            public string ReportSeries { get; set; }

            public string pivotColumn { get; set; }
            public string pivotFunction { get; set; }
            public string reportData { get; set; }
            public bool SubTotalMode { get; set; }
        }

        public class SqlQuery
        {
            public string sql { get; set; } = "";
            public List<KeyValuePair<string, string>> parameters { get; set; } = null;
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> RunReport(RunReportParameters data)
        {
            string reportSql = data.reportSql;
            string connectKey = data.connectKey;
            string reportType = data.reportType;
            int pageNumber = data.pageNumber;
            int pageSize = data.pageSize;
            string sortBy = data.sortBy;
            bool desc = data.desc;
            string reportSeries = data.ReportSeries;
            string pivotColumn = data.pivotColumn;
            string pivotFunction = data.pivotFunction;
            string reportData = data.reportData;
            bool subtotalMode = data.SubTotalMode;
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
                        qry = JsonSerializer.Deserialize<SqlQuery>(sql);
                        sql = qry.sql;
                    }
                    if (!sql.StartsWith("EXEC"))
                    {
                        var fromIndex = DotNetReportHelper.FindFromIndex(sql);
                        sqlFields = DotNetReportHelper.SplitSqlColumns(sql);

                        var sqlFrom = $"SELECT {sqlFields[0]} {sql.Substring(fromIndex)}";
                        bool hasDistinct = sql.Contains("DISTINCT", StringComparison.OrdinalIgnoreCase);
                        if (hasDistinct)
                        {
                            int distinctIndex = sqlFrom.IndexOf("DISTINCT", StringComparison.OrdinalIgnoreCase) + 8;
                            int fromClauseIndex = sqlFrom.IndexOf("FROM", StringComparison.OrdinalIgnoreCase);
                            string distinctColumns = sqlFrom.Substring(distinctIndex, fromClauseIndex - distinctIndex).Trim();

                            sqlCount = $"SELECT COUNT(*) FROM (SELECT DISTINCT {distinctColumns} {sql.Substring(fromIndex)}) AS countQry";
                        }
                        else
                        {
                            sqlCount = $"SELECT COUNT(*) FROM ({(sqlFrom.Contains("ORDER BY", StringComparison.OrdinalIgnoreCase) ? sqlFrom.Substring(0, sqlFrom.IndexOf("ORDER BY", StringComparison.OrdinalIgnoreCase)) : sqlFrom)}) AS countQry";
                        }
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

                        if (!sql.Contains("ORDER BY"))
                            sql = sql + $" ORDER BY {(hasDistinct ? "1" : "NEWID()")} ";
                        if (!sql.Contains(" TOP ") && string.IsNullOrEmpty(pivotColumn))
                            sql = sql + $" OFFSET {(pageNumber - 1) * pageSize} ROWS FETCH NEXT {pageSize} ROWS ONLY";

                        if (sql.Contains("__jsonc__"))
                            sql = sql.Replace("__jsonc__", "");
                    }

                    // Execute sql
                    var connect = DotNetReportHelper.GetConnection();
                    var dbConfig = DotNetReportHelper.GetDbConnectionSettings(connect.AccountApiKey, connect.DatabaseApiKey);
                    if (dbConfig == null)
                    {
                        throw new Exception("Data Connection settings not found");
                    }

                    var dbtype = dbConfig["DatabaseType"].ToString();
                    string connectionString = dbConfig["ConnectionString"].ToString();
                    IDatabaseConnection databaseConnection = DatabaseConnectionFactory.GetConnection(dbtype);

                    var dtPagedRun = new DataTable();

                    if (!string.IsNullOrEmpty(pivotColumn))
                    {
                        sql = sql.Remove(sql.IndexOf("SELECT "), "SELECT ".Length).Insert(sql.IndexOf("SELECT "), "SELECT TOP 1 ");
                    }
                    else
                    {
                        totalRecords = databaseConnection.GetTotalRecords(connectionString, sqlCount, sql, qry.parameters);
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
                            var pd = await DotNetReportHelper.GetPivotTable(databaseConnection, connectionString, dtPagedRun, sql, sqlFields, reportData, pivotColumn, pivotFunction, pageNumber, pageSize, sortBy, desc, subtotalMode);
                            dtPagedRun = pd.dt; 
                            if (!string.IsNullOrEmpty(pd.sql)) sql = pd.sql;
                            totalRecords =pd.totalRecords;

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

                if (dtPaged.Rows.Count > pageSize)
                {
                    dtPaged = dtPaged.AsEnumerable().Skip((pageNumber - 1) * pageSize).Take(pageSize).CopyToDataTable();
                }

                var model = new DotNetReportResultModel
                {
                    ReportData = DotNetReportHelper.DataTableToDotNetReportDataModel(dtPaged, fields),
                    //Warnings = GetWarnings(sql),
                    ReportSql = sql,
                    ReportDebug = Request.Host.Host.Contains("localhost"),
                    Pager = new DotNetReportPagerModel
                    {
                        CurrentPage = pageNumber,
                        PageSize = pageSize,
                        TotalRecords = totalRecords,
                        TotalPages = (int)(totalRecords == pageSize ? (totalRecords / pageSize) : (totalRecords / pageSize) + 1)
                    }
                };

                return new JsonResult(model, new JsonSerializerOptions() { PropertyNamingPolicy = null });

            }

            catch (Exception ex)
            {
                var model = new DotNetReportResultModel
                {
                    ReportData = new DotNetReportDataModel(),
                    ReportSql = sql,
                    HasError = true,
                    Exception = ex.Message,
                    ReportDebug = Request.Host.Host.Contains("localhost"),
                };

                return new JsonResult(model, new JsonSerializerOptions() { PropertyNamingPolicy = null });
            }
        }

        [HttpGet]
        public async Task<IActionResult> RunReportLink(int reportId, int? filterId = null, string filterValue = "", bool adminMode = false)
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
                    new KeyValuePair<string, string>("dataFilters", JsonSerializer.Serialize(settings.DataFilters)),
                    new KeyValuePair<string, string>("useParameters", "true")
                });

                var response = await client.PostAsync(new Uri(settings.ApiUrl + $"/ReportApi/RunLinkedReport"), content);
                var stringContent = await response.Content.ReadAsStringAsync();

                model = JsonSerializer.Deserialize<DotNetReportModel>(stringContent); 

            }

            return new JsonResult(model, new JsonSerializerOptions() { PropertyNamingPolicy = null });
        }


        [HttpGet]
        public async Task<IActionResult> GetDashboards(bool adminMode = false)
        {
            var model = await GetDashboardsData(adminMode);          
            return Ok(model);
        }


        [HttpGet]
        public async Task<IActionResult> LoadSavedDashboard(int? id = null, bool adminMode = false)
        {
            var settings = GetSettings();
            var model = new List<DotNetDasboardReportModel>();
            var dashboards = (await GetDashboardsData(adminMode));
            if (!id.HasValue && dashboards.Count > 0)
            {
                id = ((dynamic)dashboards.First()).Id;
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
                    new KeyValuePair<string, string>("dataFilters", JsonSerializer.Serialize(settings.DataFilters))
                });

                var response = await client.PostAsync(new Uri(settings.ApiUrl + $"/ReportApi/LoadDashboardData"), content);
                var stringContent = await response.Content.ReadAsStringAsync();

                model = JsonSerializer.Deserialize<List<DotNetDasboardReportModel>>(stringContent);
            }

            return new JsonResult(model, new JsonSerializerOptions() { PropertyNamingPolicy = null });
        }

        private async Task<dynamic> GetDashboardsData(bool adminMode = false)
        {
            var settings = GetSettings();
            if (string.IsNullOrEmpty(settings.AccountApiToken))
            {
                return new { noAccount = true };
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
                    new KeyValuePair<string, string>("adminMode", adminMode.ToString()),
                });

                var response = await client.PostAsync(new Uri(settings.ApiUrl + $"/ReportApi/GetDashboards"), content);
                var stringContent = await response.Content.ReadAsStringAsync();

                var model = JsonSerializer.Deserialize<dynamic>(stringContent);
                return model;
            }
        }

        [HttpGet]
        public IActionResult GetUsersAndRoles()
        {
            // These report permission settings will be applied by default to any new report user creates, leave black to allow access to all
            var newReportClientId = ""; // comma separated client ids to set report permission when new report is created
            var newReportEditUserId = ""; // comma separated user ids for report edit permission when new report is created
            var newReportViewUserId = ""; // comma separated user ids for report view permission when new report is created
            var newReportEditUserRoles = ""; // comma separated user roles for report edit permission when new report is created
            var newReportViewUserRoles = ""; // comma separated user roles for report view permission when new report is created

            var settings = GetSettings();
            return Ok(new
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

        private static string TryDecrypt(string sql)
        {
            try
            {
                return DotNetReportHelper.Decrypt(sql);
            }catch (Exception ex)
            {
                return sql;
            }
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> GetSchemaFromSql([FromBody] SchemaFromSqlCall data)
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

                data.value = TryDecrypt(data.value);

                if (string.IsNullOrEmpty(data.value) || !data.value.StartsWith("SELECT ", StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception("Invalid SQL");
                }
                table.CustomTableSql = data.value;

                var connString = await DotNetReportHelper.GetConnectionString(DotNetReportHelper.GetConnection(data.dataConnectKey), false);
                IDatabaseConnection databaseConnection = DatabaseConnectionFactory.GetConnection(dbtype);
                table = await databaseConnection.GetSchemaFromSql(connString, table, data.value, data.dynamicColumns);

                return new JsonResult(table, new JsonSerializerOptions() { PropertyNamingPolicy = null });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return new JsonResult(new { ex.Message }, new JsonSerializerOptions() { PropertyNamingPolicy = null });
            }
        }


        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> GetPreviewFromSql(SchemaFromSqlCall data)
        {
            string reportSql = data.value;
            int pageNumber = 1;
            int pageSize = 100;
            var sql = "";

            try
            {
                if (string.IsNullOrEmpty(reportSql))
                {
                    throw new Exception("Query not found");
                }
                sql = TryDecrypt(HttpUtility.HtmlDecode(reportSql));


                List<string> fields = new List<string>();
                List<string> sqlFields = new List<string>();
                // Execute sql
                var connString = await DotNetReportHelper.GetConnectionString(DotNetReportHelper.GetConnection(data.dataConnectKey), false);
                IDatabaseConnection databaseConnection = DatabaseConnectionFactory.GetConnection(dbtype);
                var dtPaged = databaseConnection.ExecuteQuery(connString, sql);

                var model = new DotNetReportResultModel
                {
                    ReportData = DotNetReportHelper.DataTableToDotNetReportDataModel(dtPaged, fields),
                    ReportSql = sql,
                    ReportDebug = Request.Host.Host.Contains("localhost"),
                    Pager = new DotNetReportPagerModel
                    {
                        CurrentPage = pageNumber,
                        PageSize = pageSize,
                        TotalRecords = 100,
                        TotalPages = 1
                    }
                };

                return new JsonResult(model, new JsonSerializerOptions() { PropertyNamingPolicy = null });
            }

            catch (Exception ex)
            {
                var model = new DotNetReportResultModel
                {
                    ReportData = new DotNetReportDataModel(),
                    ReportSql = sql,
                    HasError = true,
                    Exception = ex.Message,
                    ReportDebug = Request.Host.Host.Contains("localhost"),
                };

                return new JsonResult(model, new JsonSerializerOptions() { PropertyNamingPolicy = null });
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
        public async Task<IActionResult> GetAllTimezones()
        {
            try
            {
                var timeZones = GetTimezones(); // Call your existing GetTimezones method
                return new JsonResult(timeZones, new JsonSerializerOptions() { PropertyNamingPolicy = null });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return new JsonResult(new { ex.Message }, new JsonSerializerOptions() { PropertyNamingPolicy = null });
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

        [CustomAuthorize(ClaimsStore.AllowSetupPageAccess)]
        [HttpGet]
        public async Task<IActionResult> LoadSetupSchema(string? databaseApiKey = "", bool? onlyApi = null)
        {
            try
            {
                var settings = GetSettings();

                if (string.IsNullOrEmpty(settings.AccountApiToken))
                {
                    return new JsonResult(new { noAccount = true }, new JsonSerializerOptions() { PropertyNamingPolicy = null });
                }

                if (!settings.CanUseAdminMode)
                {
                    throw new Exception("Not Authorized to access this Resource");
                }

                var connect = DotNetReportHelper.GetConnection(databaseApiKey);
                var dbConfig = DotNetReportHelper.GetDbConnectionSettings(connect.AccountApiKey, connect.DatabaseApiKey) ?? new JObject();
                if (dbConfig == null)
                {
                    throw new Exception("Data Connection settings not found");
                }

                var _dbtype = dbConfig["DatabaseType"]?.ToString() ?? dbtype;
                string connectionString = dbConfig["ConnectionString"]?.ToString();
                IDatabaseConnection databaseConnection = DatabaseConnectionFactory.GetConnection(dbtype);

                var tables = new List<TableViewModel>();
                var procedures = new List<TableViewModel>();
                var functions = new List<CustomFunctionModel>();
                if (onlyApi.GetValueOrDefault())
                {
                    tables.AddRange(await DotNetReportHelper.GetApiTables(connect.AccountApiKey, connect.DatabaseApiKey, true));
                }
                else
                {
                    tables.AddRange(await databaseConnection.GetTables(connectionString, "TABLE", connect.AccountApiKey, connect.DatabaseApiKey));
                    tables.AddRange(await databaseConnection.GetTables(connectionString, "VIEW", connect.AccountApiKey, connect.DatabaseApiKey));
                }
                procedures.AddRange(await DotNetReportHelper.GetApiProcs(connect.AccountApiKey, connect.DatabaseApiKey));
                functions.AddRange(await DotNetReportHelper.GetApiFunctions(connect.AccountApiKey, connect.DatabaseApiKey));

                var model = new ManageViewModel
                {
                    ApiUrl = connect.ApiUrl,
                    AccountApiKey = connect.AccountApiKey,
                    DatabaseApiKey = connect.DatabaseApiKey,
                    Tables = tables,
                    Procedures = procedures,
                    Functions = functions,
                    DbConfig = dbConfig.ToObject<Dictionary<string, string>>(),
                    UserAndRolesConfig = new UserRolesConfig { RequireLogin = true, UserRolesSource = true, UsersSource = true }
                };

                return new JsonResult(model, new JsonSerializerOptions() { PropertyNamingPolicy = null });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Message = ex.Message }, new JsonSerializerOptions() { PropertyNamingPolicy = null }) { StatusCode = (int)HttpStatusCode.InternalServerError };
            }
        }

        [CustomAuthorize(ClaimsStore.AllowSetupPageAccess)]
        [HttpPost]
        public async Task<IActionResult> UpdateDbConnection(UpdateDbConnectionModel model)
        {
            try
            {
                var settings = GetSettings();
                if (!settings.CanUseAdminMode)
                {
                    throw new Exception("Not Authorized to access this Resource");
                }

                DotNetReportHelper.UpdateDbConnection(model);

                // Update to Account as well
                //var result = await ExecuteCallReportApi("ReportApi/UpdateDataConnection", JsonSerializer.Serialize(new
                //{
                //    model.dataConnect,
                //    model.dbType,
                //}));

                return new JsonResult(new
                {
                    success = true,
                    message = $"Connection Settings {(model.testOnly ? "Tested" : "Saved")} Successfully"
                }, new JsonSerializerOptions() { PropertyNamingPolicy = null });
            }
            catch(Exception ex)
            {

                return new JsonResult(new
                {
                    success = false,
                    message = ex.Message
                }, new JsonSerializerOptions() { PropertyNamingPolicy = null });

            }
        }

        [CustomAuthorize(ClaimsStore.AllowSetupPageAccess)]
        [HttpPost]
        public async Task<IActionResult> UpdateUserConfigSetting(UpdateUserConfigModel model)
        {
            try
            {
                var settings = GetSettings();
                if (!settings.CanUseAdminMode)
                {
                    throw new Exception("Not Authorized to access this Resource");
                }

                DotNetReportHelper.UpdateUserConfigSetting(model);


                return new JsonResult(new
                {
                    success = true,
                    message = $"User Setting updated successfully"
                }, new JsonSerializerOptions() { PropertyNamingPolicy = null });
            }
            catch (Exception ex)
            {

                return new JsonResult(new
                {
                    success = false,
                    message = ex.Message
                }, new JsonSerializerOptions() { PropertyNamingPolicy = null });

            }
        }
        

        [CustomAuthorize(ClaimsStore.AllowSetupPageAccess)]
        [HttpPost]
        public async Task<IActionResult> SaveAppSettings(AppSettingModel model)
        {
            try
            {
                var settings = GetSettings();
                if (!settings.CanUseAdminMode)
                {
                    throw new Exception("Not Authorized to access this Resource");
                }

                DotNetReportHelper.SaveAppSettings(model);

                return new JsonResult(new
                {
                    success = true,
                    message = $"Settings Access Saved Successfully"
                }, new JsonSerializerOptions() { PropertyNamingPolicy = null });
            }
            catch (Exception ex)
            {

                return new JsonResult(new
                {
                    success = false,
                    message = ex.Message
                }, new JsonSerializerOptions() { PropertyNamingPolicy = null });

            }
        }

        [CustomAuthorize(ClaimsStore.AllowSetupPageAccess)]
        [HttpPost]
        public async Task<IActionResult> SwitchDbConnection([FromBody] string dataConnection)
        {
            try
            {
                var settings = GetSettings();
                if (!settings.CanUseAdminMode)
                {
                    throw new Exception("Not Authorized to access this Resource");
                }
               
                if (!string.IsNullOrEmpty(dataConnection))
                {
                    DotNetReportHelper.UpdateConfigurationFile(settings.AccountApiToken, "", dataConnection);
                }
                return new JsonResult(new
                {
                    success = true,
                    message = $"Connection Switched Successfully"
                }, new JsonSerializerOptions() { PropertyNamingPolicy = null });
            }
            catch (Exception ex)
            {

                return new JsonResult(new
                {
                    success = false,
                    message = ex.Message
                }, new JsonSerializerOptions() { PropertyNamingPolicy = null });

            }
        }

        public async Task<IActionResult> AppSettings()
        {
            try
            {
                var settings = DotNetReportHelper.GetAppSettings();
                return new JsonResult(settings, new JsonSerializerOptions() { PropertyNamingPolicy = null });
            }
            catch (Exception ex)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = ex.Message
                }, new JsonSerializerOptions() { PropertyNamingPolicy = null });
            }
        }

        public class SearchProcCall { 
            public string value { get; set; } 
            public string accountKey { get; set; } 
            public string dataConnectKey { get; set; }
            public bool dynamicColumns { get; set; } = false;
        }

        public class SchemaFromSqlCall : SearchProcCall
        {
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> SearchProcedure([FromBody] SearchProcCall data)
        {
            try
            {
                string value = data.value; string accountKey = data.accountKey; string dataConnectKey = data.dataConnectKey;
                var dbConfig = DotNetReportHelper.GetDbConnectionSettings(accountKey, dataConnectKey) ?? new JObject();
                if (dbConfig == null)
                {
                    throw new Exception("Data Connection settings not found");
                }

                var dbtype = dbConfig["DatabaseType"].ToString();
                string connectionString = dbConfig["ConnectionString"].ToString();
                IDatabaseConnection databaseConnection = DatabaseConnectionFactory.GetConnection(dbtype);


                return new JsonResult(await databaseConnection.GetSearchProcedure(connectionString, value, accountKey, dataConnectKey), new JsonSerializerOptions() { PropertyNamingPolicy = null });
            }
            catch (Exception ex)

            {
                Response.StatusCode = 500;
                return new JsonResult(new { ex.Message }, new JsonSerializerOptions() { PropertyNamingPolicy = null });
            }
        }       
    }

}