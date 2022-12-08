﻿<%@ Page Title="" Language="C#" MasterPageFile="~/DotNetReport/ReportLayout.Master" AutoEventWireup="true" CodeBehind="Report.aspx.cs" Inherits="ReportBuilder.WebForms.DotNetReport.Report" Async="true" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">    
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="scripts" runat="server">

<!--
    This razor view renders the Report from the Data Table as a Html Table. You have complete control over this page and you can change the code and style to meet your requirements.

    Its Recommended you use it as is, and only change styling as needed to match your application. You will be responsible for managing and maintaining any changes.

    Note: To allow bigger file downloads in Excel, Please increase maxRequestLength in web.config. For example, <httpRuntime maxRequestLength="1048576" />

-->
        <script type="text/javascript">

        $(document).ready(function () {

            var queryParams = Object.fromEntries((new URLSearchParams(window.location.search)).entries());

            ajaxcall({ url: '/DotNetReport/ReportService.asmx/GetUsersAndRoles', type: 'POST' }).done(function (data) {
                if (data.d) data = data.d;
                var svc = "/DotNetReport/ReportService.asmx/";
                var vm = new reportViewModel({
                    runReportUrl: "/DotnetReport/Report.aspx",
                    execReportUrl: svc + "RunReport",
                    runLinkReportUrl: svc + "RunReportLink",
                    reportWizard: $("#filter-panel"),
                    reportHeader: "report-header",
                    lookupListUrl: svc + "GetLookupList",
                    apiUrl: svc + "CallReportApi",
                    runReportApiUrl: svc + "RunReportApi",
                    reportFilter: htmlDecode('<%= Model.ReportFilter %>'),
                    reportMode: queryParams.linkedreport == "true" ? "linked" : "execute",
                    reportSql: "<%= Model.ReportSql %>",
                    reportConnect: "<%= Model.ConnectKey %>",
                    reportSeries: "<%= Model.ReportSeries %>",
                    AllSqlQuries: "<%= Model.ReportSql %>",
                    userSettings: data,
                    dataFilters: data.dataFilters,
                    runExportUrl: svc,
                    printReportUrl: window.location.protocol + "//" + window.location.host + "/DotnetReport/ReportPrint.aspx"
                });

                vm.loadProcs().done(function () {
                    vm.LoadReport(<%= Model.ReportId %>, true, "<%= Model.ReportSeries %>").done(function () {
                        ko.applyBindings(vm);
                        vm.headerDesigner.resizeCanvas();
                    });
            });

            $(window).resize(function () {
                vm.DrawChart();
                vm.headerDesigner.resizeCanvas();
            });
        });
    });

        </script>
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="body" runat="server">

<div data-bind="with: ReportResult">

    <div data-bind="ifnot: HasError">
        <div class="report-view" data-bind="with: $root">
            <div class="pull-right">
                <a href="/DotNetReport/Index.aspx?folderId=<%= Model.SelectedFolder %>" class="btn btn-primary">
                    Back to Reports
                </a>
                <button onclick="history.back()" class="btn btn-primary" data-bind="visible: ReportMode()=='linked'">
                    Back to Parent Report
                </button>
                <a href="/DotNetReport/Index.aspx?reportId=<%= Model.ReportId %>&folderId=<%= Model.SelectedFolder%>" class="btn btn-primary" data-bind="visible: $root.CanEdit()">
                    Edit Report
                </a>

                <div class="btn-group">
                    <button type="button" class="btn btn-secondary dropdown-toggle" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false">
                        <span class="fa fa-download"></span> Export <span class="caret"></span>
                    </button>
                    <ul class="dropdown-menu">
                        <li class="dropdown-item">
                            <a href="#" data-bind="click: downloadPdfAlt">
                                <span class="fa fa-file-pdf-o"></span> Pdf
                            </a>
                        </li>
                        <li class="dropdown-item">
                            <a href="#" data-bind="click: downloadExcel">
                                <span class="fa fa-file-excel-o"></span> Excel
                            </a>
                        </li>
                        <li class="dropdown-item">
                            <a href="#" data-bind="click: downloadCsv">
                                <span class="fa fa-file-excel-o"></span> Csv
                            </a>
                        </li>
                        <li class="dropdown-item">
                            <a href="#" data-bind="click: downloadXml">
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
                            <button class="btn btn-secondary btn-sm" data-bind="visible: !$root.isChart() || $root.ShowDataWithGraph(), click: $root.downloadExcel" title="Export to Excel">
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
                        <div class="canvas-container">
                            <canvas id="report-header" width="900" height="120" data-bind="visible: useReportHeader"></canvas>
                        </div>
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
    </div>
    <div data-bind="if: HasError">
        <h2><%= Model.ReportName %></h2>
        <p>
            <%= Model.ReportDescription %>
        </p>
        <a href="/DotNetReport/Index.aspx?folderId=<%=Model.SelectedFolder %>" class="btn btn-primary">
            Back to Reports
        </a>
        <a href="/DotNetReport/Index.aspx?reportId=<%=Model.ReportId %>&folderId=<%=Model.SelectedFolder %>" class="btn btn-primary" data-bind="visible: $root.CanEdit()">
            Edit Report
        </a>
        <h3>An unexpected error occured while running the Report</h3>
        <hr />
        <b>Error Details</b>
        <div>
            <div data-bind="text: Exception"></div>
        </div>

    </div>
    <div data-bind="if: ReportDebug() || HasError()">
        <br />
        <br />
        <hr />
        <code data-bind="text: ReportSql">

        </code>
    </div>
</div>
    
</asp:Content>