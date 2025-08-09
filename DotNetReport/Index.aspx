<%@ Page Title="" Language="C#" MasterPageFile="~/DotNetReport/ReportLayout.Master" AutoEventWireup="true" CodeBehind="Index.aspx.cs" Inherits="ReportBuilder.WebForms.DotNetReport.Index" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="Content3" ContentPlaceHolderID="scripts" runat="server">
<!--
The html and JavaScript code below is related to presentation for the Report Builder. You don't have to change it, unless you intentionally want to
change something in the Report Builder's behavior in your Application.

Its Recommended you use it as is, and only change styling as needed to match your application. You will be responsible for managing and maintaining any changes.
-->
    <script type="text/javascript">

        var toastr = toastr || { error: function (msg) { window.alert(msg); }, success: function (msg) { window.alert(msg); } };

        var queryParams = Object.fromEntries((new URLSearchParams(window.location.search)).entries());

        $(document).ready(function () {
            ajaxcall({ url: '/DotNetReport/ReportService.asmx/GetUsersAndRoles', type: 'POST' }).done(function (data) {
                if (data.d) data = data.d;
                var svc = "/DotNetReport/ReportService.asmx/";
                var vm = new reportViewModel({
                    runReportUrl: "/DotNetReport/Report.aspx",
                    reportWizard: $("#modal-reportbuilder"),
                    execReportUrl: svc + "RunReport",
                    runLinkReportUrl: svc + "RunReportLink",
                    getSchemaFromSql: svc + "GetSchemaFromSql",
                    linkModal: $("#linkModal"),
                    reportHeader: "report-header",
                    fieldOptionsModal: $("#fieldOptionsModal"),
                    lookupListUrl: svc + "GetLookupList",
                    apiUrl: svc + "CallReportApi",
                    runReportApiUrl: svc + "RunReportApi",
                    getUsersAndRolesUrl: svc + "GetUsersAndRoles",
                    reportId: queryParams.reportId || 0,
                    userSettings: data,
                    dataFilters: data.dataFilters,
                    printReportUrl: window.location.protocol + "//" + window.location.host + "/DotnetReport/ReportPrint.aspx",
                    getTimeZonesUrl: svc + "GetAllTimezones",
                    samePageOnRun: true,
                    runExportUrl: svc
                });

                vm.init(queryParams.folderid || 0, data.noAccount);
                ko.applyBindings(vm);
                vm.textQuery.setupSearch();

                $(window).resize(function () {
                    vm.DrawChart();
                    vm.headerDesigner.resizeCanvas();
                });
            });
        });

    </script>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="body" runat="server">

