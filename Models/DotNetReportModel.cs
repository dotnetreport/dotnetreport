using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout;
using PdfSharp.Pdf;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace ReportBuilder.Web.Models
{
    public class DotNetReportModel
    {
        public int ReportId { get; set; }
        public string ReportName { get; set; }
        public string ReportDescription { get; set; }
        public string ReportSql { get; set; }

        public bool IncludeSubTotals { get; set; }
        public bool ShowUniqueRecords { get; set; }
        public string ReportFilter { get; set; }
        public string ReportType { get; set; }
        public bool ShowDataWithGraph { get; set; }

        public string ConnectKey { get; set; }

        public string ChartData { get; set; }
        public bool IsDashboard { get; set; }
        public int SelectedFolder { get; set; }

        public string ReportSeries { get; set; }
        public bool ExpandAll { get; set; }
    }

    public class DotNetReportPrintModel : DotNetReportModel
    {
        public string ClientId { get; set; }
        public string UserId { get; set; }
        public string CurrentUserRoles { get; set; }
        public string DataFilters { get; set; }
    }

    public class DotNetReportResultModel
    {
        public string ReportSql { get; set; }
        public DotNetReportDataModel ReportData { get; set; }
        public DotNetReportPagerModel Pager { get; set; }

        public bool HasError { get; set; }
        public string Exception { get; set; }
        public string Warnings { get; set; }

        public bool ReportDebug { get; set; }
    }

    public class DotNetReportPagerModel
    {
        public int CurrentPage { get; set; }
        public int TotalRecords { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class DotNetReportDataColumnModel
    {
        public string SqlField { get; set; }
        public string ColumnName { get; set; }
        public string DataType { get; set; }
        public bool IsNumeric { get; set; }
        public string FormatType { get; set; }
    }

    public class DotNetReportDataRowItemModel
    {
        public string Value { get; set; }
        public string FormattedValue { get; set; }
        public string LabelValue { get; set; }
        public double? NumericValue { get; set; }
        public DotNetReportDataColumnModel Column { get; set; }
    }

    public class DotNetReportDataRowModel
    {
        public DotNetReportDataRowItemModel[] Items { get; set; }
    }

    public class DotNetReportDataModel
    {
        public List<DotNetReportDataRowModel> Rows { get; set; }
        public List<DotNetReportDataColumnModel> Columns { get; set; }
    }

    public class TableViewModel
    {
        public int Id { get; set; }
        public string SchemaName { get; set; }
        public string TableName { get; set; }
        public string DisplayName { get; set; }
        public bool Selected { get; set; }
        public bool IsView { get; set; }
        public int DisplayOrder { get; set; }
        public string AccountIdField { get; set; }

        public List<ColumnViewModel> Columns { get; set; }

        public DataTable dataTable { get; set; }
        public List<ParameterViewModel> Parameters { get; set; }
        public List<string> AllowedRoles { get; set; }
        public bool? DoNotDisplay { get; set; }

        public bool CustomTable { get; set; }
        public string CustomTableSql { get; set; }
    }

    public class ParameterViewModel
    {
        public int Id { get; set; }
        public string ParameterName { get; set; }
        public string DisplayName { get; set; }
        public string ParameterValue { get; set; }
        public string ParameterDataTypeString { get; set; }
        public Type ParameterDataTypeCLR { get; set; }
        public OleDbType ParamterDataTypeOleDbType { get; set; }
        public int ParamterDataTypeOleDbTypeInteger { get; set; }
        public bool Required { get; set; }
        public bool ForeignKey { get; set; }
        public string ForeignTable { get; set; }
        public string ForeignJoin { get; set; }
        public string ForeignKeyField { get; set; }
        public string ForeignValueField { get; set; }
        public bool Hidden { get; set; }

    }
    public class RelationModel
    {
        public int Id { get; set; }
        public int TableId { get; set; }
        public int JoinedTableId { get; set; }
        public string JoinType { get; set; }
        public string FieldName { get; set; }
        public string JoinFieldName { get; set; }
    }

    public enum FieldTypes
    {
        Boolean,
        DateTime,
        Varchar,
        Money,
        Int,
        Double
    }

    public enum JoinTypes
    {
        Inner,
        Left,
        Right
    }

    public class ColumnViewModel
    {
        public int Id { get; set; }
        public string ColumnName { get; set; }
        public string DisplayName { get; set; }
        public bool Selected { get; set; }
        public int DisplayOrder { get; set; }
        public string FieldType { get; set; }
        public bool PrimaryKey { get; set; }
        public bool ForeignKey { get; set; }
        public bool AccountIdField { get; set; }
        public string ForeignTable { get; set; }
        public string ForeignJoin { get; set; }
        public string ForeignKeyField { get; set; }
        public string ForeignValueField { get; set; }
        public bool DoNotDisplay { get; set; }
        public bool ForceFilter { get; set; }
        public bool ForceFilterForTable { get; set; }
        public string RestrictedDateRange { get; set; }
        public DateTime? RestrictedStartDate { get; set; }
        public DateTime? RestrictedEndDate { get; set; }
        public List<string> AllowedRoles { get; set; }
        public bool ForeignParentKey { get; set; }
        public string ForeignParentTable { get; set; }
        public string ForeignParentApplyTo { get; set; }
        public string ForeignParentKeyField { get; set; }
        public string ForeignParentValueField { get; set; }
        public bool ForeignParentRequired { get; set; }
        public string JsonStructure { get; set; }
    }

    public class ConnectViewModel
    {
        public string Provider { get; set; }
        public string ServerName { get; set; }
        public string InitialCatalog { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool IntegratedSecurity { get; set; }
        public string ApiUrl { get; set; }
        public string AccountApiKey { get; set; }
        public string DatabaseApiKey { get; set; }
    }

    public class ManageViewModel
    {
        public string ApiUrl { get; set; }
        public string AccountApiKey { get; set; }
        public string DatabaseApiKey { get; set; }

        public List<TableViewModel> Tables { get; set; }
        public List<TableViewModel> Procedures { get; set; }
    }

    public class DotNetReportApiCall
    {
        public string Method { get; set; }
        public bool SaveReport { get; set; }
        public string ReportJson { get; set; }
        public bool adminMode { get; set; }
        public bool SubTotalMode { get; set; }
    }

    public class DotNetDasboardReportModel : DotNetReportModel
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool IsWidget { get; set; }
    }

    public class DotNetDashboardModel
    {
        public List<dynamic> Dashboards { get; set; }
        public List<DotNetDasboardReportModel> Reports { get; set; }
    }

    public class DotNetReportSettings
    {
        /// <summary>
        /// dotnet Report Service Api Url
        /// </summary>
        public string ApiUrl { get; set; }

        /// <summary>
        /// Your dotnet Report Account Key
        /// </summary>
        public string AccountApiToken { get; set; }

        /// <summary>
        /// Your dotnet Report Data Connection Key
        /// </summary>
        public string DataConnectApiToken { get; set; }

        /// <summary>
        /// Current Client Id if using Multi-tenant
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Current User Id if using Authentication
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// If you want to use user id for schedule but not for authentication, use this property
        /// </summary>
        public string UserIdForSchedule { get; set; }

        /// <summary>
        /// Current User name to display
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// List of Current User's Roles if using Authentication
        /// </summary>
        public List<string> CurrentUserRole { get; set; } = new List<string>();

        /// <summary>
        /// List of all User Ids in your Application
        /// </summary>
        public List<dynamic> Users { get; set; } = new List<dynamic>();

        /// <summary>
        /// List of all User Roles in your Application
        /// </summary>
        public List<string> UserRoles { get; set; } = new List<string>();

        /// <summary>
        /// A list of Global Data filters using format { Column1: 'val1, val2, ...', Column2: '1,2,3,...', ...}
        /// </summary>
        public dynamic DataFilters { get; set; }

        /// <summary>
        /// Set true if the current user can enter Admin Mode
        /// </summary>
        public bool CanUseAdminMode { get; set; }
    }

    public class ReportHeaderColumn
    {
        public string fieldName { get; set; }
        public string fieldLabel { get; set; }
        public bool hideStoredProcColumn { get; set; }
        public int? decimalPlaces { get; set; }
        public string fieldAlign { get; set; }
        public string fieldFormat { get; set; }
        public bool dontSubTotal { get; set; }

        public bool isNumeric { get; set; }
        public bool isCurrency { get; set; }
        public bool isJsonColumn { get; set; }
    }

    public class DotNetReportHelper
    {
        public static string GetConnectionString(string key)
        {
            var connString = Startup.StaticConfig.GetConnectionString(key);
            connString = connString.Replace("Trusted_Connection=True", "");

            if (!connString.ToLower().StartsWith("provider"))
            {
                connString = "Provider=sqloledb;" + connString;
            }

            return connString;
        }

        public static bool IsNumericType(Type type)
        {

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    return true;

                case TypeCode.Boolean:
                case TypeCode.DateTime:
                case TypeCode.String:
                default:
                    return false;
            }
        }

        public static string GetLabelValue(DataColumn col, DataRow row)
        {
            if (@row[col] != null && row[col] != DBNull.Value)
            {
                switch (Type.GetTypeCode(col.DataType))
                {
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                    case TypeCode.Single:
                        return row[col].ToString();

                    case TypeCode.Double:
                    case TypeCode.Decimal:
                        return @row[col].ToString();// "'" + (Convert.ToDouble(@row[col].ToString()).ToString("C")) + "'";

                    case TypeCode.Boolean:
                        return (Convert.ToBoolean(@row[col]) ? "Yes" : "No");

                    case TypeCode.DateTime:
                        try
                        {
                            return "'" + @Convert.ToDateTime(@row[col]).ToShortDateString() + "'";
                        }
                        catch
                        {
                            return "'" + @row[col] + "'";
                        }

                    case TypeCode.String:
                    default:
                        return "'" + @row[col].ToString().Replace("'", "") + "'";
                }
            }

            return "";
        }


        static string ParseJsonValue(JToken json, string columnToExtract, bool asTable = true)
        {
            if (!string.IsNullOrEmpty(columnToExtract))
                return json.Value<dynamic>(columnToExtract)?.ToString();

            StringBuilder sb = new StringBuilder();
            ParseJson(json, sb, "", asTable);

            return asTable ? $"<table>{sb}</table>" : sb.ToString();
        }

        static void ParseJson(JToken token, StringBuilder sb, string prefix, bool asTable = true)
        {
            if (token.Type == JTokenType.Object)
            {
                foreach (JProperty prop in token.Children<JProperty>())
                {
                    string propName = prop.Name;
                    if (!string.IsNullOrEmpty(prefix))
                    {
                        propName = prefix + " > " + propName;
                    }

                    ParseJson(prop.Value, sb, propName, asTable);
                }
            }
            else if (token.Type == JTokenType.Array)
            {
                int index = 0;
                foreach (JToken child in token.Children())
                {
                    ParseJson(child, sb, prefix + " - " + index + 1, asTable);
                    index++;
                }
            }
            else
            {
                string value = token.ToString();
                if (!string.IsNullOrEmpty(prefix))
                {
                    if (asTable)
                        sb.AppendFormat($"<tr><td>{prefix}</td><td>{value}</td></tr>\n");
                    else
                        sb.AppendFormat($"{prefix} is {value}\n");
                }
            }
        }

        static bool IsValidJson(string json)
        {
            try
            {
                JToken.Parse(json);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public static bool IsNumeric(string value)
        {
            double result;
            return double.TryParse(value, out result);
        }


        public static string GetFormattedValue(DataColumn col, DataRow row, string formatType, bool jsonAsTable = true)
        {
            if (row[col] != null && row[col] != DBNull.Value && !string.IsNullOrEmpty(row[col].ToString()))
            {
                var val = row[col].ToString().Trim();
                if ((val.StartsWith("[") || val.StartsWith("{")) && IsValidJson(val) && !IsNumeric(val))
                {
                    JToken json = JToken.Parse(val);
                    return ParseJsonValue(json, formatType == "Json" ? col.ColumnName : "", jsonAsTable);
                }

                switch (Type.GetTypeCode(col.DataType))
                {
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                    case TypeCode.Single:
                        return row[col].ToString();


                    case TypeCode.Double:
                    case TypeCode.Decimal:
                        return col.ColumnName.Contains("%")
                            ? (Convert.ToDouble(row[col].ToString()) / 100).ToString("P2")
                            : Convert.ToDouble(row[col].ToString()).ToString("C");


                    case TypeCode.Boolean:
                        return (Convert.ToBoolean(row[col]) ? "Yes" : "No");


                    case TypeCode.DateTime:
                        try
                        {
                            return Convert.ToDateTime(row[col]).ToShortDateString();
                        }
                        catch
                        {
                            return row[col] != null ? row[col].ToString() : null;
                        }

                    case TypeCode.String:
                    default:
                        if (row[col].ToString() == "System.Byte[]")
                        {

                            return "<img src=\"data:image/png;base64," + Convert.ToBase64String((byte[])row[col], 0, ((byte[])row[col]).Length) + "\" style=\"max-width: 200px;\" />";
                        }
                        else
                        {
                            return row[col].ToString();
                        }

                }
            }
            return "";
        }

        private static void FormatExcelSheet(DataTable dt, ExcelWorksheet ws, int rowstart, int colstart, List<ReportHeaderColumn> columns = null, bool includeSubtotal = false)
        {
            ws.Cells[rowstart, colstart].LoadFromDataTable(dt, true);
            ws.Cells[rowstart, colstart, rowstart, dt.Columns.Count].Style.Font.Bold = true;

            int i = 1; var isNumeric = false;
            foreach (DataColumn dc in dt.Columns)
            {
                isNumeric = dc.DataType.Name.StartsWith("Int") || dc.DataType.Name == "Double" || dc.DataType.Name == "Decimal";
                if (dc.DataType == typeof(decimal))
                {
                    ws.Column(i).Style.Numberformat.Format = "###,###,##0.00";
                    isNumeric = true;
                }
                if (dc.DataType == typeof(DateTime))
                    ws.Column(i).Style.Numberformat.Format = "mm/dd/yyyy";

                var formatColumn = columns?.FirstOrDefault(x => dc.ColumnName.StartsWith(x.fieldName));
                if (formatColumn != null && formatColumn.fieldFormat == "Currency")
                {
                    ws.Column(i).Style.Numberformat.Format = "$###,###,##0.00";
                    isNumeric = true;
                }
                if (formatColumn != null && formatColumn.isJsonColumn)
                {
                    ws.Column(i).Style.Numberformat.Format = "@";
                    ws.Column(i).Style.WrapText = true;
                }
                if (formatColumn != null)
                {
                    ws.Column(i).Style.HorizontalAlignment = formatColumn.fieldAlign == "Right" || (isNumeric && (formatColumn.fieldAlign == "Auto" || string.IsNullOrEmpty(formatColumn.fieldAlign))) ? OfficeOpenXml.Style.ExcelHorizontalAlignment.Right : formatColumn.fieldAlign == "Center" ? OfficeOpenXml.Style.ExcelHorizontalAlignment.Center : OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;
                }

                if (includeSubtotal)
                {
                    if (isNumeric && !(formatColumn?.dontSubTotal ?? false))
                    {
                        ws.Cells[dt.Rows.Count + rowstart + 1, i].Formula = $"=SUM({ws.Cells[rowstart, i].Address}:{ws.Cells[dt.Rows.Count + rowstart, i].Address})";
                        ws.Cells[dt.Rows.Count + rowstart + 1, i].Style.Font.Bold = true;
                    }
                }

                i++;
            }

            ws.Cells[ws.Dimension.Address].AutoFitColumns();
        }

        public static DataTable Transpose(DataTable dt)
        {
            DataTable dtNew = new DataTable();

            for (int i = 0; i <= dt.Rows.Count; i++)
            {
                dtNew.Columns.Add(i.ToString());
            }

            dtNew.Columns[0].ColumnName = " ";

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                dtNew.Columns[i + 1].ColumnName = dt.Rows[i].ItemArray[0].ToString();
            }

            for (int k = 1; k < dt.Columns.Count; k++)
            {
                DataRow r = dtNew.NewRow();
                r[0] = dt.Columns[k].ToString();
                for (int j = 1; j <= dt.Rows.Count; j++)
                    r[j] = dt.Rows[j - 1][k];
                dtNew.Rows.Add(r);
            }

            return dtNew;
        }

        public static async Task<string> RunReportApiCall(string postData)
        {
            using (var client = new HttpClient())
            {
                var settings = new DotNetReportSettings
                {
                    ApiUrl = Startup.StaticConfig.GetValue<string>("dotNetReport:apiUrl"),
                    AccountApiToken = Startup.StaticConfig.GetValue<string>("dotNetReport:accountApiToken"), // Your Account Api Token from your http://dotnetreport.com Account
                    DataConnectApiToken = Startup.StaticConfig.GetValue<string>("dotNetReport:dataconnectApiToken") // Your Data Connect Api Token from your http://dotnetreport.com Account
                };
                var keyvalues = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("Account", settings.AccountApiToken),
                    new KeyValuePair<string, string>("DataConnect", settings.DataConnectApiToken),
                    new KeyValuePair<string, string>("ClientId", ""),
                    new KeyValuePair<string, string>("UserId", ""),
                    new KeyValuePair<string, string>("SubTotalMode", "false"),
                    new KeyValuePair<string, string>("AdminMode", "false"),
                    new KeyValuePair<string, string>("UserIdForSchedule", ""),
                    new KeyValuePair<string, string>("ReportJson", postData),
                    new KeyValuePair<string, string>("SaveReport", "false"),
                };

                var encodedItems = keyvalues.Select(i => WebUtility.UrlEncode(i.Key) + "=" + WebUtility.UrlEncode(i.Value));
                var encodedContent = new StringContent(String.Join("&", encodedItems), null, "application/x-www-form-urlencoded");

                var response = await client.PostAsync(new Uri(settings.ApiUrl + "/ReportApi/RunDrillDownReport"), encodedContent);
                var stringContent = await response.Content.ReadAsStringAsync();
                var sql = "";
                if (stringContent.Contains("\"sql\":"))
                {
                    var sqlQuery = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(stringContent);
                    object value;
                    var keyValuePair = sqlQuery.TryGetValue("sql", out value);
                    sql = Decrypt(value.ToString());
                }

                return sql;
            }
        }

        public static int FindFromIndex(string sql)
        {
            int parenthesesCount = 0;

            for (int i = 0; i < sql.Length - 4; i++)  // -4 because "FROM" has 4 characters
            {
                if (sql[i] == '(')
                {
                    parenthesesCount++;
                }
                else if (sql[i] == ')')
                {
                    parenthesesCount--;
                }
                else if (parenthesesCount == 0 && sql.Substring(i, 4).Equals("FROM", StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        public static List<string> SplitSqlColumns(string sql)
        {
            var fromIndex = FindFromIndex(sql);
            var sqlSplit = sql.Substring(0, fromIndex).Replace("SELECT", "").Trim();
            var sqlFields = Regex.Split(sqlSplit, "], (?![^\\(]*?\\))").Where(x => x != "CONVERT(VARCHAR(3)")
                .Select(x => x.EndsWith("]") ? x : x + "]")
                .Select(x => x.StartsWith("DISTINCT ") ? x.Replace("DISTINCT ", "") : x)
                .Select(x => x.StartsWith("TOP ") ? Regex.Replace(x, @"TOP\s+\d+", "") : x)
                .Where(x => x.Contains(" AS "))
                .ToList();

            return sqlFields;
        }

        public static DotNetReportDataModel DataTableToDotNetReportDataModel(DataTable dt, List<string> sqlFields, bool jsonAsTable = true)
        {
            var model = new DotNetReportDataModel
            {
                Columns = new List<DotNetReportDataColumnModel>(),
                Rows = new List<DotNetReportDataRowModel>()
            };

            int i = 0;
            foreach (DataColumn col in dt.Columns)
            {
                var sqlField = sqlFields[i++];
                model.Columns.Add(new DotNetReportDataColumnModel
                {
                    SqlField = sqlField.Contains(" FROM ") ? col.ColumnName : sqlField.Substring(0, sqlField.LastIndexOf(" AS ")).Trim().Replace("__jsonc__", ""),
                    ColumnName = col.ColumnName,
                    DataType = col.DataType.ToString(),
                    IsNumeric = IsNumericType(col.DataType),
                    FormatType = sqlField.Contains("__jsonc__") ? "Json" : (sqlField.Contains(" FROM ") ? "Csv" : "")
                });

                col.ColumnName = col.ColumnName.Replace("__jsonc__", "");
            }

            foreach (DataRow row in dt.Rows)
            {
                i = 0;
                var items = new List<DotNetReportDataRowItemModel>();

                foreach (DataColumn col in dt.Columns)
                {
                    var item = new DotNetReportDataRowItemModel
                    {
                        Column = model.Columns[i],
                        Value = row[col] != null ? row[col].ToString() : null,
                        FormattedValue = GetFormattedValue(col, row, model.Columns[i].FormatType, jsonAsTable),
                        LabelValue = GetLabelValue(col, row)
                    };

                    items.Add(item);

                    try
                    {
                        row[col] = item.FormattedValue;
                    }
                    catch (Exception ex)
                    {
                        // ignore
                    }
                    i += 1;
                }

                model.Rows.Add(new DotNetReportDataRowModel
                {
                    Items = items.ToArray()
                });
            }

            return model;
        }

        public static byte[] GetExcelFile(string reportSql, string connectKey, string reportName, bool allExpanded = false,
                string expandSqls = null, List<ReportHeaderColumn> columns = null, bool includeSubtotal = false, bool pivot = false)
        {
            var sql = Decrypt(reportSql);
            var sqlFields = SplitSqlColumns(sql);

            // Execute sql
            var dt = new DataTable();
            using (var conn = new OleDbConnection(GetConnectionString(connectKey)))
            {
                conn.Open();
                var command = new OleDbCommand(sql, conn);
                var adapter = new OleDbDataAdapter(command);

                adapter.Fill(dt);

                if (pivot) dt = Transpose(dt);

                if (columns?.Count > 0)
                {
                    foreach (var col in columns)
                    {
                        if (dt.Columns.Contains(col.fieldName) && col.hideStoredProcColumn)
                        {
                            dt.Columns.Remove(col.fieldName);
                        }
                        else if (!String.IsNullOrWhiteSpace(col.fieldLabel) && dt.Columns.Contains(col.fieldName))
                        {
                            dt.Columns[col.fieldName].ColumnName = col.fieldLabel;
                        }
                    }
                }

                using (ExcelPackage xp = new ExcelPackage())
                {
                    ExcelWorksheet ws = xp.Workbook.Worksheets.Add(reportName);

                    int rowstart = 1;
                    int colstart = 1;
                    int rowend = rowstart;
                    int colend = dt.Columns.Count;

                    ws.Cells[rowstart, colstart, rowend, colend].Merge = true;
                    ws.Cells[rowstart, colstart, rowend, colend].Value = reportName;
                    ws.Cells[rowstart, colstart, rowend, colend].Style.Font.Bold = true;
                    ws.Cells[rowstart, colstart, rowend, colend].Style.Font.Size = 14;

                    rowstart += 2;
                    rowend = rowstart + dt.Rows.Count;

                    DataTableToDotNetReportDataModel(dt, sqlFields, false);
                    FormatExcelSheet(dt, ws, rowstart, colstart, columns, includeSubtotal);

                    if (allExpanded)
                    {
                        var insertRowIndex = 5;
                        foreach (DataRow dr in dt.Rows)
                        {
                            var drilldownRow = new List<string>();

                            int i = 0;
                            foreach (DataColumn dc in dt.Columns)
                            {
                                var col = sqlFields[i++]; //columns.FirstOrDefault(x => x.fieldName == dc.ColumnName) ?? new ReportHeaderColumn();
                                drilldownRow.Add($@"
                                    {{
                                        ""Value"":""{dr[dc]}"",
                                        ""FormattedValue"":""{dr[dc]}"",
                                        ""LabelValue"":""'{dr[dc]}'"",
                                        ""NumericValue"":null,
                                        ""Column"":{{
                                            ""SqlField"":""{col}"",
                                            ""ColumnName"":""{dc.ColumnName}"",
                                            ""DataType"":""{dc.DataType.ToString()}"",
                                            ""IsNumeric"":{(dc.DataType.Name.StartsWith("Int") || dc.DataType.Name == "Double" || dc.DataType.Name == "Decimal" ? "true" : "false")},
                                            ""FormatType"":""""
                                        }}
                                     }}
                                ");
                            }

                            var reportData = expandSqls.Replace("\"DrillDownRow\":[]", $"\"DrillDownRow\": [{string.Join(',', drilldownRow)}]").Replace("\"IsAggregateReport\":true", "\"IsAggregateReport\":false");
                            var drilldownSql = RunReportApiCall(reportData).Result;
                            if (!string.IsNullOrEmpty(drilldownSql))
                            {
                                var ddt = new DataTable();
                                var cmd = new OleDbCommand(drilldownSql, conn);
                                var adp = new OleDbDataAdapter(cmd);
                                adp.Fill(ddt);

                                ws.InsertRow(insertRowIndex, ddt.Rows.Count + 1);
                                ws.Cells[insertRowIndex, dt.Columns.Count + 1].LoadFromDataTable(ddt, true);
                                insertRowIndex += ddt.Rows.Count + 2;
                            }
                        }
                    }

                    return xp.GetAsByteArray();
                }
            }
        }

        public static ReportHeaderColumn GetColumnFormatting(DataColumn dc, List<ReportHeaderColumn> columns, ref string value)
        {
            var isCurrency = false;
            var isNumeric = dc.DataType.Name.StartsWith("Int") || dc.DataType.Name == "Double" || dc.DataType.Name == "Decimal";
            var formatColumn = columns?.FirstOrDefault(x => dc.ColumnName.StartsWith(x.fieldName));

            try
            {
                if (dc.DataType == typeof(decimal) || (formatColumn != null && (formatColumn.fieldFormat == "Decimal" || formatColumn.fieldFormat == "Double")))
                {
                    isNumeric = true;
                    value = Convert.ToDecimal(value).ToString("###,###,##0.00");
                }
                if (formatColumn != null && formatColumn.fieldFormat == "Currency")
                {
                    value = Convert.ToDecimal(value).ToString("C");
                    isCurrency = true;
                }
                if (formatColumn != null && (formatColumn.fieldFormat == "Date" || formatColumn.fieldFormat == "Date and Time" || formatColumn.fieldFormat == "Time") && dc.DataType.Name == "DateTime")
                {
                    var date = Convert.ToDateTime(value);
                    value = formatColumn.fieldFormat.StartsWith("Date") ? date.ToShortDateString() + " " : "";
                    value += formatColumn.fieldFormat.EndsWith("Time") ? date.ToShortTimeString() : "";
                    value = value.Trim();
                }
            } catch (Exception ex)
            {
                // ignore formatting exceptions
            }

            if (formatColumn != null)
            {
                formatColumn.isNumeric = isNumeric;
                formatColumn.isCurrency = isCurrency;
            }

            return formatColumn ?? new ReportHeaderColumn
            {
                isNumeric = isNumeric,
                isCurrency = isCurrency
            };
        }


        /// <summary>
        /// Customize this method with a login for dotnet report so that it can login to print pdf reports
        /// </summary>
        public static async Task PerformLogin(Page page, string printUrl)
        {
            var loginUrl = printUrl.Replace("/DotNetReport/ReportPrint", "/Account/Login"); // link to your login page
            var loginEmail = "yourloginid@yourcompany.com"; // your login id
            var loginPassword = "yourPassword"; // your login password

            await page.GoToAsync(loginUrl, new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Networkidle0 }
            });
            await page.TypeAsync("#Email", loginEmail); // Make sure #Email is replaced with the username form input id
            await page.TypeAsync("#Password", loginPassword); // Make sure #Password is replaced with the password form input id
            await page.ClickAsync("#LoginSubmit"); // Make sure #LoginSubmit is replaced with the login button form input id
        }

        private static XSolidBrush GetBrushWithColor(string htmlColor = "")
        {
            var color = ColorTranslator.FromHtml(!string.IsNullOrEmpty(htmlColor) ? htmlColor : "#007bff");
            var xColor = XColor.FromArgb(color.R, color.G, color.B);
            return new XSolidBrush(xColor);
        }

        private static List<string> WrapText(XGraphics gfx, string value, XRect rect, XFont font, XStringFormat format)
        {
            // Manually wrap the text if it's too long
            List<string> lines = new List<string>();
            string line = "";
            foreach (char c in value)
            {
                XSize size = gfx.MeasureString(line + c, font);
                if (size.Width > rect.Width || c == '\r' || c == '\n')
                {
                    lines.Add(line);
                    line = c == '\r' || c == '\n' ? "" : c.ToString();
                }
                else
                {
                    line += c;
                }
            }
            lines.Add(line);

            return lines;
        }

        public static byte[] GetPdfFileAlt(string reportSql, string connectKey, string reportName, string chartData = null,
                    List<ReportHeaderColumn> columns = null, bool includeSubtotal = false, bool pivot = false)
        {
            var sql = Decrypt(reportSql); 
            var sqlFields = SplitSqlColumns(sql);

            var dt = new DataTable();
            using (var conn = new OleDbConnection(GetConnectionString(connectKey)))
            {
                conn.Open();
                var command = new OleDbCommand(sql, conn);
                var adapter = new OleDbDataAdapter(command);

                adapter.Fill(dt);
            }

            var document = new PdfDocument();
            var page = document.AddPage();
            var gfx = XGraphics.FromPdfPage(page);
            var tfx = new XTextFormatter(gfx);
            int maxColumnsPerPage = 10;
            int leftMargin = 40;

            if (pivot)
            {
                dt = Transpose(dt);
                page.Orientation = PageOrientation.Landscape;
            }

            if (dt.Columns.Count > maxColumnsPerPage)
            {
                page.Orientation = PdfSharp.PageOrientation.Landscape;
            }

            DataTableToDotNetReportDataModel(dt, sqlFields, false);

            // Calculate the height of the page
            double pageHeight = page.Height.Point - 50;

            var subTotals = new decimal[dt.Columns.Count];

            using (var ms = new MemoryStream())
            {
                Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                var fontNormal = new XFont("Arial", 11, XFontStyle.Regular);
                var fontBold = new XFont("Arial", 12, XFontStyle.Bold);
                var tableWidth = page.Width - 100;
                var columnWidth = Math.Max(tableWidth / dt.Columns.Count, 100f);
                var currentYPosition = 30;
                var currentXPosition = leftMargin;
                double cellPadding = 3; // set the padding value
                XRect rect = new XRect();

                // Report header
                gfx.DrawString(reportName,
                    new XFont("Arial", 14, XFontStyle.Bold), XBrushes.Black,
                    new XRect(0, currentYPosition, page.Width, 30),
                    XStringFormats.Center);

                currentYPosition += 40;

                // Render chart
                if (!string.IsNullOrEmpty(chartData) && chartData != "undefined")
                {
                    byte[] sPDFDecoded = Convert.FromBase64String(chartData.Substring(chartData.LastIndexOf(',') + 1));
                    var imageStream = new MemoryStream(sPDFDecoded);
                    var image = XImage.FromStream(imageStream);
                    var maxWidth = page.Width - 100;
                    var maxHeight = page.Height - currentYPosition - 20;

                    if (image.PixelWidth > maxWidth || image.PixelHeight > maxHeight)
                    {
                        var aspectRatio = (double)image.PixelWidth / image.PixelHeight;
                        var width = maxWidth;
                        var height = maxWidth / aspectRatio;

                        if (height > maxHeight)
                        {
                            height = maxHeight;
                            width = maxHeight * aspectRatio;
                        }

                        rect = new XRect(50, currentYPosition, width, height);
                        gfx.DrawImage(image, rect);
                    }
                    else
                    {
                        rect = new XRect(50, currentYPosition, image.PixelWidth, image.PixelHeight);
                        gfx.DrawImage(image, rect);
                    }

                    currentYPosition += (int)rect.Height + 20;
                }

                var usingMultipleRows = false;
                currentXPosition = leftMargin;
                for (int k = 0; k < dt.Columns.Count; k++)
                {
                    // Draw column headers
                    var columnFormatting = columns[k];
                    var columnName = dt.Columns[k].ColumnName;
                    
                    rect = new XRect(currentXPosition, currentYPosition, columnWidth, 20);

                    gfx.DrawRectangle(XPens.LightGray, rect);
                    rect.Inflate(-cellPadding, -cellPadding);
                    tfx.DrawString(columnName, fontBold, GetBrushWithColor(), rect, XStringFormats.TopLeft);
                    currentXPosition += (int)columnWidth;
                    if (currentXPosition + columnWidth > page.Width && k != dt.Columns.Count - 1)
                    {
                        // Move to the next row
                        currentYPosition += 20;
                        currentXPosition = leftMargin;
                        usingMultipleRows = true;
                    }
                }

                currentYPosition += 20;

                // Draw table rows
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    
                    // Check if we need to add a new page
                    if (currentYPosition > pageHeight)
                    {
                        // Add a new page to the document
                        page = document.AddPage();
                        gfx = XGraphics.FromPdfPage(page);
                        tfx = new XTextFormatter(gfx);
                        currentYPosition = 20;

                        if (dt.Columns.Count > maxColumnsPerPage)
                        {
                            page.Orientation = PdfSharp.PageOrientation.Landscape;
                        }

                        currentXPosition = leftMargin;
                        for (int k = 0; k < dt.Columns.Count; k++)
                        {
                            // Draw column headers
                            var columnName = dt.Columns[k].ColumnName;
                            rect = new XRect(currentXPosition, currentYPosition, columnWidth, 20);
                            gfx.DrawRectangle(XPens.LightGray, rect);
                            tfx.DrawString(columnName, fontBold, GetBrushWithColor(), rect, XStringFormats.TopLeft);
                            currentXPosition += (int)columnWidth;
                            if (currentXPosition + columnWidth > page.Width && k != dt.Columns.Count - 1)
                            {
                                // Move to the next row
                                currentYPosition += 20;
                                currentXPosition = leftMargin;
                            }

                        }

                        currentYPosition += 20;
                    }

                    var maxLines = 1;
                    currentXPosition = leftMargin;

                    for (int j = 0; j < dt.Columns.Count; j++)
                    {   
                        var value = dt.Rows[i][j].ToString();
                        var dc = dt.Columns[j];
                        var formatColumn = GetColumnFormatting(dc, columns, ref value);

                        var lines = WrapText(gfx, value, rect, fontNormal, XStringFormats.Center);
                        maxLines = Math.Max(maxLines, lines.Count);
                        
                        rect = new XRect(currentXPosition, currentYPosition, columnWidth, 20 * maxLines);
                        gfx.DrawRectangle(XPens.WhiteSmoke, rect);

                        var horizontalAlignment = XStringFormat.Center;
                        if (formatColumn != null)
                            horizontalAlignment = formatColumn.fieldAlign == "Right" || (formatColumn.isNumeric && (formatColumn.fieldAlign == "Auto" || string.IsNullOrEmpty(formatColumn.fieldAlign))) ? XStringFormats.CenterRight : formatColumn.fieldAlign == "Center" ? XStringFormats.Center : XStringFormats.CenterLeft;

                        var yPosition = currentYPosition + 1;
                        foreach (string l in lines)
                        {
                            XRect lineRect = new XRect(rect.Left, yPosition, rect.Width, fontNormal.Height);
                            lineRect.Inflate(-cellPadding, -cellPadding);
                            gfx.DrawString(l, fontNormal, XBrushes.Black, lineRect, horizontalAlignment);

                            yPosition += fontNormal.Height;
                        }

                        currentXPosition += (int)columnWidth;
                        if (currentXPosition + columnWidth > page.Width && j != dt.Columns.Count - 1)
                        {
                            // Move to the next row
                            currentYPosition += (20 * maxLines);
                            currentXPosition = leftMargin;
                        }
                    }

                    currentYPosition += (20 * maxLines);
                    if (usingMultipleRows) // add extra spacing
                    {
                        currentYPosition += 10;
                    }
                }

                if (includeSubtotal)
                {
                    // Draw subtotals
                    currentYPosition += 10;
                    currentXPosition = leftMargin;

                    for (int j = 0; j < dt.Columns.Count; j++)
                    {
                        var value = subTotals[j].ToString();
                        var dc = dt.Columns[j];
                        var formatColumn = GetColumnFormatting(dc, columns, ref value);

                        rect = new XRect(currentXPosition, currentYPosition, columnWidth, 20);
                        gfx.DrawRectangle(XPens.LightGray, rect);

                        if (formatColumn.isNumeric && !(formatColumn?.dontSubTotal ?? false))
                        {
                            gfx.DrawString(value, fontNormal, XBrushes.Black, rect, XStringFormats.CenterRight);
                        }
                        else
                        {
                            gfx.DrawString(" ", fontNormal, XBrushes.Black, rect, XStringFormats.Center);
                        }

                        currentXPosition += (int)columnWidth;
                        if (currentXPosition + columnWidth > page.Width && j != dt.Columns.Count - 1)
                        {
                            // Move to the next row
                            currentYPosition += 20;
                            currentXPosition = leftMargin;
                        }
                    }
                }

                gfx.Save();
                document.Save(ms);
                return ms.ToArray();
            }
        }

        public static async Task<byte[]> GetPdfFile(string printUrl, int reportId, string reportSql, string connectKey, string reportName,
                    string userId = null, string clientId = null, string currentUserRole = null, string dataFilters = "", bool expandAll = false)
        {
            var installPath = AppContext.BaseDirectory + $"{(AppContext.BaseDirectory.EndsWith("\\") ? "" : "\\")}App_Data\\local-chromium";
            await new BrowserFetcher(new BrowserFetcherOptions { Path = installPath }).DownloadAsync(BrowserFetcher.DefaultChromiumRevision);
            var executablePath = "";
            foreach(var d in Directory.GetDirectories(installPath))
            {
                executablePath = $"{d}\\chrome-win\\chrome.exe";
                if (File.Exists(executablePath)) break;
            }

            var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true, ExecutablePath = executablePath });
            var page = await browser.NewPageAsync();
            await page.SetRequestInterceptionAsync(true);

            var formPosted = false;
            var formData = new StringBuilder();
            formData.AppendLine("<html><body>");
            formData.AppendLine($"<form action=\"{printUrl}\" method=\"post\">");
            formData.AppendLine($"<input name=\"reportSql\" value=\"{HttpUtility.HtmlEncode(reportSql)}\" />");
            formData.AppendLine($"<input name=\"connectKey\" value=\"{HttpUtility.HtmlEncode(connectKey)}\" />");
            formData.AppendLine($"<input name=\"reportId\" value=\"{reportId}\" />");
            formData.AppendLine($"<input name=\"pageNumber\" value=\"{1}\" />");
            formData.AppendLine($"<input name=\"pageSize\" value=\"{99999}\" />");
            formData.AppendLine($"<input name=\"userId\" value=\"{userId}\" />");
            formData.AppendLine($"<input name=\"clientId\" value=\"{clientId}\" />");
            formData.AppendLine($"<input name=\"currentUserRole\" value=\"{currentUserRole}\" />");
            formData.AppendLine($"<input name=\"expandAll\" value=\"{expandAll}\" />");
            formData.AppendLine($"<input name=\"dataFilters\" value=\"{HttpUtility.HtmlEncode(dataFilters)}\" />");
            formData.AppendLine($"</form>");
            formData.AppendLine("<script type=\"text/javascript\">document.getElementsByTagName('form')[0].submit();</script>");
            formData.AppendLine("</body></html>");

            page.Request += async (sender, e) =>
            {
                if (formPosted)
                {
                    await e.Request.ContinueAsync();
                    return;
                }

                await e.Request.RespondAsync(new ResponseData
                {
                    Status = System.Net.HttpStatusCode.OK,
                    Body = formData.ToString()
                });

                formPosted = true;
            };

            await page.GoToAsync(printUrl, new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Networkidle0 }
            });

            await page.WaitForSelectorAsync(".report-inner", new WaitForSelectorOptions { Visible = true });

            int height = await page.EvaluateExpressionAsync<int>("document.body.offsetHeight");
            int width = await page.EvaluateExpressionAsync<int>("$('table').width()");
            var pdfFile = Path.Combine(AppContext.BaseDirectory, $"App_Data\\{reportName}.pdf");

            var pdfOptions = new PdfOptions
            {
                PreferCSSPageSize = false,
                MarginOptions = new MarginOptions() { Top = "0.75in", Bottom = "0.75in", Left = "0.1in", Right = "0.1in" }
            };

            if (width < 900)
            {
                pdfOptions.Format = PaperFormat.Letter;
                pdfOptions.Landscape = false;
            }
            else
            {
                await page.SetViewportAsync(new ViewPortOptions { Width = width });
                await page.AddStyleTagAsync(new AddTagOptions { Content = "@page {size: landscape }" });
                pdfOptions.Width = $"{width}px";
            }

            await page.PdfAsync(pdfFile, pdfOptions);
            return File.ReadAllBytes(pdfFile);
        }

        private static byte[] Combine(byte[] a, byte[] b)
        {
            byte[] c = new byte[a.Length + b.Length];
            System.Buffer.BlockCopy(a, 0, c, 0, a.Length);
            System.Buffer.BlockCopy(b, 0, c, a.Length, b.Length);
            return c;
        }
        public static string GetXmlFile(string reportSql, string connectKey, string reportName)
        {
            var sql = Decrypt(reportSql);

            // Execute sql
            var dt = new DataTable();
            var ds = new DataSet();
            using (var conn = new OleDbConnection(GetConnectionString(connectKey)))
            {
                conn.Open();
                var command = new OleDbCommand(sql, conn);
                var adapter = new OleDbDataAdapter(command);

                adapter.Fill(dt);
            }

            ds.Tables.Add(dt);
            ds.DataSetName = "data";
            foreach (DataColumn c in dt.Columns)
            {
                c.ColumnName = c.ColumnName.Replace(" ", "_").Replace("(", "").Replace(")", "");
            }
            dt.TableName = "item";
            var xml = ds.GetXml();
            return xml;
        }

        /// <summary>
        /// Method to Deycrypt encrypted sql statement. PLESE DO NOT CHANGE THIS METHOD
        /// </summary>
        public static string Decrypt(string encryptedText)
        {
            encryptedText = encryptedText.Split(new string[] { "%2C" }, StringSplitOptions.RemoveEmptyEntries)[0];
            byte[] initVectorBytes = Encoding.ASCII.GetBytes("yk0z8f39lgpu70gi"); // PLESE DO NOT CHANGE THIS KEY
            int keysize = 256;

            byte[] cipherTextBytes = Convert.FromBase64String(encryptedText.Replace("%3D", "="));
            var passPhrase = Startup.StaticConfig.GetValue<string>("dotNetReport:privateApiToken").ToLower();
            using (PasswordDeriveBytes password = new PasswordDeriveBytes(passPhrase, null))
            {
                byte[] keyBytes = password.GetBytes(keysize / 8);
                using (RijndaelManaged symmetricKey = new RijndaelManaged())
                {
                    symmetricKey.Mode = CipherMode.CBC;
                    using (ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, initVectorBytes))
                    {
                        using (MemoryStream memoryStream = new MemoryStream(cipherTextBytes))
                        {
                            using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                            {
                                byte[] plainTextBytes = new byte[cipherTextBytes.Length];
                                int decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                                var decryptedString = Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
                                while (decryptedByteCount > 0)
                                {
                                    decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                                    decryptedString += Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
                                }

                                return decryptedString;
                            }
                        }
                    }
                }
            }
        }
        public static byte[] GetCSVFile(string reportSql, string connectKey, List<ReportHeaderColumn> columns = null, bool includeSubtotal = false)
        {
            var sql = Decrypt(reportSql);

            // Execute sql
            var dt = new DataTable();
            using (var conn = new OleDbConnection(GetConnectionString(connectKey)))
            {
                conn.Open();
                var command = new OleDbCommand(sql, conn);
                var adapter = new OleDbDataAdapter(command);

                adapter.Fill(dt);
                var subTotals = new decimal[dt.Columns.Count];

                //Build the CSV file data as a Comma separated string.
                string csv = string.Empty;
                foreach (DataColumn column in dt.Columns)
                {
                    //Add the Header row for CSV file.
                    csv += column.ColumnName + ',';
                }

                //Add new line.
                csv += "\r\n";

                foreach (DataRow row in dt.Rows)
                {
                    var i = 0;
                    foreach (DataColumn column in dt.Columns)
                    {
                        var value = row[column.ColumnName].ToString();
                        var formatColumn = GetColumnFormatting(column, columns, ref value);

                        if (includeSubtotal)
                        {
                            if (formatColumn.isNumeric && !(formatColumn?.dontSubTotal ?? false))
                            {
                                subTotals[i] += Convert.ToDecimal(row[column.ColumnName]);
                            }
                        }

                        //Add the Data rows.
                        csv += $"{(i == 0 ? "" : ",")}\"{value}\"";
                        i++;
                    }

                    //Add new line.
                    csv += "\r\n";
                }

                if (includeSubtotal)
                {
                    for (int j = 0; j < dt.Columns.Count; j++)
                    {
                        var value = subTotals[j].ToString();
                        var dc = dt.Columns[j];
                        var formatColumn = GetColumnFormatting(dc, columns, ref value);
                        if (formatColumn.isNumeric && !(formatColumn?.dontSubTotal ?? false))
                        {
                            csv += $"{(j == 0 ? "" : ",")}\"{value}\"";
                        }
                        else
                        {
                            csv += $"{(j == 0 ? "" : ",")}\"\"";
                        }
                    }

                    csv += "\r\n";
                }

                return Encoding.ASCII.GetBytes(csv);
                //return csv;
            }
        }
    }
}
