using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportBuilder.Web.Models;
using System.Data;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using JsonConvert = Newtonsoft.Json.JsonConvert;

namespace ReportBuilder.Web.Controllers
{
    //[Authorize]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class DotNetReportApiController : ControllerBase
    {
        private readonly IConfigurationRoot _configuration;
        public readonly static string _configFileName = "appsettings.dotnetreport.json";
        
        public DotNetReportApiController()
        {
            var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            _configuration = builder.Build();
        }

        private DotNetReportSettings GetSettings()
        {
            DotNetReportHelper.dbtype = DbTypes.MS_SQL.ToDbString();

            var settings = new DotNetReportSettings
            {
                ApiUrl = _configuration.GetValue<string>("dotNetReport:apiUrl"),
                AccountApiToken = _configuration.GetValue<string>("dotNetReport:accountApiToken"), // Your Account Api Token from your http://dotnetreport.com Account
                DataConnectApiToken = _configuration.GetValue<string>("dotNetReport:dataconnectApiToken") // Your Data Connect Api Token from your http://dotnetreport.com Account            };
            };

            // Populate the values below using your Application Roles/Claims if applicable
            settings.ClientId = "";  // You can pass your multi-tenant client id here to track their reports and folders
            settings.UserId = ""; // You can pass your current authenticated user id here to track their reports and folders            
            settings.UserName = "";
            settings.CurrentUserRole = new List<string>(); // Populate your current authenticated user's roles

            settings.Users = new List<dynamic>() { }; // Populate all your application's user, ex  { "Jane", "John" } or { new { id="1", text="Jane" }, new { id="2", text="John" }}
            settings.UserRoles = new List<string>() { }; // Populate all your application's user roles, ex  { "Admin", "Normal" }       
            settings.CanUseAdminMode = true; // Set to true only if current user can use Admin mode to setup reports, dashboard and schema
            settings.DataFilters = new { }; // add global data filters to apply as needed https://dotnetreport.com/kb/docs/advance-topics/global-filters/

            return settings;
        }

        public class GetLookupListParameters
        {
            public string lookupSql { get; set; }
            public string connectKey { get; set; }
            public string token { get; set; } = "";
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
                DotNetReportHelper.dbtype = qry.dbType;
            }

            // Uncomment if you want to restrict max records returned
            sql = sql.Substring(0, 0) + "SELECT DISTINCT TOP 500 " + sql.Substring(0 + "SELECT ".Length);
            string token = model.token;
            string lastToken = "";
            if (sql.Contains("{{token}}"))
            {
                token = Uri.UnescapeDataString(token);
                if (!string.IsNullOrWhiteSpace(token))
                {
                    var parts = token
                        .Split(',', StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length > 0)
                    {
                        lastToken = parts[parts.Length - 1].Trim();
                    }
                }
                lastToken = lastToken.Replace("'", "");
                sql = sql.Replace("{{token}}", $"'%{lastToken}%'");
            }
            sql = ConvertTopQuery(sql, DotNetReportHelper.dbtype);
            var json = new StringBuilder();
            var dt = new DataTable();

            var connectionString = DotNetReportHelper.GetConnectionString(connectKey);
            IDatabaseConnection databaseConnection = DatabaseConnectionFactory.GetConnection(DotNetReportHelper.dbtype);

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
            public string headerClientId { get; set; } = "";
            public string? userId { get; set; }

        }

        public class ReportApiCallModel
        {
            public string method { get; set; }
            public string model { get; set; }
            public string? userId { get; set; }
        }

