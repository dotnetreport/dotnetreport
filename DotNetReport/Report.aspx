<%@ Page Title="" Language="C#" MasterPageFile="~/DotNetReport/ReportLayout.Master" AutoEventWireup="true" CodeBehind="Report.aspx.cs" Inherits="ReportBuilder.WebForms.DotNetReport.Report" Async="true" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">    
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="scripts" runat="server">
    <script type="text/javascript">
        function printReport() {
            var printWindow = window.open("");
            printWindow.document.open();
            printWindow.document.write('<html><head>' +
                '<link href="/Content/bootstrap.css" rel="stylesheet" />' +
                '<style type="text/css">a[href]:after {content: none !important;}</style>' +
                '</head><body>' + $('.report-inner').html() +
                '</body></html>');

            setTimeout(function () {
                printWindow.document.close();
                printWindow.focus();
                printWindow.print();
                printWindow.close();
            }, 250);
        }

        function downloadPdf(reportId, currentSql, currentConnectKey, reportName, chartData, columnDetails, includeSubTotals) {
            if (!currentSql) return;
            redirectToReport("/DotNetReport/ReportService.asmx/DownloadPdf", {
                reportId: reportId,
                reportSql: unescape(currentSql),
                connectKey: unescape(currentConnectKey),
                reportName: unescape(reportName),
                chartData: unescape(chartData),
                columnDetails: unescape(columnDetails),
                includeSubTotal: unescape(includeSubTotals)
            }, true, false);
        }

        function downloadExcel(currentSql, currentConnectKey, reportName, allExpanded, expandSqls, columnDetails, includeSubTotals) {
            if (!currentSql) return;
            redirectToReport("/DotNetReport/ReportService.asmx/DownloadExcel", {
                reportSql: unescape(currentSql),
                connectKey: unescape(currentConnectKey),
                reportName: unescape(reportName),
                allExpanded: unescape(allExpanded),
                expandSqls: unescape(expandSqls),
                columnDetails: unescape(columnDetails),
                includeSubTotal: unescape(includeSubTotals)
            }, true, false);
        }

        function downloadCsv(currentSql, currentConnectKey, reportName, columnDetails, includeSubTotals) {
            if (!currentSql) return;
            redirectToReport("/DotNetReport/ReportService.asmx/DownloadCsv", {
                reportSql: unescape(currentSql),
                connectKey: unescape(currentConnectKey),
                reportName: unescape(reportName),
                columnDetails: unescape(columnDetails),
                includeSubTotal: unescape(includeSubTotals)
            }, true, false);
        }

        function downloadXml(currentSql, currentConnectKey, reportName) {
            if (!currentSql) return;
            redirectToReport("/DotNetReport/ReportService.asmx/DownloadXml", {
                reportSql: unescape(currentSql),
                connectKey: unescape(currentConnectKey),
                reportName: unescape(reportName)
            }, true, false);
        }

        $(document).ready(function () {
            ajaxcall({ url: '/DotNetReport/ReportService.asmx/GetUsersAndRoles', type: 'POST' }).done(function (data) {
                var svc = "/DotNetReport/ReportService.asmx/";
                var vm = new reportViewModel({
                    runReportUrl: svc + "Report",
                    execReportUrl: svc + "RunReport",
                    runLinkReportUrl: svc + "ReportLink",
                    reportWizard: $("#filter-panel"),
                    reportHeader: "report-header",
                    lookupListUrl: svc + "GetLookupList",
                    apiUrl: svc + "CallReportApi",
                    runReportApiUrl: svc + "RunReportApi",
                    reportFilter: htmlDecode('<%= Model.ReportFilter %>'),
                    reportMode: "execute",
                    reportSql: "<%= Model.ReportSql %>",
                    reportConnect: "<%= Model.ConnectKey %>",
                    reportSeries: "<%= Model.ReportSeries %>",
                    AllSqlQuries: "<%= Model.ReportSql %>",
                    userSettings: data,
                    dataFilters: data.dataFilters
                });

                vm.loadProcs().done(function () {
                    vm.LoadReport(<%= Model.ReportId %>, true, "<%= Model.ReportSeries %>").done(function () {
                        ko.applyBindings(vm);
                    });
                });

                $(window).resize(function () {
                    vm.DrawChart();
                    vm.headerDesigner.resizeCanvas();
                });
            });
        });

    </script>
}

</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="body" runat="server">
    
