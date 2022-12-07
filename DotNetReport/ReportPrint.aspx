﻿<!DOCTYPE html>
<html>

<head>
    <meta charset="utf-8" />
    <link rel="shortcut icon" href="/favicon.ico">
    <meta http-equiv="X-UA-Compatible" content="IE=edge">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Asp .Net Ad Hoc Report Builder - @ViewBag.Title </title>
    <meta name="keywords" content="ad-hoc reporting, reporting, asp .net reporting, asp .net report, report builder, ad hoc report builder, ad-hoc report builder, adhoc report, ad hoc reports, .net report viewer, reportviewer, sql reportviewer, report builder mvc, report mvc, report builder web forms, query builder, sql report builder,visual report builder,custom query,query maker" />
    <meta name="description" content="Ad hoc Reporting software that allows programmers to easily add Reporting functionality to their ASP .NET Web Software Solution" />
    <link href="../Content/bootstrap.min.css" rel="stylesheet" />
    <link href="../Content/bootstrap-mvc-validation.css" rel="stylesheet" />
    <link href="../Content/themes/base/datepicker.css" rel="stylesheet" />
    <link href="../Content/themes/base/theme.css" rel="stylesheet" />
    <link href="../Content/toastr.min.css" rel="stylesheet" />
    <link href="../Content/font-awesome.min.css" rel="stylesheet" />
    <link href="../Content/css/select2.min.css" rel="stylesheet" />
    <link href="../Content/dotnetreport.css?v=4.2.0" rel="stylesheet" />
    <script type="text/javascript" src="https://www.gstatic.com/charts/loader.js"></script>
    <style type="text/css">
        .report-view {
            margin: 0 50px 0 50px;
            max-width: inherit;
        }
    </style>
</head>

