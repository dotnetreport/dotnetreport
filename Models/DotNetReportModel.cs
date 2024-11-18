using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout;
using PdfSharp.Pdf;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Net;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Reflection;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Net.Http;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using System.Data.SqlClient;
using static ReportBuilder.Web.Controllers.DotNetReportApiController;
using ReportBuilder.Web.Models;
using PdfSharp.Pdf.IO;

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
        public string ReportData { get; set; }
    }

    public class DotNetReportPrintModel : DotNetReportModel
    {
        public string ClientId { get; set; }
        public string UserId { get; set; }
        public string CurrentUserRoles { get; set; }
        public string DataFilters { get; set; }
        public string ReportData { get; set; }
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
        public bool IsPivotField { get; set; }
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
        public List<ParameterViewModel> Parameters { get; set; } = new List<ParameterViewModel>();
        public List<string> AllowedRoles { get; set; } = new List<string>();
        public bool? DoNotDisplay { get; set; } = false;

        public bool CustomTable { get; set; }
        public string CustomTableSql { get; set; }
        public List<CategoryViewModel> Categories { get; set; } = new List<CategoryViewModel>();
    }
    public class CategoryViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
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


    public class CustomFunctionModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Description { get; set; } = "";
        public int DataConnectionId { get; set; }
        public int DisplayOrder { get; set; }

        public string FunctionType { get; set; } = "";
        public string References { get; set; } = "";
        public string ResultDataType { get; set; } = "";
        public string Code { get; set; } = "";
        public List<CustomFunctionParameterModel> Parameters { get; set; } = new List<CustomFunctionParameterModel>();
        public List<string> AllowedRoles { get; set; } = new List<string>();
    }

    public class CustomFunctionParameterModel
    {
        public int Id { get; set; }
        public int CustomFunctionId { get; set; }
        public string ParameterName { get; set; } = "";
        public string DisplayName { get; set; } = "";

        public string Description { get; set; } = "";

        public bool Required { get; set; }
        public string DefaultValue { get; set; } = "";
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

    public enum DbTypes
    {
        MS_SQL,
        MySql,
        Postgre_Sql
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
        public string ForeignJoin { get; set; } = "Inner";
        public string ForeignKeyField { get; set; }
        public string ForeignValueField { get; set; }
        public bool DoNotDisplay { get; set; } = false;
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
        public bool ForeignFilterOnly { get; set; }
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
        public List<CustomFunctionModel> Functions { get; set; }

        public dynamic DbConfig { get; set; }
        public UserRolesConfig UserAndRolesConfig { get; set; }

    }
    public class UserRolesConfig
    {
        public bool RequireLogin { get; set; }
        public bool UsersSource { get; set; }
        public bool UserRolesSource { get; set; }
        public string SelectedUserConfig { get; set; }
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
        public string WidgetSettings { get; set; }
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
        /// If you want to use user id for filter but not for authentication, use this property
        /// </summary>
        public string UserIdForFilter { get; set; }

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
        public int? decimalPlacesDigit { get; set; }
        public string fieldAlign { get; set; }
        public string fieldFormating { get; set; }
        public bool dontSubTotal { get; set; }
        public string currencySymbol { get; set; }
        public bool isNumeric { get; set; }
        public bool isCurrency { get; set; }
        public bool isJsonColumn { get; set; }
    }
    public class ExportReportModel
    {
        public int reportId { get; set; }
        public string reportSql { get; set; }
        public string connectKey { get; set; }
        public string reportName { get; set; }
        public bool expandAll { get; set; }
        public string printUrl { get; set; }
        public string clientId { get; set; }
        public string userId { get; set; }
        public string userRoles { get; set; }
        public string dataFilters { get; set; }
        public string expandSqls { get; set; }
        public string pivotColumn { get; set; }
        public string pivotFunction { get; set; }
        public string chartData { get; set; }
        public string columnDetails { get; set; }
        public bool includeSubTotal { get; set; }
        public bool pivot { get; set; }
    }
    public interface IDnrDataConnection
    {
        string DbConnection { get; set; }
        string ConnectKey { get; set; }

        DataTable ExecuteSql(string sql);
    }

    public class MsSqlDnrDataConnection : IDnrDataConnection
    {
        public string DbConnection { get; set; }
        public string ConnectKey { get; set; }

        public MsSqlDnrDataConnection(string connectKey)
        {
            //DbConnection = dbConnection;
            ConnectKey = connectKey;
        }

        public DataTable ExecuteSql(string sql)
        {
            var dt = new DataTable();
            using (var conn = new SqlConnection(DbConnection))
            {
                conn.Open();
                var command = new SqlCommand(sql, conn);
                var adapter = new SqlDataAdapter(command);

                adapter.Fill(dt);
                conn.Close();
            }

            return dt;
        }
    }


    public static class DotNetReportHelper
    {
        private readonly static string _configFileName = "appsettings.dotnetreport.json";

        public static string GetConnectionString(string key, bool addOledbProvider = false)
        {
            var connString = ConfigurationManager.ConnectionStrings[key].ConnectionString;
            if (connString == null)
            {
                return "";
            }

            connString = connString.Replace("Trusted_Connection=True", "");

            if (!connString.ToLower().StartsWith("provider") && addOledbProvider)
            {
                connString = "Provider=sqloledb;" + connString;
            }

            return connString;
        }

        public static ConnectViewModel GetConnection(string databaseApiKey = "")
        {
            return new ConnectViewModel
            {
                ApiUrl = ConfigurationManager.AppSettings["dotNetReport.apiUrl"],
                AccountApiKey = ConfigurationManager.AppSettings["dotNetReport.accountApiToken"], // Your Account Api Token from your http://dotnetreport.com Account
                DatabaseApiKey = string.IsNullOrEmpty(databaseApiKey) ? ConfigurationManager.AppSettings["dotNetReport.dataconnectApiToken"] : databaseApiKey
            };
        }

        public static async Task<string> GetConnectionString(ConnectViewModel connect, bool addOledbProvider = true)
        {
            if (connect.AccountApiKey == "Your Account API Key" || string.IsNullOrEmpty(connect.AccountApiKey) || string.IsNullOrEmpty(connect.DatabaseApiKey))
                return "";

            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(String.Format("{0}/ReportApi/GetDataConnectKey?account={1}&dataConnect={2}", connect.ApiUrl, connect.AccountApiKey, connect.DatabaseApiKey));

                if (!response.IsSuccessStatusCode)
                {
                    return "";
                }

                var content = await response.Content.ReadAsStringAsync();
                return DotNetReportHelper.GetConnectionString(content.Replace("\"", ""), addOledbProvider);
            }

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

        private static void FormatExcelSheet(DataTable dt, ExcelWorksheet ws, int rowstart, int colstart, List<ReportHeaderColumn> columns = null, bool includeSubtotal = false, bool loadHeader = true, string chartData = null)
        {
            ws.Cells[rowstart, colstart].LoadFromDataTable(dt, loadHeader);
            if (loadHeader) ws.Cells[rowstart, colstart, rowstart, colstart + dt.Columns.Count - 1].Style.Font.Bold = true;
            if (!string.IsNullOrEmpty(chartData) && chartData != "undefined")
            {
                byte[] imageBytes = Convert.FromBase64String(chartData.Substring(chartData.LastIndexOf(',') + 1));
                using (MemoryStream ms = new MemoryStream(imageBytes))
                {
                    Image image = Image.FromStream(ms);
                    // Add the image to the worksheet
                    var picture = ws.Drawings.AddPicture("ChartImage", image);
                    picture.SetPosition(1, 0, dt.Columns.Count + 1, 0); // Set the position of the image
                    picture.SetSize(400, 300); // Set the size of the image in pixels (width, height)
                }
            }
            int i = colstart; var isNumeric = false;
            foreach (DataColumn dc in dt.Columns)
            {
                var formatColumn = columns?.FirstOrDefault(x => dc.ColumnName.StartsWith(x.fieldName)) ?? new ReportHeaderColumn();
                string decimalFormat = new string('0', formatColumn.decimalPlacesDigit.GetValueOrDefault());
                isNumeric = dc.DataType.Name.StartsWith("Int") || dc.DataType.Name == "Double" || dc.DataType.Name == "Decimal";
                if (!string.IsNullOrEmpty(formatColumn.fieldLabel))
                {
                    ws.Cells[rowstart, i].Value = formatColumn.fieldLabel;
                }
                if (dc.DataType == typeof(decimal) || (formatColumn != null && formatColumn.fieldFormating == "Decimal"))
                {
                    if (formatColumn != null && formatColumn.decimalPlacesDigit != null)
                    {
                        ws.Column(i).Style.Numberformat.Format = "###,###,##0." + decimalFormat;
                    }
                    else
                    {
                        ws.Column(i).Style.Numberformat.Format = "###,###,##0.00";
                    }
                    isNumeric = true;
                }
                if (dc.DataType == typeof(DateTime))
                    ws.Column(i).Style.Numberformat.Format = "mm/dd/yyyy";

                if (formatColumn != null && formatColumn.fieldFormating == "Currency")
                {
                    if (formatColumn.currencySymbol != null && formatColumn.decimalPlacesDigit != null)
                    {
                        ws.Column(i).Style.Numberformat.Format = formatColumn.currencySymbol + "###,###,##0." + decimalFormat;
                    }
                    else if (formatColumn.currencySymbol != null)
                    {
                        ws.Column(i).Style.Numberformat.Format = formatColumn.currencySymbol + "###,###,##0.00";
                    }
                    else
                    {
                        ws.Column(i).Style.Numberformat.Format = "$###,###,##0.00";
                    }
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

        public static async Task<List<CustomFunctionModel>> GetApiFunctions()
        {
            var settings = new DotNetReportSettings
            {
                ApiUrl = ConfigurationManager.AppSettings["dotNetReport.apiUrl"],
                AccountApiToken = ConfigurationManager.AppSettings["dotNetReport.accountApiToken"], // Your Account Api Token from your http://dotnetreport.com Account
                DataConnectApiToken = ConfigurationManager.AppSettings["dotNetReport.dataconnectApiToken"] // Your Data Connect Api Token from your http://dotnetreport.com Account
            };

            return await GetApiFunctions(settings.AccountApiToken, settings.DataConnectApiToken);
        }

        public static async Task<List<CustomFunctionModel>> GetApiFunctions(string accountKey, string dataConnectKey)
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(String.Format("{0}/ReportApi/GetCustomFunctions?account={1}&dataConnect={2}&clientId=", ConfigurationManager.AppSettings["dotNetReport.apiUrl"], accountKey, dataConnectKey));
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var functions = JsonConvert.DeserializeObject<List<CustomFunctionModel>>(content);

                return functions;
            }
        }

        public static async Task<string> RunReportApiCall(string postData)
        {
            using (var client = new HttpClient())
            {
                var settings = new DotNetReportSettings
                {
                    ApiUrl = ConfigurationManager.AppSettings["dotNetReport.apiUrl"],
                    AccountApiToken = ConfigurationManager.AppSettings["dotNetReport.accountApiToken"], // Your Account Api Token from your http://dotnetreport.com Account
                    DataConnectApiToken = ConfigurationManager.AppSettings["dotNetReport.dataconnectApiToken"] // Your Data Connect Api Token from your http://dotnetreport.com Account
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
                    var sqlQuery = JsonConvert.DeserializeObject<Dictionary<string, object>>(stringContent);
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
            if (sql.StartsWith("EXEC")) return new List<string>();
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

            if (!sqlFields.Any())
            {
                foreach (DataColumn c in dt.Columns) { sqlFields.Add($"{c.ColumnName} AS {c.ColumnName}"); }
            }

            int i = 0;
            foreach (DataColumn col in dt.Columns)
            {
                var sqlField = sqlFields[i++];
                if (col.ColumnName.Contains("__prm__")) continue;
                model.Columns.Add(new DotNetReportDataColumnModel
                {
                    SqlField = sqlField.Contains(" FROM ") ? col.ColumnName : sqlField.Substring(0, sqlField.LastIndexOf(" AS ")).Trim().Replace("__jsonc__", ""),
                    ColumnName = col.ColumnName,
                    DataType = col.DataType.ToString(),
                    IsNumeric = IsNumericType(col.DataType),
                    FormatType = sqlField.Contains("__jsonc__") ? "Json" : (sqlField.Contains(" FROM ") ? "Csv" : ""),
                    IsPivotField = sqlField.Contains("__ AS") ? true : false
                });

                col.ColumnName = col.ColumnName.Replace("__jsonc__", "");
            }

            foreach (DataRow row in dt.Rows)
            {
                i = 0;
                var items = new List<DotNetReportDataRowItemModel>();

                foreach (DataColumn col in dt.Columns)
                {
                    if (!col.ColumnName.Contains("__prm__"))
                    {
                        var item = new DotNetReportDataRowItemModel
                        {
                            Column = model.Columns[i],
                            Value = row[col] != null ? row[col].ToString() : null,
                            FormattedValue = GetFormattedValue(col, row, model.Columns[i].FormatType, jsonAsTable),
                            LabelValue = GetLabelValue(col, row)
                        };

                        items.Add(item);

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

        private static string GetWhereClause(string sql)
        {

            int whereIndex = sql.LastIndexOf("WHERE ", StringComparison.OrdinalIgnoreCase);
            // If there is no WHERE clause, return an empty string
            if (whereIndex < 0)
            {
                return "";
            }
            int nextClauseIndex = sql.IndexOf("GROUP BY ", whereIndex, StringComparison.OrdinalIgnoreCase);
            if (nextClauseIndex < 0)
            {
                nextClauseIndex = sql.IndexOf("ORDER BY ", whereIndex, StringComparison.OrdinalIgnoreCase);
            }
            nextClauseIndex = nextClauseIndex < 0 ? sql.Length : nextClauseIndex;
            var modifiedSql = sql.Substring(0, whereIndex) + sql.Substring(nextClauseIndex);
            var whereClause = sql.Substring(whereIndex, nextClauseIndex - whereIndex);

            if (whereClause.Contains(" ON ") && whereClause.Contains("\""))
            {
                whereClause = "";
            }

            return whereClause;
        }

        private static string ReplaceWhereClause(string sql, string where)
        {
            int whereIndex = sql.IndexOf("WHERE ", StringComparison.OrdinalIgnoreCase);
            int nextClauseIndex = sql.IndexOf("ORDER BY ", whereIndex, StringComparison.OrdinalIgnoreCase);
            nextClauseIndex = nextClauseIndex < 0 ? sql.Length : nextClauseIndex;
            var modifiedSql = sql.Substring(0, whereIndex) + where + sql.Substring(nextClauseIndex);
            return modifiedSql;
        }

        public async static Task<DataSet> GetDrillDownData(OleDbConnection conn, DataTable dt, List<string> sqlFields, string reportDataJson)
        {
            var drilldownRow = new List<string>();
            var dr = dt.Rows[0];
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
                                            ""SqlField"":""{col.Substring(0, col.LastIndexOf(" AS "))}"",
                                            ""ColumnName"":""{dc.ColumnName}"",
                                            ""DataType"":""{dc.DataType.ToString()}"",
                                            ""IsNumeric"":{(dc.DataType.Name.StartsWith("Int") || dc.DataType.Name == "Double" || dc.DataType.Name == "Decimal" ? "true" : "false")},
                                            ""FormatType"":""""
                                        }}
                                     }}
                                ");
            }

            var reportData = reportDataJson.Replace("\"DrillDownRow\":[]", $"\"DrillDownRow\": [{string.Join(",", drilldownRow)}]").Replace("\"IsAggregateReport\":true", "\"IsAggregateReport\":false");
            var drilldownSql = await RunReportApiCall(reportData);

            var dts = new DataSet();
            var combinedSqls = "";
            if (!string.IsNullOrEmpty(drilldownSql))
            {
                foreach (DataRow ddr in dt.Rows)
                {
                    i = 0;
                    var filteredSql = drilldownSql;
                    foreach (DataColumn dc in dt.Columns)
                    {
                        var value = ddr[dc].ToString().Replace("'", "''");
                        filteredSql = filteredSql.Replace($"<{dc.ColumnName}>", value);
                    }

                    combinedSqls += filteredSql += ";\n";
                }

                using (var cmd = new OleDbCommand(combinedSqls, conn))
                using (var adp = new OleDbDataAdapter(cmd))
                {
                    adp.Fill(dts);
                }
            }

            return dts;
        }

        public async static Task<(DataTable dt, string sql, int totalRecords)> GetPivotTable(IDatabaseConnection databaseConnection, string connectionString, DataTable dt, string sql, List<string> sqlFields, string reportDataJson, string pivotColumn, string pivotFunction, int pageNumber, int pageSize, string sortBy, bool desc, bool returnSubtotal = false)
        {
            var pivotColumnOrder = GetPivotColumnOrder(reportDataJson);
            var dts = new DataTable();
            var drilldownRow = new List<string>();
            if (dt.Rows.Count == 0)
                return (dts, "", 0);

            var dr = dt.Rows[0];
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
                            ""SqlField"":""{col.Substring(0, col.LastIndexOf(" AS "))}"",
                            ""ColumnName"":""{dc.ColumnName}"",
                            ""DataType"":""{dc.DataType.ToString()}"",
                            ""IsNumeric"":{(dc.DataType.Name.StartsWith("Int") || dc.DataType.Name == "Double" || dc.DataType.Name == "Decimal" ? "true" : "false")},
                            ""FormatType"":""""
                        }}
                    }}
                ");
            }

            var reportData = reportDataJson.Replace("\"DrillDownRow\":[]", $"\"DrillDownRow\": [{string.Join(",", drilldownRow)}]").Replace("\"IsAggregateReport\":true", "\"IsAggregateReport\":false");
            var drilldownSql = await RunReportApiCall(reportData);

            if (!string.IsNullOrEmpty(drilldownSql))
            {
                var lastWhereIndex = drilldownSql.LastIndexOf("WHERE");
                var baseQuery = drilldownSql.Substring(0, lastWhereIndex) + " " + GetWhereClause(sql);
                var monthNames = new List<string>
                {
                    "january", "february", "march", "april", "may", "june",
                    "july", "august", "september", "october", "november", "december"
                };
                var baseDataTable = databaseConnection.ExecuteQuery(connectionString, baseQuery.Replace("SELECT ", "SELECT "));
                var distinctValues = baseDataTable
                    .AsEnumerable()
                    .Select(row => "[" + Convert.ToString(row[pivotColumn])?.Trim() + "]")
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Where(x=>x != "[]" && x.Length <=128)
                    .OrderBy(x =>
                    {
                        string lowerTrimmedValue = x.Trim('[', ']').ToLower();
                        int monthIndex = monthNames.IndexOf(lowerTrimmedValue);
                        if (monthIndex >= 0)
                        {
                            return monthIndex;
                        }
                        return int.MaxValue; 
                    })
                    .ThenBy(x => x) 
                    .ToList();
                distinctValues = (pivotColumnOrder.Count == distinctValues.Count && !pivotColumnOrder.Except(distinctValues).Any()) ? pivotColumnOrder : distinctValues;

                int pivotColumnIndex = baseDataTable.Columns[pivotColumn].Ordinal;
                string nextColumnName = baseDataTable.Columns[pivotColumnIndex + 1].ColumnName;
                var validFunctions = new[] { "Sum", "Count", "Avg" };
                pivotFunction = validFunctions.Contains(pivotFunction) ? pivotFunction : "Max";

                if (returnSubtotal)
                {
                    var sqlQryforCount = $@"
                        SELECT 
                            {string.Join(", ", distinctValues.Select(v => $"SUM(COALESCE({v}, 0)) AS {v}"))}
                        FROM (
                            {baseQuery}
                        ) src
                        PIVOT (
                            COUNT([{nextColumnName}]) 
                            FOR [{pivotColumn}] IN ({string.Join(", ", distinctValues)})
                        ) AS pvt;";

                    var countdata = databaseConnection.ExecuteQuery(connectionString, sqlQryforCount);
                    return (countdata, sqlQryforCount, 1);
                }
                else
                {
                    var sqlQry = $@"
                        SELECT * FROM (
                            {baseQuery}
                        ) src
                        PIVOT (
                            {pivotFunction} ([{nextColumnName}])
                            FOR [{pivotColumn}] IN ({string.Join(", ", distinctValues)})
                        ) AS pvt
                        ";

                    var sqlCount = $"SELECT COUNT(*) FROM ({sqlQry}) as countQry";
                    var totalRecords = databaseConnection.GetTotalRecords(connectionString, sqlCount, sql);

                    sqlQry = sqlQry + "\r\n" +
                        $" ORDER BY {(string.IsNullOrEmpty(sortBy) ? "1" : "1" /*sortBy*/) + (desc ? " DESC" : "")} \r\n" +
                        $" OFFSET {(pageNumber - 1) * pageSize} ROWS FETCH NEXT {pageSize} ROWS ONLY";
                    dts = databaseConnection.ExecuteQuery(connectionString, sqlQry);
                    return (dts, sqlQry, totalRecords);
                }
            }

            return (dts, "", 0);
        }

        public async static Task<DataSet> GetDrillDownDataAlternate(IDatabaseConnection databaseConnection, string connectionString, DataTable dt, List<string> sqlFields, string reportDataJson)
        {
            var drilldownRow = new StringBuilder();
            var dr = dt.Rows[0];
            int i = 0;

            foreach (DataColumn dc in dt.Columns)
            {
                var col = sqlFields[i++];
                var formattedCol = col.Substring(0, col.LastIndexOf(" AS "));

                if (drilldownRow.Length > 0) drilldownRow.Append(',');

                drilldownRow.Append($@"
                            {{
                                ""Value"":""{dr[dc]}"",
                                ""FormattedValue"":""{dr[dc]}"",
                                ""LabelValue"":""'{dr[dc]}'"",
                                ""NumericValue"":null,
                                ""Column"":{{
                                    ""SqlField"":""{formattedCol}"",
                                    ""ColumnName"":""{dc.ColumnName}"",
                                    ""DataType"":""{dc.DataType}"",
                                    ""IsNumeric"":{(dc.DataType.Name.StartsWith("Int") || dc.DataType.Name == "Double" || dc.DataType.Name == "Decimal" ? "true" : "false")},
                                    ""FormatType"":""""
                                }}
                             }}
                        ");
            }

            var reportData = reportDataJson
                .Replace("\"DrillDownRow\":[]", $"\"DrillDownRow\": [{drilldownRow.ToString()}]")
                .Replace("\"IsAggregateReport\":true", "\"IsAggregateReport\":false");

            var drilldownSql = await RunReportApiCall(reportData);
            var combinedSqls = new StringBuilder();

            if (!string.IsNullOrEmpty(drilldownSql))
            {
                foreach (DataRow ddr in dt.Rows)
                {
                    i = 0;
                    var filteredSql = drilldownSql;
                    foreach (DataColumn dc in dt.Columns)
                    {
                        var value = ddr[dc].ToString().Replace("'", "''");
                        filteredSql = filteredSql.Replace($"<{dc.ColumnName}>", value);
                    }

                    combinedSqls.AppendLine(filteredSql + ";");
                }

                return databaseConnection.ExecuteDataSetQuery(connectionString, combinedSqls.ToString());
            }

            return new DataSet();
        }

        public async static Task<DataSet> GetDrillDownData(IDatabaseConnection databaseConnection, string connectionString, DataTable dt, List<string> sqlFields, string reportDataJson, List<KeyValuePair<string, string>> parameters = null)
        {
            var drilldownRow = new List<string>();
            var dr = dt.Rows[0];
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
                                            ""SqlField"":""{col.Substring(0, col.LastIndexOf(" AS "))}"",
                                            ""ColumnName"":""{dc.ColumnName}"",
                                            ""DataType"":""{dc.DataType.ToString()}"",
                                            ""IsNumeric"":{(dc.DataType.Name.StartsWith("Int") || dc.DataType.Name == "Double" || dc.DataType.Name == "Decimal" ? "true" : "false")},
                                            ""FormatType"":""""
                                        }}
                                     }}
                                ");
            }

            var reportData = reportDataJson.Replace("\"DrillDownRow\":[]", $"\"DrillDownRow\": [{string.Join(",", drilldownRow)}]").Replace("\"IsAggregateReport\":true", "\"IsAggregateReport\":false");
            var drilldownSql = await RunReportApiCall(reportData);

            var dts = new DataSet();
            var combinedSqls = "";
            if (!string.IsNullOrEmpty(drilldownSql))
            {
                foreach (DataRow ddr in dt.Rows)
                {
                    i = 0;
                    var filteredSql = drilldownSql;
                    foreach (DataColumn dc in dt.Columns)
                    {
                        var value = ddr[dc].ToString().Replace("'", "''");
                        filteredSql = filteredSql.Replace($"<{dc.ColumnName}>", value);
                    }

                    combinedSqls += filteredSql += ";\n";
                }

                dts = databaseConnection.ExecuteDataSetQuery(connectionString, combinedSqls, parameters);
            }

            return dts;
        }

        private static int CompareValues(object value1, object value2, Type dtType)
        {
            bool isInt = dtType == typeof(int) || dtType == typeof(long) || dtType == typeof(Int16) || dtType == typeof(Int32);
            bool isDecimal = dtType == typeof(decimal) || dtType == typeof(double) || dtType == typeof(float);

            if (value2 == null) { return 1; }
            if (value1 == null) { return -1; }

            if (dtType == typeof(DateTime))
            {
                DateTime date1 = Convert.ToDateTime(value1);
                DateTime date2 = Convert.ToDateTime(value2);
                return DateTime.Compare(date1, date2);
            }
            else if (isInt || isDecimal)
            {
                double dblValue1 = Convert.ToDouble(value1);
                double dblValue2 = Convert.ToDouble(value2);
                return dblValue1.CompareTo(dblValue2);
            }
            return value1.ToString().CompareTo(value2.ToString());
        }

        public static async Task<DataTable> ExecuteCustomFunction(DataTable dataTable, string sql)
        {
            if (!sql.Contains("/*|")) return dataTable;

            Regex regex = new Regex(@"/\*\|(.*?)\|\*/");
            var matches = regex.Matches(sql);

            foreach (Match match in matches)
            {
                string functionCall = match.Groups[1].Value;
                int columnIndex = -1;

                // Find the column that contains the function call to replace it later
                foreach (DataColumn column in dataTable.Columns)
                {
                    if (column.Expression.Contains(match.Value))
                    {
                        columnIndex = column.Ordinal;
                        break;
                    }
                }

                var functionCalls = new List<string>();
                foreach (DataRow row in dataTable.Rows)
                {
                    string modifiedFunctionCall = functionCall;

                    // Iterate over all columns that end with "__prm__"
                    foreach (DataColumn column in dataTable.Columns.Cast<DataColumn>().Where(c => c.ColumnName.EndsWith("__prm__")))
                    {
                        string paramName = "{" + column.ColumnName.Replace("__prm__", "") + "}";

                        if (modifiedFunctionCall.Contains(paramName))
                        {
                            string valueReplacement = row[column].ToString();
                            // Check if the datatype is numeric
                            if (column.DataType == typeof(int) || column.DataType == typeof(decimal) || column.DataType == typeof(double) || column.DataType == typeof(long))
                            {
                                modifiedFunctionCall = modifiedFunctionCall.Replace(paramName, valueReplacement);
                            }
                            else
                            {
                                modifiedFunctionCall = modifiedFunctionCall.Replace(paramName, "\"" + valueReplacement + "\"");
                            }
                        }
                    }

                    //var result = await DynamicCodeRunner.RunCode(modifiedFunctionCall + ";");
                    //if (columnIndex != -1)
                    //{
                    //    row[columnIndex] = result;
                    //}
                }
            }

            foreach (var column in dataTable.Columns.Cast<DataColumn>().Where(c => c.ColumnName.EndsWith("__prm__")).ToList())
            {
                dataTable.Columns.Remove(column);
            }
            return dataTable;
        }
        public static List<string> GetPivotColumnOrder(string reportDataJson)
        {
            var desiredOrder = new List<string>();

            if (!string.IsNullOrEmpty(reportDataJson))
            {
                JObject reportSettingObject = JObject.Parse(reportDataJson);
                var reportSettingsObject = (string)reportSettingObject["ReportSettings"];
                if (reportSettingsObject != null)
                {
                    JObject pivotColumnsObject = JObject.Parse(reportSettingsObject);
                    string pivotColumnOrder = (string)pivotColumnsObject["PivotColumns"]?.ToString();
                    if (!string.IsNullOrEmpty(pivotColumnOrder))
                    {
                        desiredOrder = pivotColumnOrder.Split(',').Select(c => c.Trim()).ToList();
                    }
                }
            }

            return desiredOrder;
        }
        static bool ContainsGroupInDetail(string jsonString)
        {
            if (string.IsNullOrEmpty(jsonString))
                return false;
            JObject jsonObject;
            try
            {
                jsonObject = JObject.Parse(jsonString);
            }
            catch
            {
                return false;
            }
            if (jsonObject["GroupFunctionList"] == null)
                return false;
            JArray groupFunctionList = jsonObject["GroupFunctionList"] as JArray;
            if (groupFunctionList == null || !groupFunctionList.Any())
                return false;
            return groupFunctionList.Any(item =>
                item?["GroupFunc"]?.ToString() == "Group in Detail"
            );
        }

        public static DataTable PushDatasetIntoDataTable(DataTable tbl, DataSet dts, string pivotColumnName, string pivotFunction, string reportDataJson = null)
        {
            var dt = tbl.Copy();

            if (!string.IsNullOrEmpty(reportDataJson))
            {
                var desiredOrder = new List<string>();
                JObject reportSettingObject = JObject.Parse(reportDataJson);
                var reportSettingsObject = (string)reportSettingObject["ReportSettings"];
                if (reportSettingsObject != null)
                {
                    JObject pivotColumnsObject = JObject.Parse(reportSettingsObject);
                    string pivotColumnOrder = (string)pivotColumnsObject["PivotColumns"]?.ToString();
                    if (!string.IsNullOrEmpty(pivotColumnOrder))
                    {
                        var pivotColumns = pivotColumnOrder.Split(',').ToList();
                        desiredOrder.AddRange(pivotColumns);
                        if (desiredOrder.Count == dts.Tables.Count)
                        {
                            // Reorder each DataTable in the DataSet
                            List<DataTable> reorderedTables = new List<DataTable>();
                            foreach (var name in desiredOrder)
                            {
                                foreach (DataTable table in dts.Tables)
                                {
                                    if (table.Rows.Count > 0 && table.Rows[0][1].ToString().Trim() == name.Trim())
                                    {
                                        reorderedTables.Add(table.Copy());
                                        break;
                                    }
                                }
                            }
                            // Clear the existing tables in the DataSet and add reordered ones
                            dts.Tables.Clear();
                            foreach (var table in reorderedTables)
                            {
                                dts.Tables.Add(table);
                            }
                        }
                    }
                }
            }

            foreach (DataRow row in dt.Rows)
            {
                int rowIndex = dt.Rows.IndexOf(row);
                DataTable dtsTable = dts.Tables[rowIndex];
                var distinctValues = new Dictionary<string, HashSet<object>>();
                var maxValues = new Dictionary<string, object>();

                if (dtsTable.Columns.Contains(pivotColumnName))
                {
                    int pivotColumnIndex = dtsTable.Columns[pivotColumnName].Ordinal;
                    int dtColumnIndex = (pivotColumnIndex + 1 < dtsTable.Columns.Count) ? pivotColumnIndex + 1 : pivotColumnIndex;

                    var dtType = dtsTable.Columns[dtColumnIndex].DataType;
                    bool isInt = dtType == typeof(int) || dtType == typeof(long) || dtType == typeof(Int16) || dtType == typeof(Int32);
                    bool isDecimal = dtType == typeof(decimal) || dtType == typeof(double) || dtType == typeof(float);
                    bool isDate = dtType == typeof(DateTime);

                    foreach (DataRow dtsRow in dtsTable.Rows)
                    {
                        string newColumnName = dtsRow[pivotColumnName].ToString();
                        if (string.IsNullOrEmpty(newColumnName)) newColumnName = "(Blank)";
                        if (!dt.Columns.Contains(newColumnName))
                        {
                            dt.Columns.Add(newColumnName, pivotFunction.StartsWith("Count") ? typeof(int) : dtType);
                        }

                        if (pivotFunction == "Count Distinct")
                        {
                            if (!distinctValues.ContainsKey(newColumnName))
                            {
                                distinctValues[newColumnName] = new HashSet<object>();
                            }
                            distinctValues[newColumnName].Add(dtsRow[dtColumnIndex]);
                        }
                        else if (pivotFunction == "Max")
                        {
                            object currentValue = dtsRow[dtColumnIndex];
                            if (!maxValues.ContainsKey(newColumnName) || CompareValues(currentValue, maxValues[newColumnName], dtType) > 0)
                            {
                                maxValues[newColumnName] = currentValue;
                            }
                        }
                        else if (pivotColumnIndex + 1 < dtsTable.Columns.Count)
                        {
                            if (pivotFunction == "Count")
                            {
                                row[newColumnName] = (string.IsNullOrEmpty(row[newColumnName].ToString()) ? 0 : Convert.ToInt32(row[newColumnName])) + 1;
                            }
                            else if (isInt)
                            {
                                row[newColumnName] = (string.IsNullOrEmpty(row[newColumnName].ToString()) ? 0 : Convert.ToInt32(row[newColumnName])) + (string.IsNullOrEmpty(dtsRow[pivotColumnIndex + 1].ToString()) ? 0 : Convert.ToInt32(dtsRow[pivotColumnIndex + 1]));

                            }
                            else if (isDecimal)
                            {
                                row[newColumnName] = (string.IsNullOrEmpty(row[newColumnName].ToString()) ? 0 : Convert.ToDecimal(row[newColumnName])) + (string.IsNullOrEmpty(dtsRow[pivotColumnIndex + 1].ToString()) ? 0 : Convert.ToDecimal(dtsRow[pivotColumnIndex + 1]));

                            }
                            else if (isDate)
                            {
                                row[newColumnName] = (string.IsNullOrEmpty(row[newColumnName].ToString()) ? "" : Convert.ToDateTime(row[newColumnName]).ToShortDateString()) + " " + (string.IsNullOrEmpty(dtsRow[pivotColumnIndex + 1].ToString()) ? "" : Convert.ToDateTime(dtsRow[pivotColumnIndex + 1]).ToShortDateString());
                            }
                            else
                            {
                                row[newColumnName] = row[newColumnName].ToString() + " " + dtsRow[pivotColumnIndex + 1].ToString();
                            }
                        }
                    }

                    if (pivotFunction == "Count Distinct")
                    {
                        foreach (var column in distinctValues)
                        {
                            row[column.Key] = column.Value.Count;
                        }
                    }
                    if (pivotFunction == "Max")
                    {
                        foreach (var column in maxValues)
                        {
                            row[column.Key] = column.Value;
                        }
                    }
                }
            }
            return dt;
        }

        private static (DataTable dt, SqlQuery qry, List<string> sqlFields) GetDataTable(string reportSql, string connectKey)
        {
            var qry = new SqlQuery();
            var sql = Decrypt(reportSql);
            if (sql.StartsWith("{\"sql\""))
            {
                qry = JsonConvert.DeserializeObject<SqlQuery>(sql);
                sql = qry.sql;
            }
            else
            {
                qry.sql = sql;
            }
            var sqlFields = SplitSqlColumns(sql);

            // Execute sql
            var connectionString = DotNetReportHelper.GetConnectionString(connectKey);
            IDatabaseConnection databaseConnection = DatabaseConnectionFactory.GetConnection(dbtype);
            var dt = databaseConnection.ExecuteQuery(connectionString, sql, qry.parameters);

            return (dt, qry, sqlFields);
        }

        public static async Task<byte[]> GetExcelFile(string reportSql, string connectKey, string reportName, string chartData = null, bool allExpanded = false,
                string expandSqls = null, List<ReportHeaderColumn> columns = null, bool includeSubtotal = false, bool pivot = false, string pivotColumn = null, string pivotFunction = null)
        {
            var connectionString = DotNetReportHelper.GetConnectionString(connectKey);
            IDatabaseConnection databaseConnection = DatabaseConnectionFactory.GetConnection(dbtype);
            var data = GetDataTable(reportSql, connectKey);

            var qry = data.qry;
            var sqlFields = data.sqlFields;
            var dt = data.dt;

            if (pivot) dt = Transpose(dt);

            if (columns?.Count > 0)
            {
                foreach (var col in columns)
                {
                    if (dt.Columns.Contains(col.fieldName) && col.hideStoredProcColumn)
                    {
                        dt.Columns.Remove(col.fieldName);
                    }
                    else if (!String.IsNullOrWhiteSpace(col.fieldLabel))
                    {
                        dt.Columns[col.fieldName].ColumnName = col.fieldLabel;
                    }
                }
            }
            if (!string.IsNullOrEmpty(pivotColumn))
            {
                var pd = await DotNetReportHelper.GetPivotTable(databaseConnection, connectionString, dt, qry.sql, sqlFields, expandSqls, pivotColumn, pivotFunction, 1, int.MaxValue, null, false);
                dt = pd.dt;
                if (!string.IsNullOrEmpty(pd.sql)) qry.sql = pd.sql;
                allExpanded = false;

                //var ds = await DotNetReportHelper.GetDrillDownData(databaseConnection, connectionString, dt, sqlFields, expandSqls);
                //dt = DotNetReportHelper.PushDatasetIntoDataTable(dt, ds, pivotColumn, pivotFunction, expandSqls);
            }
            using (ExcelPackage xp = new ExcelPackage())
            {
                ExcelWorksheet ws = xp.Workbook.Worksheets.Add(reportName);

                int rowstart = 1;
                int colstart = 1;
                int rowend = rowstart;
                int colend = dt.Columns.Count;

                if (dt.Rows.Count > 0)
                {
                    ws.Cells[rowstart, colstart, rowend, colend].Merge = true;
                    ws.Cells[rowstart, colstart, rowend, colend].Value = reportName;
                    ws.Cells[rowstart, colstart, rowend, colend].Style.Font.Bold = true;
                    ws.Cells[rowstart, colstart, rowend, colend].Style.Font.Size = 14;

                    rowstart += 2;
                    rowend = rowstart + dt.Rows.Count;

                    FormatExcelSheet(dt, ws, rowstart, colstart, columns, includeSubtotal, true, chartData);

                    if (allExpanded)
                    {
                        var insertRowIndex = 3;

                        var drilldownRow = new List<string>();
                        var dr = dt.Rows[0];

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
                                            ""SqlField"":""{col.Substring(0, col.LastIndexOf(" AS "))}"",
                                            ""ColumnName"":""{dc.ColumnName}"",
                                            ""DataType"":""{dc.DataType.ToString()}"",
                                            ""IsNumeric"":{(dc.DataType.Name.StartsWith("Int") || dc.DataType.Name == "Double" || dc.DataType.Name == "Decimal" ? "true" : "false")},
                                            ""FormatType"":""""
                                        }}
                                     }}
                                ");
                        }
                        bool isGroupInDetailExist = ContainsGroupInDetail(expandSqls);
                        var drillDownRowValue = $"\"DrillDownRow\": [{string.Join(",", drilldownRow)}]";
                        var reportData = expandSqls.Replace("\"DrillDownRow\":[]", drillDownRowValue);
                        // If Group in Detail does not exist, set IsAggregateReport to false
                        if (!isGroupInDetailExist)
                        {
                            reportData = reportData.Replace("\"IsAggregateReport\":true", "\"IsAggregateReport\":false");
                        }
                        var drilldownSql = await RunReportApiCall(reportData);
                        if (drilldownSql.StartsWith("{\"sql\""))
                        {
                            qry = JsonConvert.DeserializeObject<SqlQuery>(drilldownSql);
                            drilldownSql = qry.sql;
                        }

                        var combinedSqls = "";
                        if (!string.IsNullOrEmpty(drilldownSql))
                        {
                            foreach (DataRow ddr in dt.Rows)
                            {
                                i = 0;
                                var filteredSql = drilldownSql;
                                foreach (DataColumn dc in dt.Columns)
                                {
                                    var value = ddr[dc].ToString().Replace("'", "''");
                                    filteredSql = filteredSql.Replace($"<{dc.ColumnName}>", value);
                                }

                                combinedSqls += filteredSql += ";\n";
                            }

                            var dts = databaseConnection.ExecuteDataSetQuery(connectionString, combinedSqls, qry.parameters);

                            foreach (DataTable ddt in dts.Tables)
                            {
                                ws.InsertRow(insertRowIndex + 2, ddt.Rows.Count);

                                FormatExcelSheet(ddt, ws, insertRowIndex == 3 ? 3 : (insertRowIndex + 1), ddt.Columns.Count + 1, columns, false, insertRowIndex == 3);

                                insertRowIndex += ddt.Rows.Count + 1;
                            }
                        }
                    }
                }
                ws.View.FreezePanes(4, 1);
                return xp.GetAsByteArray();
            }
        }

        public static ReportHeaderColumn GetColumnFormatting(DataColumn dc, List<ReportHeaderColumn> columns, ref string value)
        {
            var isCurrency = false;
            var isNumeric = dc.DataType.Name.StartsWith("Int") || dc.DataType.Name == "Double" || dc.DataType.Name == "Decimal";
            var formatColumn = columns?.FirstOrDefault(x => dc.ColumnName.StartsWith(x.fieldName)) ?? new ReportHeaderColumn();
            string decimalFormat = new string('0', formatColumn.decimalPlacesDigit.GetValueOrDefault());
            try
            {
                if (dc.DataType == typeof(decimal) || (formatColumn != null && (formatColumn.fieldFormating == "Decimal" || formatColumn.fieldFormating == "Double")))
                {
                    if (formatColumn.decimalPlacesDigit != null)
                    {
                        value = Convert.ToDecimal(value).ToString("###,###,##0." + decimalFormat);
                    }
                    else
                    {
                        value = Convert.ToDecimal(value).ToString("###,###,##0.00");
                    }
                    isNumeric = true;
                }
                if (formatColumn != null && formatColumn.fieldFormating == "Currency")
                {
                    if (formatColumn.currencySymbol != null && formatColumn.decimalPlacesDigit != null)
                    {
                        value = Convert.ToDecimal(value).ToString(formatColumn.currencySymbol + "###,###,##0." + decimalFormat);
                    }
                    else if (formatColumn.currencySymbol != null)
                    {
                        value = Convert.ToDecimal(value).ToString(formatColumn.currencySymbol + "###,###,##0.00");
                    }
                    isCurrency = true;
                }
                if (formatColumn != null && (formatColumn.fieldFormating == "Date" || formatColumn.fieldFormating == "Date and Time" || formatColumn.fieldFormating == "Time") && dc.DataType.Name == "DateTime")
                {
                    var date = Convert.ToDateTime(value);
                    value = formatColumn.fieldFormating.StartsWith("Date") ? date.ToShortDateString() + " " : "";
                    value += formatColumn.fieldFormating.EndsWith("Time") ? date.ToShortTimeString() : "";
                    value = value.Trim();
                }
            }
            catch (Exception ex)
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
            var data = GetDataTable(reportSql, connectKey);
            var sqlFields = data.sqlFields;
            var dt = data.dt;

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
                //Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
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
                    var columnName = !string.IsNullOrEmpty(columns[k].fieldLabel) ? columns[k].fieldLabel : columns[k].fieldName;

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

        public static async Task<byte[]> GetWordFile(string reportSql, string connectKey, string reportName, string chartData = null, bool allExpanded = false,
            string expandSqls = null, List<ReportHeaderColumn> columns = null, bool includeSubtotal = false, bool pivot = false, string pivotColumn = null, string pivotFunction = null)
        {
            var connectionString = DotNetReportHelper.GetConnectionString(connectKey);
            IDatabaseConnection databaseConnection = DatabaseConnectionFactory.GetConnection(dbtype);
            var data = GetDataTable(reportSql, connectKey);

            var qry = data.qry;
            var sqlFields = data.sqlFields;
            var dt = data.dt;
            var subTotals = new decimal[dt.Columns.Count];

            if (pivot) dt = Transpose(dt);

            if (columns?.Count > 0)
            {
                foreach (var col in columns)
                {
                    if (dt.Columns.Contains(col.fieldName) && col.hideStoredProcColumn)
                    {
                        dt.Columns.Remove(col.fieldName);
                    }
                    else if (!String.IsNullOrWhiteSpace(col.fieldLabel))
                    {
                        dt.Columns[col.fieldName].ColumnName = col.fieldLabel;
                    }
                }
            }

            if (!string.IsNullOrEmpty(pivotColumn))
            {
                var pd = await DotNetReportHelper.GetPivotTable(databaseConnection, connectionString, dt, qry.sql, sqlFields, expandSqls, pivotColumn, pivotFunction, 1, int.MaxValue, null, false);
                dt = pd.dt;
                if (!string.IsNullOrEmpty(pd.sql)) qry.sql = pd.sql;
                allExpanded = false;
                subTotals = new decimal[pd.dt.Columns.Count];
                //ds = await DotNetReportHelper.GetDrillDownData(databaseConnection, connectionString, dt, sqlFields, expandSqls);
                //dt = DotNetReportHelper.PushDatasetIntoDataTable(dt, ds, pivotColumn, pivotFunction, expandSqls);
            }

            using (MemoryStream memStream = new MemoryStream())
            {
                using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(memStream, WordprocessingDocumentType.Document))
                {
                    MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();
                    mainPart.Document = new Document();
                    Body body = mainPart.Document.AppendChild(new Body());
                    // Add report header
                    Paragraph header = new Paragraph(new Run(new RunProperties()
                    {
                        FontSize = new DocumentFormat.OpenXml.Wordprocessing.FontSize() { Val = "28" },// Font size 14 points (2 * 14)
                        Bold = new Bold(),
                    }, new Text(reportName)));
                    header.ParagraphProperties = new ParagraphProperties(new Justification() { Val = JustificationValues.Center });
                    body.AppendChild(header);

                    // Render chart
                    if (!string.IsNullOrEmpty(chartData) && chartData != "undefined")
                    {
                        byte[] imageDecoded = Convert.FromBase64String(chartData.Substring(chartData.LastIndexOf(',') + 1));
                        using (MemoryStream imageStream = new MemoryStream(imageDecoded))
                        {
                            ImagePart imagePart = mainPart.AddImagePart(ImagePartType.Jpeg);
                            imagePart.FeedData(imageStream);
                            // Specify the size in pixels and convert to EMUs
                            int widthInPixels = 500;
                            int heightInPixels = 400;
                            long widthInEmus = widthInPixels * 9525;
                            long heightInEmus = heightInPixels * 9525;
                            AddImageToBody(wordDocument, mainPart.GetIdOfPart(imagePart), widthInEmus, heightInEmus);
                        }
                    }
                    // Add data in table format
                    if (dt.Rows.Count > 0)
                    {
                        // Create table
                        Table table = new Table();
                        TableProperties props = new TableProperties(new Justification() { Val = JustificationValues.Center },
                         new TableBorders(
                         new TopBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 8 },
                         new BottomBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 10 },
                         new LeftBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 10 },
                         new RightBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 10 },
                         new InsideHorizontalBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 8 },
                         new InsideVerticalBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 10 }
                         ));

                        // Append table properties
                        table.AppendChild<TableProperties>(props);
                        // Add header row
                        TableRow headerRow = new TableRow();
                        // Calculate max text width for each column
                        int[] maxColumnWidths = new int[dt.Columns.Count];
                        foreach (DataColumn column in dt.Columns)
                        {
                            maxColumnWidths[column.Ordinal] = EstimateTextWidth(column.ColumnName);
                            RunProperties runProperties = new RunProperties(
                                new Bold(),
                                new DocumentFormat.OpenXml.Wordprocessing.Color() { Val = "#156082" } // Example color
                            );
                            Run run = new Run(runProperties, new Text(column.ColumnName));
                            ParagraphProperties paragraphProperties = new ParagraphProperties(
                                new SpacingBetweenLines() { Before = "100", After = "100", Line = "240", LineRule = LineSpacingRuleValues.Auto },
                                new Indentation() { Left = "180", Right = "180" } // Adjust values as needed
                            );
                            Paragraph paragraph = new Paragraph(paragraphProperties, run);
                            TableCell cell = new TableCell(paragraph);
                            headerRow.AppendChild(cell);
                        }
                        table.AppendChild(headerRow);

                        // Normalize column widths to fit the available width
                        int totalWidth = 0;
                        foreach (int width in maxColumnWidths)
                        {
                            totalWidth += width;
                        }
                        if (totalWidth > 13900)
                        {
                            // Set landscape orientation
                            SectionProperties sectionProperties = new SectionProperties();
                            DocumentFormat.OpenXml.Wordprocessing.PageSize pageSize = new DocumentFormat.OpenXml.Wordprocessing.PageSize() { Width = Convert.ToUInt32(totalWidth + (1440 * 2)), Orient = PageOrientationValues.Landscape };
                            sectionProperties.Append(pageSize);
                            body.Append(sectionProperties);
                        }
                        else
                        {
                            // Set page orientation to landscape
                            SectionProperties sectionProps = new SectionProperties();
                            DocumentFormat.OpenXml.Wordprocessing.PageSize defaultpageSize = new DocumentFormat.OpenXml.Wordprocessing.PageSize()
                            {
                                Orient = PageOrientationValues.Landscape,
                                Width = 16838,  // 11.69 inch in Twips (297 mm)
                            };
                            sectionProps.Append(defaultpageSize);
                            body.Append(sectionProps);
                        }
                        // Add data rows
                        foreach (DataRow row in dt.Rows)
                        {
                            var i = 0;
                            TableRow dataRow = new TableRow();
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
                                Run run = new Run(new Text(value));
                                ParagraphProperties paragraphProperties = new ParagraphProperties(
                                    new SpacingBetweenLines() { Before = "100", After = "100", Line = "240", LineRule = LineSpacingRuleValues.Auto },
                                    new Indentation() { Left = "180", Right = "180" } // Adjust values as needed
                                );
                                Paragraph paragraph = new Paragraph(paragraphProperties, run);
                                TableCell cell = new TableCell(paragraph);
                                dataRow.AppendChild(cell);
                                i++;
                            }
                            table.AppendChild(dataRow);
                        }
                        if (includeSubtotal)
                        {
                            TableRow dataRow = new TableRow();

                            for (int j = 0; j < dt.Columns.Count; j++)
                            {
                                var value = subTotals[j].ToString();
                                var dc = dt.Columns[j];
                                var formatColumn = GetColumnFormatting(dc, columns, ref value);
                                bool isNumericAndNotExcluded = formatColumn?.isNumeric == true && !(formatColumn?.dontSubTotal ?? false);
                                Run run = new Run(new Text(isNumericAndNotExcluded ? value : " "));
                                ParagraphProperties paragraphProperties = new ParagraphProperties(
                                    new SpacingBetweenLines() { Before = "100", After = "100", Line = "240", LineRule = LineSpacingRuleValues.Auto },
                                    new Indentation() { Left = "180", Right = "180" } // Adjust values as needed
                                );
                                Paragraph paragraph = new Paragraph(paragraphProperties, run);
                                TableCell cell = new TableCell(paragraph);
                                dataRow.AppendChild(cell);
                            }

                            table.AppendChild(dataRow);
                        }

                        // Add expanded data if applicable
                        if (allExpanded)
                        {
                            Paragraph expandedData = new Paragraph(new Run(new Text("Additional expanded data")));
                            body.AppendChild(expandedData);
                        }
                        body.AppendChild(table);
                    }
                    else
                    {
                        Paragraph expandedData = new Paragraph(new Run(new Text("No RecordS Found")));
                        body.AppendChild(expandedData);
                    }
                    // Ensure word wrapping doesn't break words
                    foreach (TableCell cell in body.Descendants<TableCell>())
                    {
                        cell.TableCellProperties = new TableCellProperties();
                        NoWrap noWrap = new NoWrap();
                        cell.TableCellProperties.Append(noWrap);
                    }
                    wordDocument.Save();
                }
                return memStream.ToArray();
            }
        }

        static void AddImageToBody(WordprocessingDocument wordDoc, string relationshipId, long cx, long cy)
        {
            // Define the reference of the image.
            var element =
                 new Drawing(
                     new DW.Inline(
                         new DW.Extent() { Cx = cx, Cy = cy },
                         new DW.EffectExtent()
                         {
                             LeftEdge = 0L,
                             TopEdge = 0L,
                             RightEdge = 0L,
                             BottomEdge = 0L
                         },
                         new DW.DocProperties()
                         {
                             Id = (UInt32Value)1U,
                             Name = "Chart Image"
                         },
                         new DW.NonVisualGraphicFrameDrawingProperties(
                             new A.GraphicFrameLocks() { NoChangeAspect = true }),
                         new A.Graphic(
                             new A.GraphicData(
                                 new PIC.Picture(
                                     new PIC.NonVisualPictureProperties(
                                         new PIC.NonVisualDrawingProperties()
                                         {
                                             Id = (UInt32Value)0U,
                                             Name = "New Bitmap Chart Image.jpg"
                                         },
                                         new PIC.NonVisualPictureDrawingProperties()),
                                     new PIC.BlipFill(
                                         new A.Blip(
                                             new A.BlipExtensionList(
                                                 new A.BlipExtension()
                                                 {
                                                     Uri =
                                                        "{28A0092B-C50C-407E-A947-70E740481C1C}"
                                                 })
                                         )
                                         {
                                             Embed = relationshipId,
                                             CompressionState =
                                             A.BlipCompressionValues.Print
                                         },
                                         new A.Stretch(
                                             new A.FillRectangle())),
                                     new PIC.ShapeProperties(
                                         new A.Transform2D(
                                             new A.Offset() { X = 0L, Y = 0L },
                                             new A.Extents() { Cx = 990000L, Cy = 792000L }),
                                         new A.PresetGeometry(
                                             new A.AdjustValueList()
                                         )
                                         { Preset = A.ShapeTypeValues.Rectangle }))
                             )
                             { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" })
                     )
                     {
                         DistanceFromTop = (UInt32Value)0U,
                         DistanceFromBottom = (UInt32Value)0U,
                         DistanceFromLeft = (UInt32Value)0U,
                         DistanceFromRight = (UInt32Value)0U,
                         EditId = "50D07946"
                     });

            if (wordDoc.MainDocumentPart is null || wordDoc.MainDocumentPart.Document.Body is null)
            {
                throw new ArgumentNullException("MainDocumentPart and/or Body is null.");
            }
            Paragraph paragraph = new Paragraph(new Run(element));
            paragraph.ParagraphProperties = new ParagraphProperties(new Justification() { Val = JustificationValues.Center });
            wordDoc.MainDocumentPart.Document.Body.AppendChild(paragraph);
        }
        static private int EstimateTextWidth(string text)
        {
            // Simple estimation: number of characters * average width of a character in twips
            // Note: 1 inch = 1440 twips, and average character width can vary. Adjust this multiplier as needed.
            int averageCharWidthInTwips = 120;
            return text.Length * averageCharWidthInTwips;
        }
        public static async Task<byte[]> GetPdfFile(string printUrl, int reportId, string reportSql, string connectKey, string reportName,
                    string userId = null, string clientId = null, string currentUserRole = null, string dataFilters = "", bool expandAll = false, string expandSqls = null, string pivotColumn = null, string pivotFunction = null)
        {
            var installPath = AppContext.BaseDirectory + $"{(AppContext.BaseDirectory.EndsWith("\\") ? "" : "\\")}App_Data\\local-chromium";
            await new BrowserFetcher(new BrowserFetcherOptions { Path = installPath }).DownloadAsync();
            var executablePath = "";
            foreach (var d in Directory.GetDirectories(installPath))
            {
                executablePath = $"{d}\\chrome-win\\chrome.exe";
                if (File.Exists(executablePath)) break;
            }

            var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true, ExecutablePath = executablePath });
            var page = await browser.NewPageAsync();
            await page.SetRequestInterceptionAsync(true);

            var connectionString = DotNetReportHelper.GetConnectionString(connectKey);
            IDatabaseConnection databaseConnection = DatabaseConnectionFactory.GetConnection(dbtype);
            var data = GetDataTable(reportSql, connectKey);

            var qry = data.qry;
            var sqlFields = data.sqlFields;
            var dt = data.dt;
            if (!string.IsNullOrEmpty(pivotColumn))
            {
                var pd = await DotNetReportHelper.GetPivotTable(databaseConnection, connectionString, dt, qry.sql, sqlFields, expandSqls, pivotColumn, pivotFunction, 1, int.MaxValue, null, false);
                dt = pd.dt;
                if (!string.IsNullOrEmpty(pd.sql)) qry.sql = pd.sql;
                var keywordsToExclude = new[] { "Count", "Sum", "Max", "Avg" };
                sqlFields = sqlFields
                    .Where(field => !keywordsToExclude.Any(keyword => field.Contains(keyword)))  // Filter fields to exclude unwanted keywords
                    .ToList();
                sqlFields.AddRange(dt.Columns.Cast<DataColumn>().Skip(sqlFields.Count).Select(x => $"__ AS {x.ColumnName}").ToList());
            }
            var model = new DotNetReportResultModel
            {
                ReportData = DotNetReportHelper.DataTableToDotNetReportDataModel(dt, sqlFields, false),
                Warnings = "",
                ReportSql = qry.sql,
                ReportDebug = false,
                Pager = new DotNetReportPagerModel
                {
                    CurrentPage = 1,
                    PageSize = 100000,
                    TotalRecords = dt.Rows.Count,
                    TotalPages = 1
                }
            };

            var formPosted = false;
            var formData = new StringBuilder();
            formData.AppendLine("<html><body>");
            formData.AppendLine($"<form action=\"{printUrl}\" method=\"post\">");
            formData.AppendLine($"<input name=\"reportSql\" value=\"{HttpUtility.UrlEncode(reportSql)}\" />");
            formData.AppendLine($"<input name=\"connectKey\" value=\"{HttpUtility.UrlEncode(connectKey)}\" />");
            formData.AppendLine($"<input name=\"reportId\" value=\"{reportId}\" />");
            formData.AppendLine($"<input name=\"pageNumber\" value=\"{1}\" />");
            formData.AppendLine($"<input name=\"pageSize\" value=\"{99999}\" />");
            formData.AppendLine($"<input name=\"userId\" value=\"{userId}\" />");
            formData.AppendLine($"<input name=\"clientId\" value=\"{clientId}\" />");
            formData.AppendLine($"<input name=\"currentUserRole\" value=\"{currentUserRole}\" />");
            formData.AppendLine($"<input name=\"expandAll\" value=\"{expandAll}\" />");
            formData.AppendLine($"<input name=\"dataFilters\" value=\"{HttpUtility.UrlEncode(dataFilters)}\" />");
            formData.AppendLine($"<input name=\"reportData\" value=\"{HttpUtility.UrlEncode(JsonConvert.SerializeObject(model))}\" />");
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
                PrintBackground = true,
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
        
        public static byte[] GetCombinePdfFile(List<byte[]> pdfFiles)
        {
            using (var outputDocument = new PdfDocument())
            {
                foreach (var pdf in pdfFiles)
                {
                    using (var inputDocument = PdfReader.Open(new MemoryStream(pdf), PdfDocumentOpenMode.Import))
                    {
                        for (int i = 0; i < inputDocument.PageCount; i++)
                        {
                            outputDocument.AddPage(inputDocument.Pages[i]);
                        }
                    }
                }
                using (var ms = new MemoryStream())
                {
                    outputDocument.Save(ms);
                    return ms.ToArray();
                }
            }
        }
        public static byte[] GetCombineExcelFile(List<byte[]> excelFiles, List<string> sheetNames)
        {
            using (var package = new ExcelPackage())
            {
                for (int i = 0; i < excelFiles.Count; i++)
                {
                    var fileBytes = excelFiles[i];
                    var sheetName = sheetNames[i] ?? $"Sheet{i + 1}";

                    using (var stream = new MemoryStream(fileBytes))
                    using (var tempPackage = new ExcelPackage(stream))
                    {
                        var tempSheet = tempPackage.Workbook.Worksheets[1];
                        var newSheet = package.Workbook.Worksheets.Add(sheetName, tempSheet);
                    }
                }

                return package.GetAsByteArray();
            }
        }
        public static byte[] GetCombineWordFile(List<byte[]> wordFiles)
        {
            using (MemoryStream memStream = new MemoryStream())
            {
                using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(memStream, WordprocessingDocumentType.Document))
                {
                    MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();
                    mainPart.Document = new Document(new Body());
                    for (int i = 0; i < wordFiles.Count; i++)
                    {
                        byte[] wordFile = wordFiles[i];
                        using (MemoryStream tempStream = new MemoryStream(wordFile))
                        using (WordprocessingDocument tempDoc = WordprocessingDocument.Open(tempStream, false))
                        {
                            // Copy each image part to the main document
                            foreach (var imagePart in tempDoc.MainDocumentPart.ImageParts)
                            {
                                var newImagePart = mainPart.AddImagePart(imagePart.ContentType);
                                newImagePart.FeedData(imagePart.GetStream());
                                string newRelId = mainPart.GetIdOfPart(newImagePart);
                                // Update references in the body to the new image part ID
                                foreach (var drawing in tempDoc.MainDocumentPart.Document.Body.Descendants<Drawing>())
                                {
                                    var blip = drawing.Descendants<A.Blip>().FirstOrDefault();
                                    if (blip != null && blip.Embed.HasValue)
                                    {
                                        blip.Embed = newRelId;
                                    }
                                }
                            }
                            // Copy the document body elements
                            Body tempBody = tempDoc.MainDocumentPart.Document.Body.CloneNode(true) as Body;
                            foreach (var element in tempBody.Elements())
                            {
                                mainPart.Document.Body.AppendChild(element.CloneNode(true));
                            }
                            if (i < wordFiles.Count - 1)
                            {
                                mainPart.Document.Body.AppendChild(new Paragraph(new Run(new Break() { Type = BreakValues.Page })));
                            }
                        }
                    }
                    mainPart.Document.Save();
                }
                return memStream.ToArray();
            }
        }
        public static async Task<string> GetChartImage(string printUrl, int reportId, string connectKey, string reportSql = null, string dataFilters = "")
        {
            var installPath = AppContext.BaseDirectory + $"{(AppContext.BaseDirectory.EndsWith("\\") ? "" : "\\")}App_Data\\local-chromium";
            await new BrowserFetcher(new BrowserFetcherOptions { Path = installPath }).DownloadAsync();
            var executablePath = "";
            foreach (var d in Directory.GetDirectories($"{installPath}\\chrome"))
            {
                executablePath = $"{d}\\chrome-win64\\chrome.exe";
                if (File.Exists(executablePath)) break;
            }
            var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true, ExecutablePath = executablePath });
            var page = await browser.NewPageAsync();
            await page.SetRequestInterceptionAsync(true);

            var connectionString = DotNetReportHelper.GetConnectionString(connectKey);
            IDatabaseConnection databaseConnection = DatabaseConnectionFactory.GetConnection(dbtype);
            var data = GetDataTable(reportSql, connectKey);

            var qry = data.qry;
            var sqlFields = data.sqlFields;
            var dt = data.dt;
            var model = new DotNetReportResultModel
            {
                ReportData = DotNetReportHelper.DataTableToDotNetReportDataModel(dt, sqlFields, false),
                Warnings = "",
                ReportSql = qry.sql,
                ReportDebug = false,
                Pager = new DotNetReportPagerModel
                {
                    CurrentPage = 1,
                    PageSize = 100000,
                    TotalRecords = dt.Rows.Count,
                    TotalPages = 1
                }
            };

            var formPosted = false;
            var formData = new StringBuilder();
            formData.AppendLine("<html><body>");
            formData.AppendLine($"<form action=\"{printUrl}\" method=\"post\">");
            formData.AppendLine($"<input name=\"reportSql\" value=\"{HttpUtility.UrlEncode(reportSql)}\" />");
            formData.AppendLine($"<input name=\"connectKey\" value=\"{HttpUtility.UrlEncode(connectKey)}\" />");
            formData.AppendLine($"<input name=\"reportId\" value=\"{reportId}\" />");
            formData.AppendLine($"<input name=\"pageNumber\" value=\"{1}\" />");
            formData.AppendLine($"<input name=\"pageSize\" value=\"{99999}\" />");
            formData.AppendLine($"<input name=\"dataFilters\" value=\"{HttpUtility.UrlEncode(dataFilters)}\" />");
            formData.AppendLine($"<input name=\"reportData\" value=\"{HttpUtility.UrlEncode(JsonConvert.SerializeObject(model))}\" />");
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
            // Navigate to the chart URL
            await page.GoToAsync(printUrl, new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Networkidle0 }
            });

            // Wait for the chart element to be visible
            await page.WaitForSelectorAsync(".report-inner", new WaitForSelectorOptions { Visible = true });
            var imageData = await page.EvaluateExpressionAsync<string>("window.chartImageUrl");
            await page.EvaluateExpressionAsync("delete window.chartImageUrl;");
            return imageData;
        }

        public static async Task<string> GetXmlFile(string reportSql, string connectKey, string reportName, string expandSqls = null, string pivotColumn = null, string pivotFunction = null)
        {
            var ds = new DataSet();
            var data = GetDataTable(reportSql, connectKey);
            var dt = data.dt;
            var connectionString = DotNetReportHelper.GetConnectionString(connectKey);
            IDatabaseConnection databaseConnection = DatabaseConnectionFactory.GetConnection(dbtype);
            var qry = data.qry;
            var sqlFields = data.sqlFields;
            if (!string.IsNullOrEmpty(pivotColumn))
            {
                var pd = await DotNetReportHelper.GetPivotTable(databaseConnection, connectionString, dt, qry.sql, sqlFields, expandSqls, pivotColumn, pivotFunction, 1, int.MaxValue, null, false);
                dt = pd.dt;
                if (!string.IsNullOrEmpty(pd.sql)) qry.sql = pd.sql;
                var keywordsToExclude = new[] { "Count", "Sum", "Max", "Avg" };
                sqlFields = sqlFields
                    .Where(field => !keywordsToExclude.Any(keyword => field.Contains(keyword)))  // Filter fields to exclude unwanted keywords
                    .ToList();
                sqlFields.AddRange(dt.Columns.Cast<DataColumn>().Skip(sqlFields.Count).Select(x => $"__ AS {x.ColumnName}").ToList());
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
            encryptedText = encryptedText.Split(new string[] { "%2C", "," }, StringSplitOptions.RemoveEmptyEntries)[0];
            byte[] initVectorBytes = Encoding.ASCII.GetBytes("yk0z8f39lgpu70gi"); // PLESE DO NOT CHANGE THIS KEY
            int keysize = 256;

            byte[] cipherTextBytes = Convert.FromBase64String(encryptedText.Replace("%3D", "="));
            var passPhrase = ConfigurationManager.AppSettings["dotNetReport.privateApiToken"].ToLower();
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
        public static async Task<byte[]> GetCSVFile(string reportSql, string connectKey, List<ReportHeaderColumn> columns = null, bool includeSubtotal = false, string expandSqls = null, string pivotColumn = null, string pivotFunction = null)
        {
            var data = GetDataTable(reportSql, connectKey);
            var dt = data.dt;
            var subTotals = new decimal[dt.Columns.Count];
            var connectionString = DotNetReportHelper.GetConnectionString(connectKey);
            IDatabaseConnection databaseConnection = DatabaseConnectionFactory.GetConnection(dbtype);
            var qry = data.qry;
            var sqlFields = data.sqlFields;
            if (!string.IsNullOrEmpty(pivotColumn))
            {
                var pd = await DotNetReportHelper.GetPivotTable(databaseConnection, connectionString, dt, qry.sql, sqlFields, expandSqls, pivotColumn, pivotFunction, 1, int.MaxValue, null, false);
                dt = pd.dt;
                subTotals = new decimal[dt.Columns.Count];
                if (!string.IsNullOrEmpty(pd.sql)) qry.sql = pd.sql;
                var keywordsToExclude = new[] { "Count", "Sum", "Max", "Avg" };
                sqlFields = sqlFields
                    .Where(field => !keywordsToExclude.Any(keyword => field.Contains(keyword)))  // Filter fields to exclude unwanted keywords
                    .ToList();
                sqlFields.AddRange(dt.Columns.Cast<DataColumn>().Skip(sqlFields.Count).Select(x => $"__ AS {x.ColumnName}").ToList());
            }
            //Build the CSV file data as a Comma separated string.
            string csv = string.Empty;
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                DataColumn column = dt.Columns[i];
                var columnName = !string.IsNullOrEmpty(columns[i].fieldLabel) ? columns[i].fieldLabel : columns[i].fieldName;
                csv += columnName + ',';
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
        }

        public static dynamic GetDbConnectionSettings(string account, string dataConnect, bool addOledbProvider = true)
        {
            var _configFilePath = Path.Combine(Directory.GetCurrentDirectory(), _configFileName);
            if (!System.IO.File.Exists(_configFilePath))
            {
                var emptyConfig = new JObject();
                System.IO.File.WriteAllText(_configFilePath, emptyConfig.ToString(Newtonsoft.Json.Formatting.Indented));
            }

            string configContent = System.IO.File.ReadAllText(_configFilePath);

            var config = JObject.Parse(configContent);
            var dotNetReportSection = config[$"dotNetReport"] as JObject;

            // First try to get connection from the dotnetreport appsettings file
            if (dotNetReportSection != null && !string.IsNullOrEmpty(dataConnect))
            {
                var dataConnectSection = dotNetReportSection[dataConnect] as JObject;
                if (dataConnectSection != null)
                {
                    return dataConnectSection.ToObject<dynamic>();
                }
            }
            else
            {
                // Next try to get config from appsettings (original method)
                var connection = DotNetReportHelper.GetConnection();
                var connectionString = DotNetReportHelper.GetConnectionString(connection, addOledbProvider).Result;

                if (!string.IsNullOrEmpty(connectionString))
                {
                    var dbConfig = new JObject
                    {
                        ["DatabaseType"] = "MS Sql",
                        ["ConnectionKey"] = "Default",
                        ["ConnectionString"] = connectionString
                    };
                    return dbConfig.ToObject<dynamic>();
                }
            }

            return null;
        }

        public static void UpdateDbConnection(UpdateDbConnectionModel model)
        {
            // Use dependency injection to get the appropriate implementation based on the database type
            var databaseConnection = DatabaseConnectionFactory.GetConnection(model.dbType);

            var connectionString = "";
            if (model.connectionType == "Build")
            {
                connectionString = databaseConnection.CreateConnection(model);
            }
            else
            {
                connectionString = DotNetReportHelper.GetConnectionString(model.connectionKey, false);

                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new Exception($"Connection string with key '{model.connectionKey}' was not found in App Config");
                }
            }

            try
            {
                // Test the database connection
                if (!databaseConnection.TestConnection(connectionString))
                {
                    throw new Exception("Could not connect to the Database.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Could not connect to the Database. Error: {ex.Message}");
            }

            if (!model.testOnly)
            {
                var _configFilePath = Path.Combine(Directory.GetCurrentDirectory(), _configFileName);
                if (!System.IO.File.Exists(_configFilePath))
                {
                    var emptyConfig = new JObject();
                    System.IO.File.WriteAllText(_configFilePath, emptyConfig.ToString(Newtonsoft.Json.Formatting.Indented));
                }

                // Get the existing JSON configuration
                var config = JObject.Parse(System.IO.File.ReadAllText(_configFilePath));
                if (config["dotNetReport"] == null)
                {
                    config["dotNetReport"] = new JObject();
                }

                // Update the specified properties within the "dotNetReport" and "dataConfig" section
                var dotNetReportSection = config["dotNetReport"] as JObject;
                if (dotNetReportSection[model.dataConnect] == null)
                {
                    dotNetReportSection[model.dataConnect] = new JObject();
                }

                var dataConnectSection = dotNetReportSection[model.dataConnect] as JObject;

                dataConnectSection["DatabaseType"] = model.dbType;
                dataConnectSection["ConnectionType"] = model.connectionType;
                if (model.connectionType == "Build")
                {
                    dataConnectSection["ConnectionKey"] = "Default";
                    dataConnectSection["ConnectionString"] = connectionString;
                    dataConnectSection["DatabaseHost"] = model.dbServer;
                    dataConnectSection["DatabasePort"] = model.dbPort;
                    dataConnectSection["DatabaseName"] = model.dbName;
                    dataConnectSection["Username"] = model.dbUsername;
                    dataConnectSection["Password"] = model.dbPassword;
                    dataConnectSection["AuthenticationType"] = model.dbAuthType;

                }
                else if (model.connectionType == "Key")
                {
                    dataConnectSection["ConnectionKey"] = model.connectionKey;
                    dataConnectSection["ConnectionString"] = connectionString;
                }

                if (model.isDefault)
                {
                    dotNetReportSection["DefaultConnection"] = model.dataConnect;
                }

                // Save the updated JSON back to the file
                File.WriteAllText(_configFilePath, config.ToString());
            }

        }

        public static void UpdateUserConfigSetting(UpdateUserConfigModel model)
        {
            var _configFilePath = Path.Combine(Directory.GetCurrentDirectory(), _configFileName);
            if (!System.IO.File.Exists(_configFilePath))
            {
                var emptyConfig = new JObject();
                System.IO.File.WriteAllText(_configFilePath, emptyConfig.ToString(Newtonsoft.Json.Formatting.Indented));
            }

            // Get the existing JSON configuration
            var config = JObject.Parse(System.IO.File.ReadAllText(_configFilePath));
            if (config["dotNetReport"] == null)
            {
                config["dotNetReport"] = new JObject();
            }

            // Update the specified properties within the "dotNetReport" and "dataConfig" section
            var dotNetReportSection = config["dotNetReport"] as JObject;
            if (dotNetReportSection[model.dataConnect] == null)
            {
                dotNetReportSection[model.dataConnect] = new JObject();
            }

            var dataConnectSection = dotNetReportSection[model.dataConnect] as JObject;
            dataConnectSection["UserConfig"] = model.userConfig;

            // Save the updated JSON back to the file
            System.IO.File.WriteAllText(_configFilePath, config.ToString());

        }

        public static void UpdateConfigurationFile(string accountApiKey, string privateApiKey, string dataConnectKey, bool onlyIfEmpty = false)
        {
            var _configFileName = "appsettings.json";
            var _configFilePath = Path.Combine(Directory.GetCurrentDirectory(), _configFileName);

            JObject existingConfig;
            if (System.IO.File.Exists(_configFilePath))
            {
                existingConfig = JObject.Parse(System.IO.File.ReadAllText(_configFilePath));
                if (existingConfig["dotNetReport"] is JObject dotNetReportObject)
                {
                    if (!onlyIfEmpty || (onlyIfEmpty && dotNetReportObject["accountApiToken"] != null && dotNetReportObject["accountApiToken"].ToString() == "Your Account API Key"))
                    {
                        dotNetReportObject["accountApiToken"] = accountApiKey;
                        dotNetReportObject["dataconnectApiToken"] = dataConnectKey;

                        if (!string.IsNullOrEmpty(privateApiKey))
                            dotNetReportObject["privateApiToken"] = privateApiKey;

                        System.IO.File.WriteAllText(_configFilePath, existingConfig.ToString(Newtonsoft.Json.Formatting.Indented));
                    }
                }
            }
        }

        public static AppSettingModel GetAppSettings()
        {
            var _configFilePath = Path.Combine(Directory.GetCurrentDirectory(), _configFileName);
            if (!System.IO.File.Exists(_configFilePath))
            {
                var emptyConfig = new JObject();
                System.IO.File.WriteAllText(_configFilePath, emptyConfig.ToString(Newtonsoft.Json.Formatting.Indented));
            }

            string configContent = System.IO.File.ReadAllText(_configFilePath);

            var config = JObject.Parse(configContent);
            // Extract the AppSetting section
            var appSetting = config["dotNetReport"]?["AppSetting"];

            // Create an instance of ApiSettingModel and populate its properties
            var settings = new AppSettingModel
            {
                emailAddress = appSetting?["email"]?["fromemail"]?.ToString(),
                emailName = appSetting?["email"]?["fromname"]?.ToString(),
                emailServer = appSetting?["email"]?["server"]?.ToString(),
                emailPort = appSetting?["email"]?["port"]?.ToString(),
                emailUserName = appSetting?["email"]?["username"]?.ToString(),
                emailPassword = appSetting?["email"]?["password"]?.ToString(),
                backendApiUrl = appSetting?["BaseApiUrl"]?.ToString() ?? "https://dotnetreport.com/api",
                timeZone = appSetting?["TimeZone"]?.ToString() ?? "-6",
                appThemes = appSetting?["AppTheme"]?.ToString() ?? "default"
            };

            return settings;
        }

        public static void SaveAppSettings(AppSettingModel model)
        {
            var _configFilePath = Path.Combine(Directory.GetCurrentDirectory(), _configFileName);
            if (!System.IO.File.Exists(_configFilePath))
            {
                var emptyConfig = new JObject();
                System.IO.File.WriteAllText(_configFilePath, emptyConfig.ToString(Newtonsoft.Json.Formatting.Indented));
            }

            //// Get the existing JSON configuration
            var config = JObject.Parse(System.IO.File.ReadAllText(_configFilePath));
            if (config["dotNetReport"] == null)
            {
                config["dotNetReport"] = new JObject();
            }
            var appsetting = config["dotNetReport"]["AppSetting"] ?? new JObject();
            appsetting["email"] = new JObject
            {
                ["fromemail"] = model.emailAddress,
                ["fromname"] = model.emailName,
                ["server"] = model.emailServer,
                ["port"] = model.emailPort,
                ["username"] = model.emailUserName,
                ["password"] = model.emailPassword
            };

            appsetting["BaseApiUrl"] = model.backendApiUrl;
            appsetting["TimeZone"] = model.timeZone;
            appsetting["AppTheme"] = model.appThemes;

            config["dotNetReport"]["AppSetting"] = appsetting;

            // Save the updated JSON back to the file
            System.IO.File.WriteAllText(_configFilePath, config.ToString());

        }

    }

    public class UpdateUserConfigModel
    {
        public string account { get; set; }
        public string dataConnect { get; set; }
        public string userConfig { get; set; }
    }

    public class UpdateDbConnectionModel
    {
        public string account { get; set; } = "";
        public string dataConnect { get; set; } = "";
        public string dbType { get; set; } = "";
        public string connectionType { get; set; } = "";
        public string connectionKey { get; set; } = "";
        public string connectionString { get; set; } = "";
        public string dbServer { get; set; } = "";
        public string dbPort { get; set; } = "";
        public string dbName { get; set; } = "";
        public string dbAuthType { get; set; } = "";
        public string dbUsername { get; set; } = "";
        public string dbPassword { get; set; } = "";
        public string providerName { get; set; } = "";
        public bool isDefault { get; set; }
        public bool testOnly { get; set; }
    }

    public class AppSettingModel
    {
        public string account { get; set; } = "";
        public string dataConnect { get; set; } = "";
        public string emailUserName { get; set; } = "";
        public string emailPassword { get; set; } = "";
        public string emailServer { get; set; } = "";
        public string emailPort { get; set; } = "";
        public string emailName { get; set; } = "";
        public string emailAddress { get; set; } = "";
        public string backendApiUrl { get; set; } = "";
        public string timeZone { get; set; } = "";
        public string appThemes { get; set; } = "";
    }

    public static class DatabaseConnectionFactory
    {
        public static IDatabaseConnection GetConnection(string dbtype)
        {
            IDatabaseConnection databaseConnection;
            switch (dbtype.ToLower())
            {
                case "ms sql":
                    databaseConnection = new SqlServerDatabaseConnection();
                    break;
                default:
                    databaseConnection = new OleDbDatabaseConnection();
                    break;
            }

            return databaseConnection;
        }


    }
    public interface IDatabaseConnection
    {
        bool TestConnection(string connectionString);
        string CreateConnection(UpdateDbConnectionModel model);
        int GetTotalRecords(string connectionString, string sqlCount, string sql, List<KeyValuePair<string, string>> parameters = null);
        DataTable ExecuteQuery(string connectionString, string sql, List<KeyValuePair<string, string>> parameters = null);
        DataSet ExecuteDataSetQuery(string connectionString, string combinedSqls, List<KeyValuePair<string, string>> parameters = null);
    }
    public class SqlServerDatabaseConnection : IDatabaseConnection
    {
        public bool TestConnection(string connectionString)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    //Test Connection
                    conn.Open();
                    conn.Close();
                    return true;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }


        public string CreateConnection(UpdateDbConnectionModel model)
        {
            SqlConnectionStringBuilder sqlConnectionStringBuilder = new SqlConnectionStringBuilder();

            // Set other SQL connection properties
            sqlConnectionStringBuilder.DataSource = model.dbServer;
            sqlConnectionStringBuilder.Add("Initial Catalog", model.dbName);
            sqlConnectionStringBuilder.Encrypt = false;
            if (model.dbAuthType.ToLower() == "username")
            {
                sqlConnectionStringBuilder.Add("User ID", model.dbUsername);
                sqlConnectionStringBuilder.Add("Password", model.dbPassword);
            }
            else
            {
                sqlConnectionStringBuilder.Add("Integrated Security", "SSPI");
            }

            return sqlConnectionStringBuilder.ConnectionString;
        }

        public int GetTotalRecords(string connectionString, string sqlCount, string sql, List<KeyValuePair<string, string>> parameters = null)
        {
            int totalRecords = 0;

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    using (SqlCommand command = new SqlCommand(sqlCount, conn))
                    {
                        if (parameters != null)
                        {
                            parameters.ForEach(x => command.Parameters.Add(new SqlParameter(x.Key, x.Value)));
                        }

                        if (!sql.StartsWith("EXEC")) totalRecords = Math.Max(totalRecords, (int)command.ExecuteScalar());

                    }

                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                // Handle exception (log, rethrow, etc.)
                throw new Exception($"Error executing SQL query for total records: {ex.Message}", ex);
            }

            return totalRecords;
        }

        public DataTable ExecuteQuery(string connectionString, string sql, List<KeyValuePair<string, string>> parameters = null)
        {
            DataTable dataTable = new DataTable();

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    using (SqlCommand command = new SqlCommand(sql, conn))
                    {
                        command.CommandTimeout = 60 * 5;
                        if (parameters != null)
                        {
                            if (sql.StartsWith("EXEC "))
                            {
                                command.CommandText = sql.Replace("EXEC ", "");
                                command.CommandType = CommandType.StoredProcedure;
                            }
                            parameters.ForEach(x => command.Parameters.Add(new SqlParameter(x.Key, x.Value)));
                        }
                        using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                        {
                            adapter.Fill(dataTable);
                        }
                    }

                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                // Handle exception (log, rethrow, etc.)
                throw new Exception($"Error executing SQL query: {ex.Message}", ex);
            }

            return dataTable;
        }

        public DataSet ExecuteDataSetQuery(string connectionString, string combinedSqls, List<KeyValuePair<string, string>> parameters = null)
        {
            var dts = new DataSet();
            try
            {
                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand(combinedSqls, conn))
                using (var adp = new SqlDataAdapter(cmd))
                {
                    if (parameters != null)
                    {
                        parameters.ForEach(x => cmd.Parameters.Add(new SqlParameter(x.Key, x.Value)));
                    }
                    adp.Fill(dts);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error executing SQL query: {ex.Message}", ex);
            }
            return dts;
        }
    }

    public class OleDbDatabaseConnection : IDatabaseConnection
    {
        public bool TestConnection(string connectionString)
        {
            using (OleDbConnection conn = new OleDbConnection(connectionString))
            {
                try
                {
                    //Test Connection
                    conn.Open();
                    conn.Close();
                    return true;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }


        public string CreateConnection(UpdateDbConnectionModel model)
        {
            OleDbConnectionStringBuilder OleDbConnectionStringBuilder = new OleDbConnectionStringBuilder();

            // Set other OleDb connection properties
            OleDbConnectionStringBuilder.Provider = model.providerName;
            OleDbConnectionStringBuilder.DataSource = model.dbServer;
            OleDbConnectionStringBuilder.Add("Initial Catalog", model.dbName);
            if (model.dbAuthType.ToLower() == "username")
            {
                OleDbConnectionStringBuilder.Add("User ID", model.dbUsername);
                OleDbConnectionStringBuilder.Add("Password", model.dbPassword);
            }
            else
            {
                OleDbConnectionStringBuilder.Add("Integrated Security", "SSPI");
            }

            return OleDbConnectionStringBuilder.ConnectionString;
        }

        public int GetTotalRecords(string connectionString, string sqlCount, string sql, List<KeyValuePair<string, string>> parameters = null)
        {
            int totalRecords = 0;

            try
            {
                using (OleDbConnection conn = new OleDbConnection(connectionString))
                {
                    conn.Open();

                    using (OleDbCommand command = new OleDbCommand(sqlCount, conn))
                    {
                        if (!sql.StartsWith("EXEC")) totalRecords = Math.Max(totalRecords, (int)command.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exception (log, rethrow, etc.)
                throw new Exception($"Error executing OleDb query for total records: {ex.Message}", ex);
            }

            return totalRecords;
        }

        public DataTable ExecuteQuery(string connectionString, string sql, List<KeyValuePair<string, string>> parameters = null)
        {
            DataTable dataTable = new DataTable();

            try
            {
                using (OleDbConnection conn = new OleDbConnection(connectionString))
                {
                    conn.Open();

                    using (OleDbCommand command = new OleDbCommand(sql, conn))
                    {
                        if (parameters != null)
                        {
                            parameters.ForEach(x => command.Parameters.Add(new OleDbParameter(x.Key, x.Value)));
                        }
                        using (OleDbDataAdapter adapter = new OleDbDataAdapter(command))
                        {
                            adapter.Fill(dataTable);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exception (log, rethrow, etc.)
                throw new Exception($"Error executing OleDb query: {ex.Message}", ex);
            }

            return dataTable;
        }
        public DataSet ExecuteDataSetQuery(string connectionString, string combinedSqls, List<KeyValuePair<string, string>> parameters = null)
        {
            var dts = new DataSet();
            try
            {
                using (var conn = new OleDbConnection(connectionString))
                using (var cmd = new OleDbCommand(combinedSqls, conn))
                using (var adp = new OleDbDataAdapter(cmd))
                {
                    if (parameters != null)
                    {
                        parameters.ForEach(x => cmd.Parameters.Add(new OleDbParameter(x.Key, x.Value)));
                    }
                    adp.Fill(dts);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error executing SQL query: {ex.Message}", ex);
            }
            return dts;
        }
    }

}
