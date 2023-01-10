﻿<%@ Page Title="" Language="C#" MasterPageFile="~/DotNetReport/ReportLayout.Master" AutoEventWireup="true" CodeBehind="Index.aspx.cs" Inherits="ReportBuilder.WebForms.DotNetReport.Index" %>

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
                    linkModal: $("#linkModal"),
                    reportHeader: "report-header",
                    fieldOptionsModal: $("#fieldOptionsModal"),
                    lookupListUrl: svc + "GetLookupList",
                    apiUrl: svc + "CallReportApi",
                    runReportApiUrl: svc + "RunReportApi",
                    getUsersAndRolesUrl: svc + "GetUsersAndRoles",
                    reportId: queryParams.reportId || 0,
                    userSettings: data,
                    dataFilters: data.dataFilters
                });

                vm.init(queryParams.folderid || 0, data.noAccount);
                ko.applyBindings(vm);
            });
        });

    </script>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="body" runat="server">

<div class="row">
    <div class="col-md-6">
        <p>
            Manage your Reports below. You can use the intuitive and user friendly Report Builder to create many types of Ad-hoc Reports.
        </p>
    </div>
    <div class="col-md-6">
        <div class="pull-right">
            <a href="/DotnetReport/Dashboard.aspx")">View Dashboard</a> | Learn how to <a href="https://dotnetreport.com/getting-started-with-dotnet-report/" target="_blank">Integrate in your App here</a>.
        </div>
    </div>

</div>
<div data-bind="template: {name: 'admin-mode-template'}, visible: allowAdmin" style="display: none;"></div>

<!--
    The markup code below is related to presentation. You don't have to change it, unless you
    intentionally want to change something in the Report Builder's behavior in your Application.
    It's Recommended you use it as is. You will be responsible for managing and maintaining any changes.
-->