<div data-bind="with: ReportResult">

    <!-- ko ifnot: HasError -->
    <div class="report-view" data-bind="with: $root">       
        <div class="pull-right">
            <a href="/DotNetReport/Index.aspx?folderId=<%= Model.SelectedFolder %>" class="btn btn-primary">
                Back to Reports
            </a>
            <a href="/DotNetReport/Index.aspx?reportId=<%= Model.ReportId %>&folderId=<%= Model.SelectedFolder%>" class="btn btn-primary">
                Edit Report
            </a>
            <button type="button" class="btn btn-secondary" onclick="printReport();">
                <span class="fa fa-print" aria-hidden="true"></span> Print Report
            </button>

           <div class="btn-group">
                <button type="button" class="btn btn-secondary dropdown-toggle" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false">
                    <span class="fa fa-download"></span> Export <span class="caret"></span>
                </button>
                <ul class="dropdown-menu">
                    <li class="dropdown-item">
                        <a href="#" data-bind="click: downloadPdf(ReportID(), currentSql(), currentConnectKey(), ReportName(), ChartData(), getColumnDetails(), IncludeSubTotal())">
                            <span class="fa fa-file-pdf-o"></span> Pdf
                        </a>
                    </li>
                    <li class="dropdown-item">
                        <a href="#" data-bind="click: downloadExcel(currentSql(), currentConnectKey(), ReportName(), allExpanded(), getExpandSqls(), getColumnDetails(), IncludeSubTotal())">
                            <span class="fa fa-file-excel-o"></span> Excel
                        </a>
                    </li>
                    <li class="dropdown-item">
                        <a href="#" data-bind="click: downloadCsv(currentSql(), currentConnectKey(), ReportName(), getColumnDetails(), IncludeSubTotal())">
                            <span class="fa fa-file-excel-o"></span> Csv
                        </a>
                    </li>
                    <li class="dropdown-item">
                        <a href="#" data-bind="click: downloadXml(currentSql(), currentConnectKey(), ReportName())">
                            <span class="fa fa-file-code-o"></span> Xml
                        </a>
                    </li>
                </ul>
            </div>
        </div>
        <br />
        <br />
        <div style="clear: both;"></div>
        <br />

        <div data-bind="if: EditFiltersOnReport">
            <div class="card">
                <div class="card-header">
                    <h5 class="card-title">
                        <a data-toggle="collapse" data-target="#filter-panel" href="#">
                            <i class="fa fa-filter"></i>Choose filter options
                        </a>
                    </h5>
                </div>
                <div id="filter-panel" class="card-body">
                    <div>
                        <div class="row">
                            <div data-bind="template: {name: 'filter-group'}" class="col"></div>
                        </div>

                        <br />
                        <button class="btn btn-primary" data-bind="click: SaveFilterAndRunReport">Update Filters</button>
                    </div>
                </div>
            </div>
            <br />
        </div>
        <div data-bind="ifnot: EditFiltersOnReport">
            <div data-bind="template: {name: 'fly-filter-template'}"></div>
            <br />
        </div>
        <div data-bind="if: canDrilldown">
            <button class="btn btn-secondary btn-xs" data-bind="click: ExpandAll">Expand All</button>
            <button class="btn btn-secondary btn-xs" data-bind="click: CollapseAll">Collapse All</button>
            <br />
            <br />
        </div>
        <div class="report-menubar">
            <div class="col-xs-12 col-centered" data-bind="with: pager">
                <div class="form-inline" data-bind="visible: pages()">
                    <div class="form-group pull-left total-records">
                        <span data-bind="text: 'Total Records: ' + totalRecords()"></span><br />
                    </div>
                    <div class="pull-left">
                       <button class="btn btn-secondary btn-sm" data-bind="visible: !$root.isChart() || $root.ShowDataWithGraph(), click: downloadExcel($root.currentSql(), $root.currentConnectKey(), $root.ReportName(), $root.allExpanded(), $root.getExpandSqls(), $root.getColumnDetails(), $root.IncludeSubTotal());" " title="Export to Excel">
                            <span class="fa fa-file-excel-o"></span>
                        </button>
                    </div>
                    <div class="form-group pull-right">
                        <div data-bind="template: 'pager-template', data: $data"></div>
                    </div>
                </div>
            </div>
        </div>
        <div class="report-canvas">
            <div class="report-container">
                <div class="report-inner">
                    <canvas id="report-header" width="900" height="120" data-bind="visible: useReportHeader"></canvas>
                    <h2 data-bind="text: ReportName"></h2>
                    <p data-bind="html: ReportDescription">
                    </p>
                    <div data-bind="with: ReportResult">
                        <div data-bind="template: 'report-template', data: $data"></div>
                    </div>
                </div>
            </div>
        </div>
        <br />
        <span>Report ran on: <%=DateTime.Now.ToShortDateString() %> <%=@DateTime.Now.ToShortTimeString() %></span>         
    </div>
    <!-- /ko -->
    <!-- ko if: HasError -->
    <h2><%= Model.ReportName %></h2>
    <p>
        <%= Model.ReportDescription %>
    </p>

    <a href="/DotNetReport/Index.aspx?folderId=<%=Model.SelectedFolder %>" class="btn btn-primary">
        Back to Reports
    </a>
    <a href="/DotNetReport/Index.aspx?reportId=<%=Model.ReportId %>&folderId=<%=Model.SelectedFolder %>" class="btn btn-primary">
        Edit Report
    </a>
    <h3>An unexpected error occured while running the Report</h3>
    <hr />
    <b>Error Details</b>
    <p>
        <div data-bind="text: Exception"></div>
    </p>

    <!-- /ko -->
    <!-- ko if: ReportDebug() || HasError() -->
    <br />
    <br />
    <hr />
    <code data-bind="text: ReportSql">

    </code>
    <!-- /ko -->
</div>

</asp:Content>