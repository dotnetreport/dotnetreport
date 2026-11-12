using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReportBuilder.Web.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
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
            DotNetReportHelper.dbtype = DbTypes.MS_SQL.ToDbString();

            var settings = new DotNetReportSettings
            {
                ApiUrl = ConfigurationManager.AppSettings["dotNetReport.apiUrl"],
                AccountApiToken = ConfigurationManager.AppSettings["dotNetReport.accountApiToken"], // Your Account Api Token from your http://dotnetreport.com Account
                DataConnectApiToken = DotNetReportHelper.CurrentDataConnect // Your Data Connect Api Token from your http://dotnetreport.com Account
            };

            // Populate the values below using your Application Roles/Claims if applicable
            settings.ClientId = "";  // You can pass your multi-tenant client id here to track their reports and folders
            settings.UserId = ""; // You can pass your current authenticated user id here to track their reports and folders            
            settings.UserName = "";
            settings.CurrentUserRole = new List<string>(); // Populate your current authenticated user's roles
            settings.UserIdForFilter = settings.UserId;
            settings.UserIdForSchedule = settings.UserId;
            settings.Users = new List<dynamic>(); // Populate all your application's user, ex  { "Jane", "John" } or { new { id="1", text="Jane" }, new { id="2", text="John" }}
            settings.UserRoles = new List<string>(); // Populate all your application's user roles, ex  { "Admin", "Normal" }       
            settings.ClientIds = new List<string>() { }; // Populate all your application's client/tenant ids, ex  { "ACME", "CONTOSO" }
            settings.CanUseAdminMode = true; // Set to true only if current user can use Admin mode to setup reports and dashboard
            settings.Permissions = new List<string>(); // optional, ex { DotNetReportPermissions.ManageAccess }
            settings.DataFilters = new { }; // add global data filters to apply as needed https://dotnetreport.com/docs/advance-topics/global-filters/

            // View Only wins over Admin Mode
            if (GetUserPermissions(settings).Contains(DotNetReportPermissions.ViewOnly)) settings.CanUseAdminMode = false;

            DotNetReportHelper.CurrentDataFilters = JsonConvert.SerializeObject(settings.DataFilters);

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
                var id = dr[0];
                var text = dr[1];
                // Skip null, DBNull, or empty values
                if (id == null || id is DBNull || string.IsNullOrEmpty(id.ToString()) ||
                    text == null || text is DBNull || string.IsNullOrWhiteSpace(text.ToString()))
                    continue;

                data.Add(new { id = id, text = text });
            }

            return data;
        }

        public class PostReportApiCallMode
        {
            public string method { get; set; }
            public string headerJson { get; set; } = "";
            public bool useReportHeader { get; set; }
            public string headerClientId { get; set; } = "";
            public int id { get; set; }
            public string name { get; set; } = "";
            public bool isDefault { get; set; }
            public string footerJson { get; set; } = "";
            public bool useReportFooter { get; set; }
            public string footerClientId { get; set; } = "";
            public bool includeOnEveryPage { get; set; }
            public string userId { get; set; } = "";

        }
        public class SaveExportSessionRequest
        {
            public string clientId { get; set; }
            public string userId { get; set; }
            public string currentUserRole { get; set; } // comma separated
            public string dataFilters { get; set; } // json string
            public int reportId { get; set; }
            public string reportSql { get; set; }
            public string connectKey { get; set; }
        }
        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public async Task<object> SaveExportSession(string clientId, string userId, string currentUserRole, string dataFilters)
        {

            var settings = new DotNetReportSettings
            {
                ClientId = HttpUtility.HtmlDecode(clientId ?? ""),
                UserId = HttpUtility.HtmlDecode(userId ?? ""),
                CurrentUserRole = HttpUtility.HtmlDecode(currentUserRole ?? "")
                    .Split(',', (char)StringSplitOptions.RemoveEmptyEntries)
                    .ToList(),
                DataFilters = string.IsNullOrEmpty(dataFilters)
                    ? new { }
                    : JsonConvert.DeserializeObject<object>(HttpUtility.HtmlDecode(dataFilters)) ?? new { }
            };

            var exportId = ExportSessionStore.Save(settings);
            return exportId;

        }
        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public async Task<object> CallReportApiUnAuth(string method, string model)
        {
            string exportId = HttpContext.Current.Request.QueryString["exportId"];
            var settings = ExportSessionStore.Get(exportId);
            if (settings == null)
                throw new Exception("Unauthorized");
            DotNetReportHelper.CurrentDataFilters = JsonConvert.SerializeObject(settings.DataFilters ?? new { });
            return await CallReportApi(method, model, null);
        }
        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public object PostReportHeaderApi(string method, string headerJson, bool useReportHeader,string headerClientId,bool includeOnEveryPage,string name,bool isDefault,int id)
        {
            return CallReportApi(method, JsonConvert.SerializeObject(new PostReportApiCallMode
            {
                method = method,
                headerJson = headerJson,
                useReportHeader = useReportHeader,
                headerClientId= headerClientId,
                includeOnEveryPage = includeOnEveryPage,
                name=name,
                isDefault= isDefault,
                id=id
            }));
        }
        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public object PostReportFooterApi(string method, string footerJson, bool useReportFooter, string footerClientId, bool includeOnEveryPage)
        {
            return CallReportApi(method, JsonConvert.SerializeObject(new PostReportApiCallMode
            {
                method = method,
                footerJson = footerJson,
                useReportFooter = useReportFooter,
                footerClientId = footerClientId,
                includeOnEveryPage = includeOnEveryPage
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
            var methodName = (method ?? "").Trim();
            // Only plain /ReportApi/Name, so the lists below can't be side-stepped
            if (!Regex.IsMatch(methodName, "^/ReportApi/[A-Za-z0-9_]+$", RegexOptions.IgnoreCase))
            {
                throw new Exception("Invalid method");
            }
            if (ServerOnlyApiMethods.Contains(methodName, StringComparer.OrdinalIgnoreCase)
                || (!settings.CanUseAdminMode && AdminOnlyApiMethods.Contains(methodName, StringComparer.OrdinalIgnoreCase))
                || (!settings.CanUseAdminMode && PermissionApiMethods.TryGetValue(methodName, out var requiredPermission) && !GetUserPermissions(settings).Contains(requiredPermission)))
            {
                throw new Exception("You are not authorized to perform this action");
            }

            // View Only: run reports, never change anything
            var viewOnly = GetUserPermissions(settings).Contains(DotNetReportPermissions.ViewOnly);
            if (viewOnly && methodName.Equals("/ReportApi/UpdateReportColumnWidth", StringComparison.OrdinalIgnoreCase))
            {
                return new { }; // resizing a column while viewing, nothing is saved
            }
            if (viewOnly && DotNetReportPermissions.IsChangeMethod(methodName))
            {
                throw new Exception("You have view only access");
            }
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
                    { "useParameters", DotNetReportHelper.dbtype=="MS SQL" ? "true" : "false" },
                    // so the Api can check permissions
                    { "userPermissions", string.Join(",", settings.Permissions ?? new List<string>()) },
                    { "enforcePermissions", "true" }
                };

                var data = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(model);
                if (methodName.Equals("/ReportApi/ChangeAccountSettings", StringComparison.OrdinalIgnoreCase)
                    && data.TryGetValue("settings", out var accountSettings) && (object)accountSettings is string accountSettingsJson)
                {
                    data["settings"] = WithoutEmailSettings(accountSettingsJson);
                }
                var adminMode = false; var dashboardId = 0; var folderId = 0; var reportId = 0;
                foreach (var key in data.Keys)
                {
                    if (key == "dataConnect" && data[key] != null)
                    {
                        requestData["dataConnect"] = data[key];
                        continue;
                    }
                    if (key == "account" && data[key] != null)
                    {
                        requestData["account"] = data[key];
                        continue;
                    }
                    // the Api ignores case
                    if (ServerOnlyApiKeys.Contains(key, StringComparer.OrdinalIgnoreCase)) continue;
                    if (string.Equals(key, "adminMode", StringComparison.OrdinalIgnoreCase))
                    {
                        if (settings.CanUseAdminMode && bool.TryParse(Convert.ToString((object)data[key]), out bool am))
                        {
                            adminMode = am;
                            requestData["adminMode"] = am;
                        }
                        continue;
                    }
                    if (viewOnly && string.Equals(key, "SaveReport", StringComparison.OrdinalIgnoreCase))
                    {
                        requestData[key] = false; // run without saving
                        continue;
                    }
                    requestData[key] = data[key];
                    if (string.Equals(key, "dashboardId", StringComparison.OrdinalIgnoreCase))
                    {
                        dashboardId = Convert.ToInt32(data[key]);
                    }
                    if (string.Equals(key, "folderId", StringComparison.OrdinalIgnoreCase))
                    {
                        folderId = Convert.ToInt32(data[key]);
                    }
                    if (string.Equals(key, "reportId", StringComparison.OrdinalIgnoreCase))
                    {
                        reportId = Convert.ToInt32(data[key]);
                    }
                }

                if (methodName.Equals("/ReportApi/RunReport", StringComparison.OrdinalIgnoreCase)
                    && data.TryGetValue("SaveReport", out var saveReport) && (object)saveReport is bool saving && saving)
                {
                    requestData["userIdForFilter"] = "";
                }

                // the designer edits the report's filters, not the user's own
                if (methodName.Equals("/ReportApi/LoadReport", StringComparison.OrdinalIgnoreCase)
                    && data.TryGetValue("reportFilters", out var reportFilters) && (object)reportFilters is bool forReport && forReport)
                {
                    requestData["userIdForFilter"] = "";
                }

                if (methodName.Equals("/ReportApi/ChangeAccountSettings", StringComparison.OrdinalIgnoreCase)
                    && GivesViewOnlyTo(settings, data.TryGetValue("settings", out var posted) ? Convert.ToString((object)posted) : null))
                {
                    throw new Exception("You can't give View Only to yourself or a role you have, you would lose Admin Mode");
                }
                if (!adminMode)
                {
                    if (dashboardId > 0) await ValidateAccess(userId, dashboardId: dashboardId);
                    if (folderId > 0) await ValidateAccess(userId, folderId: folderId);
                    if (reportId > 0) await ValidateAccess(userId, reportId: reportId);
                }
                var jsonContent = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = client.PostAsync(new Uri(settings.ApiUrl + methodName), content).Result;
                var stringContent = response.Content.ReadAsStringAsync().Result;
                if (viewOnly && response.IsSuccessStatusCode) stringContent = DotNetReportPermissions.HideChangeOptions(stringContent);

                // Permission grants may have just changed
                if (response.IsSuccessStatusCode && methodName.Equals("/ReportApi/ChangeAccountSettings", StringComparison.OrdinalIgnoreCase))
                {
                    ClearPermissionCache();
                }

                Context.Response.StatusCode = (int)response.StatusCode;
                return new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Deserialize<dynamic>(stringContent);
            }
        }

        // Account level changes are only available in Admin Mode, the front end flag can be tampered with so check it here too
        private static readonly string[] AdminOnlyApiMethods = new[]
        {
            "/ReportApi/SaveFolderData",
            "/ReportApi/DeleteFolder",
            "/ReportApi/ChangeAccountSettings",
            "/ReportApi/GetAccountInfo",
            "/ReportApi/SaveAccountRole",
            "/ReportApi/DeleteAccountRole"
        };

        // Admin Mode or the matching permission; the Api checks again
        private static readonly Dictionary<string, string> PermissionApiMethods = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "/ReportApi/SaveReportAccess", DotNetReportPermissions.ManageAccess },
            { "/ReportApi/SaveFolderAccess", DotNetReportPermissions.ManageAccess },
            { "/ReportApi/SaveDashboardAccess", DotNetReportPermissions.ManageAccess },
            { "/ReportApi/SaveReportHeader", DotNetReportPermissions.ManageHeaderFooter },
            { "/ReportApi/SaveReportFooter", DotNetReportPermissions.ManageHeaderFooter },
            { "/ReportApi/DeleteReportHeader", DotNetReportPermissions.ManageHeaderFooter },
            { "/ReportApi/SaveAccountUser", DotNetReportPermissions.ManageUsers },
            { "/ReportApi/DeleteAccountUser", DotNetReportPermissions.ManageUsers },
            { "/ReportApi/SetAccountUserPassword", DotNetReportPermissions.ManageUsers }
        };

        // Called only by this server, never forwarded from the browser
        private static readonly string[] ServerOnlyApiMethods = new[]
        {
            "/ReportApi/LoginAccountUser",
            "/ReportApi/CreatePasswordResetToken",
            "/ReportApi/ResetAccountUserPassword",
            "/ReportApi/GetSignInSettings"
        };

        // Set only from GetSettings, never from the browser
        private static readonly string[] ServerOnlyApiKeys = new[] { "clientId", "userId", "userRole", "userPermissions", "enforcePermissions" };

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (DateTime expires, List<string> permissions)> PermissionCache
            = new System.Collections.Concurrent.ConcurrentDictionary<string, (DateTime, List<string>)>();

        public static void ClearPermissionCache() => PermissionCache.Clear();

        // Cached for a minute per user
        public List<string> GetUserPermissions(DotNetReportSettings settings)
        {
            var codePermissions = settings.Permissions ?? new List<string>();
            if (string.IsNullOrEmpty(settings.AccountApiToken) || string.IsNullOrEmpty(settings.ApiUrl)) return DotNetReportPermissions.ApplyViewOnly(codePermissions);

            var roles = string.Join(",", settings.CurrentUserRole ?? new List<string>());
            var cacheKey = string.Join("|", settings.AccountApiToken, settings.DataConnectApiToken, settings.UserId, roles, string.Join(",", codePermissions));
            if (PermissionCache.TryGetValue(cacheKey, out var cached) && cached.expires > DateTime.UtcNow) return cached.permissions;

            var permissions = codePermissions;
            try
            {
                using (var client = new HttpClient())
                {
                    var content = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("account", settings.AccountApiToken),
                        new KeyValuePair<string, string>("dataConnect", settings.DataConnectApiToken),
                        new KeyValuePair<string, string>("clientId", settings.ClientId ?? ""),
                        new KeyValuePair<string, string>("userId", settings.UserId ?? ""),
                        new KeyValuePair<string, string>("userRole", roles),
                        new KeyValuePair<string, string>("userPermissions", string.Join(",", codePermissions))
                    });
                    // off the request thread, so the blocking wait can't deadlock
                    var response = Task.Run(() => client.PostAsync(new Uri(settings.ApiUrl + "/ReportApi/GetUserPermissions"), content)).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        var result = JsonConvert.DeserializeObject<UserPermissionsResult>(Task.Run(() => response.Content.ReadAsStringAsync()).Result);
                        if (result?.permissions != null) permissions = result.permissions;
                    }
                }
            }
            catch
            {
                // Api not reachable: code permissions only
            }

            permissions = DotNetReportPermissions.ApplyViewOnly(permissions);
            PermissionCache[cacheKey] = (DateTime.UtcNow.AddMinutes(1), permissions);
            return permissions;
        }

        // Stops an admin giving View Only to themselves
        private static bool GivesViewOnlyTo(DotNetReportSettings settings, string postedSettings)
        {
            try
            {
                var grant = JObject.Parse(postedSettings ?? "{}")["permissions"]?[DotNetReportPermissions.ViewOnly];
                if (grant == null) return false;
                var users = grant["users"]?.Select(x => x.ToString()) ?? Enumerable.Empty<string>();
                var roles = grant["roles"]?.Select(x => x.ToString()) ?? Enumerable.Empty<string>();
                return (!string.IsNullOrWhiteSpace(settings.UserId) && users.Contains(settings.UserId, StringComparer.OrdinalIgnoreCase))
                    || (settings.CurrentUserRole ?? new List<string>()).Any(r => roles.Contains(r, StringComparer.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
        }

        private class UserPermissionsResult
        {
            public List<string> permissions { get; set; }
        }

        // Email settings are kept in appsettings.dotnetreport.json on this server, so they are taken out of the
        // Account Settings sent to Dotnet Report.
        private static string WithoutEmailSettings(string settingsJson)
        {
            try
            {
                var values = JObject.Parse(settingsJson ?? "{}");
                foreach (var key in values.Properties().Select(p => p.Name).ToList())
                {
                    if (key.StartsWith("email", StringComparison.OrdinalIgnoreCase) || key.StartsWith("sendGrid", StringComparison.OrdinalIgnoreCase))
                        values.Remove(key);
                }
                return values.ToString(Formatting.None);
            }
            catch
            {
                return settingsJson;
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
        public object GetDataFilterKeys()
        {
            var settings = GetSettings();
            if (!settings.CanUseAdminMode)
                return (new string[0]);

            var keys = new List<string>();
            if (settings.DataFilters != null)
            {
                try
                {
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                        JsonConvert.SerializeObject(settings.DataFilters));
                    if (dict != null) keys = new List<string>(dict.Keys);
                }
                catch { }
            }
            return keys;
        }
        // SQL only for Admin Mode or View SQL
        private async Task<DotNetReportResultModel> ExecuteRunReport(RunReportParameters data, bool showSql = false)
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
                    if (!sql.StartsWith("EXEC") && !sql.StartsWith("CALL"))
                    {
                        sqlFields = DotNetReportHelper.SplitSqlColumns(sql, DotNetReportHelper.dbtype);
                        bool hasDistinct = Regex.IsMatch(sql, @"^\s*SELECT\s+(TOP\s+\d+\s+)?DISTINCT\b", RegexOptions.IgnoreCase);

                        var countFields = new List<string>(sqlFields);
                        var aliasCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                        bool anyDuped = false;
                        for (int f = 0; f < countFields.Count; f++)
                        {
                            var aliasPos = countFields[f].LastIndexOf(" AS ", StringComparison.Ordinal);
                            if (aliasPos < 0) continue;
                            var expr = countFields[f].Substring(0, aliasPos);
                            var aliasToken = countFields[f].Substring(aliasPos + 4).Trim();
                            var alias = aliasToken.Trim('[', ']', '`', '"');
                            if (aliasCounts.TryGetValue(alias, out var count))
                            {
                                aliasCounts[alias] = ++count;
                                var open = aliasToken.StartsWith("`") ? "`" : aliasToken.StartsWith("\"") ? "\"" : "[";
                                var close = open == "[" ? "]" : open;
                                countFields[f] = $"{expr} AS {open}{alias}_{count}{close}";
                                anyDuped = true;
                            }
                            else
                            {
                                aliasCounts[alias] = 1;
                            }
                        }
                        var countSource = sql;
                        if (anyDuped)
                        {
                            var selectEnd = sql.IndexOf("{FROM}", StringComparison.OrdinalIgnoreCase);
                            if (selectEnd >= 0) // custom SQL has no {FROM} placeholder — leave it untouched
                            {
                                var selectPrefix = Regex.Match(sql, @"^\s*SELECT\s+(TOP\s+\d+\s+)?(DISTINCT\s+)?",
                                                                RegexOptions.IgnoreCase).Value;
                                countSource = selectPrefix + string.Join(", ", countFields) + sql.Substring(selectEnd);
                            }
                        }
                        var countInner = countSource.Replace("{FROM}", "FROM");
                        int countOrderByIndex = countInner.LastIndexOf("ORDER BY", StringComparison.OrdinalIgnoreCase);
                        if (countOrderByIndex > -1)
                        {
                            countInner = countInner.Substring(0, countOrderByIndex).Trim();
                        }

                        if (DotNetReportHelper.dbtype == "Oracle")
                            sqlCount = "SELECT COUNT(*) FROM (" + countInner + ") countQry";
                        else
                            sqlCount = "SELECT COUNT(*) FROM (" + countInner + ") AS countQry";

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

                            // Month/Year columns are text ("Apr 2026", "04/2026"), so sort them by the underlying date instead
                            var sortParts = new List<string> { sortBy };
                            var monthYear = Regex.Match(sortBy, @"^CONVERT\(VARCHAR\(3\),\s*DATENAME\(MONTH,\s*(?<col>.+?)\)\)\s*\+\s*' '\s*\+\s*CONVERT\(VARCHAR\(4\),\s*YEAR\(\k<col>\)\)\s*$");
                            if (monthYear.Success)
                            {
                                var col = monthYear.Groups["col"].Value;
                                sortParts = new List<string> { $"YEAR({col})", $"MONTH({col})" };
                            }
                            var monthYearText = Regex.Match(sortBy, @"^(TO_CHAR|VARCHAR_FORMAT)\((?<col>.+?),\s*'MM/YYYY'\)\s*$");
                            if (monthYearText.Success)
                            {
                                var col = monthYearText.Groups["col"].Value;
                                sortParts = new List<string> { sql.Contains("Group By") ? $"MIN({col})" : col };
                            }
                            var orderBy = string.Join(", ", sortParts.Select(x => x + (desc ? " DESC" : "")));

                            if (!sql.Contains("ORDER BY"))
                            {
                                sql = sql + "ORDER BY " + orderBy;
                            }
                            else
                            {
                                sql = sql.Substring(0, sql.IndexOf("ORDER BY")) + "ORDER BY " + orderBy;
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

                    if (sql.StartsWith("EXEC") || sql.StartsWith("CALL"))
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
                                var pd = await DotNetReportHelper.GetPivotTable(databaseConnection, connectionString, dtPagedRun, sql, sqlFields, reportData, pivotColumn, pivotFunction, pageNumber, pageSize, sortBy, desc, false, includeColumnTotal, subtotalMode, parameters: qry.parameters);
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
                                var ds = await DotNetReportHelper.GetDrillDownData(databaseConnection, connectionString, dtPagedRun, sqlFields, reportData, qry.parameters);
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
                    ReportSql = showSql ? sql : " ",
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
                    ReportSql = showSql ? sql : " ",
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
            var settings = GetSettings();
            if (!settings.CanUseAdminMode) data.adminmode = false;
            var firstSql = (data.reportSql ?? "").Split(new string[] { "%2C", "," }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
            ValidateAccess(settings.UserId, HttpUtility.HtmlDecode(firstSql), adminMode: data.adminmode);
            DotNetReportHelper.CurrentDataFilters = JsonConvert.SerializeObject(settings.DataFilters ?? new { });

            return await ExecuteRunReport(data, data.adminmode || GetUserPermissions(settings).Contains(DotNetReportPermissions.ViewSql));
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
            DotNetReportHelper.CurrentDataFilters = JsonConvert.SerializeObject(settings.DataFilters ?? new { });
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
            DotNetReportHelper.CurrentDataFilters = JsonConvert.SerializeObject(settings.DataFilters ?? new { });
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
            settings.DataConnectApiToken = DotNetReportHelper.CurrentDataConnect; // Your Data Connect Api Token from your http://dotnetreport.com Account
            settings.CanUseAdminMode = true;
            DotNetReportHelper.CurrentDataFilters = JsonConvert.SerializeObject(settings.DataFilters ?? new { });
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
            if (!settings.CanUseAdminMode) adminMode = false;
            ValidateAccess(settings.UserId, reportId: reportId, adminMode: adminMode);

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
        private async Task<string> ResolveLinkedReportTemplate(int reportId, int filterId, bool adminMode = false)
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
            new KeyValuePair<string, string>("reportId", reportId.ToString()),
            new KeyValuePair<string, string>("filterId", filterId.ToString()),
            new KeyValuePair<string, string>("filterValue", ReportConstants.SubReportValueToken), // placeholder, not a real value
            new KeyValuePair<string, string>("adminMode", adminMode.ToString()),
            new KeyValuePair<string, string>("dataFilters", JsonConvert.SerializeObject(settings.DataFilters)),
            new KeyValuePair<string, string>("useParameters", DotNetReportHelper.dbtype == "MS SQL" ? "true" : "false")
        });
                var response = client.PostAsync(new Uri(settings.ApiUrl + $"/ReportApi/RunLinkedReport"), content).Result;
                var stringContent = response.Content.ReadAsStringAsync().Result;
                Context.Response.StatusCode = (int)response.StatusCode;
                var model=(new JavaScriptSerializer()).Deserialize<DotNetReportModel>(stringContent);
                if (model == null || string.IsNullOrEmpty(model.ReportSql))
                {
                    return null;
                }
                return DotNetReportHelper.Decrypt(model.ReportSql);
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
            if (!settings.CanUseAdminMode) adminMode = false;
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
            if (!settings.CanUseAdminMode) adminMode = false;
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
                if (GetUserPermissions(settings).Contains(DotNetReportPermissions.ViewOnly)) stringContent = DotNetReportPermissions.HideChangeOptions(stringContent);

                var model = (new JavaScriptSerializer()).Deserialize<List<dynamic>>(stringContent);
                return model;
            }
        }
        public class AccountUsersResult
        {
            public string userSource { get; set; } = "code";
            public string clientIdLabel { get; set; }
            public List<AccountUserItem> users { get; set; } = new List<AccountUserItem>();
            public List<AccountListItem> roles { get; set; } = new List<AccountListItem>();
            public List<AccountListItem> clientIds { get; set; } = new List<AccountListItem>();
        }
        public class AccountListItem
        {
            public string id { get; set; }
            public string text { get; set; }
        }
        public class AccountUserItem : AccountListItem
        {
            public string email { get; set; }
            public string name { get; set; }
            public bool isPrimary { get; set; }
            public List<string> roles { get; set; } = new List<string>();
            public List<string> clientIds { get; set; } = new List<string>();
        }
        private AccountUsersResult GetManagedUsersAndRoles(DotNetReportSettings settings)
        {
            if (string.IsNullOrEmpty(settings.AccountApiToken)) return null;

            try
            {
                using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) })
                {
                    var content = new FormUrlEncodedContent(new[]
                    {
                new KeyValuePair<string, string>("account", settings.AccountApiToken),
                new KeyValuePair<string, string>("dataConnect", settings.DataConnectApiToken ?? "")
            });

                    var response = client.PostAsync(new Uri(settings.ApiUrl + "/ReportApi/GetAccountUsersAndRoles"), content).Result;
                    if (!response.IsSuccessStatusCode) return null;

                    var json = response.Content.ReadAsStringAsync().Result;
                    var result = JsonConvert.DeserializeObject<AccountUsersResult>(json);
                    if (result == null) return null;

                    // API can return nulls for empty lists - keep callers safe
                    result.userSource = string.IsNullOrEmpty(result.userSource) ? "code" : result.userSource;
                    result.users = result.users ?? new List<AccountUserItem>();
                    result.roles = result.roles ?? new List<AccountListItem>();
                    result.clientIds = result.clientIds ?? new List<AccountListItem>();
                    return result;
                }
            }
            catch (Exception ex)
            {
                Context.Response.StatusCode = 500;
                return null;
            }
        }


        /// <summary>
        /// SQL Query source: runs the saved Users / UserRoles / Clients query
        /// </summary>
        private List<AccountListItem> GetSqlSourceList(DotNetReportSettings settings, string queryType)
        {
            var items = new List<AccountListItem>();
            try
            {
                using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) })
                {
                    var baseQuery = "?account=" + Uri.EscapeDataString(settings.AccountApiToken ?? "")
                                  + "&dataConnect=" + Uri.EscapeDataString(settings.DataConnectApiToken ?? "");

                    // 1) find the saved query id
                    var listUrl = settings.ApiUrl + "/ReportApi/GetDataDrivenQueries" + baseQuery
                                + "&includeGlobal=true&queryType=" + Uri.EscapeDataString(queryType);
                    var listResponse = client.GetAsync(new Uri(listUrl)).Result;
                    if (!listResponse.IsSuccessStatusCode) return items;

                    var list = JToken.Parse(listResponse.Content.ReadAsStringAsync().Result);
                    // SelectToken returns null for missing/empty array (no exception like JArray[0])
                    int? id = (int?)list.SelectToken("queries[0].id");
                    if (!id.HasValue) return items;

                    // 2) get its (encrypted) SQL + connect key
                    var sqlUrl = settings.ApiUrl + "/ReportApi/GetDataDrivenQuerySql" + baseQuery
                               + "&id=" + id.Value
                               + "&clientId=" + Uri.EscapeDataString(settings.ClientId ?? "")
                               + "&userId=" + Uri.EscapeDataString(settings.UserId ?? "")
                               + "&dataFilters=" + Uri.EscapeDataString(DotNetReportHelper.CurrentDataFilters ?? "");
                    var sqlResponse = client.GetAsync(new Uri(sqlUrl)).Result;
                    if (!sqlResponse.IsSuccessStatusCode) return items;

                    var result = JToken.Parse(sqlResponse.Content.ReadAsStringAsync().Result);
                    var encryptedSql = (string)result["sql"];
                    var connectKey = (string)result["connectKey"];
                    if (string.IsNullOrEmpty(encryptedSql)) return items;

                    // 3) run it (GetDataDrivenQueryRows returns Task<DataTable>; if yours is already sync, drop .Result)
                    var rows = DotNetReportHelper.GetDataDrivenQueryRows(encryptedSql, connectKey).Result;
                    if (rows == null || rows.Columns.Count == 0) return items;

                    var columns = rows.Columns.Cast<DataColumn>().ToList();
                    var idColumn = columns.FirstOrDefault(c => string.Equals(c.ColumnName, "id", StringComparison.OrdinalIgnoreCase)) ?? columns[0];
                    var textColumn = columns.FirstOrDefault(c => string.Equals(c.ColumnName, "text", StringComparison.OrdinalIgnoreCase))
                        ?? (columns.Count > 1 ? columns[1] : columns[0]);

                    foreach (DataRow row in rows.Rows)
                    {
                        var itemId = row[idColumn]?.ToString()?.Trim() ?? "";
                        if (itemId.Length == 0) continue;
                        var text = row[textColumn]?.ToString()?.Trim();
                        items.Add(new AccountListItem { id = itemId, text = string.IsNullOrEmpty(text) ? itemId : text });
                    }
                }
            }
            catch (Exception ex)
            {
                // A broken query leaves the list empty; the setup page shows the SQL error itself.
            }
            return items;
        }

        /// <summary>
        /// Asks the Dotnet Report portal to email a user a link to set their portal password.
        /// </summary>
        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public object SendUserPasswordSetup(string email)
        {
            var settings = GetSettings();
            if (!settings.CanUseAdminMode)
                throw new Exception("Unauthorized");

            if (string.IsNullOrWhiteSpace(email))
            {
                Context.Response.StatusCode = 400;
                return new { success = false, message = "Email is required" };
            }

            var portalUrl = ConfigurationManager.AppSettings["dotNetReport.portalUrl"] ?? "https://dotnetreport.com/portal";
            var forgotUrl = portalUrl.TrimEnd('/') + "/Account/ForgotPassword";

            try
            {
                using (var handler = new HttpClientHandler { CookieContainer = new CookieContainer(), AllowAutoRedirect = false })
                using (var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) })
                {
                    var page = client.GetAsync(forgotUrl).Result;
                    if (!page.IsSuccessStatusCode)
                    {
                        Context.Response.StatusCode = 400;
                        return new { success = false, message = "Could not reach the Dotnet Report portal" };
                    }

                    var html = page.Content.ReadAsStringAsync().Result;
                    var token = Regex.Match(html, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""").Groups[1].Value;
                    if (string.IsNullOrEmpty(token))
                        token = Regex.Match(html, @"value=""([^""]+)""[^>]*name=""__RequestVerificationToken""").Groups[1].Value;
                    if (string.IsNullOrEmpty(token))
                    {
                        Context.Response.StatusCode = 400;
                        return new { success = false, message = "Could not read the portal request token" };
                    }

                    var form = new FormUrlEncodedContent(new[]
                    {
                new KeyValuePair<string, string>("__RequestVerificationToken", token),
                new KeyValuePair<string, string>("Email", email.Trim())
            });

                    var response = client.PostAsync(forgotUrl, form).Result;

                    // Success redirects to ForgotPasswordConfirmation; a failure re-renders the form.
                    var redirected = (int)response.StatusCode == 302
                                     && (response.Headers.Location?.ToString() ?? "").IndexOf("ForgotPasswordConfirmation", StringComparison.OrdinalIgnoreCase) >= 0;
                    if (!redirected)
                    {
                        Context.Response.StatusCode = 400;
                        return new { success = false, message = "Could not send the password setup email. Please verify the user has a valid email on the portal." };
                    }

                    return new { success = true };
                }
            }
            catch (Exception ex)
            {
                Context.Response.StatusCode = 500;
                return new { success = false, message = ex.GetBaseException().Message };
            }
        }

        public class SendPasswordSetupCall
        {
            public string email { get; set; }
        }


        [WebMethod(EnableSession = true)]
        [ScriptMethod(UseHttpGet = true, ResponseFormat = ResponseFormat.Json)]
        public object PreviewEmailList(int? id)
        {
            var settings = GetSettings();
            if (!settings.CanUseAdminMode)
                throw new Exception("Unauthorized");

            if (!id.HasValue)
            {
                Context.Response.StatusCode = 400;
                return new { success = false, message = "Email List id is required." };
            }

            try
            {
                using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) })
                {
                    var url = settings.ApiUrl + "/ReportApi/GetDataDrivenQuerySql"
                        + "?account=" + Uri.EscapeDataString(settings.AccountApiToken ?? "")
                        + "&dataConnect=" + Uri.EscapeDataString(settings.DataConnectApiToken ?? "")
                        + "&id=" + id.Value
                        + "&clientId=" + Uri.EscapeDataString(settings.ClientId ?? "")
                        + "&userId=" + Uri.EscapeDataString(settings.UserId ?? "")
                        + "&dataFilters=" + Uri.EscapeDataString(DotNetReportHelper.CurrentDataFilters ?? "");

                    var response = client.GetAsync(new Uri(url)).Result;
                    var stringContent = response.Content.ReadAsStringAsync().Result;
                    if (!response.IsSuccessStatusCode)
                        return new { success = false, message = "Could not load the Email List." };

                    var result = JToken.Parse(stringContent);
                    var encryptedSql = (string)result["sql"];
                    var connectKey = (string)result["connectKey"];
                    if (string.IsNullOrEmpty(encryptedSql))
                        return new { success = false, message = "This Email List has no query." };

                    // GetDataDrivenQueryRows returns Task<DataTable>; if yours is already sync, drop .Result
                    var rows = DotNetReportHelper.GetDataDrivenQueryRows(encryptedSql, connectKey).Result;
                    var emails = rows == null
                        ? new List<string>()
                        : DotNetReportHelper.ExtractEmailRecipients(rows).ToList();
                    var columns = rows == null
                        ? new List<string>()
                        : rows.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();

                    return new
                    {
                        success = true,
                        total = emails.Count,
                        rowCount = rows == null ? 0 : rows.Rows.Count,
                        columns = columns,
                        emails = emails.Take(200).ToList()
                    };
                }
            }
            catch (Exception ex)
            {
                Context.Response.StatusCode = 500;
                return new { success = false, message = ex.GetBaseException().Message };
            }
        }
        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public object GetUsersAndRoles()
        {
            var settings = GetSettings();

            // These report permission settings will be applied by default to any new report user creates, leave black to allow access to all
            var newReportClientId = ""; // comma separated client ids to set report permission when new report is created
            var newReportEditUserId = ""; // comma separated user ids for report edit permission when new report is created
            var newReportViewUserId = ""; // comma separated user ids for report view permission when new report is created
            var newReportEditUserRoles = ""; // comma separated user roles for report edit permission when new report is created
            var newReportViewUserRoles = ""; // comma separated user roles for report view permission when new report is created

            var managed = GetManagedUsersAndRoles(settings);
            var userSource = managed?.userSource ?? "code";
            var codeClientIds = settings.ClientIds.Select(c => (object)new AccountListItem { id = c, text = c }).ToList();
            List<object> users, clientIds;
            List<string> userRoles;
            if (userSource == "portal")
            {
                // Access is granted by email, so it matches the settings.UserId an application passes in.
                users = managed.users.Select(u => (object)new AccountListItem { id = u.email, text = u.email }).ToList();
                userRoles = managed.roles.Select(r => r.text).ToList();
                clientIds = managed.clientIds != null && managed.clientIds.Any()
                    ? managed.clientIds.Cast<object>().ToList()
                    : codeClientIds;
            }
            else if (userSource == "sql")
            {
                users = GetSqlSourceList(settings, "Users").Cast<object>().ToList();
                userRoles = GetSqlSourceList(settings, "UserRoles").Select(r => r.text).ToList();
                clientIds = GetSqlSourceList(settings, "Clients").Cast<object>().ToList();
            }
            else
            {
                users = settings.Users.Cast<object>().ToList();
                userRoles = settings.UserRoles;
                clientIds = codeClientIds;
            }
            return new
            {
                noAccount = string.IsNullOrEmpty(settings.AccountApiToken) || settings.AccountApiToken == "Your Public Account Api Token",
                users,
                userRoles,
                clientIds,
                clientIdLabel = managed?.clientIdLabel,
                userSource,
                codeUsers = settings.Users,
                codeUserRoles = settings.UserRoles,
                codeClientIds = settings.ClientIds,
                currentUserId = settings.UserId,
                currentUserRoles = settings.CurrentUserRole,
                currentUserName = settings.UserName,
                allowAdminMode = settings.CanUseAdminMode,
                permissions = GetUserPermissions(settings),
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

                var decrypted = TryDecrypt(value);
                // Outside Admin Mode only server-encrypted SQL
                if (!GetSettings().CanUseAdminMode && decrypted == value)
                {
                    throw new Exception("You are not authorized to perform this action");
                }
                value = decrypted;

                if (!DotNetReportHelper.IsReadOnlySelectSql(value, out var sqlReason))
                {
                    throw new Exception("Invalid SQL: " + sqlReason);
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
        public object GetPreviewFromSql(string value, string accountKey, string dataConnectKey, bool dynamicColumns = false)
        {
            // Admin Mode only
            if (!GetSettings().CanUseAdminMode)
            {
                Context.Response.StatusCode = 403;
                return new { Message = "You are not authorized to perform this action" };
            }
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
        public async Task DownloadAllPdf(string reportdata, string dashboardName = "CombinedReports", string defaultDateFormat = null)
        {
            DotNetReportHelper.defaultDateFormat = string.IsNullOrEmpty(defaultDateFormat) ? "United States" : defaultDateFormat;
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
        public async Task DownloadAllPdfAlt(string reportdata, string dashboardName = "CombinedReports", string defaultDateFormat = null)
        {
            DotNetReportHelper.defaultDateFormat = string.IsNullOrEmpty(defaultDateFormat) ? "United States" : defaultDateFormat;
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
                await ValidateAccess(report.userId, report.reportSql);
                var columns = report.columnDetails == null ? new List<ReportHeaderColumn>() : JsonConvert.DeserializeObject<List<ReportHeaderColumn>>(HttpUtility.UrlDecode(report.columnDetails));
                var onlyAndGroupInDetailColumns = string.IsNullOrEmpty(report.onlyAndGroupInColumnDetail) ? new List<ReportHeaderColumn>() : Newtonsoft.Json.JsonConvert.DeserializeObject<List<ReportHeaderColumn>>(HttpUtility.UrlDecode(report.onlyAndGroupInColumnDetail));
                var pdf = await DotNetReportHelper.GetPdfFileAlt(report.reportSql, report.connectKey, HttpUtility.UrlDecode(report.reportName), report.chartData, report.expandAll, report.expandSqls, columns, report.includeSubTotal, report.pivot, report.pivotColumn, report.pivotFunction, report.pageSize, report.pageOrientation, reportDescription: HttpUtility.UrlDecode(report.reportDescription),onlyAndGroupInDetailColumns: onlyAndGroupInDetailColumns, reportType: report.reportType);
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
        public async Task DownloadAllExcel(string reportdata, string dashboardName = "CombinedReports", string defaultDateFormat = null)
        {
            DotNetReportHelper.defaultDateFormat = string.IsNullOrEmpty(defaultDateFormat) ? "United States" : defaultDateFormat;
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
                var excelreport = await DotNetReportHelper.GetExcelFile(report.reportSql, report.connectKey, HttpUtility.UrlDecode(report.reportName), report.chartData, report.expandAll, report.hasSubreports, HttpUtility.UrlDecode(report.expandSqls), columns, report.includeSubTotal, report.pivot, report.pivotColumn, report.pivotFunction, onlyAndGroupInDetailColumns);
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
        public async Task DownloadAllWord(string reportdata, string dashboardName = "CombinedReports", string defaultDateFormat = null)
        {
            DotNetReportHelper.defaultDateFormat = string.IsNullOrEmpty(defaultDateFormat) ? "United States" : defaultDateFormat;
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
            bool hasSubreports,
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
            string filterDetailsText = null,
            string defaultDateFormat = null,
            string outerGroupLayout = null,
            bool outerGroupPageBreak = false,
            string outerGroupBandStyles = null,
            string headerStyle = null,
            bool hideReportName = false)
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
            DotNetReportHelper.defaultDateFormat = string.IsNullOrEmpty(defaultDateFormat) ? "United States" : defaultDateFormat;
            var columns = string.IsNullOrEmpty(columnDetails) ? new List<ReportHeaderColumn>() : Newtonsoft.Json.JsonConvert.DeserializeObject<List<ReportHeaderColumn>>(HttpUtility.UrlDecode(columnDetails));
            var onlyAndGroupInDetailColumns = string.IsNullOrEmpty(onlyAndGroupInColumnDetail) ? new List<ReportHeaderColumn>() : Newtonsoft.Json.JsonConvert.DeserializeObject<List<ReportHeaderColumn>>(HttpUtility.UrlDecode(onlyAndGroupInColumnDetail));
            Func<int, int, bool, Task<string>> linkedReportResolver = hasSubreports ? new Func<int, int, bool, Task<string>>((reportId, filterId, isAdmin) => ResolveLinkedReportTemplate(reportId, filterId, adminMode)) : null;
            var excel = await DotNetReportHelper.GetExcelFile(reportSql, connectKey, HttpUtility.UrlDecode(reportName), chartData, allExpanded, hasSubreports, HttpUtility.UrlDecode(expandSqls), columns, includeSubtotal, pivot, pivotColumn, pivotFunction, onlyAndGroupInDetailColumns, isSubReport, subTotalPerGroup, totalRowFormat, HttpUtility.UrlDecode(filterDetailsText), linkedReportResolver, outerGroupLayout: outerGroupLayout, outerGroupPageBreak: outerGroupPageBreak,
                bandStyles: string.IsNullOrEmpty(outerGroupBandStyles) ? null : Newtonsoft.Json.JsonConvert.DeserializeObject<List<NestedBandStyle>>(outerGroupBandStyles),
                headerStyle: string.IsNullOrEmpty(headerStyle) ? null : Newtonsoft.Json.JsonConvert.DeserializeObject<NestedBandStyle>(headerStyle),
                hideReportName: hideReportName);
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
            string filterDetailsText = null,
            string headerHtml = null,
            string footerHtml = null,
            bool headerEveryPage = false,
            bool footerEveryPage = false,
            string currentUserName = null,
            string currentUserRoles = null,
            string customHtml = null,
            string defaultDateFormat = null,
            string headerStyle = null,
            bool hideReportName = false,
            string outerGroupLayout = null,
            bool outerGroupPageBreak = false,
            string outerGroupBandStyles = null,
            bool subTotalPerGroup = false)
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
            DotNetReportHelper.defaultDateFormat = string.IsNullOrEmpty(defaultDateFormat) ? "United States" : defaultDateFormat;
            var columns = columnDetails == null ? new List<ReportHeaderColumn>() : JsonConvert.DeserializeObject<List<ReportHeaderColumn>>(HttpUtility.UrlDecode(columnDetails));
            var word = await DotNetReportHelper.GetWordFile(reportSql, connectKey, HttpUtility.UrlDecode(reportName), chartData, allExpanded, HttpUtility.UrlDecode(expandSqls), columns, includeSubtotal, pivot, pivotColumn, pivotFunction, pageSize, pageOrientation, HttpUtility.UrlDecode(filterDetailsText),
                headerHtml: headerHtml != null ? HttpUtility.UrlDecode(headerHtml) : null,
                footerHtml: footerHtml != null ? HttpUtility.UrlDecode(footerHtml) : null,
                headerEveryPage: headerEveryPage,
                footerEveryPage: footerEveryPage,
                currentUserName: currentUserName,
                currentUserRoles: currentUserRoles,
                customHtml: !string.IsNullOrEmpty(customHtml) ? HttpUtility.UrlDecode(customHtml) : null,
                headerStyle: string.IsNullOrEmpty(headerStyle) ? null : Newtonsoft.Json.JsonConvert.DeserializeObject<NestedBandStyle>(headerStyle),
                hideReportName: hideReportName,
                outerGroupLayout: outerGroupLayout, outerGroupPageBreak: outerGroupPageBreak,
                bandStyles: string.IsNullOrEmpty(outerGroupBandStyles) ? null : Newtonsoft.Json.JsonConvert.DeserializeObject<List<NestedBandStyle>>(outerGroupBandStyles),
                subTotalPerGroup: subTotalPerGroup);
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
                                settings.UserId, settings.ClientId, string.Join(",", settings.CurrentUserRole), JsonConvert.SerializeObject(settings.DataFilters), expandAll, expandSqls, pivotColumn, pivotFunction, false, debug, pageSize, pageOrientation, includeSubTotal, includeColumnTotal, isSubreport, pageNumber, currentPageSize, canUseAdminMode: settings.CanUseAdminMode);

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
           string onlyAndGroupInColumnDetail = null,
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
           string filterDetailsText = null,
           string reportDescription = null,
           string defaultDateFormat = null,
           string reportType = null)
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
            DotNetReportHelper.defaultDateFormat = string.IsNullOrEmpty(defaultDateFormat) ? "United States" : defaultDateFormat;
            var columns = columnDetails == null ? new List<ReportHeaderColumn>() : JsonConvert.DeserializeObject<List<ReportHeaderColumn>>(HttpUtility.UrlDecode(columnDetails));
            var onlyAndGroupInDetailColumns = string.IsNullOrEmpty(onlyAndGroupInColumnDetail) ? new List<ReportHeaderColumn>() : Newtonsoft.Json.JsonConvert.DeserializeObject<List<ReportHeaderColumn>>(HttpUtility.UrlDecode(onlyAndGroupInColumnDetail));
            var pdf = await DotNetReportHelper.GetPdfFileAlt(reportSql, connectKey, reportName, chartData, allExpanded, expandSqls, columns, includeSubtotal, pivot, pivotColumn, pivotFunction, pageSize, pageOrientation, subTotalPerGroup, HttpUtility.UrlDecode(filterDetailsText), HttpUtility.UrlDecode(reportDescription), onlyAndGroupInDetailColumns, reportType);

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
            bool adminMode = false,
            string defaultDateFormat = null)
        {
            var settings = GetSettings();
            if (!string.IsNullOrEmpty(settings.UserId) && settings.UserId != userId)
            {
                throw new Exception("User context mismatch");
            }
            reportSql = HttpUtility.HtmlDecode(reportSql);
            await ValidateAccess(userId, reportSql, adminMode: adminMode);
            DotNetReportHelper.defaultDateFormat = string.IsNullOrEmpty(defaultDateFormat) ? "United States" : defaultDateFormat;
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
                    Functions = functions,
                    DbConfig = GetDbConfigForPage(connect.AccountApiKey, connect.DatabaseApiKey),
                    CurrentUserId = settings.UserId
                };

                return model;
            }
            catch (Exception ex)
            {
                Context.Response.StatusCode = 500;
                return new { ex.Message };
            }
        }

        private object Forbidden()
        {
            Context.Response.StatusCode = 403;
            return new { success = false, message = "You are not authorized to perform this action" };
        }

        // The email settings for the Setup page, with the password and SendGrid key masked. Admin Mode only.
        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public object GetEmailSettings(string userId = null)
        {
            if (!GetSettings().CanUseAdminMode) return Forbidden();
            return DotNetReportHelper.GetEmailSettings(maskSecrets: true);
        }

        // Saves the email settings in appsettings.dotnetreport.json on this server. Admin Mode only.
        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public object SaveEmailSettings(string emailProvider, string emailAddress, string emailName, string emailServer, string emailPort,
            string emailUserName, string emailPassword, bool emailEnableSsl, string sendGridApiKey, string userId = null)
        {
            if (!GetSettings().CanUseAdminMode) return Forbidden();
            try
            {
                DotNetReportHelper.SaveEmailSettings(new DotNetReportEmailSettings
                {
                    emailProvider = emailProvider,
                    emailAddress = emailAddress,
                    emailName = emailName,
                    emailServer = emailServer,
                    emailPort = emailPort,
                    emailUserName = emailUserName,
                    emailPassword = emailPassword,
                    emailEnableSsl = emailEnableSsl,
                    sendGridApiKey = sendGridApiKey
                });
                return new { success = true };
            }
            catch (Exception ex)
            {
                return new { success = false, message = ex.Message };
            }
        }

        // Switches the whole app to another Workspace of this account, saved in appsettings.dotnetreport.json. Admin Mode only.
        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public object SwitchWorkspace(string dataConnect, string userId = null)
        {
            var settings = GetSettings();
            if (!settings.CanUseAdminMode) return Forbidden();

            dataConnect = (dataConnect ?? "").Trim();
            if (string.IsNullOrEmpty(dataConnect)) return new { success = false, message = "Please choose a Workspace" };

            try
            {
                // Only a Workspace of this account, so the app can't be pointed at something else
                using (var client = new HttpClient())
                {
                    var response = Task.Run(() => client.GetAsync($"{settings.ApiUrl}/ReportApi/GetDataConnectKey?account={Uri.EscapeDataString(settings.AccountApiToken ?? "")}&dataConnect={Uri.EscapeDataString(dataConnect)}")).Result;
                    if (!response.IsSuccessStatusCode) return new { success = false, message = "That Workspace was not found in your account" };
                }

                DotNetReportHelper.SwitchWorkspace(dataConnect);
                ClearPermissionCache();
                return new { success = true, message = "The app now uses this Workspace" };
            }
            catch (Exception ex)
            {
                return new { success = false, message = ex.Message };
            }
        }

        // Sends a test email with the saved email settings. Admin Mode only.
        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public object SendTestEmail(string email, string userId = null)
        {
            if (!GetSettings().CanUseAdminMode) return Forbidden();

            email = (email ?? "").Trim();
            if (string.IsNullOrEmpty(email)) return new { success = false, message = "Email is required" };

            try
            {
                using (var mail = new System.Net.Mail.MailMessage
                {
                    Subject = "Dotnet Report test email",
                    IsBodyHtml = true,
                    Body = "<p>Your email settings are working.</p>"
                })
                {
                    mail.To.Add(email);
                    Task.Run(() => DotNetReportEmail.SendAsync(mail)).Wait();
                }
                return new { success = true };
            }
            catch (Exception ex)
            {
                return new { success = false, message = (ex.InnerException ?? ex).Message };
            }
        }

        // The server and database a connection string points to, for the setup page. The rest stays on the server.
        private static (string server, string database) DescribeConnectionString(string connectionString)
        {
            try
            {
                var builder = new System.Data.Common.DbConnectionStringBuilder { ConnectionString = connectionString ?? "" };
                string pick(params string[] keys) => keys.Select(k => builder.TryGetValue(k, out var v) ? v?.ToString() : null).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? "";
                return (pick("Server", "Data Source", "Host", "Address", "Addr", "Network Address"), pick("Database", "Initial Catalog"));
            }
            catch
            {
                return ("", "");
            }
        }

        private static (string server, string database) DescribeDbConnection(JObject saved)
        {
            if (saved["ConnectionType"]?.ToString() == "Build")
                return (saved["DatabaseHost"]?.ToString() ?? "", saved["DatabaseName"]?.ToString() ?? "");

            // A key in Web.config is read now, so a changed connection string shows straight away
            var key = saved["ConnectionKey"]?.ToString();
            var connectionString = string.IsNullOrEmpty(key) ? null : DotNetReportHelper.GetAppConnectionString(key, false);
            return DescribeConnectionString(string.IsNullOrEmpty(connectionString) ? saved["ConnectionString"]?.ToString() : connectionString);
        }

        // The workspace's saved database connection for the setup page, without the connection string or password
        private static Dictionary<string, string> GetDbConfigForPage(string accountApiKey, string databaseApiKey)
        {
            var result = new Dictionary<string, string>();
            try
            {
                if (DotNetReportHelper.GetDbConnectionSettings(accountApiKey, databaseApiKey) is JObject saved)
                {
                    foreach (var key in new[] { "DatabaseType", "ConnectionType", "ConnectionKey", "DatabaseHost", "DatabasePort", "DatabaseName", "Username", "AuthenticationType", "ProviderName" })
                        result[key] = saved[key]?.ToString() ?? "";
                    result["HasPassword"] = string.IsNullOrEmpty(saved["Password"]?.ToString()) ? "false" : "true";
                    var (server, database) = DescribeDbConnection(saved);
                    result["ConnectedServer"] = server;
                    result["ConnectedDatabase"] = database;
                }
            }
            catch
            {
                // No saved connection: the app's own connection string is used
            }
            return result;
        }

        // Tests or saves the workspace's database connection, stored in appsettings.dotnetreport.json. Admin Mode only.
        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public object UpdateDbConnection(string account, string dataConnect, string dbType, string connectionType, string connectionKey, string providerName,
            string dbServer, string dbPort, string dbName, string dbUsername, string dbPassword, string dbAuthType, bool testOnly, string userId = null)
        {
            if (!GetSettings().CanUseAdminMode) return Forbidden();

            var model = new UpdateDbConnectionModel
            {
                account = account ?? "", dataConnect = dataConnect ?? "", dbType = dbType ?? "", connectionType = connectionType ?? "",
                connectionKey = connectionKey ?? "", providerName = providerName ?? "", dbServer = dbServer ?? "", dbPort = dbPort ?? "",
                dbName = dbName ?? "", dbUsername = dbUsername ?? "", dbPassword = dbPassword ?? "", dbAuthType = dbAuthType ?? "", testOnly = testOnly
            };
            try
            {
                // The saved password is never sent to the page, so a blank one means keep it
                if (model.connectionType == "Build" && string.Equals(model.dbAuthType, "Username", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(model.dbPassword)
                    && DotNetReportHelper.GetDbConnectionSettings(model.account, model.dataConnect) is JObject saved)
                {
                    model.dbPassword = saved["Password"]?.ToString() ?? "";
                }

                DotNetReportHelper.UpdateDbConnection(model);

                var (server, database) = model.connectionType == "Build"
                    ? (model.dbServer, model.dbName)
                    : DescribeConnectionString(DotNetReportHelper.GetAppConnectionString(model.connectionKey, false));
                var where = string.Join(", ", new[] { server, database }.Where(x => !string.IsNullOrWhiteSpace(x)));
                return new
                {
                    success = true,
                    message = (model.testOnly ? "Connected to the database" : "Database connection saved") + (where == "" ? "" : " (" + where + ")"),
                    connectedServer = server,
                    connectedDatabase = database
                };
            }
            catch (Exception ex)
            {
                return new { success = false, message = ex.Message };
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
        public class DatabaseRelationsCall
        {
            public string accountKey { get; set; }
            public string dataConnectKey { get; set; }
        }
        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public object GetDatabaseRelations(string dataConnectKey)
        {
            try
            {
                var settings = GetSettings();
                if (!settings.CanUseAdminMode) throw new Exception("Not Authorized to access this Resource");

                IDatabaseConnection databaseConnection = DatabaseConnectionFactory.GetConnection(DotNetReportHelper.dbtype);
                return databaseConnection.GetForeignKeys(dataConnectKey).Result;
            }
            catch (Exception ex)
            {
                Context.Response.StatusCode = 500;
                return new { ex.Message };
            }
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
            timeZoneList[""] = "";

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

                timeZoneList[display] = timezone.Id;
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