<!-- Folders/Report List -->
<div id="report-start">
    <div class="card folder-panel">
        <div class="card-header">
            <nav class="navbar navbar-expand-lg navbar-light bg-light">
                <a class="navbar-brand" href="#" data-bind="click: function() {SelectedFolder(null); designingHeader(false); }">Manage Reports</a>
                <button class="navbar-toggler" type="button" data-toggle="collapse" data-target="#navbarSupportedContent" aria-controls="navbarSupportedContent" aria-expanded="false" aria-label="Toggle navigation">
                    <span class="navbar-toggler-icon"></span>
                </button>

                <div class="collapse navbar-collapse" id="navbarSupportedContent">
                    <ul class="navbar-nav mr-auto">

                        <li class="nav-item active" data-bind="visible: CanSaveReports() || adminMode()">
                            <a href="#" class="nav-link" data-bind="click: createNewReport" data-toggle="modal" data-target="#modal-reportbuilder">
                                <span class="fa fa-plus"></span> Create a New Report
                            </a>
                        </li>

                        <li class="nav-item" data-bind="visible: CanManageFolders() || adminMode()">
                            <a href="#" class="nav-link" data-bind="click: ManageFolder.newFolder">
                                <span class="fa fa-folder-o"></span> Add a new Folder
                            </a>
                        </li>
                        <li class="nav-item dropdown" data-bind="visible: (CanManageFolders() || adminMode()) && SelectedFolder()!=null">
                            <a href="#" class="nav-link dropdown-toggle" role="button" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false">
                                <span class="fa fa-folder"></span> Folder Options
                            </a>
                            <div class="dropdown-menu">
                                <a class="dropdown-item" href="#" data-bind="click: ManageFolder.editFolder">Edit Selected Folder</a>
                                <a class="dropdown-item" href="#" data-bind="click: ManageFolder.deleteFolder">Delete Selected Folder</a>
                            </div>
                        </li>
                        <li class="nav-item active">
                            <a href="#" class="nav-link" data-bind="click: function(){ initHeaderDesigner(); }">
                                <span class="fa fa-arrow-up"> Report Header</span>
                            </a>
                        </li>
                    </ul>
                    <div class="form-inline my-3 my-md-0">
                        <input class="form-control" type="text" placeholder="Search Reports" aria-label="Search" data-bind="textInput: searchReports">
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
                    <canvas id="report-header" width="900" height="120" style="border: solid 1px #ccc"></canvas>
                    <div class="form-inline">
                        <div class="form-group">
                            <button class="btn btn-sm" title="Add Text" data-bind="click: addText"><span class="fa fa-font"></span></button>
                            <label class="btn btn-sm" title="Add Image">
                                <span class="fa fa-image"></span>
                                <input type="file" accept="image/*" hidden data-bind="event: { change: function() { uploadImage($element.files[0]) } }" />
                            </label>
                            <button class="btn btn-sm" title="Add Line" data-bind="click: addLine"><span class="fa fa-arrows-h"></span></button>
                        </div>
                        <div data-bind="if: selectedObject()">
                            <div class="form-group">
                                &nbsp;|&nbsp;
                                <button class="btn btn-sm" data-bind="click: remove" title="Delete"><span class="fa fa-trash"></span></button>
                                <div data-bind="if: getText()">
                                    <select class="form-control form-control-sm" title="Font Family" data-bind="event: {change: setFontFamily }, value: objectProperties.fontFamily">
                                        <option value="arial" selected>Arial</option>
                                        <option value="helvetica">Helvetica</option>
                                        <option value="myriad pro">Myriad Pro</option>
                                        <option value="delicious">Delicious</option>
                                        <option value="verdana">Verdana</option>
                                        <option value="georgia">Georgia</option>
                                        <option value="courier">Courier</option>
                                        <option value="comic sans ms">Comic Sans MS</option>
                                        <option value="impact">Impact</option>
                                        <option value="monaco">Monaco</option>
                                        <option value="optima">Optima</option>
                                        <option value="hoefler text">Hoefler Text</option>
                                        <option value="plaster">Plaster</option>
                                        <option value="engagement">Engagement</option>
                                    </select>
                                    &nbsp;
                                    <select class="form-control form-control-sm" title="Text Align" data-bind="event: {change: setTextAlign }, value: objectProperties.textAlign">
                                        <option>Left</option>
                                        <option>Center</option>
                                        <option>Right</option>
                                        <option>Justify</option>
                                    </select>
                                    &nbsp;
                                    <input type="color" size="5" class="btn-object-action" title="Font Color" data-bind="event: {change: setFontColor }, value: objectProperties.fontColor">
                                    <button class="btn btn-sm" title="Bold" data-bind="event: {click: setFontBold }, value: objectProperties.fontBold"><span class="fa fa-bold"></span></button>
                                    <button class="btn btn-sm" title="Italic" data-bind="event: {click: setFontItalic }, value: objectProperties.fontItalic"><span class="fa fa-italic"></span></button>
                                    <button class="btn btn-sm" title="Underline" data-bind="event: {click: setFontUnderline }, value: objectProperties.fontUnderline"><span class="fa fa-underline"></span></button>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
                <button class="btn btn-primary" data-bind="click: saveCanvas">Save Changes</button>
            </div>
            <div data-bind="ifnot: designingHeader">
                <div data-bind="visible: !SelectedFolder() && !searchReports()">
                    <p>Please choose Folders below to view Reports</p>
                    <ul class="folder-list" data-bind="foreach: Folders">
                        <li data-bind="click: function(){ $parent.SelectedFolder($data); }">
                            <span class="fa fa-3x fa-folder" style="color: #ffd800"></span>
                            <span class="desc" data-bind="text: FolderName"></span>
                        </li>
                    </ul>
                </div>
                <div data-bind="visible: SelectedFolder() || searchReports()">
                    <div class="clearfix">
                        <p class="pull-left">Please choose a Report from this Folder</p>
                        <div class="pull-right">
                            <a href="#" data-bind="click: function(){ SelectedFolder(null); }">
                                ..back to Folders List
                            </a>
                        </div>
                    </div>
                    <div class="list-group list-overflow">
                        <div data-bind="if: SelectedFolder()!=null && reportsInFolder().length==0">
                            No Reports Saved in this Folder
                        </div>
                        <div data-bind="if: searchReports() && reportsInSearch().length==0">
                            No Reports found matching your Search
                        </div>
                        <div data-bind="foreach: searchReports() ? reportsInSearch() : reportsInFolder()">
                            <div class="list-group-item">
                                <div class="row">
                                    <div class="col-sm-7">
                                        <div class="fa fa-2x pull-left" data-bind="css: {'fa-file': reportType=='List', 'fa-th-list': reportType=='Summary', 'fa-bar-chart': reportType=='Bar', 'fa-pie-chart': reportType=='Pie',  'fa-line-chart': reportType=='Line', 'fa-globe': reportType =='Map', 'fa-window-maximize': reportType=='Single', 'fa-random': reportType=='Pivot'}"></div>
                                        <div class="pull-left">
                                            <h4><a data-bind="click: runReport" style="cursor: pointer">
                                                <span data-bind="highlightedText: { text: reportName, highlight: $parent.searchReports, css: 'highlight' }"></span>
                                             </a></h4>
                                        </div>
                                        <div class="clearfix"></div>
                                        <p data-bind="text: reportDescription"></p>
                                        <p data-bind="if: $parent.searchReports()"><span class="fa fa-folder"></span> <span data-bind="text: folderName"></span></p>
                                        <div data-bind="if: $parent.adminMode">
                                            <div class="small">
                                                <b>Report Access</b><br />
                                                Manage by User <span class="badge badge-info" data-bind="text: userId ? userId : 'No User'"></span>
                                                <br />
                                                View only by User <span class="badge badge-info" data-bind="text: (viewOnlyUserId ? viewOnlyUserId : (userId ? userId : 'Any User'))"></span>
                                                <br />
                                                <div data-bind="if: deleteOnlyUserId">
                                                    Delete only by User <span class="badge badge-info" data-bind="text: deleteOnlyUserId"></span>
                                                    <br />
                                                </div>
                                                <div data-bind="if: userRole">
                                                    Manage by Role <span class="badge badge-info" data-bind="text: userRole ? userRole : 'No Role'"></span>
                                                    <br />
                                                </div>
                                                <div data-bind="if: viewOnlyUserRole">
                                                    View only by Role <span class="badge badge-info" data-bind="text: viewOnlyUserRole ? viewOnlyUserRole : 'Any Role'"></span>
                                                    <br />
                                                </div>
                                                <div data-bind="if: deleteOnlyUserRole">
                                                    Delete only by Role <span class="badge badge-info" data-bind="text: deleteOnlyUserRole ? deleteOnlyUserRole : 'Same as Manage'"></span>
                                                    <br />
                                                </div>
                                                <div data-bind="if: clientId">
                                                    For Client <span class="label label-info" data-bind="text: clientId ? clientId : 'Any'"></span>
                                                    <br />
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                    <div class="col-sm-5 right-align">
                                        <div class="d-none d-md-block">
                                            <br />
                                            <span data-bind="if: $root.CanSaveReports() || $root.adminMode() ">
                                                <button class="btn btn-sm btn-secondary" data-bind="click: openReport, visible: canEdit" data-toggle="modal" data-target="#modal-reportbuilder">
                                                    <span class="fa fa-edit" aria-hidden="true"></span>Edit
                                                </button>
                                                <button class="btn btn-sm btn-secondary" data-bind="click: copyReport" data-toggle="modal" data-target="#modal-reportbuilder">
                                                    <span class="fa fa-copy" aria-hidden="true"></span>Copy
                                                </button>
                                            </span>
                                            <button class="btn btn-sm btn-primary" data-bind="click: runReport">
                                                <span class="fa fa-gears" aria-hidden="true"></span>Run
                                            </button>
                                            <button class="btn btn-sm btn-danger" data-bind="click: deleteReport, visible: canDelete">
                                                <span class="fa fa-trash" aria-hidden="true"></span>Delete
                                            </button>
                                            <br />
                                        </div>

                                        <div class="d-block d-md-none">
                                            <span data-bind="if: $root.CanSaveReports() || $root.adminMode()">
                                                <button class="btn btn-sm btn-secondary" title="Edit Report" data-bind="click: openReport, visible: canEdit" data-toggle="modal" data-target="#modal-reportbuilder">
                                                    <span class="fa fa-edit" aria-hidden="true"></span>
                                                </button>
                                                <button class="btn btn-sm btn-secondary" data-bind="click: copyReport" title="Copy Report" data-toggle="modal" data-target="#modal-reportbuilder">
                                                    <span class="fa fa-copy" aria-hidden="true"></span>
                                                </button>
                                            </span>
                                            <button class="btn btn-sm btn-primary" data-bind="click: runReport" title="Run Report">
                                                <span class="fa fa-gears" aria-hidden="true"></span>
                                            </button>
                                            <button class="btn btn-sm btn-danger" title="Delete Report" data-bind="click: deleteReport, visible: canDelete">
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
        </div>
    </div>
</div>

