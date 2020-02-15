using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Security.Cryptography;
using System.Text;

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
        public bool IsDashboard { get; set; }
        public int SelectedFolder { get; set; }
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
        public string TableName { get; set; }
        public string DisplayName { get; set; }
        public bool Selected { get; set; }
        public bool IsView { get; set; }
        public int DisplayOrder { get; set; }
        public string AccountIdField { get; set; }

        public List<ColumnViewModel> Columns { get; set; }
        public List<string> AllowedRoles { get; set; }
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
        public JoinTypes ForeignJoin { get; set; }
        public string ForeignKeyField { get; set; }
        public string ForeignValueField { get; set; }
        public bool DoNotDisplay { get; set; }
        public List<string> AllowedRoles { get; set; }
    }

    public class ConnectViewModel
    {
        public string Provider { get; set; }
        public string ServerName { get; set; }
        public string InitialCatalog { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool IntegratedSecurity { get; set; }


        public string AccountApiKey { get; set; }
        public string DatabaseApiKey { get; set; }
    }

    public class ManageViewModel
    {
        public string AccountApiKey { get; set; }
        public string DatabaseApiKey { get; set; }

        public List<TableViewModel> Tables { get; set; }
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
        public List<string> Users { get; set; } = new List<string>();

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

    public class DotNetReportHelper
    {
        public static byte[] GetExcelFile(string reportSql, string connectKey, string reportName)
        {
            var sql = Decrypt(reportSql);

            // Execute sql
            var dt = new DataTable();
            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings[connectKey].ConnectionString))
            {
                conn.Open();
                var command = new SqlCommand(sql, conn);
                var adapter = new SqlDataAdapter(command);

                adapter.Fill(dt);
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
                ws.Cells[rowstart, colstart].LoadFromDataTable(dt, true);
                ws.Cells[rowstart, colstart, rowstart, colend].Style.Font.Bold = true;

                int i = 1;
                foreach (DataColumn dc in dt.Columns)
                {
                    if (dc.DataType == typeof(decimal))
                        ws.Column(i).Style.Numberformat.Format = "#0.00";

                    if (dc.DataType == typeof(DateTime))
                        ws.Column(i).Style.Numberformat.Format = "dd/mm/yyyy";

                    i++;
                }
                ws.Cells[ws.Dimension.Address].AutoFitColumns();
                return xp.GetAsByteArray();
            }
        }

        public static string GetXmlFile(string reportSql, string connectKey, string reportName)
        {
            var sql = Decrypt(reportSql);

            // Execute sql
            var dt = new DataTable();
            var ds = new DataSet();
            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings[connectKey].ConnectionString))
            {
                conn.Open();
                var command = new SqlCommand(sql, conn);
                var adapter = new SqlDataAdapter(command);

                adapter.Fill(dt);
            }

            ds.Tables.Add(dt);
            ds.DataSetName = "data";
            foreach(DataColumn c in dt.Columns)
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
                                return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
                            }
                        }
                    }
                }
            }
        }
    }
}
