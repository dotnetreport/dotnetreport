﻿<%@ Page Title="" Language="C#" MasterPageFile="~/DotNetReport/ReportLayout.Master" AutoEventWireup="true" CodeBehind="Dashboard.aspx.cs" Inherits="ReportBuilder.WebForms.DotNetReport.Dashboard" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
     <link rel="stylesheet" href="//cdnjs.cloudflare.com/ajax/libs/gridstack.js/0.4.0/gridstack.min.css" />
    <link rel="stylesheet" href="//cdnjs.cloudflare.com/ajax/libs/gridstack.js/0.4.0/gridstack.min.css" />
    <style type="text/css">
        .report-chart { min-height: auto !important; }
    </style>

</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="scripts" runat="server">
    <script type="text/javascript" src='//cdnjs.cloudflare.com/ajax/libs/gridstack.js/0.4.0/gridstack.min.js'></script>
    <script type="text/javascript" src='//cdnjs.cloudflare.com/ajax/libs/gridstack.js/0.4.0/gridstack.jQueryUI.min.js'></script>
    <script type="text/javascript">
        $(document).ready(function () {
            var reports = [];
            var dashboards = [];
            var queryParams = Object.fromEntries((new URLSearchParams(window.location.search)).entries());
            var adminMode = queryParams.adminMode == 'true' ? 'true' : 'false';
            var svc = "/DotNetReport/ReportService.asmx/";

            ajaxcall({ url: svc + 'GetDashboards', data: { adminMode: adminMode } }).done(function (dashboardData) {
                if (dashboardData.d) { dashboardData = dashboardData.d; }
                _.forEach(dashboardData, function (d) {
                    dashboards.push({ id: d.Id, name: d.Name, description: d.Description, selectedReports: d.SelectedReports, userId: d.UserId, userRoles: d.UserRoles, viewOnlyUserId: d.ViewOnlyUserId, viewOnlyUserRoles: d.ViewOnlyUserRoles });
                });

                var dashboardId = parseInt(queryParams.id || 0);
                if (!dashboardId && dashboards.length > 0) { dashboardId = dashboards[0].id; }

                ajaxcall({ url: svc + 'LoadSavedDashboard', data: { id: dashboardId || null, adminMode: adminMode } }).done(function (reportsData) {
                    if (reportsData.d) { reportsData = reportsData.d; }
                    _.forEach(reportsData, function (r) {
                        reports.push({ reportSql: r.ReportSql, reportId: r.ReportId, reportFilter: r.ReportFilter, connectKey: r.ConnectKey, x: r.X, y: r.Y, width: r.Width, height: r.Height });
                    });

                    ajaxcall({ url: svc + 'GetUsersAndRoles' }).done(function (data) {
                        if (data.d) { data = data.d; }

                        var vm = new dashboardViewModel({
                            runReportUrl: svc + "Report",
                            execReportUrl: svc + "RunReport",
                            reportWizard: $("#filter-panel"),
                            lookupListUrl: svc + "GetLookupList",
                            apiUrl: svc + "CallReportApi",
                            runReportApiUrl: svc + "RunReportApi",
                            reportMode: "execute",
                            reports: reports,
                            dashboards: dashboards,
                            users: data.users,
                            userRoles: data.userRoles,
                            allowAdmin: data.allowAdminMode,
                            dataFilters: data.dataFilters,
                            dashboardId: dashboardId,
                            runExportUrl: "",
                            printReportUrl: ""
                        });

                        vm.init().done(function () {
                            ko.applyBindings(vm);
                            $(function () {
                                var options = {
                                    cellHeight: 80,
                                    verticalMargin: 10
                                };
                                $('.grid-stack').gridstack(options);
                                $('.grid-stack').on('change', function (event, items) {
                                    _.forEach(items, function (x) {
                                        vm.updatePosition(x);
                                    });
                                });
                            });

                            setTimeout(function () {
                                vm.drawChart();
                            }, 1000);
                        });

                        $(window).resize(function () {
                            vm.drawChart();
                        });
                    });
                });
            });
        });
    </script>
    
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="body" runat="server">

    <div class="row">
        <div class="col-8" data-bind="with: currentDashboard">
            <h2 data-bind="text: name ? name : 'Dashboard'">Dashboard</h2>
            <p data-bind="text: description"></p>
        </div>

        <div class="col-4 right-align" style="display: none;" data-bind="visible: dashboards().length > 0 ">
            <div class="row">

                <label class="col">Switch Dashboard</label>
                <select class="col form-control" data-bind="options: dashboards, optionsText: 'name', optionsValue: 'id', value: selectDashboard"></select>
                <div class="col">
                    <button onclick="return false;" class="btn btn-primary btn-sm" data-toggle="modal" data-target="#add-dashboard-modal" title="Edit Dashboard Settings" data-bind="click: editDashboard">Edit</button>
                    <button onclick="return false;" class="btn btn-primary btn-sm" data-toggle="modal" data-target="#add-dashboard-modal" title="Add a New Dashboard" data-bind="click: newDashboard">Add</button>
                </div>
            </div>
        </div>
    </div>
    <div class="clearfix"></div>

    <div data-bind="template: {name: 'admin-mode-template'}, visible: allowAdmin" style="display: none;"></div>

    <div class="alert alert-info">
        To manage Reports, please <a href="/DotnetReport/Index.aspx">click here</a>.
    </div>
    <div class="centered" style="display: none;" data-bind="visible: dashboards().length == 0 ">
        No Dashboards yet. Click below to Start<br />
        <button onclick="return false;" class="btn btn-lg btn-primary" data-toggle="modal" data-target="#add-dashboard-modal"><i class="fa fa-dashboard"></i> Create a New Dashboard</button>
    </div>

    <div class="modal modal-fullscreen" id="add-dashboard-modal" role="dialog">
        <div class="modal-dialog" role="document">
            <div class="modal-content" data-bind="with: dashboard">
                <div class="modal-header">
                    <h4 class="modal-title"><span data-bind="text: Id() ? 'Edit' : 'Add'"></span> Dashboard</h4>
                    <button onclick="return false;" type="button" class="close" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button>
                </div>
                <div class="modal-body">
                    <div class="form-horizontal">
                        <div class="control-group">
                            <div class="form-group row">
                                <label class="col-md-3 col-sm-3 control-label">Name</label>
                                <div class="col-md-6 col-sm-6">
                                    <input class="form-control text-box" style="width: 100%;" data-val="true" data-val-required="Dashboard Name is required." type="text" data-bind="value: Name" placeholder="Dashboard Name, ex Sales, Accounting" id="add-dash-name" />
                                </div>
                            </div>
                            <div class="form-group row">
                                <label class="col-md-3 col-sm-3 control-label">Description</label>
                                <div class="col-md-6 col-sm-6">
                                    <textarea class="form-control text-box" style="width: 100%;" data-bind="value: Description" placeholder="Optional Description for the Dashboard">
                                    </textarea>
                                </div>
                            </div>
                        </div>
                    </div>
                    <hr />
                    <h5><span class="fa fa-paperclip"></span> Choose Reports for the Dashboard</h5>
                    <div data-bind="foreach: $parent.reportsAndFolders" class="card" style="margin-left: 20px;">
                        <div class="card-body">
                            <a class="btn btn-link" role="button" data-toggle="collapse" data-bind="attr: {href: '#folder-' + folderId }">
                                <i class="fa fa-folder"></i>&nbsp;<span data-bind="text: folder"></span>
                            </a>
                            <div class="collapse" data-bind="attr: {id: 'folder-' + folderId }">
                                <ul class="list-group" data-bind="foreach: reports">
                                    <li class="list-group-item">
                                        <div class="checkbox">
                                            <label class="list-group-item-heading">
                                                <input type="checkbox" data-bind="checked: selected">
                                                <span class="fa" data-bind="css: {'fa-file': reportType=='List', 'fa-th-list': reportType=='Summary', 'fa-bar-chart': reportType=='Bar', 'fa-pie-chart': reportType=='Pie',  'fa-line-chart': reportType=='Line', 'fa-globe': reportType =='Map'}" style="font-size: 14pt; color: #808080"></span>
                                                <span data-bind="text: reportName"></span>
                                            </label>
                                        </div>
                                        <p class="list-group-item-text small" data-bind="text: reportDescription"></p>
                                    </li>
                                </ul>
                            </div>
                        </div>
                    </div>
                    <!-- ko if: $parent.adminMode -->
                    <hr />
                    <div data-bind="template: {name: 'manage-access-template'}"></div>
                    <!-- /ko -->
                </div>
                <div class="modal-footer">
                    <button onclick="return false;" type="button" class="btn btn-danger" data-bind="click: $root.deleteDashboard, visible: Id">Delete Dashboard</button>
                    <button onclick="return false;" type="button" class="btn btn-primary" data-bind="click: $root.saveDashboard">Save Dashboard</button>
                </div>
            </div>
        </div>
    </div>


    <div class="clearfix"></div>

    <div class="grid-stack" data-bind="visible: reports().length>0" style="display: none;">
        <!-- ko foreach: reports -->
        <div class="grid-stack-item" data-bind="attr: {'data-gs-x': x, 'data-gs-y': y, 'data-gs-width': width, 'data-gs-height': height, 'data-gs-auto-position': true, 'data-gs-id': ReportID}">

            <div class="card" data-bind="attr: {class: 'card ' + panelStyle + ' grid-stack-item-content'}" style="overflow-y: hidden;">
                <div class="padded-div">
                    <div class="pull-left">
                        <button type="button" class="btn" data-toggle="dropdown" aria-haspopup="false" aria-expanded="false">
                            <span class="fa fa-ellipsis-v"></span>
                        </button>
                        <ul class="dropdown-menu small" style="z-index: 1001;">
                            <!-- ko if: FlyFilters().length> 0-->
                            <li class="dropdown-item">
                                <a href="#" data-bind="click: toggleFlyFilters">
                                    <span class="fa fa-filter"></span> Filter
                                </a>
                            </li>
                            <!-- /ko -->
                            <li class="dropdown-item">
                                <a data-bind="attr: {href: '/DotNetReport/Index.aspx?reportId=' + ReportID()}">
                                    <span class="fa fa-pencil"></span> Edit
                                </a>
                            </li>
                            <li class="dropdown-item">
                                <a href="#" data-bind="click: downloadExcel">
                                    <span class="fa fa-file-excel-o"></span> Excel
                                </a>
                            </li>
                            <li class="dropdown-item">
                                <a href="#" data-bind="click: downloadPdf">
                                    <span class="fa fa-file-pdf-o"></span> PDF
                                </a>
                            </li>
                            <li class="dropdown-item">
                                <a href="#" data-bind="click: function() { $parent.removeReportFromDashboard(ReportID()); }">
                                    <span class="fa fa-close"></span> Remove
                                </a>
                            </li>
                        </ul>
                    </div>

                    <h2 class="pull-left" data-bind="text: ReportName"></h2>
                </div>
                <div class="card-body list-overflow-auto">
                    <p data-bind="html: ReportDescription, visible: ReportDescription"></p>
                    <div data-bind="template: {name: 'fly-filter-template'}, visible: showFlyFilters"></div>
                    <div data-bind="with: ReportResult" class="small">
                        <div data-bind="template: 'report-template', data: $data"></div>
                    </div>
                </div>
                <div class="form-inline">
                    <div class="small" data-bind="with: pager">
                        <div class="form-group pull-left total-records" data-bind="if: totalRecords()>1">
                            <span data-bind="text: 'Total Records: ' + totalRecords()"></span><br />
                        </div>
                        <div class="form-group pull-right" data-bind="if: pages()>1">
                            <div data-bind="template: 'pager-template', data: $data"></div>
                        </div>
                        <div class="clearfix"></div>
                    </div>
                </div>
            </div>

        </div>
        <!-- /ko -->
    </div>
    
</asp:Content>