<!-- Report Builder -->
<div class="modal modal-fullscreen" id="modal-reportbuilder" tabindex="-1" role="dialog" aria-labelledby="myModalLabel" aria-hidden="true" style="padding-right: 0px !important;">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title" id="myModalLabel">
                    <a title="Need help setting up a report?" target="_blank" href="https://dotnetreport.com/docs/designing-reports/">
                        <span class="fa fa-question-circle"></span>
                    </a>Design your Report
                </h5>
                <button type="button" class="close" data-dismiss="modal"><span aria-hidden="true">&times;</span><span class="sr-only">Close</span></button>
            </div>
            <div class="modal-body needs-validation">
                <h5><span class="fa fa-file"></span>&nbsp;Choose Report Type</h5>
                <div class="row">
                    <div class="col-md-3 col-sm-3 col-xs-6">
                        <div class="button-box" tabindex="0" data-bind="click: function(){ setReportType('List'); }, css: {active: ReportType()=='List'}">
                            <span class="fa fa-2x fa-list-alt"></span>
                            <p>List</p>
                        </div>
                    </div>
                    <div class="col-md-3 col-sm-3 col-xs-6">
                        <div class="button-box" tabindex="0" data-bind="click: function(){ setReportType('Summary'); }, css: {active: ReportType()=='Summary'}">
                            <span class="fa fa-2x fa-table"></span>
                            <p>Summary</p>
                        </div>
                    </div>
                    <div class="col-md-3 col-sm-3 col-xs-6">
                        <div class="button-box" tabindex="0" data-bind="click: function(){ setReportType('Bar'); }, css: {active: ReportType()=='Bar'}">
                            <span class="fa fa-2x fa-bar-chart"></span>
                            <p>Bar</p>
                        </div>
                    </div>
                    <div class="col-md-3 col-sm-3 col-xs-6">
                        <div class="button-box" tabindex="0" data-bind="click: function(){ setReportType('Pie'); }, css: {active: ReportType()=='Pie'}">
                            <span class="fa fa-2x fa-pie-chart"></span>
                            <p>Pie</p>
                        </div>
                    </div>
                    <div class="col-md-3 col-sm-3 col-xs-6">
                        <div class="button-box" tabindex="0" data-bind="click: function(){ setReportType('Line'); }, css: {active: ReportType()=='Line'}">
                            <span class="fa fa-2x fa-line-chart"></span>
                            <p>Line</p>
                        </div>
                    </div>
                    <div class="col-md-3 col-sm-3 col-xs-6">
                        <div class="button-box" tabindex="0" data-bind="click: function(){ setReportType('Map'); }, css: {active: ReportType()=='Map'}">
                            <span class="fa fa-2x fa-globe"></span>
                            <p>Map</p>
                        </div>
                    </div>
                    <div class="col-md-3 col-sm-3 col-xs-6">
                        <div class="button-box" tabindex="0" data-bind="click: function(){ setReportType('Single'); }, css: {active: ReportType()=='Single'}">
                            <span class="fa fa-2x fa-window-maximize"></span>
                            <p>Single Value</p>
                        </div>
                    </div>                    
                    <div class="col-md-3 col-sm-3 col-xs-6">
                        <div class="button-box" tabindex="0" data-bind="click: function(){ setReportType('Pivot'); }, css: {active: ReportType()=='Pivot'}">
                            <span class="fa fa-2x fa-random"></span>
                            <p>Pivot</p>
                        </div>
                    </div>
                </div>
                <hr />
                <h5 class="pull-left"><span class="fa fa-database"></span>&nbsp;Choose Data for Report</h5>
                <div class="pull-right btn-group btn-group-toggle btn-group-sm" data-toggle="buttons" role="group" data-bind="if: ReportID() <= 0">
                    <label class="btn btn-primary active" style="margin-right: 0px;" title="You can change the Data source only when creating a new report, changing this will clear all selections">
                        <input type="radio" name="dataoption" id="table" checked data-bind="checked: useStoredProc, checkedValue: false"> Dynamic
                    </label>
                    <label class="btn btn-primary">
                        <input type="radio" name="dataoption" id="proc" value="1" data-bind="checked: useStoredProc, checkedValue: true"> Predefined
                    </label>
                </div>
                <div class="clearfix"></div>
                <div class="row">
                    <div class="col-md-12">
                        <select class="form-control" data-bind="options: Procs, optionsCaption: 'Choose Section...', optionsText: 'DisplayName', value: SelectedProc, disable: isFormulaField, visible: useStoredProc"></select>
                        <select class="form-control" data-bind="options: Tables, optionsCaption: 'Choose Section...', optionsText: 'tableName', value: SelectedTable, disable: isFormulaField, hidden: useStoredProc"></select>
                    </div>

                    <div class="col-md-12 padded-div" data-bind="visible: ChooseFields().length>0">
                        <div class="small pull-left">
                            Check the fields you would like to use in the Report
                        </div>
                        <div class="pull-right btn-toolbar">
                            <a href="#" class="btn btn-sm" title="Custom Field using Formula" data-bind="click: function(){isFormulaField(!isFormulaField())}, text: isFormulaField()? 'Cancel': 'Customize', css: {'btn-primary': !isFormulaField(), 'btn-danger': isFormulaField}"></a>

                            <a href="#" class="btn btn-secondary btn-sm" data-bind="click: MoveAllFields, visible: !isFormulaField()">Select all</a>
                            <a href="#" class="btn btn-secondary btn-sm" data-bind="click: RemoveSelectedFields, visible: !isFormulaField()">Remove all</a>
                        </div>
                    </div>
                    <div class="row container-fluid" data-bind="foreach: ChooseFields">
                        <div class="col-md-3 col-sm-4 col-xs-6">
                            <div class="list-group-item">
                                <div class="checkbox">
                                    <label>
                                        <input type="checkbox" data-bind="checkedInArray: {array: $parent.isFormulaField() ? $parent.formulaFields : $parent.SelectedFields, value: $data}">
                                        <span data-bind="text: fieldName"></span>
                                    </label>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                <div data-bind="if: isFormulaField" id="custom-field-design">
                    <br />
                    <br />
                    <div class="card">
                        <div class="card-body">
                            <div class="row">
                                <div class="col-md-12 pull-right">
                                    <a href="#" class="btn btn-primary btn-sm" title="Save Custom Formula Field" data-bind="click: saveFormulaField, visible: isFormulaField">Save</a>
                                    <a href="#" class="btn btn-sm" title="Custom Field using Formula" data-bind="click: function(){isFormulaField(!isFormulaField())}, text: isFormulaField()? 'Cancel': 'Customize', css: {'btn-primary': !isFormulaField(), 'btn-danger': isFormulaField}"></a>

                                </div>
                                <div class="col-md-12 padded-div" data-bind="visible: ChooseFields().length>0 && isFormulaField()">
                                    <div class="alert alert-info">
                                        You are now creating a Customized Field. Check Fields you want to use in the calculation, then choose the operations you want to perform, and click "Save" on the right above to add your Custom field.
                                        Please note that Custom Field must be chosen in within the same Section and their data types must match.
                                    </div>
                                </div>
                            </div>
                            <div class="row">
                                <div class="col-sm-6 small">
                                    Setup your Formula for calculating this Field
                                </div>
                                <div class="col-sm-6 right-align">
                                    <a href="#" class="btn btn-secondary btn-sm" data-bind="click: addFormulaParentheses">Add ( )</a>
                                    <a href="#" class="btn btn-secondary btn-sm" title="Add a Constant Value" data-bind="click: addFormulaConstantValue, hidden: formulaOnlyHasDateFields">Add Value</a>
                                    <a href="#" class="btn btn-secondary btn-sm" data-bind="click: clearFormulaField">Clear</a>
                                </div>
                            </div>
                            <div class="form-group-row" data-bind="if: formulaOnlyHasDateFields">
                                <div class="col-sm-12">
                                    <div class="alert alert-info">
                                        For Dates Field calculation, you can only substract date fields and display the result as days, hours, minutes or seconds.
                                    </div>
                                </div>
                            </div>
                            <div class="form-group row">
                                <label class="col-sm-2 control-label">Field Label</label>
                                <div class="col-sm-10">
                                    <input type="text" class="form-control form-control-sm" data-bind="value: formulaFieldLabel" required placeholder="Custom Field Label" />
                                </div>
                            </div>
                            <div class="form-group row">
                                <label class="col-sm-2 control-label">Data Format</label>
                                <div class="col-sm-10">
                                    <select class="form-control form-control-sm" data-bind="value: formulaDataFormat" required>
                                        <!-- ko ifnot: formulaOnlyHasDateFields -->
                                        <option>String</option>
                                        <option>Integer</option>
                                        <option>Decimal</option>
                                        <option>Currency</option>
                                        <!-- /ko -->
                                        <!-- ko if: formulaOnlyHasDateFields -->
                                        <option>Days</option>
                                        <option>Hours</option>
                                        <option>Minutes</option>
                                        <option>Seconds</option>
                                        <!-- /ko -->
                                    </select>
                                </div>
                            </div>
                            <div class="form-group row" data-bind="visible: formulaDataFormat() == 'Decimal' || formulaDataFormat() == 'Currency'">
                                <label class="col-sm-2 control-label">Round to Decimal Places</label>
                                <div class="col-sm-10">
                                    <input type="number" class="form-control" data-bind="value: formulaDecimalPlaces" placeholder="Choose Decimal places (leave blank to not use rounding)" title="Choose Decimal places (leave blank to not use rounding)" />
                                </div>
                            </div>

                            <div class="form-group row">
                                <label class="col-sm-2 control-label">Field Formula</label>
                                <div class="col-sm-10">
                                    <div data-bind="foreach: formulaFields">
                                        <h3 class="pull-left" data-bind="visible: setupFormula.isParenthesesStart" style="margin-top: 0;">(</h3>
                                        <div data-bind="if: !setupFormula.isParenthesesStart() && !setupFormula.isParenthesesEnd()">
                                            <div class="list-group-item pull-left" style="margin-left: 15px; padding: 5px 15px">
                                                <span data-bind="text: fieldName, visible: !setupFormula.isConstantValue()"></span>
                                                <div data-bind="if: setupFormula.isConstantValue">
                                                    <input data-bind="value: setupFormula.constantValue" class="form-control form-control-sm" required />
                                                </div>
                                            </div>
                                        </div>
                                        <h3 class="pull-left" data-bind="visible: setupFormula.isParenthesesEnd" style="margin-top: 0;">)</h3>
                                        <div class="pull-left" style="margin-left: 15px;" data-bind="visible: $parent.showFormulaOperation($index())">
                                            <select class="form-control form-control-sm" style="max-width: 50px;" data-bind="value: setupFormula.formulaOperation">
                                                <!-- ko if: !$root.formulaOnlyHasDateFields() || ($root.formulaOnlyHasDateFields() && $root.isConstantOperation($index())) -->
                                                <option>+</option>
                                                <!-- /ko -->
                                                <option>-</option>
                                                <!-- ko if: ['Int','Money','Float','Double'].indexOf(fieldType) != -1 -->
                                                <option>*</option>
                                                <option>/</option>
                                                <!-- /ko -->
                                            </select>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="clearfix"></div>
                </div>
                <div data-bind="if: SelectedFields().length>0">
                    <hr />
                    <h5><span class="fa fa-table"></span>&nbsp;Selected data for the Report</h5>
                    <div>
                        <div class="alert alert-info" data-bind="visible: ReportType()=='Bar'">
                            <span class="fa fa-lightbulb-o fa-2x"></span>&nbsp;For Bar Graph, the first field below will show on the x-axis. All other fields will show on y-axis, but they must be numeric.
                        </div>
                        <div class="alert alert-info" data-bind="visible: ReportType()=='Line'">
                            <span class="fa fa-lightbulb-o fa-2x"></span>&nbsp;For Line Graph, the first field below will show on the x-axis and it needs to be numeric.
                        </div>
                        <div class="alert alert-info" data-bind="visible: ReportType()=='Pivot'">
                            <span class="fa fa-lightbulb-o fa-2x"></span>&nbsp;For Pivot report, the first field below will be used to Pivot/Transpose the rows as columns, and it must be grouped.
                        </div>
                        <div data-bind="visible: ReportType()=='Map'">
                            <div class="alert alert-info" >
                                <span class="fa fa-lightbulb-o fa-2x"></span>&nbsp;For Map Graph, the first field below has to be a Region, like a Country.
                            </div> 
                            <div class="form-group row">
                                <label class="col-sm-2 control-label">Map Display</label>
                                <div class="col-sm-4">
                                    <select class="form-control form-control-sm" data-bind="options: $root.mapRegions, value: mapRegion"></select>
                                </div>
                            </div>
                        </div>
                        <ul class="list-group" data-bind="sortable: { data: SelectedFields, options: { handle: '.sortable', cursor: 'move', placeholder: 'drop-highlight' }, strategyMove: true }">
                            <li class="list-group-item">
                                <div class="row">
                                    <div class="col">
                                        <span class="fa fa-columns"></span>
                                        <span data-bind="text: selectedFieldName"></span>
                                        <span data-bind="text: isFormulaField() ? '(' + fieldFormat() + ')' : ''"></span>
                                        <span data-bind="visible: !$root.isFieldValidForYAxis($index(), fieldType, selectedAggregate())">
                                            <span class="badge badge-danger" data-bind="visible: !groupInGraph()">Will not show in <span data-bind="text: $root.ReportType"></span>Chart</span>
                                        </span>
                                    </div>
                                    <div class="col">
                                        <div class="pull-right" style="margin-top: -5px;">
                                            <select class="form-control form-control-sm" data-bind="options: $root.canDrilldown() && $index()>0 ? fieldAggregateWithDrilldown : fieldAggregate, value: selectedAggregate, visible: $parent.AggregateReport() && !$parent.useStoredProc()"></select>
                                        </div>
                                        <div class="sortable pull-right" style="padding-right: 15px;" data-bind="ifnot: $parent.useStoredProc">
                                            <span class="fa fa-arrows " aria-hidden="true" title="Drag to reorder"></span>
                                        </div>
                                        <div class="pull-right" style="padding-right: 15px;" data-bind="ifnot: $parent.useStoredProc">
                                            <span class="fa fa-trash-o" title="Cannot Delete Required Filter" data-bind="visible: forceFilterForTable"></span>
                                            <span class="fa fa-trash" style="cursor: pointer;" aria-hidden="true" title="Delete this Field" data-bind="click: $parent.RemoveField, hidden: forceFilterForTable"></span>
                                        </div>
                                        <div class="pull-right" style="padding-right: 15px;" data-bind="ifnot: $parent.useStoredProc">
                                            <span class="button-box no-padding" tabindex="0" data-bind="click: function(){ filterOnFly(!filterOnFly()); }, css: {active: filterOnFly()==true}">
                                                <span class="fa fa-filter" aria-hidden="true" title="Filter by this field on the Report"></span>
                                            </span>
                                        </div>
                                        <div class="pull-right" style="padding-right: 15px;" data-bind="visible: $root.ReportType()!='Pivot'">
                                            <span class="button-box no-padding" tabindex="0" data-bind="click: toggleDisable, css: {active: disabled()==true}">
                                                <span class="fa fa-close" aria-hidden="true" title="Do not include in Report"></span>
                                            </span>
                                        </div>
                                        <div class="pull-right" style="padding-right: 15px;" data-bind="visible: $root.ReportType()!='Pivot' && $root.ReportType()!='List' && !$parent.useStoredProc()">
                                            <span class="button-box no-padding" tabindex="0" data-bind="click: function(){ hideInDetail(!hideInDetail()); }, css: {active: hideInDetail()==true}">
                                                <span class="fa fa-eye-slash" aria-hidden="true" title="Hide in Details"></span>
                                            </span>
                                        </div>
                                        <div class="pull-right" style="padding-right: 15px;" data-bind="visible: $root.isFieldValidForYAxis($index(), fieldType, selectedAggregate())  && $root.isChart() && $index()>0">
                                            <span class="button-box no-padding" tabindex="0" data-bind="click: function(){ groupInGraph(!groupInGraph()); }, css: {active: groupInGraph()==false}">
                                                <span class="fa fa-line-chart" aria-hidden="true" title="Include in series"></span>
                                            </span>
                                        </div>
                                        <div class="pull-right" style="padding-right: 15px;" data-bind="visible: $root.IncludeSubTotal()  &&  (['Int', 'Double', 'Money', 'Decimal', 'Currency'].indexOf(fieldType) >= 0 || ['Int', 'Double', 'Money', 'Decimal', 'Currency'].indexOf(fieldFormat()) >= 0)">
                                            <span class="button-box no-padding" tabindex="0" data-bind="click: function(){ dontSubTotal(!dontSubTotal()); }, css: {active: dontSubTotal()==true}">
                                                <span class="fa fa-plus-circle" aria-hidden="true" title="Don't add to Total row"></span>
                                            </span>
                                        </div>
                                        <div class="pull-right" style="padding-right: 15px;" data-bind="visible: $root.ReportType()!='Pivot'">
                                            <span class="button-box no-padding" tabindex="0" data-bind="click: setupLinkField, css: {active: linkField()==true}">
                                                <span class="fa fa-link" aria-hidden="true" title="Link to another Report or Url"></span>
                                            </span>
                                        </div>
                                        <div class="pull-right" style="padding-right: 15px;">
                                            <span class="button-box no-padding" tabindex="0" data-bind="click: setupFieldOptions">
                                                <span class="fa fa-gear" aria-hidden="true" title="More Options"></span>
                                            </span>
                                        </div>
                                        <div class="pull-right" style="padding-right: 15px;" data-bind="if: isFormulaField">
                                            <span class="button-box no-padding" tabindex="0" data-bind="click: editFormulaField">
                                                <span class="fa fa-pencil" aria-hidden="true" title="Edit Formula Field"></span>
                                            </span>
                                        </div>
                                    </div>
                                </div>
                            </li>
                        </ul>
                    </div>
                    <hr />
                    <div data-bind="if: useStoredProc">
                        <h5><span class="fa fa-filter"></span>&nbsp;Choose filters</h5>

                        <div class="row" style="padding: 10px 10px; overflow-x: auto;">
                            <div data-bind="template: {name: 'filter-parameters'}" class="col"></div>
                            <br />
                        </div>
                    </div>
                    <div data-bind="ifnot: useStoredProc">
                        <h5><span class="fa fa-filter"></span>&nbsp;Choose filters</h5>

                        <div class="row" style="padding: 10px 10px; overflow-x: auto;">
                            <div data-bind="template: {name: 'filter-group'}" class="col-12"></div>
                            <br />

                            <div class="checkbox col-12">
                                <label>
                                    <input type="checkbox" data-bind="checked: EditFiltersOnReport" />
                                    Allow setting up and saving filters on Report
                                </label>
                            </div>
                        </div>
                    </div>
                </div>
                <hr />
                <h5><span class="fa fa-hourglass"></span>&nbsp;Choose Schedule</h5>

                <div style="padding: 10px 10px">
                    <div data-bind="template: {name: 'report-schedule'}"></div>
                </div>
                <hr />

                <div>
                    <div class="form-group row">
                        <label class="col-sm-2 control-label">Report Name</label>
                        <div class="col-sm-10">
                            <input type="text" style="width: 100%;" class="form-control" required placeholder="Report Name" data-bind="value: ReportName">
                        </div>
                    </div>
                    <div class="form-group row">
                        <label class="col-sm-2 control-label">Report Description</label>
                        <div class="col-sm-10">
                            <textarea class="form-control" style="width: 100%;" rows="3" placeholder="Optional Report Description" data-bind="value: ReportDescription"></textarea>
                        </div>
                    </div>
                    <div data-bind="ifnot: useStoredProc">
                        <div class="form-group row">
                            <label class="col-sm-2 control-label">Sort By</label>
                            <div class="col-sm-6">
                                <select class="form-control" required data-bind="options: SelectedFields, optionsText: 'selectedFieldName', optionsValue: 'fieldId', value: SortByField"></select>
                            </div>
                            <div class="col-sm-2">
                                <button class="btn btn-sm btn-secondary" data-bind="text: !SortDesc() ? '▲ Sort Ascending' : 'Sort Descending ▼', click: function() { SortDesc(!SortDesc()); return false; }"></button>
                            </div>
                            <div class="col-sm-2">
                                <button class="btn btn-sm btn-secondary" data-bind="click: addSortField">Add Sort Field</button>
                            </div>
                        </div>
                        <div data-bind="foreach: SortFields">
                            <div class="form-group row">
                                <label class="col-sm-2 control-label">&nbsp;&nbsp;Then Sort By</label>
                                <div class="col-sm-4">
                                    <select class="form-control" required data-bind="options: $parent.SelectedFields, optionsText: 'selectedFieldName', optionsValue: 'fieldId', value: sortByFieldId"></select>
                                </div>
                                <div class="col-sm-2">
                                    <button class="btn btn-sm btn-secondary" data-bind="click: remove">Remove Sort Field</button>
                                </div>
                                <div class="col-sm-2">
                                    <button class="btn btn-sm btn-secondary" data-bind="text: !sortDesc() ? '▲ Sort Ascending' : 'Sort Descending ▼', click: function() { sortDesc(!sortDesc()); return false; }"></button>
                                </div>
                                <div class="col-sm-2">
                                    <button class="btn btn-sm btn-secondary" data-bind="click: $parent.addSortField">Add Sort Field</button>
                                </div>
                            </div>
                        </div>
                        <div class="checkbox" data-bind="visible: $root.ReportType()!='Pivot'">
                            <label>
                                <input type="checkbox" data-bind="checked: IncludeSubTotal" />
                                Include Total Row
                            </label>
                        </div>
                        <div class="checkbox" data-bind="hidden: AggregateReport">
                            <label>
                                <input type="checkbox" data-bind="checked: ShowUniqueRecords" />
                                Show only Unique Records
                            </label>
                        </div>
                    </div>
                    <div class="checkbox" data-bind="visible: isChart">
                        <label>
                            <input type="checkbox" data-bind="checked: ShowDataWithGraph" />
                            Show Data along with Graph
                        </label>
                    </div>
                    <div class="checkbox" data-bind="visible: CanSaveReports() && ReportID()>0">
                        <label>
                            <input type="checkbox" data-bind="checked: SaveReport">
                            Save Report
                        </label>
                    </div>
                    <div class="form-group row" data-bind="visible: SaveReport">
                        <label class="col-sm-2 control-label">Choose Folder</label>
                        <div class="col-sm-10">
                            <select class="form-control" style="width: 100%;" data-bind="options: Folders, optionsText: 'FolderName', optionsValue: 'Id', value: FolderID"></select>
                        </div>
                    </div>

                    <div data-bind="if: adminMode">
                        <hr />
                        <div data-bind="template: {name: 'manage-access-template'}"></div>
                    </div>
                </div>
            </div>
            <div class="modal-footer">
                <a href="#" class="btn btn-danger" data-bind="click: cancelCreateReport">Cancel editing Report</a>
                <button class="btn btn-primary" type="button" data-bind="visible: SaveReport() && CanSaveReports(), click: SaveWithoutRun" style="padding-right: 10px;">Save Report</button>
                <button class="btn btn-primary" type="button" data-bind="text: SaveReport() && CanSaveReports()? 'Save & Run Report': 'Run Report', click: RunReport">Run Report</button>
            </div>
        </div>
    </div>