<div class="container-fluid">
    <div data-bind="template: {name: 'admin-mode-template'}, visible: allowAdmin" style="display: none;"></div>

    <!--
        The markup code below is related to presentation. You don't have to change it, unless you
        intentionally want to change something in the Report Builder's behavior in your Application.
        It's Recommended you use it as is. You will be responsible for managing and maintaining any changes.
    -->
    <!-- Folders/Report List -->
    <div id="report-start" data-bind="if: ReportMode() == 'start' || ReportMode() == 'generate'">
        <div class="card folder-panel">
            <div class="card-header">
                <nav class="navbar navbar-expand-lg navbar-light bg-light">
                    <a class="navbar-brand" href="#" data-bind="click: function() {SelectedFolder(null); designingHeader(false); searchReports(''); $('#search-report').val([]).trigger('change'); }">Manage Reports</a>
                    <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target="#navbarSupportedContent" aria-controls="navbarSupportedContent" aria-expanded="false" aria-label="Toggle navigation">
                        <span class="navbar-toggler-icon"></span>
                    </button>

                    <div class="collapse navbar-collapse" id="navbarSupportedContent">
                        <ul class="navbar-nav mr-auto">

                            <li class="nav-item active" data-bind="visible: CanSaveReports() || adminMode()">
                                <a href="#" class="nav-link" data-bind="click: createNewReport" data-bs-toggle="modal" data-bs-target="#modal-reportbuilder">
                                    <span class="fa fa-plus"></span> New Report
                                </a>
                            </li>

                            <li class="nav-item" data-bind="visible: CanManageFolders() || adminMode()">
                                <a href="#" class="nav-link" data-bind="click: ManageFolder.newFolder">
                                    <span class="fa fa-folder-o"></span> Add Folder
                                </a>
                            </li>
                            <li class="nav-item dropdown" data-bind="visible: (CanManageFolders() || adminMode()) && SelectedFolder()!=null">
                                <a href="#" class="nav-link dropdown-toggle" role="button" data-bs-toggle="dropdown" aria-haspopup="true" aria-expanded="false">
                                    <span class="fa fa-folder"></span> Folder Options
                                </a>
                                <div class="dropdown-menu">
                                    <a class="dropdown-item" href="#" data-bind="click: ManageFolder.editFolder">Edit Selected Folder</a>
                                    <a class="dropdown-item" href="#" data-bind="click: ManageFolder.deleteFolder">Delete Selected Folder</a>
                                </div>
                            </li>
                            <li class="nav-item active" data-bind="visible: adminMode() && SelectedFolder() ==null">
                                <a href="#" class="nav-link" data-bind="click: function(){ initHeaderDesigner(); }">
                                    <span class="fa fa-arrow-up"></span> Report Header
                                </a>
                            </li>
                            <li class="nav-item active" data-bind="visible: adminMode">
                                <a href="#" class="nav-link" data-bs-toggle="modal" data-bs-target="#uploadFileModal" aria-haspopup="true" aria-expanded="false" title="Import Report from JSON file">
                                    <span class="fa fa-upload"></span> Import Report
                                </a>
                            </li>
                        </ul>
                        <div class="form-inline my-2 my-lg-0 ms-auto w-50">
                            <div class="input-group w-100">
                                <p id="search-input" class="form-control search-input" data-placeholder="Search Report by Name, Description or Data Field..."></p>
                                <span data-bind="click: searchForReports" class="input-group-text" style="height: 38px; cursor: pointer;" title="Search"><i class="fa fa-search"></i></span>
                                <span data-bind="click: function() {resetQuery(true, true);}" class="input-group-text" style="height: 38px; cursor: pointer;" title="Clear Search"><i class="fa fa-close"></i></span>
                            </div>
                        </div>
                    </div>
                </nav>
            </div>
            <div class="card-body">
                <div data-bind="visible: designingHeader, with: headerDesigner" style="display: none;" class="overflow-auto">
                    <div class="checkbox">
                        <label>
                            <input type="checkbox" data-bind="checked: UseReportHeader"> Use Report Header
                        </label>
                    </div>
                    <div data-bind="visible: UseReportHeader">
                        <p class="alert alert-info">
                            You can design the common report header below that will be applied to all reports where report headers are turned on.
                        </p>
                        <div class="d-flex align-items-center report-menubar">
                            <div class="btn-group me-2">
                                <button class="btn btn-sm btn-outline-secondary" title="Add Text" data-bind="click: addText">
                                    <span class="fa fa-font"></span>
                                </button>
                                <label class="btn btn-sm btn-outline-secondary" title="Add Image">
                                    <span class="fa fa-image"></span>
                                    <input type="file" accept="image/*" hidden data-bind="event: { change: function() { uploadImage($element.files[0]) } }" />
                                </label>
                                <button class="btn btn-sm btn-outline-secondary" title="Add Line" data-bind="click: addLine">
                                    <span class="fa fa-arrows-h"></span>
                                </button>
                            </div>
                            <div data-bind="if: selectedObject()" class="d-flex align-items-center me-2">
                                <span class="mx-2">|</span>
                                <button class="btn btn-sm btn-outline-danger me-2" data-bind="click: remove" title="Delete">
                                    <span class="fa fa-trash"></span>
                                </button>
                                <div data-bind="if: getText()" class="d-flex align-items-center">
                                    <select class="form-select form-select-sm me-2" title="Font Family" data-bind="event: {change: setFontFamily }, value: objectProperties.fontFamily">
                                        <option value="arial" selected>Arial</option>
                                        <option value="helvetica">Helvetica</option>
                                        <option value="myriad pro">Myriad Pro</option>
                                        <option value="delicious">Delicious</option>
                                        <option value="verdana">Verdana</option>
                                        <option value="georgia">Georgia</option>
                                        <option value="courier">Courier</option>
                                        <option value="comic sans ms">Comic Sans MS</option>
                                    </select>
                                    <select class="form-select form-select-sm me-2" title="Text Align" data-bind="event: {change: setTextAlign }, value: objectProperties.textAlign">
                                        <option>Left</option>
                                        <option>Center</option>
                                        <option>Right</option>
                                        <option>Justify</option>
                                    </select>
                                    <input type="color" class="form-control form-control-color me-2" style="width: 100px; height: 30px;" title="Font Color" data-bind="event: {change: setFontColor }, value: objectProperties.fontColor" style="min-width: 20px;">
                                    <button class="btn btn-sm btn-outline-secondary me-2" title="Bold" data-bind="event: {click: setFontBold }, value: objectProperties.fontBold">
                                        <span class="fa fa-bold"></span>
                                    </button>
                                    <button class="btn btn-sm btn-outline-secondary me-2" title="Italic" data-bind="event: {click: setFontItalic }, value: objectProperties.fontItalic">
                                        <span class="fa fa-italic"></span>
                                    </button>
                                    <button class="btn btn-sm btn-outline-secondary me-2" title="Underline" data-bind="event: {click: setFontUnderline }, value: objectProperties.fontUnderline">
                                        <span class="fa fa-underline"></span>
                                    </button>
                                </div>
                            </div>
                            <button class="btn btn-primary btn-sm ms-auto" data-bind="click: saveCanvas">Save Changes</button>
                        </div>

                        <canvas id="report-header" width="900" height="120" style="border: solid 1px #ccc"></canvas>

                    </div>
                </div>
                <div data-bind="ifnot: designingHeader">
                    <div class="btn-group pull-right" role="group">
                        <button type="button" class="btn btn-light" data-bs-toggle="tooltip" title="List View"
                                data-bind="css: { 'active': layout() === 'list' }, click: toggleLayout">
                            <span class="fa fa-list"></span>
                        </button>
                        <button type="button" class="btn btn-light" data-bs-toggle="tooltip" title="Icon View"
                                data-bind="css: { 'active': layout() === 'icons' }, click: toggleLayout">
                            <span class="fa fa-th-large"></span>
                        </button>
                    </div>

                    <div data-bind="if: layout()=='icons'">
                        <div data-bind="visible: !SelectedFolder() && !searchReports()">
                            <p>Please choose Folders below to view Reports</p>
                            <ul class="folder-list" data-bind="foreach: Folders">
                                <li data-bind="click: function(){ $parent.SelectedFolder($data); }">
                                    <span class="fa fa-3x fa-folder text-secondary"></span>
                                    <span class="desc" data-bind="text: FolderName"></span>
                                </li>
                            </ul>
                        </div>
                        <div data-bind="visible: SelectedFolder() || searchReports()">
                            <div class="clearfix">
                                <p class="pull-left">Please choose a Report from this Folder</p>
                                <div class="pull-right">
                                    <a href="#" class="btn btn-light" data-bind="click: function(){ SelectedFolder(null); searchReports(''); $('#search-report').val([]).trigger('change');}">
                                        ..back to Folders
                                    </a>
                                </div>
                            </div>
                            <div class="list-group list-overflow">
                                <div data-bind="if: SelectedFolder()!=null && reportsInFolder().length==0">
                                    This folder is empty.
                                </div>
                                <div data-bind="if: searchReports() && reportsInSearch().length==0">
                                    No Reports found matching your Search
                                </div>
                                <div class="list-group" data-bind="foreach: searchReports() ? reportsInSearch() : reportsInFolder()">
                                    <div class="list-group-item">
                                        <div class="row">
                                            <div class="col-sm-7">
                                                <div class="fa fa-2x pull-left" data-bind="css: {'fa-file': reportType=='List', 'fa-th-list': reportType=='Summary', 'fa-bar-chart': reportType=='Bar', 'fa-pie-chart': reportType=='Pie',  'fa-line-chart': reportType=='Line', 'fa-globe': reportType.indexOf('Map')==0, 'fa-window-maximize': reportType=='Single', 'fa-random': reportType=='Pivot', 'fa-window-restore': reportType=='Treemap', 'fa-signal': reportType == 'Combo'}"></div>
                                                <div class="pull-left">
                                                    <h4>
                                                        <a data-bind="click: runReport" style="cursor: pointer">
                                                            <span data-bind="highlightedText: { text: reportName, highlight: $parent.searchReports, css: 'highlight' }"></span>
                                                        </a>
                                                    </h4>
                                                </div>
                                                <div class="clearfix"></div>
                                                <p data-bind="text: reportDescription"></p>
                                                <p data-bind="if: $parent.searchReports()">
                                                    <span class="fa fa-folder"></span> <span data-bind="text: folderName"></span>
                                                    <div>
                                                        <span data-bind="text: message" class="highlight small"></span>
                                                    </div>
                                                </p>
                                                <div data-bind="if: $parent.adminMode">
                                                    <div class="small">
                                                        <b>Report Access</b><br />
                                                        Manage by User <span class="badge text-bg-info text-white" data-bind="text: userId ? userId : 'Any User'"></span>
                                                        <br />
                                                        View only by User <span class="badge text-bg-info text-white" data-bind="text: (viewOnlyUserId ? viewOnlyUserId : (userId ? userId : 'Any User'))"></span>
                                                        <br />
                                                        <div data-bind="if: deleteOnlyUserId">
                                                            Delete only by User <span class="badge text-bg-info text-white" data-bind="text: deleteOnlyUserId"></span>
                                                            <br />
                                                        </div>
                                                        <div data-bind="if: userRole">
                                                            Manage by Role <span class="badge text-bg-info text-white" data-bind="text: userRole ? userRole : 'No Role'"></span>
                                                            <br />
                                                        </div>
                                                        <div data-bind="if: viewOnlyUserRole">
                                                            View only by Role <span class="badge text-bg-info text-white" data-bind="text: viewOnlyUserRole ? viewOnlyUserRole : 'Any Role'"></span>
                                                            <br />
                                                        </div>
                                                        <div data-bind="if: deleteOnlyUserRole">
                                                            Delete only by Role <span class="badge text-bg-info text-white" data-bind="text: deleteOnlyUserRole ? deleteOnlyUserRole : 'Same as Manage'"></span>
                                                            <br />
                                                        </div>
                                                        <div data-bind="if: clientId">
                                                            For Client <span class="label label-info" data-bind="text: clientId ? clientId : 'Any'"></span>
                                                            <br />
                                                        </div>
                                                        <div>
                                                            Direct Link to Run Report: <a data-bind="attr: {href: '/DotNetReport/Report?linkedreport=true&noparent=true&reportId=' + reportId }" target="_blank"><span class="fa fa-link"></span></a>
                                                            &nbsp;<a href="#" data-bind="click: navigator.clipboard.writeText(window.location.href + '/Report?linkedreport=true&noparent=true&reportId=' + reportId )"><span class="fa fa-copy" title="Click to Copy Link"></span></a>
                                                        </div>
                                                    </div>
                                                </div>
                                            </div>
                                            <div class="col-sm-5 right-align">
                                                <div class="d-none d-md-block">
                                                    <br />
                                                    <span data-bind="if: $root.CanSaveReports() || $root.adminMode() ">
                                                        <button class="btn btn-sm btn-outline-secondary" data-bind="click: openReport, visible: canEdit" data-bs-toggle="modal" data-bs-target="#modal-reportbuilder">
                                                            <span class="fa fa-edit" aria-hidden="true"></span> Edit
                                                        </button>
                                                        <button class="btn btn-sm btn-outline-secondary" data-bind="click: copyReport" data-bs-toggle="modal" data-bs-target="#modal-reportbuilder">
                                                            <span class="fa fa-copy" aria-hidden="true"></span> Copy
                                                        </button>
                                                    </span>
                                                    <div class="btn-group">
                                                        <button type="button" class="btn btn-sm btn-outline-secondary" data-bind="click: runReport">
                                                            <span class="fa fa-gears" aria-hidden="true"></span> Run
                                                        </button>
                                                        <button type="button" class="btn btn-sm btn-outline-secondary dropdown-toggle dropdown-toggle-split" data-bs-toggle="dropdown" style="margin-left: -4px;">
                                                            <span class="sr-only"></span>
                                                        </button>
                                                        <div class="dropdown-menu dropdown-menu-right">
                                                            <a class="dropdown-item" href="#" data-bind="click: function() {exportReport('pdf');}">
                                                                <span class="fa fa-file-excel-o"></span> Download PDF
                                                            </a>
                                                            <a class="dropdown-item" href="#" data-bind="click: function() {exportReport('excel');}">
                                                                <span class="fa fa-file-excel-o"></span> Download Excel
                                                            </a>
                                                            <a class="dropdown-item" href="#" data-bind="visible: hasDrilldown, click: function() {exportReport('excel-sub');}">
                                                                <span class="fa fa-file-excel-o"></span> Download Excel (Expanded)
                                                            </a>
                                                            <a class="dropdown-item" href="#" data-bind="visible:$parent.adminMode, click: function() {exportReport('json');}">
                                                                <span class="fa fa-file"></span> Export Json
                                                            </a>
                                                        </div>

                                                    </div>

                                                    <button class="btn btn-sm btn-outline-secondary" data-bind="click: deleteReport, visible: canDelete">
                                                        <span class="fa fa-trash" aria-hidden="true"></span> Delete
                                                    </button>
                                                    <br />
                                                </div>

                                                <div class="d-block d-md-none">
                                                    <span data-bind="if: $root.CanSaveReports() || $root.adminMode()">
                                                        <button class="btn btn-sm btn-outline-secondary" title="Edit Report" data-bind="click: openReport, visible: canEdit" data-bs-toggle="modal" data-bs-target="#modal-reportbuilder">
                                                            <span class="fa fa-edit" aria-hidden="true"></span>
                                                        </button>
                                                        <button class="btn btn-sm btn-light" data-bind="click: copyReport" title="Copy Report" data-bs-toggle="modal" data-bs-target="#modal-reportbuilder">
                                                            <span class="fa fa-copy" aria-hidden="true"></span>
                                                        </button>
                                                    </span>

                                                    <div class="btn-group">
                                                        <button class="btn btn-sm btn-outline-secondary" data-bind="click: runReport" title="Run Report">
                                                            <span class="fa fa-gears" aria-hidden="true"></span>
                                                        </button>
                                                        <button type="button" class="btn btn-sm btn-outline-secondary dropdown-toggle dropdown-toggle-split" data-bs-toggle="dropdown" style="margin-left: -4px;">
                                                            <span class="sr-only"></span>
                                                        </button>
                                                        <div class="dropdown-menu">
                                                            <a class="dropdown-item" href="#" data-bind="click: function() {exportReport('excel');}">
                                                                <span class="fa fa-file-excel-o"></span> Download Excel
                                                            </a>
                                                            <a class="dropdown-item" href="#" data-bind="visible: hasDrilldown, click: function() {exportReport('excel-sub');}">
                                                                <span class="fa fa-file-excel-o"></span> Download Excel with inner rows
                                                            </a>
                                                        </div>
                                                    </div>

                                                    <button class="btn btn-sm btn-outline-secondary" title="Delete Report" data-bind="click: deleteReport, visible: canDelete">
                                                        <span class="fa fa-trash" aria-hidden="true"></span>
                                                    </button>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div data-bind="if: layout()=='list'">
                        <div class="file-explorer-container">
                            <nav class="breadcrumb">
                                <a href="#" class="breadcrumb-item" data-bind="click: function(){ SelectedFolder(null); searchReports(''); }">
                                    All Folders
                                </a>
                                <span class="breadcrumb-item active" data-bind="visible: SelectedFolder && !searchReports(), text: SelectedFolder()?.FolderName"></span>
                                <span class="breadcrumb-item active" data-bind="visible: searchReports">
                                    Search Results
                                </span>
                            </nav>

                            <div data-bind="visible: !SelectedFolder() && !searchReports()">
                                <div class="container-fluid">
                                    <div class="row fw-bold border-bottom py-2 w-100">
                                        <div class="col-md-6">Name</div>
                                        <div class="col-md-6 text-end" data-bind="visible: adminMode">Access Control</div>
                                    </div>
                                    <div class="folder-list" data-bind="foreach: Folders">
                                        <div class="row clickable py-2 border-bottom align-items-center w-100" data-bind="click: function(){ $parent.SelectedFolder($data); }">
                                            <div class="col-md-6 d-flex align-items-center">
                                                <span class="fa fa-folder text-secondary me-2"></span>
                                                <span data-bind="text: FolderName"></span>
                                            </div>
                                            <div class="col-md-6 text-end" data-bind="visible: $parent.adminMode">
                                                <!--<div class="badge bg-info text-white" data-bind="text: userId ? userId : 'Any User'"></div>
                                                    <div class="badge bg-info text-white" data-bind="text: userRole ? userRole : 'No Role'"></div>-->
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>

                            <div data-bind="visible: SelectedFolder() || searchReports()">
                                <div class="container-fluid">
                                    <div data-bind="if: SelectedFolder()!=null && reportsInFolder().length==0">
                                        <p class="text-muted">This folder is empty.</p>
                                    </div>

                                    <div data-bind="if: searchReports() && reportsInSearch().length==0">
                                        <p class="text-center text-muted">No reports found matching your search.</p>
                                    </div>
                                    <div data-bind="if: reportsInFolder().length > 0 || reportsInSearch().length > 0">
                                        <div class="row fw-bold border-bottom py-2 w-100">
                                            <div class="col-md-4">Name</div>
                                            <div class="col-md-1"></div>
                                            <div class="col-md-2" data-bind="visible: searchReports()">Folder</div>
                                            <div class="col-md-2"></div>
                                            <div class="col-md-2" data-bind="visible: adminMode">Access</div>
                                            <div class="col-md-1" data-bind="visible: adminMode">Direct Link</div>
                                        </div>

                                        <div class="report-list" data-bind="foreach: searchReports() ? reportsInSearch() : reportsInFolder()">
                                            <div class="row border-bottom py-2 align-items-center w-100">
                                                <div class="col-md-4 d-flex align-items-center">
                                                    <span class="fa" data-bind="css: {
                                                    'fa-file': reportType=='List',
                                                    'fa-th-list': reportType=='Summary',
                                                    'fa-bar-chart': reportType=='Bar',
                                                    'fa-pie-chart': reportType=='Pie',
                                                    'fa-line-chart': reportType=='Line',
                                                    'fa-signal': reportType=='Combo',
                                                    'fa-globe': reportType.indexOf('Map')==0,
                                                    'fa-window-maximize': reportType=='Single',
                                                    'fa-random': reportType=='Pivot',
                                                    'fa-window-restore': reportType=='Treemap'
                                                }"></span>
                                                    <a class="ms-2" data-bind="click: runReport, highlightedText: { text: reportName, highlight: $parent.searchReports, css: 'highlight' }" style="cursor: pointer"></a>&nbsp;
                                                    <span data-bind="text: message" class="highlight small"></span>
                                                </div>

                                                <div class="col-md-1">
                                                    <div class="dropdown">
                                                        <button class="btn btn-sm" type="button" data-bs-toggle="dropdown" aria-expanded="false">
                                                            <span class="fa fa-ellipsis-h"></span>
                                                        </button>
                                                        <ul class="dropdown-menu">
                                                            <li>
                                                                <a class="dropdown-item" href="#" data-bs-toggle="modal" data-bs-target="#modal-reportbuilder" data-bind="click: openReport, visible: canEdit">
                                                                    <span class="fa fa-edit"></span> Edit
                                                                </a>
                                                            </li>
                                                            <li>
                                                                <a class="dropdown-item" href="#" data-bs-toggle="modal" data-bs-target="#modal-reportbuilder" data-bind="click: copyReport">
                                                                    <span class="fa fa-copy"></span> Copy
                                                                </a>
                                                            </li>
                                                            <li>
                                                                <a class="dropdown-item text-danger" href="#" data-bind="click: deleteReport, visible: canDelete">
                                                                    <span class="fa fa-trash"></span> Delete
                                                                </a>
                                                            </li>
                                                            <li><hr class="dropdown-divider"></li>
                                                            <li>
                                                                <a class="dropdown-item" href="#" data-bind="click: runReport">
                                                                    <span class="fa fa-gears"></span> Run Report
                                                                </a>
                                                            </li>
                                                            <li class="dropdown-submenu">
                                                                <a class="dropdown-item dropdown-toggle" href="#">Export</a>
                                                                <ul class="dropdown-menu">
                                                                    <li>
                                                                        <a class="dropdown-item" href="#" data-bind="click: function() {exportReport('pdf');}">
                                                                            <span class="fa fa-file-excel-o"></span> Download PDF
                                                                        </a>
                                                                    </li>
                                                                    <li>
                                                                        <a class="dropdown-item" href="#" data-bind="click: function() {exportReport('excel');}">
                                                                            <span class="fa fa-file-excel-o"></span> Download Excel
                                                                        </a>
                                                                    </li>
                                                                    <li>
                                                                        <a class="dropdown-item" href="#" data-bind="visible: hasDrilldown, click: function() {exportReport('excel-sub');}">
                                                                            <span class="fa fa-file-excel-o"></span> Download Excel (Expanded)
                                                                        </a>
                                                                    </li>
                                                                    <li>
                                                                        <a class="dropdown-item" href="#" data-bind="visible:$parent.adminMode, click: function() {exportReport('json');}">
                                                                            <span class="fa fa-file"></span> Export JSON
                                                                        </a>
                                                                    </li>
                                                                </ul>
                                                            </li>
                                                        </ul>
                                                    </div>

                                                </div>
                                                <div class="col-md-2" data-bind="text: folderName, visible: $parent.searchReports()"></div>
                                                <div class="col-md-2"></div>
                                                <div class="col-md-2" data-bind="visible: $parent.adminMode">
                                                    <div class="badge bg-info text-white" data-bind="text: userId ? userId : 'Any User'"></div>
                                                    <div class="badge bg-info text-white" data-bind="text: userRole ? userRole : 'No Role'"></div>
                                                </div>
                                                <div class="col-md-1" data-bind="visible: $parent.adminMode">
                                                    <a data-bind="attr: {href: '/DotNetReport/Report?linkedreport=true&noparent=true&reportId=' + reportId }" target="_blank"><span class="fa fa-link"></span></a>
                                                    &nbsp;
                                                    <a href="#" data-bind="click: function() { navigator && navigator.clipboard && navigator.clipboard.writeText('/DotNetReport/Report?linkedreport=true&noparent=true&reportId=' + reportId) }">
                                                        <span class="fa fa-copy" title="Copy Link"></span>
                                                    </a>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <div data-bind="if: ReportMode() == 'execute' || ReportMode() == 'Linked'">
        <div class="card">
            <div class="card-header">
                <nav class="navbar navbar-expand-lg navbar-light bg-light">
                    <a class="navbar-brand" href="#">Viewing Report</a>
                    <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target="#navbarSupportedContent" aria-controls="navbarSupportedContent" aria-expanded="false" aria-label="Toggle navigation">
                        <span class="navbar-toggler-icon"></span>
                    </button>
                    <div class="collapse navbar-collapse" id="navbarSupportedContent">
                        <ul class="navbar-nav me-auto"></ul>
                        <div class="d-flex align-items-center gap-2">
                            <button class="btn btn-light btn-sm" data-bind="click: function() {$root.ReportMode('start'); $root.textQuery.setupSearch();}">
                                <i class="fa fa-arrow-left"></i>
                                <span>Back to Reports</span>
                            </button>
                            <button class="btn btn-light btn-sm" data-bind="visible: ReportMode()=='linked'">
                                <i class="fa fa-arrow-circle-left"></i>
                                <span>Back to Parent Report</span>
                            </button>
                            <a href="#" class="btn btn-light btn-sm" data-bind="visible: $root.CanEdit(), click: function(){SaveReport(true);}" data-bs-toggle="modal" data-bs-target="#modal-reportbuilder">
                                <i class="fa fa-edit"></i>
                                <span>Edit Report</span>
                            </a>
                            <button class="btn btn-light btn-sm" data-bind="click: RefreshReport, visible: false">
                                <i class="fa fa-refresh"></i>
                                <span>Refresh</span>
                            </button>
                            <button class="btn btn-light btn-sm" data-bind="visible: $root.CanEdit(), click: SaveWithoutRun">
                                <i class="fa fa-save"></i>
                                <span>Save Report</span>
                            </button>
                            <div class="dropdown" data-bind="hidden: DontExecuteOnRun() && !reportRan()">
                                <button type="button" class="btn btn-light btn-sm dropdown-toggle" data-bs-toggle="dropdown" aria-expanded="false">
                                    <i class="fa fa-download"></i>
                                    <span>Export</span>
                                </button>
                                <ul class="dropdown-menu dropdown-menu-end">
                                    <li data-bind="hidden: appSettings.useAltPdf">
                                        <a href="#" class="dropdown-item" data-bind="click: function() {downloadPdf(false);}">
                                            <i class="fa fa-file-pdf-o"></i> Pdf
                                        </a>
                                    </li>
                                    <li data-bind="visible: adminMode() && !appSettings.useAltPdf">
                                        <a href="#" class="dropdown-item" data-bind="click: function() {downloadPdf(true);}">
                                            <i class="fa fa-file-pdf-o"></i> Pdf (Debug)
                                        </a>
                                    </li>
                                    <li data-bind="visible: appSettings.useAltPdf">
                                        <a href="#" class="dropdown-item" data-bind="click: function() {downloadPdfAlt();}">
                                            <i class="fa fa-file-pdf-o"></i> Pdf
                                        </a>
                                    </li>
                                    <li>
                                        <a href="#" class="dropdown-item" data-bind="click: downloadExcel">
                                            <i class="fa fa-file-excel-o"></i> Excel
                                        </a>
                                    </li>
                                    <li data-bind="visible: canDrilldown">
                                        <a class="dropdown-item" href="#" data-bind="click: downloadExcelWithDrilldown">
                                            <i class="fa fa-file-excel-o"></i> Excel (Expanded)
                                        </a>
                                    </li>
                                    <li>
                                        <a class="dropdown-item" href="#" data-bind="click: downloadWord">
                                            <span class="fa fa-file-word-o"></span> Word
                                        </a>
                                    </li>
                                    <li>
                                        <a href="#" class="dropdown-item" data-bind="click: downloadCsv">
                                            <i class="fa fa-file-excel-o"></i> Csv
                                        </a>
                                    </li>
                                    <li data-bind="hidden: appSettings.dontXmlExport">
                                        <a href="#" class="dropdown-item" data-bind="click: downloadXml">
                                            <i class="fa fa-file-code-o"></i> Xml
                                        </a>
                                    </li>
                                </ul>
                            </div>
                        </div>
                    </div>
                </nav>
            </div>
            <div class="card-body">
                <div data-bind="with: ReportResult" class="report-view">
                    <div data-bind="ifnot: HasError">
                        <div data-bind="with: $root">
                            <div>
                            <div data-bind="if: EditFiltersOnReport">
                                <div class="card">
                                    <div class="card-header">
                                        <a data-bs-toggle="collapse" data-bs-target="#filter-panel" href="#">
                                            <i class="fa fa-filter"></i>Choose Filters
                                        </a>
                                    </div>
                                    <div id="filter-panel" class="card-body collapse">
                                        <div data-bind="if: useStoredProc">
                                            <div class="row">
                                                <div data-bind="template: {name: 'filter-parameters'}" class="col-md-12"></div>
                                            </div>
                                        </div>
                                        <div data-bind="ifnot: useStoredProc">
                                            <div class="row">
                                                <div data-bind="template: {name: 'filter-group'}" class="col-md-12"></div>
                                            </div>
                                        </div>
                                        <br />
                                        <button class="btn btn-primary" data-bind="click: SaveFilterAndRunReport">Update Filters</button>
                                    </div>
                                </div>
                                <br />
                            </div>
                            <div data-bind="ifnot: EditFiltersOnReport">
                                <div data-bind="template: {name: 'fly-filter-template'}"></div>
                                <br />
                            </div>
                            </div>
                            <div class="report-render" data-bind="css: { 'report-expanded': isExpanded }">
                                <div class="report-menubar" data-bind="hidden: DontExecuteOnRun() && !reportRan()">
                                    <div class="col-xs-12 col-centered" data-bind="with: pager">
                                        <div class="form-inline pull-left" data-bind="visible: pages()">
                                            <div class="form-group pull-left total-records">
                                                <span data-bind="text: ' Total Records: ' + totalRecords()"></span><br />
                                            </div>
                                            <div class="pull-left">
                                                <button class="btn btn-secondary btn-sm" data-bind="visible: !$root.isChart() || $root.ShowDataWithGraph(), click: $root.downloadExcel" title="Export to Excel">
                                                    <span class="fa fa-file-excel-o"></span>
                                                </button>
                                                <button class="btn btn-secondary btn-sm" data-bind="click: $parent.toggleExpand">
                                                    <span class="fa" data-bind="css: {'fa-expand': !$parent.isExpanded(), 'fa-compress': $parent.isExpanded() }"></span>
                                                </button>
                                                <button class="btn btn-secondary btn-sm" data-bind="visible: $parent.canDrilldown, click: $parent.ExpandAll" title="Expand all rows">
                                                    <span class="fa fa-plus"></span>
                                                </button>
                                                <button class="btn btn-secondary btn-sm" data-bind="visible: $parent.canDrilldown, click: $parent.CollapseAll" title="Collapse all rows">
                                                    <span class="fa fa-minus"></span>
                                                </button>
                                                <div data-bind="with: $parent" class="pull-left">
                                                    <div class="dropdown-selected" data-bind="click: toggleDropdown">
                                                        <div data-bind="with: selectedTableStyle" class="btn btn-sm btn-secondary" title="Format Table">
                                                            <span class="fa fa- fa-paint-brush"></span>
                                                        </div>
                                                    </div>
                                                    <div class="dropdown-content" style="position: absolute;" data-bind="visible: dropdownOpen">
                                                        <!-- ko foreach: tableStyles -->
                                                        <div class="dropdown-option" data-bind="title: $data.name, click: $parent.selectStyle">
                                                            <div class="mini-table-preview" data-bind="foreach: new Array(5)">
                                                                <div class="mini-table-row" data-bind="foreach: new Array(5)">
                                                                    <div class="mini-table-cell" data-bind="style: { backgroundColor: $parentContext.$index() === 0 ? $parents[1].headerBg : ($parentContext.$index() % 2 === 0 ? $parents[1].altRowBg : $parents[1].rowBg), color: $parents[1].textColor }"></div>

                                                                </div>
                                                            </div>
                                                        </div>
                                                        <!-- /ko -->
                                                    </div>
                                                </div>
                                                <div data-bind="with: $parent" class="pull-left">
                                                    <div  data-bind="template: 'table-settings', data: $data"></div>
                                                </div>
                                            </div>
                                        </div>
                                        <div class="form-inline pull-right">
                                            <div data-bind="template: 'pager-template', data: $data"></div>
                                        </div>
                                        <div class="btn-group btn-group-sm pull-right" role="group">
                                            <button class="btn btn-light" data-bs-toggle="modal" data-bs-target="#sqlModal" data-bind="visible: $parent.adminMode">
                                                <i class="fa fa-code"></i> View Report Code
                                            </button>
                                            <button class="btn btn-light" data-bind="click: $parent.zoomOut" data-bs-toggle="tooltip" title="Zoom Out">
                                                <i class="fa fa-search-minus"></i>
                                            </button>
                                            <button class="btn btn-light" data-bind="click: $parent.resetZoom" data-bs-toggle="tooltip" title="Reset Zoom">
                                                <i class="fa fa-compress"></i>
                                            </button>
                                            <button class="btn btn-light" data-bind="click: $parent.zoomIn" data-bs-toggle="tooltip" title="Zoom In">
                                                <i class="fa fa-search-plus"></i>
                                            </button>
                                        </div>
                                    </div>
                                </div>
                                <div class="report-canvas" data-bind="hidden: DontExecuteOnRun() && !reportRan()">
                                    <div class="report-container">
                                        <div class="report-inner">
                                            <div data-bind="template: 'chart-settings', data: $data"></div>
                                            <div class="canvas-container">
                                                <canvas id="report-header" width="900" height="120" data-bind="visible: useReportHeader"></canvas>
                                            </div>
                                            <h2 data-bind="text: ReportName"></h2>
                                            <p data-bind="html: ReportDescription">
                                            </p>

                                            <div data-bind="with: ReportResult" class="report-expanded-scroll">
                                                <div data-bind="visible: !ReportData()">
                                                    <div class="report-spinner"></div>
                                                </div>
                                                <div data-bind="template: 'report-template', data: $data"></div>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                            <br />
                            <span>Report ran on: <span data-bind="text: new Date().toLocaleDateString() + ' ' + new Date().toLocaleTimeString()"></span></span>
                        </div>
                    </div>
                    <div data-bind="if: HasError">
                        <h2 data-bind="text: $root.ReportName"></h2>
                        <p data-bind="text: $root.ReportDescription"></p>

                        <h3>An unexpected error occured while running the Report</h3>
                        <hr />
                        <b>Error Details</b>
                        <div data-bind="text: Exception"></div>
                        <br />
                        <button class="btn btn-light" data-bs-toggle="modal" data-bs-target="#sqlModal" data-bind="visible: $parent.adminMode">
                            <i class="fa fa-code"></i> View Report Code
                        </button>
                        <button class="btn btn-light" data-bind="click: $root.RunReport">
                            <i class="fa fa-play"></i> Re-run Report
                        </button>
                        <hr />
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>