<body>
    
    <form id="form1" runat="server">
    <div data-bind="with: ReportResult">

        <!-- ko ifnot: HasError -->

        <div class="report-view" data-bind="with: $root">
            <div class="report-inner">
                <canvas id="report-header" width="1100" height="120" data-bind="visible: useReportHeader"></canvas>
                <h2 data-bind="text: ReportName"></h2>
                <p data-bind="html: ReportDescription">
                </p>
                <div data-bind="with: ReportResult">
                    <div data-bind="template: 'report-template', data: $data"></div>
                </div>
            </div>
            <br />
            <span>Report ran on: @DateTime.Now.ToShortDateString() @DateTime.Now.ToShortTimeString()</span>
        </div>
        <!-- /ko -->
        <!-- ko if: HasError -->
        <h2>@Model.ReportName</h2>
        <p>
            @Model.ReportDescription
        </p>

        <h3>An unexpected error occured while running the Report</h3>
        <hr />
        <b>Error Details</b>
        <p>
            <div data-bind="text: Exception"></div>
        </p>

        <!-- /ko -->

    </div>

    <script type="text/html" id="report-template">
        <div class="report-chart" data-bind="attr: {id: 'chart_div_' + $parent.ReportID()}, visible: $parent.isChart"></div>

        <div class="table-responsive" data-bind="with: ReportData">
            <table class="table table-hover table-condensed">
                <thead>
                    <tr class="no-highlight">
                        <!-- ko if: $parentContext.$parent.canDrilldown() && !IsDrillDown() -->
                        <th></th>
                        <!-- /ko -->
                        <!-- ko foreach: Columns -->
                        <th data-bind="attr: { id: 'col' + $index() }, css: {'right-align': IsNumeric}">
                            <a href="" data-bind="click: function(){ $parentContext.$parentContext.$parent.changeSort(SqlField); }">
                                <span data-bind="text: ColumnName"></span>
                                <span data-bind="text: $parentContext.$parentContext.$parent.pager.sortColumn() === SqlField ? ($parentContext.$parentContext.$parent.pager.sortDescending() ? '&#9660;' : '&#9650;') : ''"></span>
                            </a>
                        </th>
                        <!-- /ko -->
                    </tr>
                </thead>
                <tbody>
                    <tr style="display: none;" data-bind="visible: Rows.length < 1">
                        <td class="text-info" data-bind="attr:{colspan: Columns.length}">
                            No records found
                        </td>
                    </tr>
                    <!-- ko foreach: Rows  -->
                    <tr>
                        <!-- ko if: $parentContext.$parentContext.$parent.canDrilldown() && !$parent.IsDrillDown() -->
                        <td>&nbsp;</td>
                        <!-- /ko -->
                        <!-- ko foreach: Items -->
                        <!-- ko if: LinkTo-->
                        <td data-bind="css: {'right-align': Column.IsNumeric}">
                            <a data-bind="attr: {href: LinkTo}"><span data-bind="html: FormattedValue"></span></a>
                        </td>
                        <!-- /ko-->
                        <!-- ko ifnot: LinkTo-->
                        <td data-bind="html: FormattedValue, css: {'right-align': Column.IsNumeric}"></td>
                        <!-- /ko-->
                        <!-- /ko -->
                    </tr>
                    <!-- ko if: isExpanded -->
                    <tr>
                        <td></td>
                        <td data-bind="attr:{colspan: $parent.Columns.length }">
                            <!-- ko if: DrillDownData -->
                            <table class="table table-hover table-condensed" data-bind="with: DrillDownData">
                                <thead>
                                    <tr class="no-highlight">
                                        <!-- ko foreach: Columns -->
                                        <th data-bind="css: {'right-align': IsNumeric}">
                                            <a href="" data-bind="click: function(){ $parents[1].changeSort(SqlField); }">
                                                <span data-bind="text: ColumnName"></span>
                                                <span data-bind="text: $parents[1].pager.sortColumn() === SqlField ? ($parents[1].pager.sortDescending() ? '&#9660;' : '&#9650;') : ''"></span>
                                            </a>
                                        </th>
                                        <!-- /ko -->
                                    </tr>
                                </thead>
                                <tbody>
                                    <tr style="display: none;" data-bind="visible: Rows.length < 1">
                                        <td class="text-info" data-bind="attr:{colspan: Columns.length}">
                                            No records found
                                        </td>
                                    </tr>
                                    <!-- ko foreach: Rows  -->
                                    <tr>
                                        <!-- ko foreach: Items -->
                                        <td data-bind="html: FormattedValue, css: {'right-align': Column.IsNumeric}"></td>
                                        <!-- /ko -->
                                    </tr>
                                    <!-- /ko -->
                                </tbody>
                            </table>
                            <!-- /ko -->
                        </td>
                    </tr>
                    <!-- /ko -->
                    <!-- /ko -->
                </tbody>
                <!-- ko if: $parent.SubTotals().length == 1 -->
                <tfoot data-bind="foreach: $parent.SubTotals">
                    <tr>
                        <!-- ko if: $parentContext.$parentContext.$parent.canDrilldown() && !$parent.IsDrillDown() -->
                        <td></td>
                        <!-- /ko -->
                        <!-- ko foreach: Items -->
                        <td data-bind="html: FormattedValue, css: {'right-align': Column.IsNumeric}"></td>
                        <!-- /ko -->
                    </tr>
                </tfoot>
                <!-- /ko -->
            </table>
        </div>

    </script>

    <script src="../Scripts/jquery-3.6.0.min.js"></script>
    <script src="../Scripts/bootstrap.bundle.min.js"></script>
    <script src="../Scripts/jquery-ui-1.12.1.min.js""></script>
    <script src="../Scripts/jquery.validate.min.js"></script>
    <script src="../Scripts/knockout-3.5.1.js"></script>
    <script src="../Scripts/jquery.blockUI.js"></script>
    <script src="../Scripts/bootbox.min.js"></script>
    <script src="../Scripts/toastr.min.js"></script>
    <script src="../Scripts/knockout-sortable.min.js"></script>
    <script src="../Scripts/select2.min.js"></script>
    <script src="../Scripts/dotnetreport-helper.js"></script>
    <script src="../Scripts/lodash.min.js"></script>
    <script src="../Scripts/fabric.min.js"></script>
    <script src="../Scripts/dotnetreport.js?v=5.0.0"></script>

    <style type="text/css">
        a[href]:after {
            content: none !important;
        }
    </style>
    <script type="text/javascript">

        $(document).ready(function () {
        var data = {
            currentUserId: '<%= @Model.UserId %>',
            currentUserRoles: '<%= @Model.CurrentUserRoles %>',
            dataFilters: '<%= @Model.DataFilters %>',
            clientId: '<%= @Model.ClientId %>'
        };

        var svc = "/DotNetReport/ReportService.asmx/";

        var vm = new reportViewModel({
            runReportUrl: svc + "Report",
            execReportUrl: svc + "RunReport",
            runLinkReportUrl: svc + "RunReportLink",
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
            dataFilters: data.dataFilters,
            runExportUrl: svc,
        });
        vm.pager.pageSize(10000);

        ko.applyBindings(vm);

        vm.loadProcs().done(function () {
            vm.LoadReport(<%= Model.ReportId %>, true, "<%= Model.ReportSeries %>").done(function () {
                vm.headerDesigner.resizeCanvas();
                if (<%= Model.ExpandAll ? "true" : "false" %>) {
                setTimeout(function () {
                    vm.ExpandAll();
                }, 500);
            }
            });

        });

        $(window).resize(function(){
            vm.DrawChart();
        });

    });

    </script>
        
    </form>
</body>
</html>