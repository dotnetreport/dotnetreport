using Newtonsoft.Json;
using ReportBuilder.Web.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using System.Web.Services;
using static ReportBuilder.Web.Controllers.DotNetReportApiController;

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
            DotNetReportHelper.dbtype = DbTypes.MS_SQL.ToDbString();

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
            settings.DataFilters = new { }; // add global data filters to apply as needed https://dotnetreport.com/docs/advance-topics/global-filters/

            return settings;
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public object GetLookupList(string lookupSql, string connectKey, string token = "")
        {
            var qry = new SqlQuery();
            var sql = DotNetReportHelper.Decrypt(lookupSql);
            if (sql.StartsWith("{\"sql\""))
            {
                qry = JsonConvert.DeserializeObject<SqlQuery>(sql);
                sql = qry.sql;
                DotNetReportHelper.dbtype = qry.dbType;
            }

            // Uncomment if you want to restrict max records returned
            sql = sql.Substring(0, 0) + "SELECT DISTINCT TOP 500 " + sql.Substring(0 + "SELECT ".Length);
            string tokenvalue = token;
            string lastToken = "";
            if (sql.Contains("{{token}}"))
            {
                tokenvalue = Uri.UnescapeDataString(tokenvalue);
                if (!string.IsNullOrWhiteSpace(tokenvalue))
                {
                    var parts = token
                        .Split(',', (char)StringSplitOptions.RemoveEmptyEntries);

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

            return data;
        }

        public class PostReportApiCallMode
        {
            public string method { get; set; }
            public string headerJson { get; set; }
            public bool useReportHeader { get; set; }
            public string headerClientId { get; set; } = "";
            public string userId { get; set; } = "";

        }
        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public async Task<object> CallReportApiUnAuth(string method, string model)
        {
            string exportId = HttpContext.Current.Request.QueryString["exportId"];
            var settings = ExportSessionStore.Get(exportId);
            if (settings == null)
                throw new Exception("Unauthorized");

            return await CallReportApi(method, model, null);
        }
        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public object PostReportApi(string method, string headerJson, bool useReportHeader,string headerClientId)
        {
            return CallReportApi(method, JsonConvert.SerializeObject(new PostReportApiCallMode
            {
                method = method,
                headerJson = headerJson,
                useReportHeader = useReportHeader,
                headerClientId= headerClientId
            }));
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public object RunReportApi(DotNetReportApiCall data)
        {
            return CallReportApi(data.Method, (new JavaScriptSerializer()).Serialize(data),data.userId);
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public object RunReportApi(string method, bool SaveReport, string ReportJson, bool adminMode, bool SubTotalMode = false, string userId = "")
        {
            return CallReportApi(method, (new JavaScriptSerializer()).Serialize(new DotNetReportApiCall
            {
                Method = method,
                ReportJson = ReportJson,
                SaveReport = SaveReport,
                adminMode = adminMode,
                SubTotalMode = SubTotalMode
            }), userId);
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public async Task<object> CallReportApi(string method, string model,string userId = null)
        {
            var settings = GetSettings();
            if (!string.IsNullOrEmpty(settings.UserId) && settings.UserId != userId)
            {
                throw new Exception("User context mismatch");
            }
            using (var client = new HttpClient())
            {
                var requestData = new Dictionary<string, object>
                {
                    { "account", settings.AccountApiToken },
                    { "dataConnect", settings.DataConnectApiToken },
                    { "clientId", settings.ClientId },
                    { "userId", settings.UserId },
                    { "userIdForSchedule", settings.UserIdForSchedule },
                    { "userIdForFilter", settings.UserIdForFilter },
                    { "userRole", string.Join(",", settings.CurrentUserRole) },
                    { "dataFilters", JsonConvert.SerializeObject(settings.DataFilters) },
                    { "useParameters", DotNetReportHelper.dbtype=="MS SQL" ? "true" : "false" }
                };

                var data = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(model);
                var adminMode = false; var dashboardId = 0; var folderId = 0; var reportId = 0;
                foreach (var key in data.Keys)
                {
                    if (key == "dataConnect" && data[key] != null)
                    {
                        requestData["dataConnect"] = data[key];
                    }
                    else if (key == "account" && data[key] != null)
                    {
                        requestData["account"] = data[key];
                    }
                    else if (key != "adminMode" || (key == "adminMode" && settings.CanUseAdminMode))
                    {
                        requestData[key] = data[key];
                    }
                    if (key == "adminMode" && settings.CanUseAdminMode)
                    {
                        adminMode = data[key];
                    }
                    if (key == "dashboardId")
                    {
                        dashboardId = Convert.ToInt32(data[key]);
                    }
                    if (key == "folderId")
                    {
                        folderId = Convert.ToInt32(data[key]);
                    }
                    if (key == "reportId")
                    {
                        reportId = Convert.ToInt32(data[key]);
                    }
                }
                if (!adminMode)
                {
                    if (dashboardId > 0) await ValidateAccess(userId, dashboardId: dashboardId);
                    if (folderId > 0) await ValidateAccess(userId, folderId: folderId);
                    if (reportId > 0) await ValidateAccess(userId, reportId: reportId);
                }
                var jsonContent = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = client.PostAsync(new Uri(settings.ApiUrl + method), content).Result;
                var stringContent = response.Content.ReadAsStringAsync().Result;

                Context.Response.StatusCode = (int)response.StatusCode;
                return new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Deserialize<dynamic>(stringContent);
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
        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public async Task<DotNetReportResultModel> ExecuteRunReport(RunReportParameters data)
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
                        qry = JsonConvert.DeserializeObject<SqlQuery>(sql);
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
                                sqlFrom.Contains("ORDER BY")
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
                    ReportSql = adminmode ? sql : " ",
                    HasError = true,
                    Exception = ex.Message,
                    ReportDebug = Context.Request.Url.Host.Contains("localhost"),
                };

                return model;
            }
        }
        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public async Task<object> RunReport(string reportSql, string connectKey, string reportType, int pageNumber = 1, int pageSize = 50, string sortBy = null,
            bool desc = false, string reportSeries = null, string pivotColumn = null, string pivotFunction = null, string reportData = null, bool subtotalMode = false, bool adminMode = false, bool useAltPivot = false, bool includeColumnTotal = false)
        {
            // ✅ data object construct karo
            var data = new RunReportParameters
            {
                reportSql = reportSql,
                connectKey = connectKey,
                reportType = reportType,
                pageNumber = pageNumber,
                pageSize = pageSize,
                sortBy = sortBy,
                desc = desc,
                ReportSeries = reportSeries,
                pivotColumn = pivotColumn,
                pivotFunction = pivotFunction,
                reportData = reportData,
                SubTotalMode = subtotalMode,
                adminmode = adminMode,
                useAltPivot = useAltPivot,
                includeColumnTotal = includeColumnTotal
            };
            return await ExecuteRunReport(data);
        }
        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public async Task<object> RunReportUnAuth(string method, string reportSql, string connectKey, string reportType, int pageNumber = 1, int pageSize = 50, string sortBy = null,
            bool desc = false, string reportSeries = null, string pivotColumn = null, string pivotFunction = null, string reportData = null, bool subtotalMode = false, bool adminMode = false, bool useAltPivot = false, bool includeColumnTotal = false)
        {
            string exportId = HttpContext.Current.Request.QueryString["exportId"];
            var settings = ExportSessionStore.Get(exportId);
            if (settings == null)
                throw new Exception("Unauthorized");
            var data = new RunReportParameters
            {
                reportSql = reportSql,
                connectKey = connectKey,
                reportType = reportType,
                pageNumber = pageNumber,
                pageSize = pageSize,
                sortBy = sortBy,
                desc = desc,
                ReportSeries = reportSeries,
                pivotColumn = pivotColumn,
                pivotFunction = pivotFunction,
                reportData = reportData,
                SubTotalMode = subtotalMode,
                adminmode = adminMode,
                useAltPivot = useAltPivot,
                includeColumnTotal = includeColumnTotal
            };
            return await ExecuteRunReport(data);
        }


        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public async Task<object> RunReportApiUnAuth(string method, string reportSql, string connectKey, string reportType, int pageNumber = 1, int pageSize = 50, string sortBy = null,
            bool desc = false, string reportSeries = null, string pivotColumn = null, string pivotFunction = null, string reportData = null, bool subtotalMode = false, bool adminMode = false, bool useAltPivot = false, bool includeColumnTotal = false)
        {
            string exportId = HttpContext.Current.Request.QueryString["exportId"];
            var settings = ExportSessionStore.Get(exportId);
            if (settings == null)
                throw new Exception("Unauthorized");
            var data = new
            {
                reportSql = reportSql,
                connectKey = connectKey,
                reportType = reportType,
                pageNumber = pageNumber,
                pageSize = pageSize,
                sortBy = sortBy,
                desc = desc,
                reportSeries = reportSeries,
                pivotColumn = pivotColumn,
                pivotFunction = pivotFunction,
                reportData = reportData,
                subtotalMode = subtotalMode,
                adminMode = adminMode,
                useAltPivot = useAltPivot,
                includeColumnTotal = includeColumnTotal
            };
            return await CallReportApi(method, (new JavaScriptSerializer()).Serialize(data));
        }
        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public object RunReportLinkUnAuth(int reportId, int? filterId = null, string filterValue = "", bool adminMode = false)
        {
            var model = new DotNetReportModel();
            string exportId = HttpContext.Current.Request.QueryString["exportId"];
            var settings = ExportSessionStore.Get(exportId);
            if (settings == null)
                throw new Exception("Unauthorized");
            settings.ApiUrl = ConfigurationManager.AppSettings["dotNetReport.apiUrl"];
            settings.AccountApiToken = ConfigurationManager.AppSettings["dotNetReport.accountApiToken"]; // Your Account Api Token from your http://dotnetreport.com Account
            settings.DataConnectApiToken = ConfigurationManager.AppSettings["dotNetReport.dataconnectApiToken"]; // Your Data Connect Api Token from your http://dotnetreport.com Account
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
                    new KeyValuePair<string, string>("dataFilters", JsonConvert.SerializeObject(settings.DataFilters)),
                    new KeyValuePair<string, string>("useParameters", DotNetReportHelper.dbtype=="MS SQL" ? "true" : "false")
                });

                var response =  client.PostAsync(new Uri(settings.ApiUrl + $"/ReportApi/RunLinkedReport"), content).Result; 
                var stringContent = response.Content.ReadAsStringAsync().Result;
                Context.Response.StatusCode = (int)response.StatusCode;
                return (new JavaScriptSerializer()).Deserialize<DotNetReportModel>(stringContent);

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
                    new KeyValuePair<string, string>("dataFilters", JsonConvert.SerializeObject(settings.DataFilters)),
                    new KeyValuePair<string, string>("useParameters", DotNetReportHelper.dbtype=="MS SQL" ? "true" : "false")
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
                    new KeyValuePair<string, string>("dataFilters", JsonConvert.SerializeObject(settings.DataFilters))
                });

                var response = client.PostAsync(new Uri(settings.ApiUrl + $"/ReportApi/LoadDashboardData"), content).Result;
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
            };
        }
        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public object GetSchemaFromSql(string value, string accountKey, string dataConnectKey, bool dynamicColumns)
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

                value = TryDecrypt(value);

                if (string.IsNullOrEmpty(value) || !value.StartsWith("SELECT ", StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception("Invalid SQL");
                }

                table.CustomTableSql = value;

                var connect = DotNetReportHelper.GetConnection(dataConnectKey);
                using (var client = new HttpClient())
                {
                    var response = client.GetAsync(String.Format("{0}/ReportApi/GetDataConnectKey?account={1}&dataConnect={2}", connect.ApiUrl, connect.AccountApiKey, connect.DatabaseApiKey)).Result;

                    response.EnsureSuccessStatusCode();

                    var content = response.Content.ReadAsStringAsync().Result;
                    dataConnectKey = content.Replace("\"", "");
                }

                var connString = DotNetReportHelper.GetConnectionString(dataConnectKey);

                IDatabaseConnection databaseConnection = DatabaseConnectionFactory.GetConnection(DotNetReportHelper.dbtype);
                table = databaseConnection.GetSchemaFromSql(connString, table, value, dynamicColumns).Result;

                return table;
            }
            catch (Exception ex)
            {
                Context.Response.StatusCode = 500;
                return new { ex.Message };
            }
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public object GetPreviewFromSql(string value, string accountKey, string dataConnectKey)
        {
            string reportSql = value;
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
                sql = ConvertTopQuery(sql, DotNetReportHelper.dbtype);

                List<string> fields = new List<string>();
                List<string> sqlFields = new List<string>();
                // Execute sql
                var connString = DotNetReportHelper.GetConnectionString(DotNetReportHelper.GetConnection(dataConnectKey), false).Result;

                IDatabaseConnection databaseConnection = DatabaseConnectionFactory.GetConnection(DotNetReportHelper.dbtype);
                var dtPaged = databaseConnection.ExecuteQuery(connString, sql);

                var model = new DotNetReportResultModel
                {
                    ReportData = DotNetReportHelper.DataTableToDotNetReportDataModel(dtPaged, fields),
                    ReportSql = sql,
                    ReportDebug = false,
                    Pager = new DotNetReportPagerModel
                    {
                        CurrentPage = pageNumber,
                        PageSize = pageSize,
                        TotalRecords = 100,
                        TotalPages = 1
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
                    ReportDebug = false,
                };
                Context.Response.StatusCode = 500;
                return model;
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

            if (sql.Contains("TOP"))
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
        [WebMethod(EnableSession = true)]
        public async Task DownloadAllPdf(string reportdata, string dashboardName = "CombinedReports")
        {
            var pdfBytesList = new List<byte[]>();
            var settings = GetSettings();
            var reports = reportdata != null ? JsonConvert.DeserializeObject<List<ExportReportModel>>(reportdata) : null;
            foreach (var report in reports)
            {
                var pdf = await DotNetReportHelper.GetPdfFile(report.printUrl, report.reportId, HttpUtility.HtmlDecode(report.reportSql), HttpUtility.UrlDecode(report.connectKey), HttpUtility.UrlDecode(report.reportName), settings.UserId,
                    settings.ClientId, string.Join(",", settings.CurrentUserRole), JsonConvert.SerializeObject(settings.DataFilters), report.expandAll, report.expandSqls, report.pivotColumn, report.pivotFunction, pageSize: report.pageSize, pageOrientation: report.pageOrientation, subTotalMode: report.includeSubTotal, includeColumnTotal: report.includeColumnTotal);
                pdfBytesList.Add(pdf);
            }
            var combinedPdf = DotNetReportHelper.GetCombinePdfFile(pdfBytesList);
            var fileName = string.IsNullOrWhiteSpace(dashboardName) ? "CombinedReports" : dashboardName;

            Context.Response.AddHeader("content-disposition", "attachment; filename="+$"{fileName}.pdf");
            Context.Response.ContentType = "application/pdf";
            Context.Response.BinaryWrite(combinedPdf);
            Context.Response.End();
        }

        [WebMethod(EnableSession = true)]
        public async Task DownloadAllPdfAlt(string reportdata, string dashboardName = "CombinedReports")
        {
            var pdfBytesList = new List<byte[]>();
            var reports = reportdata != null ? JsonConvert.DeserializeObject<List<ExportReportModel>>(reportdata) : null;
            var settings = GetSettings();
            foreach (var report in reports)
            {
                if (!string.IsNullOrEmpty(settings.UserId) && settings.UserId != report.userId)
                {
                    throw new Exception("User context mismatch");
                }
                report.reportSql = HttpUtility.HtmlDecode(report.reportSql);
                report.chartData = HttpUtility.UrlDecode(report.chartData)?.Replace(" ", " +");
                var columns = report.columnDetails == null ? new List<ReportHeaderColumn>() : JsonConvert.DeserializeObject<List<ReportHeaderColumn>>(HttpUtility.UrlDecode(report.columnDetails));

                var pdf = await DotNetReportHelper.GetPdfFileAlt(report.reportSql, report.connectKey, HttpUtility.UrlDecode(report.reportName), report.chartData, report.expandAll, report.expandSqls, columns, report.includeSubTotal, report.pivot, report.pivotColumn, report.pivotFunction, report.pageSize, report.pageOrientation);
                pdfBytesList.Add(pdf);
            }
            var combinedPdf = DotNetReportHelper.GetCombinePdfFile(pdfBytesList);
            var fileName = string.IsNullOrWhiteSpace(dashboardName) ? "CombinedReports" : dashboardName;
            Context.Response.AddHeader("content-disposition", "attachment; filename=" + $"{fileName}.pdf");
            Context.Response.ContentType = "application/pdf";
            Context.Response.BinaryWrite(combinedPdf);
            Context.Response.End();
        }

        [WebMethod(EnableSession = true)]
        public async Task DownloadAllExcel(string reportdata, string dashboardName = "CombinedReports")
        {
            var excelbyteList = new List<byte[]>();
            var reports = reportdata != null ? JsonConvert.DeserializeObject<List<ExportReportModel>>(reportdata) : null;
            var settings = GetSettings(); 
            foreach (var report in reports)
            {
                if (!string.IsNullOrEmpty(settings.UserId) && settings.UserId != report.userId)
                {
                    throw new Exception("User context mismatch");
                }
                report.reportSql = HttpUtility.HtmlDecode(report.reportSql);
                report.chartData = HttpUtility.UrlDecode(report.chartData)?.Replace(" ", " +");
                await ValidateAccess(report.userId, report.reportSql);
                var columns = report.columnDetails == null ? new List<ReportHeaderColumn>() : JsonConvert.DeserializeObject<List<ReportHeaderColumn>>(HttpUtility.UrlDecode(report.columnDetails));
                var onlyAndGroupInDetailColumns = string.IsNullOrEmpty(report.onlyAndGroupInColumnDetail) ? new List<ReportHeaderColumn>() : JsonConvert.DeserializeObject<List<ReportHeaderColumn>>(HttpUtility.UrlDecode(report.onlyAndGroupInColumnDetail));
                var excelreport = await DotNetReportHelper.GetExcelFile(report.reportSql, report.connectKey, HttpUtility.UrlDecode(report.reportName), report.chartData, report.expandAll, HttpUtility.UrlDecode(report.expandSqls), columns, report.includeSubTotal, report.pivot, report.pivotColumn, report.pivotFunction, onlyAndGroupInDetailColumns,report.isSubReport,report.subTotalPerGroup,report.totalRowFormat);
                excelbyteList.Add(excelreport);
            }
            // Combine all Excel files into one workbook
            var combinedExcel = DotNetReportHelper.GetCombineExcelFile(excelbyteList, reports.Select(r => r.reportName).ToList());
            var fileName = string.IsNullOrWhiteSpace(dashboardName) ? "CombinedReports" : dashboardName;
            Context.Response.ClearContent();

            Context.Response.AddHeader("content-disposition", "attachment; filename=" + $"{fileName}.xlsx");
            Context.Response.ContentType = "application/vnd.ms-excel";
            Context.Response.BinaryWrite(combinedExcel);
            Context.Response.End();
        }

        [WebMethod(EnableSession = true)]
        public async Task DownloadAllWord(string reportdata, string dashboardName = "CombinedReports")
        {
            var wordbyteList = new List<byte[]>();
            var ListofReports = reportdata != null ? JsonConvert.DeserializeObject<List<ExportReportModel>>(reportdata) : null;
            var settings = GetSettings();
            foreach (var report in ListofReports)
            {
                if (!string.IsNullOrEmpty(settings.UserId) && settings.UserId != report.userId)
                {
                    throw new Exception("User context mismatch");
                }
                report.reportSql = HttpUtility.HtmlDecode(report.reportSql);
                report.chartData = HttpUtility.UrlDecode(report.chartData)?.Replace(" ", " +");
                await ValidateAccess(report.userId, report.reportSql);
                var columns = report.columnDetails == null ? new List<ReportHeaderColumn>() : JsonConvert.DeserializeObject<List<ReportHeaderColumn>>(HttpUtility.UrlDecode(report.columnDetails));
                var wordreport = await DotNetReportHelper.GetWordFile(report.reportSql, report.connectKey, HttpUtility.UrlDecode(report.reportName), report.chartData, report.expandAll, HttpUtility.UrlDecode(report.expandSqls), columns, report.includeSubTotal, report.pivot, report.pivotColumn, report.pivotFunction, report.pageSize, report.pageOrientation);
                wordbyteList.Add(wordreport);
            }
            var combinedWord = DotNetReportHelper.GetCombineWordFile(wordbyteList);
            var fileName = string.IsNullOrWhiteSpace(dashboardName) ? "CombinedReports" : dashboardName;
            Context.Response.AddHeader("content-disposition", $"attachment; filename ={fileName}.docx");
            Context.Response.ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
            Context.Response.BinaryWrite(combinedWord);
            Context.Response.End();
        }

        [WebMethod(EnableSession = true)]
        public async Task DownloadExcel(string reportSql,
            string connectKey,
            string reportName,
            bool allExpanded,
            string expandSqls,
            string chartData = null,
            string columnDetails = null,
            bool includeSubtotal = false,
            bool pivot = false,
            string pivotColumn = null,
            string pivotFunction = null,
            string onlyAndGroupInColumnDetail = null,
            bool isSubReport = false,
            string userId = "",
            bool adminMode = false,
            bool subTotalPerGroup = false,
            string totalRowFormat = "row",
            string filterDetailsText = null)
        {
            var settings = GetSettings();
            if (!string.IsNullOrEmpty(settings.UserId) && settings.UserId != userId)
            {
                throw new Exception("User context mismatch");
            }
            reportSql = HttpUtility.HtmlDecode(reportSql);
            await ValidateAccess(userId, reportSql, adminMode: adminMode);
            chartData = HttpUtility.UrlDecode(chartData);
            chartData = chartData?.Replace(" ", " +");
            var columns = string.IsNullOrEmpty(columnDetails) ? new List<ReportHeaderColumn>() : Newtonsoft.Json.JsonConvert.DeserializeObject<List<ReportHeaderColumn>>(HttpUtility.UrlDecode(columnDetails));
            var onlyAndGroupInDetailColumns = string.IsNullOrEmpty(onlyAndGroupInColumnDetail) ? new List<ReportHeaderColumn>() : Newtonsoft.Json.JsonConvert.DeserializeObject<List<ReportHeaderColumn>>(HttpUtility.UrlDecode(onlyAndGroupInColumnDetail));

            var excel = await DotNetReportHelper.GetExcelFile(reportSql, connectKey, HttpUtility.UrlDecode(reportName), chartData, allExpanded, HttpUtility.UrlDecode(expandSqls), columns, includeSubtotal, pivot, pivotColumn, pivotFunction, onlyAndGroupInDetailColumns, isSubReport, subTotalPerGroup, totalRowFormat, HttpUtility.UrlDecode(filterDetailsText));
            Context.Response.ClearContent();

            Context.Response.AddHeader("content-disposition", "attachment; filename=" + HttpUtility.UrlDecode(reportName) + ".xlsx");
            Context.Response.ContentType = "application/vnd.ms-excel";
            Context.Response.BinaryWrite(excel);
            Context.Response.End();
        }

        [WebMethod(EnableSession = true)]
        public async Task DownloadWord(
            string reportSql,
            string connectKey,
            string reportName,
            bool allExpanded,
            string expandSqls,
            string chartData = null,
            string columnDetails = null,
            bool includeSubtotal = false,
            bool pivot = false,
            string pivotColumn = null,
            string pivotFunction = null,
            string pageSize = "",
            string pageOrientation = "",
            string userId = "",
            bool adminMode = false,
            string filterDetailsText = null)
        {
            var settings = GetSettings();
            if (!string.IsNullOrEmpty(settings.UserId) && settings.UserId != userId)
            {
                throw new Exception("User context mismatch");
            }
            reportSql = HttpUtility.HtmlDecode(reportSql);
            await ValidateAccess(userId, reportSql, adminMode: adminMode);
            chartData = HttpUtility.UrlDecode(chartData);
            chartData = chartData?.Replace(" ", " +");
            var columns = columnDetails == null ? new List<ReportHeaderColumn>() : JsonConvert.DeserializeObject<List<ReportHeaderColumn>>(HttpUtility.UrlDecode(columnDetails));
            var word = await DotNetReportHelper.GetWordFile(reportSql, connectKey, HttpUtility.UrlDecode(reportName), chartData, allExpanded, HttpUtility.UrlDecode(expandSqls), columns, includeSubtotal, pivot, pivotColumn, pivotFunction, pageSize, pageOrientation, HttpUtility.UrlDecode(filterDetailsText));

            Context.Response.AddHeader("content-disposition", "attachment; filename=" + HttpUtility.UrlDecode(reportName) + ".docx");
            Context.Response.ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
            Context.Response.BinaryWrite(word);
            Context.Response.End();
        }

        [WebMethod(EnableSession = true)]
        public async Task DownloadXml(string reportSql,
            string connectKey,
            string reportName,
            string expandSqls = null,
            string pivotColumn = null,
            string pivotFunction = null,
            string userId = "",
            bool adminMode = false)
        {
            var settings = GetSettings();
            if (!string.IsNullOrEmpty(settings.UserId) && settings.UserId != userId)
            {
                throw new Exception("User context mismatch");
            }
            reportSql = HttpUtility.HtmlDecode(reportSql);
            await ValidateAccess(userId, reportSql, adminMode: adminMode);
            string xml = await DotNetReportHelper.GetXmlFile(reportSql, HttpUtility.UrlDecode(connectKey), HttpUtility.UrlDecode(reportName), expandSqls, pivotColumn, pivotFunction);
            var data = System.Text.Encoding.UTF8.GetBytes(xml);

            Context.Response.AddHeader("content-disposition", "attachment; filename=" + HttpUtility.UrlDecode(reportName) + ".xml");
            Context.Response.ContentType = "text/txt";
            Context.Response.Write(xml);
            Context.Response.End();
        }

        [WebMethod(EnableSession = true)]
        public async Task DownloadPdf(
            bool adminMode,
            string printUrl,
            int reportId,
            string reportSql,
            string connectKey,
            string reportName,
            bool expandAll,
            string expandSqls = null,
            string pivotColumn = null,
            string pivotFunction = null,
            bool debug = false,
            string pageSize = "",
            string pageOrientation = "",
            bool includeSubTotal = false,
            bool includeColumnTotal = false,
            string userId = "",
            bool isSubreport = false,
            int pageNumber = 1,
            int currentPageSize = 1)
        {
            var settings = GetSettings();

            reportSql = HttpUtility.HtmlDecode(reportSql);
            if (!adminMode)
            {
                await ValidateAccess(userId, reportSql, adminMode: adminMode);
            }
            var pdf = await DotNetReportHelper.GetPdfFile(HttpUtility.UrlDecode(printUrl), reportId, reportSql, HttpUtility.UrlDecode(connectKey), HttpUtility.UrlDecode(reportName),
                                settings.UserId, settings.ClientId, string.Join(",", settings.CurrentUserRole), JsonConvert.SerializeObject(settings.DataFilters), expandAll, expandSqls, pivotColumn, pivotFunction, false, debug, pageSize, pageOrientation, includeSubTotal, includeColumnTotal, isSubreport, pageNumber, currentPageSize);

            Context.Response.AddHeader("content-disposition", "attachment; filename=" + reportName + ".pdf");
            Context.Response.ContentType = "application/pdf";
            Context.Response.BinaryWrite(pdf);
            Context.Response.End();
        }

        [WebMethod(EnableSession = true)]
        public async Task DownloadPdfAlt(
           string reportSql,
           string connectKey,
           string reportName,
           bool allExpanded,
           string expandSqls,
           string chartData = null,
           string columnDetails = null,
           bool includeSubtotal = false,
           bool pivot = false,
           string pivotColumn = null,
           string pivotFunction = null,
           string pageSize = "",
           string pageOrientation = "",
           string userId = "",
           bool adminMode = false,
           bool subTotalPerGroup = false,
           string filterDetailsText = null)
        {
            var settings = GetSettings();
            if (!string.IsNullOrEmpty(settings.UserId) && settings.UserId != userId)
            {
                throw new Exception("User context mismatch");
            }
            reportSql = HttpUtility.HtmlDecode(reportSql);
            await ValidateAccess(userId, reportSql, adminMode: adminMode);
            chartData = HttpUtility.UrlDecode(chartData);
            chartData = chartData?.Replace(" ", " +");
            reportName = HttpUtility.UrlDecode(reportName);
            var columns = columnDetails == null ? new List<ReportHeaderColumn>() : JsonConvert.DeserializeObject<List<ReportHeaderColumn>>(HttpUtility.UrlDecode(columnDetails));

            var pdf = await DotNetReportHelper.GetPdfFileAlt(reportSql, connectKey, reportName, chartData, allExpanded, expandSqls, columns, includeSubtotal, pivot, pivotColumn, pivotFunction, pageSize, pageOrientation, subTotalPerGroup, HttpUtility.UrlDecode(filterDetailsText));

            Context.Response.AddHeader("content-disposition", "attachment; filename=" + reportName + ".pdf");
            Context.Response.ContentType = "application/pdf";
            Context.Response.BinaryWrite(pdf);
            Context.Response.End();
        }

        [WebMethod(EnableSession = true)]
        public async Task DownloadCsv(
           string reportSql,
            string connectKey,
            string reportName,
            bool allExpanded,
            string expandSqls,
            string chartData = null,
            string columnDetails = null,
            bool includeSubtotal = false,
            bool pivot = false,
            string pivotColumn = null,
            string pivotFunction = null,
            string userId = "",
            bool adminMode = false)
        {
            var settings = GetSettings();
            if (!string.IsNullOrEmpty(settings.UserId) && settings.UserId != userId)
            {
                throw new Exception("User context mismatch");
            }
            reportSql = HttpUtility.HtmlDecode(reportSql);
            await ValidateAccess(userId, reportSql, adminMode: adminMode);
            var columns = columnDetails == null ? new List<ReportHeaderColumn>() : JsonConvert.DeserializeObject<List<ReportHeaderColumn>>(HttpUtility.UrlDecode(columnDetails));

            var csv = await DotNetReportHelper.GetCSVFile(reportSql, HttpUtility.UrlDecode(connectKey), columns, includeSubtotal, expandSqls, pivot, pivotColumn, pivotFunction);

            Context.Response.ClearContent();
            Context.Response.AddHeader("content-disposition", "attachment; filename=" + HttpUtility.UrlDecode(reportName) + ".csv");
            Context.Response.ContentType = "text/csv";
            Context.Response.BinaryWrite(csv);
            Context.Response.End();
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
        private static string TryDecrypt(string sql)
        {
            try
            {
                return DotNetReportHelper.Decrypt(sql);
            }
            catch (Exception ex)
            {
                return sql;
            }
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
                    Functions = functions
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
        public async Task<object> BuildDynamicFunctions()
        {
            try
            {
                var settings = GetSettings();
                var functions = await DotNetReportHelper.GetApiFunctions();
                DynamicCodeRunner.BuildAssembly(functions);

                return true;
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

            var connString = DotNetReportHelper.GetConnectionString(DotNetReportHelper.GetConnection(dataConnectKey), false).Result;
            IDatabaseConnection databaseConnection = DatabaseConnectionFactory.GetConnection(DotNetReportHelper.dbtype);

            return databaseConnection.GetSearchProcedure(value, accountKey, dataConnectKey);
        }
        private class checkAccessModel
        {
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

    }
}