</div>

<!-- Folder Edit Modal -->
<div class="modal" id="folderModal" tabindex="-1" role="dialog" aria-hidden="true">
    <div class="modal-dialog">
        <div class="modal-content" data-bind="with: ManageFolder">
            <div class="modal-header">
                <h5 class="modal-title">Manage Folder</h5>
                <button type="button" class="close" data-dismiss="modal" aria-label="Close">
                    <span aria-hidden="true">&times;</span>
                </button>
            </div>
            <div class="modal-body">
                <div class="form-group">
                    <label class="col-sm-4 col-md-4 control-label">Folder Name</label>
                    <div class="col-sm-8 col-md-8">
                        <input type="text" class="form-control" id="folderName" required placeholder="Folder Name" data-bind="value: FolderName">
                    </div>
                </div>
                <br />
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-dismiss="modal">Cancel</button>
                <button type="button" class="btn btn-primary" data-bind="click: saveFolder">Save</button>
            </div>
        </div>
    </div>
</div>

<!-- Link Edit Modal -->
<div class="modal" id="linkModal" tabindex="-1" role="dialog" aria-hidden="true">
    <div class="modal-dialog" style="max-width: 750px;">
        <div class="modal-content" data-bind="with: editLinkField">
            <div class="modal-header">
                <h5 class="modal-title">Setup Link Field</h5>
            </div>
            <div class="modal-body needs-validation" data-bind="with: linkFieldItem">
                <div class="form-group row">
                    <label class="col-sm-5 col-md-5 control-label">This field will link to</label>
                    <div class="col-sm-7 col-md-7">
                        <select class="form-control" required data-bind="options: linkTypes, value: selectedLinkType"></select>
                    </div>
                </div>
                <div data-bind="if: LinksToReport">
                    <div class="form-group row">
                        <label class="col-sm-5 col-md-5 control-label">Link to this Report</label>
                        <div class="col-sm-7 col-md-7">
                            <select class="form-control" required data-bind="options: $root.SavedReports, optionsText: 'reportName', optionsValue: 'reportId', value: LinkedToReportId, optionsCaption: 'Choose the Report to link to'"></select>
                        </div>
                    </div>
                    <div class="checkbox" data-bind="if: LinkedToReportId">
                        <label>
                            <input type="checkbox" data-bind="checked: SendAsFilterParameter" />
                            Send field value as Report Filter Parameter
                        </label>
                    </div>
                    <div class="form-group row" data-bind="if: SendAsFilterParameter">
                        <label class="col-sm-5 col-md-5 control-label">Link to this Report Filter</label>
                        <div class="col-sm-7 col-md-7">
                            <select class="form-control" required data-bind="options: allFields, optionsText: 'fieldName', optionsValue: 'fieldId', value: SelectedFilterId, optionsCaption: 'Choose the Field to Filter by value'"></select>
                        </div>
                    </div>
                </div>
                <div data-bind="ifnot: LinksToReport">
                    <div class="form-group row">
                        <label class="col-sm-5 col-md-5 control-label">Link to this URL</label>
                        <div class="col-sm-7 col-md-7">
                            <input type="url" class="form-control" required placeholder="Enter a url to open" data-bind="value: LinkToUrl" title="Valid URL needs to start with http:// or https://">
                        </div>
                    </div>
                    <div class="checkbox">
                        <label>
                            <input type="checkbox" data-bind="checked: SendAsQueryParameter" />
                            Send field value as Query Parameter
                        </label>
                    </div>
                    <div class="form-group row" data-bind="if: SendAsQueryParameter">
                        <label class="col-sm-5 col-md-5 control-label">Parameter Name to use</label>
                        <div class="col-sm-7 col-md-7">
                            <input type="text" class="form-control" required placeholder="Enter parameter name to send value as" data-bind="value: QueryParameterName">
                        </div>
                    </div>
                    <br />
                </div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-danger" data-bind="click: removeLinkField">Remove Link</button>
                <button type="button" class="btn btn-primary" data-bind="click: saveLinkField">Save Changes</button>
            </div>
        </div>
    </div>