<!-- Report Builder -->
<div class="modal modal-fullscreen" id="modal-reportbuilder" role="dialog" aria-labelledby="myModalLabel" aria-hidden="true" style="padding-right: 0px !important;">
    <div data-bind="template: {name: 'report-designer', data: $data}"></div>
</div>

<!-- Field Options Modal -->
<div class="modal" id="fieldOptionsModal" tabindex="-1" role="dialog" aria-hidden="true">
    <div data-bind="template: {name: 'report-field-options', data: $data}"></div>
</div>

<!-- Link Edit Modal -->
<div class="modal" id="linkModal" tabindex="-1" role="dialog" aria-hidden="true">
    <div data-bind="template: {name: 'report-link-edit', data: $data}"></div>
</div>

<!-- Folder Edit Modal -->
<div class="modal" id="folderModal" tabindex="-1" role="dialog" aria-hidden="true">
    <div class="modal-dialog" data-bind="css: {'modal-lg': adminMode}">
        <div class="modal-content" data-bind="with: ManageFolder">
            <div class="modal-header">
                <h5 class="modal-title  fs-5">Manage Folder</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
            </div>
            <div class="modal-body">
                <div class="form-group">
                    <label class="col-sm-4 col-md-4 control-label">Folder Name</label>
                    <div class="col-sm-8 col-md-8">
                        <input type="text" class="form-control" id="folderName" required placeholder="Folder Name" data-bind="value: FolderName">
                    </div>
                </div>
                <br />
                <div data-bind="visible: $root.adminMode" class="card card-body">
                    <div data-bind="template: {name: 'manage-access-template', data: $root.manageFolderAccess}"></div>
                </div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                <button type="button" class="btn btn-primary" data-bind="click: saveFolder">Save</button>
            </div>
        </div>
    </div>
</div>

<!-- Import Json File Modal -->
<div class="modal" id="uploadFileModal" tabindex="-1" aria-labelledby="uploadFileModalLabel" aria-hidden="true">
    <div class="modal-dialog">
        <div class="modal-content" data-bind="with: ManageJsonFile">
            <div class="modal-header">
                <h5 class="modal-title" id="uploadFileModalLabel">Import JSON File Upload</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
            </div>
            <div class="modal-body">
                <div id="dropzone" class="dropzone"
                     data-bind="event: {click: triggerFileInput }"
                     style="border: 2px dashed #007bff; border-radius: 5px; padding: 30px; text-align: center; color: #007bff; cursor: pointer;">
                    click to select files
                </div>
                <input type="file" id="fileInputJson" accept=".json" style="display: none;" data-bind="event: { change: handleFileSelect }">
                <div data-bind="visible: fileName">
                    <p>Selected File: <span data-bind="text: fileName"></span></p>
                </div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
                <button type="button" class="btn btn-primary" data-bind="click: uploadFile">Upload</button>
            </div>
        </div>
    </div>
</div>

</asp:Content>