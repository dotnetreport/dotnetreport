using iTextSharp.text;
using iTextSharp.text.pdf;
using OfficeOpenXml;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
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
    }

    public class ParameterViewModel
    {
        public int Id { get; set; }
        public string ParameterName { get; set; }
        public string DisplayName { get; set; }
        public string ParameterValue { get; set; }
        public string ParameterDataTypeString { get; set; }
        public Type ParameterDataTypeCLR { get; set; }
        public SqlDbType ParamterDataTypeOleDbType { get; set; }
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
    }

    public class DotNetReportHelper
    {
        public static string GetConnectionString(string key)
        {
            var connString = Startup.StaticConfig.GetConnectionString(key);

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

        public static string GetFormattedValue(DataColumn col, DataRow row)
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

        public static byte[] GetExcelFile(string reportSql, string connectKey, string reportName, bool allExpanded = false,
                List<string> expandSqls = null, List<ReportHeaderColumn> columns = null, bool includeSubtotal = false, bool pivot = false)
        {
            var sql = Decrypt(reportSql);

            // Execute sql
            var dt = new DataTable();
            using (var conn = new SqlConnection(GetConnectionString(connectKey)))
            {
                conn.Open();
                var command = new SqlCommand(sql, conn);
                var adapter = new SqlDataAdapter(command);

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

                    FormatExcelSheet(dt, ws, rowstart, colstart, columns, includeSubtotal);

                    if (allExpanded)
                    {
                        var j = 0;
                        foreach (DataRow dr in dt.Rows)
                        {
                            if (j < expandSqls.Count)
                            {
                                var dtNew = new DataTable();
                                command.CommandText = Decrypt(expandSqls[j++]);
                                adapter.Fill(dtNew);

                                var wsNew = xp.Workbook.Worksheets.Add(dr[0].ToString());
                                FormatExcelSheet(dtNew, wsNew, 1, 1);
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


        public static byte[] GetPdfFileAlt(string reportSql, string connectKey, string reportName, string chartData = null, 
                    List<ReportHeaderColumn> columns = null, bool includeSubtotal = false, bool pivot = false)
        {
            var sql = Decrypt(reportSql);
            var dt = new DataTable();
            using (var conn = new SqlConnection(GetConnectionString(connectKey)))
            {
                conn.Open();
                var command = new SqlCommand(sql, conn);
                var adapter = new SqlDataAdapter(command);

                adapter.Fill(dt);
            }

            Document document = new Document();

            if (pivot)
            {
                dt = Transpose(dt);
                document.SetPageSize(PageSize.LETTER.Rotate());
            }

            var subTotals = new decimal[dt.Columns.Count];

            using (var ms = new MemoryStream())
            {
                PdfWriter writer = PdfWriter.GetInstance(document, ms);
                document.Open();
                var heading = new Phrase(reportName);
                heading.Font.Size = 14f;
                heading.Font.SetStyle("bold");
                document.Add(heading);

                PdfPTable table = new PdfPTable(dt.Columns.Count);
                table.WidthPercentage = 100;

                //Set columns names in the pdf file
                for (int k = 0; k < dt.Columns.Count; k++)
                {
                    var phrase = new Phrase(dt.Columns[k].ColumnName);
                    var cell = new PdfPCell(phrase);
                    cell.HorizontalAlignment = PdfPCell.ALIGN_CENTER;
                    cell.VerticalAlignment = PdfPCell.ALIGN_CENTER;
                    cell.BorderColor = BaseColor.LIGHT_GRAY;
                    // cell.BackgroundColor = new iTextSharp.text.BaseColor(51, 102, 102);
                    table.AddCell(cell);

                    cell.BorderWidth = 0.5f;
                    
                    phrase.Font.Size = 10f;
                    phrase.Font.SetStyle("bold");
                }

                //Add values of DataTable in pdf file
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    for (int j = 0; j < dt.Columns.Count; j++)
                    {
                        var value = dt.Rows[i][j].ToString();
                        var dc = dt.Columns[j];
                        var formatColumn = GetColumnFormatting(dc, columns, ref value);

                        var phrase = new Phrase(value);
                        var cell = new PdfPCell(phrase);
                        //Align the cell in the center
                        cell.HorizontalAlignment = formatColumn.isNumeric ? PdfPCell.ALIGN_RIGHT : PdfPCell.ALIGN_LEFT;
                        cell.VerticalAlignment = PdfPCell.ALIGN_LEFT;
                        cell.BorderColor = BaseColor.LIGHT_GRAY;
                        cell.BorderWidth = 0.5f;
                        phrase.Font.Size = 10f;

                        if (formatColumn != null)
                        {
                            cell.HorizontalAlignment = formatColumn.fieldAlign == "Right" || (formatColumn.isNumeric && (formatColumn.fieldAlign == "Auto" || string.IsNullOrEmpty(formatColumn.fieldAlign))) ? PdfPCell.ALIGN_RIGHT : formatColumn.fieldAlign == "Center" ? PdfPCell.ALIGN_MIDDLE : PdfPCell.ALIGN_LEFT;
                        }

                        if (includeSubtotal)
                        {
                            if (formatColumn.isNumeric && !(formatColumn?.dontSubTotal ?? false))
                            {
                                subTotals[j] += Convert.ToDecimal(dt.Rows[i][j]);
                            }
                        }

                        table.AddCell(cell);
                    }
                }

                if (includeSubtotal)
                {
                    for (int j = 0; j < dt.Columns.Count; j++)
                    {
                        PdfPCell cell = null;
                        var value = subTotals[j].ToString();
                        var dc = dt.Columns[j];
                        var formatColumn = GetColumnFormatting(dc, columns, ref value);
                        if (formatColumn.isNumeric && !(formatColumn?.dontSubTotal ?? false))
                        {
                            cell = new PdfPCell(new Phrase(value));
                        }
                        else
                        {
                            cell = new PdfPCell(new Phrase(" "));
                        }
                        cell.HorizontalAlignment = formatColumn.isNumeric ? PdfPCell.ALIGN_RIGHT : PdfPCell.ALIGN_LEFT;
                        cell.VerticalAlignment = PdfPCell.ALIGN_LEFT;
                        cell.BorderColor = BaseColor.BLACK;
                        cell.BorderWidth = 1f;

                        table.AddCell(cell);
                    }
                }

                //Create a PdfReader bound to that byte array
                if (!string.IsNullOrEmpty(chartData) && chartData != "undefined")
                {
                    byte[] sPDFDecoded = Convert.FromBase64String(chartData.Substring(chartData.LastIndexOf(',') + 1));
                    var image = Image.GetInstance(sPDFDecoded);
                    if (image.Height > image.Width)
                    {
                        //Maximum height is 800 pixels.
                        float percentage = 0.0f;
                        percentage = 700 / image.Height;
                        image.ScalePercent(percentage * 100);
                    }
                    else
                    {
                        //Maximum width is 600 pixels.
                        float percentage = 0.0f;
                        percentage = 540 / image.Width;
                        image.ScalePercent(percentage * 100);
                    }
                    // If need to add boarder
                    //   image.Border = iTextSharp.text.Rectangle.BOX;
                    //  image.BorderColor = iTextSharp.text.BaseColor.BLACK;
                    //  image.BorderWidth = 3f;
                    document.Add(image);
                }
                document.Add(table);
                document.Close();
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
            using (var conn = new SqlConnection(GetConnectionString(connectKey)))
            {
                conn.Open();
                var command = new SqlCommand(sql, conn);
                var adapter = new SqlDataAdapter(command);

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
            using (var conn = new SqlConnection(GetConnectionString(connectKey)))
            {
                conn.Open();
                var command = new SqlCommand(sql, conn);
                var adapter = new SqlDataAdapter(command);

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