        [AllowAnonymous]
        public async Task<IActionResult> CallReportApiUnAuth(string method, string model, string exportId)
        {
            var settings = ExportSessionStore.Get(exportId);
            if (settings == null)
                return Unauthorized();

            settings.ApiUrl = _configuration.GetValue<string>("dotNetReport:apiUrl");
            settings.AccountApiToken = _configuration.GetValue<string>("dotNetReport:accountApiToken");
            settings.DataConnectApiToken = _configuration.GetValue<string>("dotNetReport:dataconnectApiToken");
            settings.CanUseAdminMode = true;
            return await ExecuteCallReportApi(method, model, null, settings);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> PostReportApi(PostReportApiCallMode data)
        {
            string method = data.method;
            return await CallReportApi(method, JsonSerializer.Serialize(data), data.userId);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> RunReportApi(DotNetReportApiCall data)
        {
            return await CallReportApi(data.Method, JsonSerializer.Serialize(data), data.userId);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> CallPostReportApi(ReportApiCallModel data)
        {
            return await CallReportApi(data.method, data.model, data.userId);
        }

        [HttpGet]
        public async Task<IActionResult> CallReportApi(string? method, string? model, string? userId = "")
        {
            var settings = GetSettings();
            if (!string.IsNullOrEmpty(settings.UserId) && settings.UserId != userId)
            {
                throw new Exception("User context mismatch");
            }
            return string.IsNullOrEmpty(method) || string.IsNullOrEmpty(model) ? Ok() : await ExecuteCallReportApi(method, model, userId);
        }

        private async Task<IActionResult> ExecuteCallReportApi(string method, string model, string userId, DotNetReportSettings settings = null)
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
                    new KeyValuePair<string, string>("userIdForFilter", settings.UserIdForFilter),
                    new KeyValuePair<string, string>("userRole", string.Join(",", settings.CurrentUserRole)),
                    new KeyValuePair<string, string>("dataFilters", JsonSerializer.Serialize(settings.DataFilters)),
                    new KeyValuePair<string, string>("useParameters", DotNetReportHelper.dbtype=="MS SQL" ? "true" : "false")
                };

                var data = JsonSerializer.Deserialize<Dictionary<string, dynamic>>(model);
                var adminMode = false; var dashboardId = 0; var folderId = 0; var reportId = 0;
                foreach (var key in data.Keys)
                {
                    if (key == "dataConnect" && data[key] is not null)
                    {
                        keyvalues.RemoveAll(kv => kv.Key == "dataConnect");
                    }
                    if (key == "account" && data[key] is not null)
                    {
                        keyvalues.RemoveAll(kv => kv.Key == "account");
                    }
                    if ((key != "adminMode" || (key == "adminMode" && settings.CanUseAdminMode)) && data[key] is not null)
                    {
                        keyvalues.Add(new KeyValuePair<string, string>(key, data[key].ToString()));
                        if (key == "adminMode") adminMode = ((JsonElement)data[key]).GetBoolean();
                    }
                    if (key == "dashboardId")
                    {
                        dashboardId = ((JsonElement)data[key]).GetInt32();
                    }
                    if (key == "folderId")
                    {
                        folderId = ((JsonElement)data[key]).GetInt32();
                    }
                    if (key == "reportId")
                    {
                        reportId = ((JsonElement)data[key]) is var el && el.ValueKind == JsonValueKind.Number
                                        ? el.GetInt32()
                                        : Convert.ToInt32(el.GetString());
                    }
                }

                if (!adminMode)
                {
                    if (dashboardId > 0) await ValidateAccess(userId, dashboardId: dashboardId);
                    if (folderId > 0) await ValidateAccess(userId, folderId: folderId);
                    if (reportId > 0) await ValidateAccess(userId, reportId: reportId);
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
            public bool useAltPivot { get; set; }
            public bool adminmode { get; set; }
            public bool includeColumnTotal { get; set; }
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> RunReport(RunReportParameters data)
        {
            return await ExecuteRunReport(data);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> RunReportUnAuth([FromQuery] string exportId, [FromBody] RunReportParameters data)
        {
            if (ExportSessionStore.Get(exportId) == null)
                return Unauthorized();
            return await ExecuteRunReport(data);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> RunReportApiUnAuth([FromQuery] string exportId, [FromBody] DotNetReportApiCall data)
        {
            var settings = ExportSessionStore.Get(exportId);
            if (settings == null)
                return Unauthorized();

            settings.ApiUrl = _configuration.GetValue<string>("dotNetReport:apiUrl");
            settings.AccountApiToken = _configuration.GetValue<string>("dotNetReport:accountApiToken");
            settings.DataConnectApiToken = _configuration.GetValue<string>("dotNetReport:dataconnectApiToken");
            settings.CanUseAdminMode = true;
            return await ExecuteCallReportApi(data.Method, JsonSerializer.Serialize(data), data.userId, settings);
        }

        private async Task<IActionResult> ExecuteRunReport(RunReportParameters data)
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
            bool includeColumnTotal = data.includeColumnTotal;
            bool adminmode = data.adminmode;
            var sql = "";
            var sqlCount = "";
            int totalRecords = 0;
            var useAltPivot = data.useAltPivot;
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
                bool hasTop = false;
                List<string> fields = new List<string>();
                List<string> sqlFields = new List<string>();
                for (int i = 0; i < allSqls.Length; i++)
                {
                    sql = DotNetReportHelper.Decrypt(HttpUtility.HtmlDecode(allSqls[i]));
                    if (sql.StartsWith("{\"sql\""))
                    {
                        qry = JsonSerializer.Deserialize<SqlQuery>(sql);
                        sql = qry.sql;
                        if (!string.IsNullOrEmpty(qry.dbType)) DotNetReportHelper.dbtype = qry.dbType;
                    }
                    if (!sql.StartsWith("EXEC"))
                    {
                        var fromIndex = DotNetReportHelper.FindFromIndex(sql);
                        sqlFields = DotNetReportHelper.SplitSqlColumns(sql, DotNetReportHelper.dbtype);

                        var sqlFrom = $"SELECT {sqlFields[0]} {sql.Substring(fromIndex)}".Replace("{FROM}", "FROM");
                        bool hasDistinct = sql.Contains("DISTINCT");
                        if (hasDistinct)
                        {
                            string distinctColumns = string.Join(", ", sqlFields);

                            string fromClause = sql.Substring(fromIndex).Replace("{FROM}", "FROM");

                            // Remove ORDER BY if present
                            int orderByIndex = fromClause.LastIndexOf("ORDER BY", StringComparison.OrdinalIgnoreCase);
                            if (orderByIndex > -1)
                            {
                                fromClause = fromClause.Substring(0, orderByIndex).Trim();
                            }

                            if (DotNetReportHelper.dbtype == "Oracle")
                                sqlCount = "SELECT COUNT(*) FROM (SELECT DISTINCT " + distinctColumns + " " + fromClause + ") countQry";
                            else
                                sqlCount = "SELECT COUNT(*) FROM (SELECT DISTINCT " + distinctColumns + " " + fromClause + ") AS countQry";
                        }
                        else
                        {
                            string inner =
                                sqlFrom.Contains("ORDER BY", StringComparison.OrdinalIgnoreCase)
                                ? sqlFrom.Substring(0, sqlFrom.LastIndexOf("ORDER BY", StringComparison.OrdinalIgnoreCase))
                                : sqlFrom;

                            if (DotNetReportHelper.dbtype == "Oracle")
                                sqlCount = "SELECT COUNT(*) FROM (" + inner + ") countQry";
                            else
                                sqlCount = "SELECT COUNT(*) FROM (" + inner + ") AS countQry";
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
                            if (sortBy.StartsWith("CONCAT(DATE_FORMAT(") || sortBy.StartsWith("DATE_FORMAT("))
                            {
                                var match = System.Text.RegularExpressions.Regex.Match(sortBy, @"`[^`]+`\.`[^`]+`");
                                if (match.Success)
                                    sortBy = $"MIN({match.Value})";
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
                        {
                            if (DotNetReportHelper.dbtype == "MS SQL")
                                sql += $" ORDER BY {(hasDistinct ? "1" : "NEWID()")} ";
                            else if (DotNetReportHelper.dbtype == "PostgreSQL")
                                sql += $" ORDER BY {(hasDistinct ? "1" : "RANDOM()")} ";
                            else if (DotNetReportHelper.dbtype == "MySql")
                                sql += $" ORDER BY {(hasDistinct ? "1" : "RAND()")} ";
                            else if (DotNetReportHelper.dbtype == "Oracle")
                                sql += $" ORDER BY {(hasDistinct ? "1" : "DBMS_RANDOM.VALUE")} ";
                            else
                                sql += " ORDER BY 1 ";
                        }
                        hasTop = sql.IndexOf(" TOP ", StringComparison.OrdinalIgnoreCase) >= 0;
                        if (!hasTop && string.IsNullOrEmpty(pivotColumn))
                        {
                            if (DotNetReportHelper.dbtype == "PostgreSQL" || DotNetReportHelper.dbtype == "MySql")
                            {
                                sql += $" LIMIT {pageSize} OFFSET {(pageNumber - 1) * pageSize}";
                            }
                            else if (DotNetReportHelper.dbtype == "Oracle")
                            {
                                sql += $" OFFSET {(pageNumber - 1) * pageSize} ROWS FETCH NEXT {pageSize} ROWS ONLY";
                            }
                            else
                            {
                                sql += $" OFFSET {(pageNumber - 1) * pageSize} ROWS FETCH NEXT {pageSize} ROWS ONLY";
                            }
                        }
                       
                        if (sql.Contains("__jsonc__"))
                            sql = sql.Replace("__jsonc__", "");

                        sql = sql.Replace("{FROM}", "FROM");
                    }
                    // Execute sql
                    var connectionString = DotNetReportHelper.GetConnectionString(connectKey);
                    IDatabaseConnection databaseConnection = DatabaseConnectionFactory.GetConnection(DotNetReportHelper.dbtype);

                    var dtPagedRun = new DataTable();

                    if (!string.IsNullOrEmpty(pivotColumn) && !useAltPivot)
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
                            var keywordsToExclude = new[] { "Count", "Sum", "Max", "Avg" };
                            if (!useAltPivot)
                            {
                                var pd = await DotNetReportHelper.GetPivotTable(databaseConnection, connectionString, dtPagedRun, sql, sqlFields, reportData, pivotColumn, pivotFunction, pageNumber, pageSize, sortBy, desc, false, includeColumnTotal, subtotalMode);
                                dtPagedRun = pd.dt;
                                if (!string.IsNullOrEmpty(pd.sql)) sql = pd.sql;
                                totalRecords = pd.totalRecords;

                                // Extract original aliases from SQL fields
                                var sqlAliases = fields
                                    .Select(f =>
                                    {
                                        var parts = f.Split(new[] { " AS " }, StringSplitOptions.RemoveEmptyEntries);
                                        return parts.Length == 2 ? parts[1].Trim().Trim('[', ']') : "";
                                    })
                                    .Where(a => !string.IsNullOrWhiteSpace(a))
                                    .ToList();

                                // Now map DataTable columns back to SQL aliases
                                var mapped = dtPagedRun.Columns.Cast<DataColumn>()
                                    .Select(col =>
                                    {
                                        var colName = col.ColumnName;
                                        var lastPart = colName.Contains("|")
                                            ? colName.Substring(colName.LastIndexOf("|") + 1)
                                            : colName;

                                        return sqlAliases.Contains(lastPart)
                                            ? fields.First(f => f.EndsWith($"[{lastPart}]"))
                                            : $"__ AS [{colName}]";
                                    })
                                    .ToList();

                                fields = mapped;

                            }
                            else
                            {
                                reportData = reportData.Replace("\"DrillDownRowUsePlaceholders\":false", $"\"DrillDownRowUsePlaceholders\":true");
                                var ds = await DotNetReportHelper.GetDrillDownData(databaseConnection, connectionString, dtPagedRun, sqlFields, reportData);
                                dtPagedRun = DotNetReportHelper.PushDatasetIntoDataTable(dtPagedRun, ds, pivotColumn, pivotFunction, reportData);
                                if (subtotalMode)
                                {
                                    var columnorder = DotNetReportHelper.GetuseAltPivotColumnOrder(reportData);
                                    dtPagedRun = DotNetReportHelper.ReorderDataTableColumns(dtPagedRun, columnorder);
                                }
                                fields = fields
                                .Where(field => !keywordsToExclude.Any(keyword => field.Contains(keyword)))  // Filter fields to exclude unwanted keywords
                                .ToList();
                                fields.AddRange(dtPagedRun.Columns.Cast<DataColumn>().Skip(fields.Count).Select(x => $"__ AS {x.ColumnName}").ToList());
                            }
                            
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
                if (hasTop && string.IsNullOrEmpty(pivotColumn))
                {
                    var match = Regex.Match(sql, @"TOP\s+(\d+)", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        totalRecords = int.Parse(match.Groups[1].Value);
                    }
                }
                var model = new DotNetReportResultModel
                {
                    ReportData = DotNetReportHelper.DataTableToDotNetReportDataModel(dtPaged, fields),
                    //Warnings = GetWarnings(sql),
                    ReportSql = adminmode ? sql : " ",
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
                    ReportSql = adminmode ? sql : " ",
                    HasError = true,
                    Exception = ex.Message,
                    ReportDebug = Request.Host.Host.Contains("localhost"),
                };

                return new JsonResult(model, new JsonSerializerOptions() { PropertyNamingPolicy = null });
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> RunReportLinkUnAuth(int reportId, int? filterId = null, string filterValue = "", bool adminMode = false, string exportId = "")
        {
            var model = new DotNetReportModel();
            var settings = ExportSessionStore.Get(exportId);
            if (settings == null)
                return Unauthorized();

            settings.ApiUrl = _configuration.GetValue<string>("dotNetReport:apiUrl");
            settings.AccountApiToken = _configuration.GetValue<string>("dotNetReport:accountApiToken");
            settings.DataConnectApiToken = _configuration.GetValue<string>("dotNetReport:dataconnectApiToken");
            settings.CanUseAdminMode = true;

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
                    new KeyValuePair<string, string>("useParameters", DotNetReportHelper.dbtype=="MS SQL" ? "true" : "false")
                });

                var response = await client.PostAsync(new Uri(settings.ApiUrl + $"/ReportApi/RunLinkedReport"), content);
                var stringContent = await response.Content.ReadAsStringAsync();

                model = JsonSerializer.Deserialize<DotNetReportModel>(stringContent);

            }

            return new JsonResult(model, new JsonSerializerOptions() { PropertyNamingPolicy = null });
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
                    new KeyValuePair<string, string>("useParameters", DotNetReportHelper.dbtype=="MS SQL" ? "true" : "false")
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

            ValidateAccess("", "", dashboardId: id.GetValueOrDefault());
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
                dataFilters = new { }, // don't expose to front end
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

                data.value = DotNetReportHelper.TryDecrypt(data.value);

                if (string.IsNullOrEmpty(data.value) || !data.value.StartsWith("SELECT ", StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception("Invalid SQL");
                }
                table.CustomTableSql = data.value;

                var connString = await DotNetReportHelper.GetConnectionString(DotNetReportHelper.GetConnection(data.dataConnectKey), false);
                IDatabaseConnection databaseConnection = DatabaseConnectionFactory.GetConnection(DotNetReportHelper.dbtype);
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
                sql = DotNetReportHelper.TryDecrypt(HttpUtility.HtmlDecode(reportSql));
                sql = ConvertTopQuery(sql, DotNetReportHelper.dbtype);
                List<string> fields = new List<string>();
                List<string> sqlFields = new List<string>();
                // Execute sql
                var connString = await DotNetReportHelper.GetConnectionString(DotNetReportHelper.GetConnection(data.dataConnectKey), false);
                IDatabaseConnection databaseConnection = DatabaseConnectionFactory.GetConnection(DotNetReportHelper.dbtype);
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
        public static string ConvertTopQuery(string sql, string dbtype)
        {
            if (string.IsNullOrWhiteSpace(sql) || string.IsNullOrWhiteSpace(dbtype))
                return sql;
            if (dbtype.Equals("MS SQL", StringComparison.OrdinalIgnoreCase))
                return sql;
            switch (dbtype)
            {
                case "MySql":
                    sql = sql.Replace("[", "`").Replace("]", "`");
                    break;

                case "PostgreSQL":
                    sql = sql.Replace("[", "\"").Replace("]", "\"");
                    break;

                case "Oracle":
                    sql = sql.Replace("[", "").Replace("]", "");
                    break;
            }

            if (sql.Contains("TOP", StringComparison.OrdinalIgnoreCase))
            {
                var m = System.Text.RegularExpressions.Regex.Match(sql, @"TOP\s+(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    var top = m.Groups[1].Value;
                    sql = System.Text.RegularExpressions.Regex.Replace(sql, @"TOP\s+\d+", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    sql = System.Text.RegularExpressions.Regex.Replace(sql, @"\[(.*?)\]", "`$1`").Trim();
                    if (dbtype.Equals("MySql", StringComparison.OrdinalIgnoreCase) ||
                        dbtype.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
                    {
                        sql = sql.TrimEnd(';') + $" LIMIT {top};";
                    }
                    else if (dbtype.Equals("Oracle", StringComparison.OrdinalIgnoreCase))
                    {
                        sql = $"SELECT * FROM ({sql}) WHERE ROWNUM <= {top}";
                    }
                }
            }
            return sql;
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

        [HttpPost]
        public async Task<IActionResult> BuildDynamicFunctions()
        {
            try
            {
                var settings = GetSettings();
                var functions = await DotNetReportHelper.GetApiFunctions();
                DynamicCodeRunner.BuildAssembly(functions);

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {               
                return BadRequest(new
                {
                    success = false,
                    errorMessage = ex.Message
                });
            }
        }

        //[Authorize(Roles="Administrator")]
        [HttpGet]
        public async Task<IActionResult> LoadSetupSchema(string? databaseApiKey = "", bool onlyApi = true)
        {
            try
            {
                var settings = GetSettings();

                if (string.IsNullOrEmpty(settings.AccountApiToken))
                {
                    return Ok(new { noAccount = true });
                }

                if (!settings.CanUseAdminMode)
                {
                    throw new Exception("Not Authorized to access this Resource");
                }

            var connect = DotNetReportHelper.GetConnection(databaseApiKey);
            IDatabaseConnection databaseConnection = DatabaseConnectionFactory.GetConnection(DotNetReportHelper.dbtype);
            var tables = new List<TableViewModel>();
            var procedures = new List<TableViewModel>();
            var functions = new List<CustomFunctionModel>();
            if (onlyApi)
            {
                tables.AddRange(await DotNetReportHelper.GetApiTables(connect.AccountApiKey, connect.DatabaseApiKey, true));
            }
            else
            {
                tables.AddRange(await databaseConnection.GetTables("TABLE", connect.AccountApiKey, connect.DatabaseApiKey));
                tables.AddRange(await databaseConnection.GetTables("VIEW", connect.AccountApiKey, connect.DatabaseApiKey));
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
                CurrentUserId = settings.UserId
            };

                return new JsonResult(model, new JsonSerializerOptions() { PropertyNamingPolicy = null });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return new JsonResult(new { ex.Message }, new JsonSerializerOptions() { PropertyNamingPolicy = null });
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
                IDatabaseConnection databaseConnection = DatabaseConnectionFactory.GetConnection(DotNetReportHelper.dbtype);

                return new JsonResult(await databaseConnection.GetSearchProcedure(value, accountKey, dataConnectKey), new JsonSerializerOptions() { PropertyNamingPolicy = null });
            }
            catch (Exception ex)

            {
                Response.StatusCode = 500;
                return new JsonResult(new { ex.Message }, new JsonSerializerOptions() { PropertyNamingPolicy = null });
            }
        }

        private class checkAccessModel {
            public bool hasAccess { get; set; }
            public string access { get; set; }
        }
        private async Task ValidateAccess(string userId, string reportSql = "", int reportId = 0, int dashboardId = 0, int folderId = 0, bool adminMode = false)
        {
            var isValid = true;
            var settings = GetSettings();
            if ((adminMode && settings.CanUseAdminMode) || (string.IsNullOrEmpty(settings.UserId) && !settings.CurrentUserRole.Any())) return;
            if (!string.IsNullOrEmpty(settings.UserId) && settings.UserId != userId)
            {
                isValid = false;
            }

            if (!string.IsNullOrEmpty(reportSql) && reportId <= 0)
            {

                var sql = DotNetReportHelper.Decrypt(reportSql);
                if (sql.StartsWith("{\"sql\""))
                {
                    var qry = JsonConvert.DeserializeObject<SqlQuery>(sql);
                    if (qry.reportId > 0)
                    {
                        reportId = qry.reportId;
                    }
                }
            }
            if (reportId > 0 || dashboardId > 0 || folderId > 0)
            {
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
                        new KeyValuePair<string, string>("dashboardId", dashboardId.ToString()),
                        new KeyValuePair<string, string>("folderId", folderId.ToString()),
                    });

                    var response = await client.PostAsync(new Uri(settings.ApiUrl + $"/ReportApi/CheckReportAccess"), content);
                    var stringContent = await response.Content.ReadAsStringAsync();

                    var model = JsonConvert.DeserializeObject<checkAccessModel>(stringContent);
                    isValid = model.hasAccess;
                }
            }

            if (!isValid)
            {
                throw new Exception("Could not validate access");
            }
        }

        [HttpPost]
        public async Task<IActionResult> DownloadExcel(
            [FromForm] string reportSql,
            [FromForm] string connectKey,
            [FromForm] string reportName,
            [FromForm] bool allExpanded,
            [FromForm] string expandSqls,
            [FromForm] string chartData = null,
            [FromForm] string columnDetails = null,
            [FromForm] bool includeSubtotal = false,
            [FromForm] bool pivot = false,
            [FromForm] string pivotColumn = null,
            [FromForm] string pivotFunction = null,
            [FromForm] string onlyAndGroupInColumnDetail = null,
            [FromForm] bool isSubReport = false,
            [FromForm] string userId = "",
            [FromForm] bool adminMode = false,
            [FromForm] bool subTotalPerGroup = false,
            [FromForm] string totalRowFormat = "row")
        {
            reportSql = HttpUtility.HtmlDecode(reportSql);
            await ValidateAccess(userId, reportSql, adminMode: adminMode);
            chartData = HttpUtility.UrlDecode(chartData);
            chartData = chartData?.Replace(" ", " +");
            var columns = string.IsNullOrEmpty(columnDetails) ? new List<ReportHeaderColumn>() : Newtonsoft.Json.JsonConvert.DeserializeObject<List<ReportHeaderColumn>>(HttpUtility.UrlDecode(columnDetails));
            var onlyAndGroupInDetailColumns = string.IsNullOrEmpty(onlyAndGroupInColumnDetail) ? new List<ReportHeaderColumn>() : Newtonsoft.Json.JsonConvert.DeserializeObject<List<ReportHeaderColumn>>(HttpUtility.UrlDecode(onlyAndGroupInColumnDetail));

            var excel = await DotNetReportHelper.GetExcelFile(reportSql, connectKey, HttpUtility.UrlDecode(reportName), chartData, allExpanded, HttpUtility.UrlDecode(expandSqls), columns, includeSubtotal, pivot, pivotColumn, pivotFunction, onlyAndGroupInDetailColumns, isSubReport, subTotalPerGroup, totalRowFormat);
            Response.Headers.Add("content-disposition", "attachment; filename=" + reportName + ".xlsx");
            Response.ContentType = "application/vnd.ms-excel";

            return File(excel, "application/vnd.ms-excel", reportName + ".xlsx");
        }

        [HttpPost]
        public async Task<IActionResult> DownloadPdf(
            [FromForm] bool adminMode,
            [FromForm] string printUrl,
            [FromForm] int reportId,
            [FromForm] string reportSql,
            [FromForm] string connectKey,
            [FromForm] string reportName,
            [FromForm] bool expandAll,
            [FromForm] string expandSqls = null,
            [FromForm] string pivotColumn = null,
            [FromForm] string pivotFunction = null,
            [FromForm] bool debug = false,
            [FromForm] string pageSize = "",
            [FromForm] string pageOrientation = "",
            [FromForm] bool includeSubTotal = false,
            [FromForm] bool includeColumnTotal = false,
            [FromForm] string userId = "",
            [FromForm] bool isSubreport = false,
            [FromForm] int pageNumber=1,
            [FromForm] int currentPageSize = 1)
        {

            var settings = GetSettings();
            
            reportSql = HttpUtility.HtmlDecode(reportSql);
            if (!adminMode)
            {
                await ValidateAccess(userId, reportSql, adminMode: adminMode);
            }
            var pdf = await DotNetReportHelper.GetPdfFile(HttpUtility.UrlDecode(printUrl), reportId, reportSql, HttpUtility.UrlDecode(connectKey), HttpUtility.UrlDecode(reportName),
                                settings.UserId, settings.ClientId, string.Join(",", settings.CurrentUserRole), JsonConvert.SerializeObject(settings.DataFilters), expandAll, expandSqls, pivotColumn, pivotFunction, false, debug, pageSize, pageOrientation,includeSubTotal,includeColumnTotal,isSubreport,pageNumber,currentPageSize);

            return File(pdf, "application/pdf", reportName + ".pdf");
        }

        [HttpPost]
        public async Task<IActionResult> DownloadPdfAlt(
           [FromForm] string reportSql,
           [FromForm] string connectKey,
           [FromForm] string reportName,
           [FromForm] bool allExpanded,
           [FromForm] string expandSqls,
           [FromForm] string chartData = null,
           [FromForm] string columnDetails = null,
           [FromForm] bool includeSubtotal = false,
           [FromForm] bool pivot = false,
           [FromForm] string pivotColumn = null,
           [FromForm] string pivotFunction = null,
           [FromForm] string pageSize = "",
           [FromForm] string pageOrientation = "",
           [FromForm] string userId = "",
           [FromForm] bool adminMode = false,
           [FromForm] bool subTotalPerGroup = false)
        {
            reportSql = HttpUtility.HtmlDecode(reportSql);
            await ValidateAccess(userId, reportSql, adminMode: adminMode);
            chartData = HttpUtility.UrlDecode(chartData);
            chartData = chartData?.Replace(" ", " +");
            reportName = HttpUtility.UrlDecode(reportName);
            var columns = columnDetails == null ? new List<ReportHeaderColumn>() : JsonConvert.DeserializeObject<List<ReportHeaderColumn>>(HttpUtility.UrlDecode(columnDetails));

            var pdf = await DotNetReportHelper.GetPdfFileAlt(reportSql, connectKey, reportName, chartData, allExpanded, expandSqls, columns, includeSubtotal, pivot, pivotColumn, pivotFunction, pageSize, pageOrientation, subTotalPerGroup);

            return File(pdf, "application/pdf", reportName + ".pdf");
        }

        [HttpPost]
        public async Task<IActionResult> DownloadWord(
            [FromForm] string reportSql,
            [FromForm] string connectKey,
            [FromForm] string reportName,
            [FromForm] bool allExpanded,
            [FromForm] string expandSqls,
            [FromForm] string chartData = null,
            [FromForm] string columnDetails = null,
            [FromForm] bool includeSubtotal = false,
            [FromForm] bool pivot = false,
            [FromForm] string pivotColumn = null,
            [FromForm] string pivotFunction = null,
            [FromForm] string pageSize = "", 
            [FromForm] string pageOrientation = "",
            [FromForm] string userId = "",
            [FromForm] bool adminMode = false)
        {
            reportSql = HttpUtility.HtmlDecode(reportSql);
            await ValidateAccess(userId, reportSql, adminMode: adminMode);            
            chartData = HttpUtility.UrlDecode(chartData);
            chartData = chartData?.Replace(" ", " +");
            var columns = columnDetails == null ? new List<ReportHeaderColumn>() : JsonConvert.DeserializeObject<List<ReportHeaderColumn>>(HttpUtility.UrlDecode(columnDetails));
            var word = await DotNetReportHelper.GetWordFile(reportSql, connectKey, HttpUtility.UrlDecode(reportName), chartData, allExpanded, HttpUtility.UrlDecode(expandSqls), columns, includeSubtotal, pivot, pivotColumn, pivotFunction, pageSize, pageOrientation);
            Response.Headers.Add("content-disposition", "attachment; filename=" + reportName + ".docx");
            Response.ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
            return File(word, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", reportName + ".docx");
        }       

        [HttpPost]
        public async Task<IActionResult> DownloadCsv(
            [FromForm] string reportSql,
            [FromForm] string connectKey,
            [FromForm] string reportName,
            [FromForm] bool allExpanded,
            [FromForm] string expandSqls,
            [FromForm] string chartData = null,
            [FromForm] string columnDetails = null,
            [FromForm] bool includeSubtotal = false,
            [FromForm] bool pivot = false,
            [FromForm] string pivotColumn = null,
            [FromForm] string pivotFunction = null,
            [FromForm] string userId = "",
            [FromForm] bool adminMode = false)
        {
            reportSql = HttpUtility.HtmlDecode(reportSql);
            await ValidateAccess(userId, reportSql, adminMode: adminMode);
            var columns = columnDetails == null ? new List<ReportHeaderColumn>() : JsonConvert.DeserializeObject<List<ReportHeaderColumn>>(HttpUtility.UrlDecode(columnDetails));

            var csv = await DotNetReportHelper.GetCSVFile(reportSql, HttpUtility.UrlDecode(connectKey), columns, includeSubtotal, expandSqls, pivot, pivotColumn, pivotFunction);

            Response.Headers.Add("content-disposition", "attachment; filename=" + HttpUtility.UrlDecode(reportName) + ".csv");
            Response.ContentType = "text/csv";

            return File(csv, "text/csv", reportName + ".csv");
        }
        [HttpPost]
        public async Task<IActionResult> DownloadXml(
            [FromForm] string reportSql,
            [FromForm] string connectKey,
            [FromForm] string reportName,
            [FromForm] string expandSqls = null,
            [FromForm] string pivotColumn = null,
            [FromForm] string pivotFunction = null,
            [FromForm] string userId = "",
            [FromForm] bool adminMode = false)
        {
            reportSql = HttpUtility.HtmlDecode(reportSql);
            await ValidateAccess(userId, reportSql, adminMode: adminMode);            
            string xml = await DotNetReportHelper.GetXmlFile(reportSql, HttpUtility.UrlDecode(connectKey), HttpUtility.UrlDecode(reportName), expandSqls, pivotColumn, pivotFunction);
            var data = System.Text.Encoding.UTF8.GetBytes(xml);
            Response.ContentType = "text/txt";
            return File(data, "text/txt", reportName + ".xml");
        }

        [HttpPost]
        public async Task<IActionResult> DownloadAllPdf([FromForm] string reportdata, [FromForm] string dashboardName = "CombinedReports")
        {
            var pdfBytesList = new List<byte[]>();
            var settings = GetSettings();
            var reports = reportdata != null ? JsonConvert.DeserializeObject<List<ExportReportModel>>(reportdata) : null;
            foreach (var report in reports)
            {
                var pdf = await DotNetReportHelper.GetPdfFile(report.printUrl, report.reportId, HttpUtility.HtmlDecode(report.reportSql), HttpUtility.UrlDecode(report.connectKey), HttpUtility.UrlDecode(report.reportName), settings.UserId,
                    settings.ClientId, string.Join(",", settings.CurrentUserRole), JsonConvert.SerializeObject(settings.DataFilters), report.expandAll, report.expandSqls, report.pivotColumn, report.pivotFunction,pageSize:report.pageSize,pageOrientation:report.pageOrientation,subTotalMode:report.includeSubTotal,includeColumnTotal:report.includeColumnTotal);
                pdfBytesList.Add(pdf);
            }
            var combinedPdf = DotNetReportHelper.GetCombinePdfFile(pdfBytesList);
            var fileName = string.IsNullOrWhiteSpace(dashboardName) ? "CombinedReports" : dashboardName;
            return File(combinedPdf, "application/pdf", $"{fileName}.pdf");
        }

        [HttpPost]
        public async Task<IActionResult> DownloadAllPdfAlt([FromForm] string reportdata, [FromForm] string dashboardName = "CombinedReports")
        {
            var pdfBytesList = new List<byte[]>();
            var reports = reportdata != null ? JsonConvert.DeserializeObject<List<ExportReportModel>>(reportdata) : null;
            var settings = GetSettings();

            foreach (var report in reports)
            {
                report.reportSql = HttpUtility.HtmlDecode(report.reportSql);
                report.chartData = HttpUtility.UrlDecode(report.chartData)?.Replace(" ", " +");
                await ValidateAccess(report.userId, report.reportSql);
                var columns = report.columnDetails == null ? new List<ReportHeaderColumn>() : JsonConvert.DeserializeObject<List<ReportHeaderColumn>>(HttpUtility.UrlDecode(report.columnDetails));

                var pdf = await DotNetReportHelper.GetPdfFileAlt(report.reportSql, report.connectKey, HttpUtility.UrlDecode(report.reportName), report.chartData, report.expandAll, report.expandSqls, columns, report.includeSubTotal, report.pivot, report.pivotColumn, report.pivotFunction, report.pageSize, report.pageOrientation);
                pdfBytesList.Add(pdf);
            }
            var combinedPdf = DotNetReportHelper.GetCombinePdfFile(pdfBytesList);
            var fileName = string.IsNullOrWhiteSpace(dashboardName) ? "CombinedReports" : dashboardName;
            return File(combinedPdf, "application/pdf", $"{fileName}.pdf");
        }

        [HttpPost]
        public async Task<IActionResult> DownloadAllExcel([FromForm] string reportdata, [FromForm] string dashboardName = "CombinedReports")
        {
            var excelbyteList = new List<byte[]>();
            var reports = reportdata != null ? JsonConvert.DeserializeObject<List<ExportReportModel>>(reportdata) : null;
            var settings = GetSettings();

            foreach (var report in reports)
            {
                report.reportSql = HttpUtility.HtmlDecode(report.reportSql);
                report.chartData = HttpUtility.UrlDecode(report.chartData)?.Replace(" ", " +");
                await ValidateAccess(report.userId, report.reportSql);
                var columns = report.columnDetails == null ? new List<ReportHeaderColumn>() : JsonConvert.DeserializeObject<List<ReportHeaderColumn>>(HttpUtility.UrlDecode(report.columnDetails));
                var onlyAndGroupInDetailColumns = string.IsNullOrEmpty(report.onlyAndGroupInColumnDetail) ? new List<ReportHeaderColumn>() : JsonConvert.DeserializeObject<List<ReportHeaderColumn>>(HttpUtility.UrlDecode(report.onlyAndGroupInColumnDetail));
                var excelreport = await DotNetReportHelper.GetExcelFile(report.reportSql, report.connectKey, HttpUtility.UrlDecode(report.reportName), report.chartData, report.expandAll, HttpUtility.UrlDecode(report.expandSqls), columns, report.includeSubTotal, report.pivot, report.pivotColumn, report.pivotFunction, onlyAndGroupInDetailColumns);
                excelbyteList.Add(excelreport);
            }
            // Combine all Excel files into one workbook
            var combinedExcel = DotNetReportHelper.GetCombineExcelFile(excelbyteList, reports.Select(r => r.reportName).ToList());
            var fileName = string.IsNullOrWhiteSpace(dashboardName) ? "CombinedReports" : dashboardName;
            Response.Headers.Add("content-disposition", $"attachment; filename={fileName}.xlsx");
            Response.ContentType = "application/vnd.ms-excel";
            return File(combinedExcel, "application/vnd.ms-excel", $"{fileName}.xlsx");
        }

        [HttpPost]
        public async Task<IActionResult> DownloadAllWord([FromForm] string reportdata, [FromForm] string dashboardName = "CombinedReports")
        {
            var wordbyteList = new List<byte[]>();
            var ListofReports = reportdata != null ? JsonConvert.DeserializeObject<List<ExportReportModel>>(reportdata) : null;
            var settings = GetSettings();

            foreach (var report in ListofReports)
            {
                report.reportSql = HttpUtility.HtmlDecode(report.reportSql);
                report.chartData = HttpUtility.UrlDecode(report.chartData)?.Replace(" ", " +");
                await ValidateAccess(report.userId, report.reportSql);
                var columns = report.columnDetails == null ? new List<ReportHeaderColumn>() : JsonConvert.DeserializeObject<List<ReportHeaderColumn>>(HttpUtility.UrlDecode(report.columnDetails));
                var wordreport = await DotNetReportHelper.GetWordFile(report.reportSql, report.connectKey, HttpUtility.UrlDecode(report.reportName), report.chartData, report.expandAll, HttpUtility.UrlDecode(report.expandSqls), columns, report.includeSubTotal, report.pivot, report.pivotColumn, report.pivotFunction,report.pageSize,report.pageOrientation);
                wordbyteList.Add(wordreport);
            }
            var combinedWord = DotNetReportHelper.GetCombineWordFile(wordbyteList);
            var fileName = string.IsNullOrWhiteSpace(dashboardName) ? "CombinedReports" : dashboardName;
            Response.Headers.Add("content-disposition", $"attachment; filename={fileName}.docx");
            Response.ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
            return File(combinedWord, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", $"{fileName}.docx");
        }

    }

}