</div>

<!-- Field Options Modal -->
<div class="modal" id="fieldOptionsModal" tabindex="-1" role="dialog" aria-hidden="true">
    <div class="modal-dialog" style="max-width: 750px;">
        <div class="modal-content" data-bind="with: editFieldOptions">
            <div class="modal-header">
                <h5 class="modal-title">Setup Additional Field Options</h5>
            </div>
            <div class="modal-body needs-validation">
                <div class="form-group row">
                    <label class="col-sm-5 col-md-5 control-label">Pick Data Format</label>
                    <div class="col-sm-7 col-md-7">
                        <select class="form-control" required data-bind="options: $root.fieldFormatTypes, value: fieldFormat"></select>
                    </div>
                </div>
                <div class="form-group row" data-bind="visible: $root.decimalFormatTypes.indexOf($data.fieldFormat())>=0">
                    <label class="col-sm-5 col-md-5 control-label">Pick Decimal Places</label>
                    <div class="col-sm-7 col-md-7">
                        <input type="number" class="form-control" data-bind="value: decimalPlaces" placeholder="" />
                    </div>
                </div>
                <div class="form-group row">
                    <label class="col-sm-5 col-md-5 control-label">Choose Column Label</label>
                    <div class="col-sm-7 col-md-7">
                        <input type="text" class="form-control" required data-bind="value: fieldLabel" />
                    </div>
                </div>
                <div class="form-group row">
                    <label class="col-sm-5 col-md-5 control-label">Choose Text Alignment</label>
                    <div class="col-sm-7 col-md-7">
                        <select class="form-control" required data-bind="options: $root.fieldAlignments, value: fieldAlign"></select>
                    </div>
                </div>
                <div class="form-group row">
                    <label class="col-sm-5 col-md-5 control-label">Pick Header Text Color</label>
                    <div class="col-sm-7 col-md-7">
                        <input type="color" style="width: 50px" class="form-control pull-left" required data-bind="value: headerFontColor" />
                        <input type="text" style="width: 100px" class="form-control pull-left" required data-bind="value: headerFontColor" />
                        <button class="btn btn-sm pull-left" title="Apply to all columns" data-bind="click: function(){applyAllHeaderFontColor(!applyAllHeaderFontColor()); }">
                            <span class="fa" data-bind="css: applyAllHeaderFontColor() ? 'fa-check' : 'fa-paste'"></span>
                        </button>
                    </div>
                </div>
                <div class="form-group row">
                    <label class="col-sm-5 col-md-5 control-label">Pick Header Background Color</label>
                    <div class="col-sm-7 col-md-7">
                        <input type="color" style="width: 50px" class="form-control pull-left" required data-bind="value: headerBackColor" />
                        <input type="text" style="width: 100px" class="form-control pull-left" required data-bind="value: headerBackColor" />
                        <button class="btn btn-sm pull-left" title="Apply to all columns" data-bind="click: function(){applyAllHeaderBackColor(!applyAllHeaderBackColor()); }">
                            <span class="fa" data-bind="css: applyAllHeaderBackColor() ? 'fa-check' : 'fa-paste'"></span>
                        </button>
                    </div>
                </div>
                <div class="form-group row">
                    <label class="col-sm-5 col-md-5 control-label">Pick Text Color</label>
                    <div class="col-sm-7 col-md-7">
                        <input type="color" style="width: 50px" class="form-control pull-left" required data-bind="value: fontColor" />
                        <input type="text" style="width: 100px" class="form-control pull-left" data-bind="value: fontColor" />
                        <button class="btn btn-sm pull-left" title="Apply to all columns" data-bind="click: function(){applyAllFontColor(!applyAllFontColor()); }">
                            <span class="fa" data-bind="css: applyAllFontColor() ? 'fa-check' : 'fa-paste'"></span>
                        </button>
                    </div>
                </div>
                <div class="form-group row">
                    <label class="col-sm-5 col-md-5 control-label">Pick Background Color</label>
                    <div class="col-sm-7 col-md-7">
                        <input type="color" style="width: 50px" class="form-control pull-left" required data-bind="value: backColor" />
                        <input type="text" style="width: 100px" class="form-control pull-left" data-bind="value: backColor" />
                        <button class="btn btn-sm pull-left" title="Apply to all columns" data-bind="click: function(){applyAllBackColor(!applyAllBackColor()); }">
                            <span class="fa" data-bind="css: applyAllBackColor() ? 'fa-check' : 'fa-paste'"></span>
                        </button>
                    </div>
                </div>
                <div class="form-group row">
                    <label class="col-sm-5 col-md-5 control-label">Choose Width (leave blank for auto)</label>
                    <div class="col-sm-7 col-md-7">
                        <input type="text" class="form-control" required data-bind="value: fieldWidth" />
                    </div>
                </div>
                <div class="form-group row">
                    <div class="col-sm-12">
                        <div class="checkbox pull-left" style="padding-top: 5px;">
                            <label>
                                <input type="checkbox" data-bind="checked: headerFontBold" />
                                Make Header Text Bold
                            </label>
                        </div>
                        <button class="btn btn-sm pull-left" title="Apply to all columns" data-bind="click: function(){applyAllHeaderBold(!applyAllHeaderBold()); }">
                            <span class="fa" data-bind="css: applyAllHeaderBold() ? 'fa-check' : 'fa-paste'"></span>
                        </button>
                    </div>
                </div>
                <div class="form-group row">
                    <div class="col-sm-12">
                        <div class="checkbox pull-left" style="padding-top: 5px;">
                            <label>
                                <input type="checkbox" data-bind="checked: fontBold" />
                                Make Text Bold
                            </label>
                        </div>
                        <button class="btn btn-sm pull-left" title="Apply to all columns" data-bind="click: function(){applyAllBold(!applyAllBold()); }">
                            <span class="fa" data-bind="css: applyAllBold() ? 'fa-check' : 'fa-paste'"></span>
                        </button>
                    </div>
                </div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-danger" data-bind="click: cancelFieldOptions">Cancel</button>
                <button type="button" class="btn btn-primary" data-bind="click: saveFieldOptions">Save Changes</button>
            </div>
        </div>
    </div>
