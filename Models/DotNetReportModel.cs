using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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
}