</div>

<script type="text/html" id="report-schedule">
    <div data-bind="with: scheduleBuilder">
        <div class="checkbox">
            <label>
                <input type="checkbox" data-bind="checked: hasSchedule" />
                Schedule Report
            </label>
        </div>
        <div data-bind="if: hasSchedule">

            <div class="form-inline form-group">
                <div class="row col-sm-12">
                    <span data-bind="text: selectedOption() != 'once' ? 'Every ' : ''"></span>&nbsp;
                    <select class="form-control" required data-bind="options: options, value: selectedOption"></select>
                    <div data-bind="if: selectedOption() == 'once'">
                        &nbsp;on&nbsp;<input data-bind="datepicker: selectedDate" class="form-control" required />
                    </div>
                    <div data-bind="if: showDays">
                        &nbsp;on&nbsp;<select multiple class="form-control" required data-bind="select2: { placeholder: 'Choose Days', allowClear: true }, options: days, selectedOptions: selectedDays"></select>
                    </div>
                    <div data-bind="if: showDates">
                        &nbsp;on&nbsp;<select multiple class="form-control" required data-bind="select2: { placeholder: 'Choose Dates', allowClear: true }, options: dates, selectedOptions: selectedDates"></select>
                    </div>
                    <div data-bind="if: showMonths">
                        &nbsp;of&nbsp;<select multiple class="form-control" required data-bind="select2: { placeholder: 'Choose Months', allowClear: true }, options: months, selectedOptions: selectedMonths"></select>
                    </div>
                    <div data-bind="if: showAtTime">
                        &nbsp;at&nbsp;<select class="form-control" data-bind="options: hours, value: selectedHour"></select>
                        <select class="form-control" data-bind="options: minutes, value: selectedMinute"></select>
                        <select class="form-control" data-bind="value: selectedAmPm">
                            <option>AM</option>
                            <option>PM</option>
                        </select>
                    </div>
                </div>
            </div>
            <div class="alert alert-info">
                Report will be run and emailed <span data-bind="text: selectedOption() != 'once' ? 'every' : ''"></span> <span data-bind="text: selectedOption"></span>
                <span data-bind="if: selectedOption() == 'once'">
                    on <span class="error" data-bind="visible: !selectedDate()">Please pick a Date</span> <span data-bind="text: selectedDate"></span>
                </span>
                <span data-bind="if: showDays">
                    on <span class="error" data-bind="visible: selectedDays().length == 0">Please pick Day(s)</span> <span data-bind="text: selectedDays"></span>
                </span>
                <span data-bind="if: showDates">
                    on <span class="error" data-bind="visible: selectedDates().length == 0">Please pick Date(s)</span>
                    <span data-bind="foreach: selectedDates"><span data-bind="visible: $index()>0">, </span><span data-bind="text: $data == 1 ? '1st': ($data == 2 ? '2nd' : ($data == 3 ? '3rd' : $data+'th'))"></span></span>
                </span>
                <span data-bind="if: showMonths">
                    of <span class="error" data-bind="visible: selectedMonths().length == 0">Please pick Month(s)</span> <span data-bind="text: selectedMonths"></span>
                </span>
                <span data-bind="if: showAtTime">
                    at <span data-bind="text: selectedHour"></span>:<span data-bind="text: selectedMinute"></span> <span data-bind="text: selectedAmPm"></span>
                </span>
            </div>
            <div class="form-horizontal form-group">
                <div class="form-group row">
                    <label class="col-sm-2 control-label">Email to</label>
                    <div class="col-sm-6">
                        <input type="text" class="form-control" style="width: 100%;" data-bind="value: emailTo" placeholder="Enter Email Addresses separated by comma to send the Report to" required />
                    </div>
                    <label class="col-sm-2 control-label">Report Format</label>
                    <div class="col-sm-2">
                        <select class="form-control" data-bind="value: format">
                            <option value="EXCEL">Excel</option>
                            <option value="CSV">CSV</option>
                            <option value="PDF">PDF</option>
                        </select>
                    </div>
                </div>
            </div>
            <div class="form-horizontal form-group">
                <div class="form-group row">
                    <div class="col-sm-2 control-label">
                        <div class="checkbox">
                            <label title="Set a date to start sending scheduled report">
                                <input type="checkbox" data-bind="checked: hasScheduleStart" />
                                Set Schedule Start Date
                            </label>
                        </div>
                    </div>
                    <div class="col-sm-4" data-bind="if: hasScheduleStart">
                        <input type="text" class="form-control" data-bind="datepicker: scheduleStart" title="Scheduled Report will not be sent before this date" required />
                    </div>
                </div>
            </div>

            <div class="form-horizontal form-group">
                <div class="form-group row">
                    <div class="col-sm-2 control-label">
                        <div class="checkbox" title="Set a date to stop sending scheduled report">
                            <label>
                                <input type="checkbox" data-bind="checked: hasScheduleEnd" />
                                Set Schedule End Date
                            </label>
                        </div>
                    </div>
                    <div class="col-sm-4" data-bind="if: hasScheduleEnd">
                        <input type="text" class="form-control" data-bind="datepicker: scheduleEnd" title="Scheduled Report will not be sent after this date" required />
                    </div>
                </div>
            </div>
        </div>
    </div>
</script>

</asp:Content>