﻿<%@ Page Title="Report Setup" Language="C#" MasterPageFile="~/DotNetReport/ReportLayout.Master" AutoEventWireup="true" CodeBehind="Setup.aspx.cs" Inherits="ReportBuilder.WebForms.DotNetReport.Setup" Async="true" %>

<asp:Content ID="Content2" ContentPlaceHolderID="head" runat="server">
    <link href="../Content/bootstrap-editable.css" rel="stylesheet" />
    <style type="text/css">
        .glyphicon-ok:before {
            content: "\f00c";
        }

        .glyphicon-remove:before {
            content: "\f00d";
        }

        .glyphicon {
            display: inline-block;
            font: normal normal normal 14px/1 FontAwesome;
            font-size: inherit;
            text-rendering: auto;
            -webkit-font-smoothing: antialiased;
            -moz-osx-font-smoothing: grayscale;
        }
    </style>
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="scripts" runat="server">
    <script src="../Scripts/tribute.min.js"></script>
    <script src="../Scripts/dotnetreport-setup.js"></script>
    <script src="../Scripts/knockout.mapping-latest.js"></script>
    <script src="../Scripts/bootstrap-editable.min.js"></script>
    <script src="../Scripts/knockout.x-editable.min.js"></script>
    <script type="text/javascript">

        $.fn.editable.defaults.mode = 'inline';
        $('.helptip').tooltip();

        function closeall() {
            $('.panel-collapse.in')
                .collapse('hide');
        };

        function openall() {
            $('.panel-collapse.in')
                .collapse('show');
        };

        $(document).ready(function () {
            var queryParams = Object.fromEntries((new URLSearchParams(window.location.search)).entries());
            ajaxcall({
                type: 'POST',
                url: '/DotNetReport/ReportService.asmx/LoadSetupSchema',
                data: JSON.stringify({
                    databaseApiKey: (queryParams.databaseApiKey || ''),
                    onlyApi: (queryParams.onlyApi === 'false' ? false : true)
                })
            }).done(function (model) {
                if (model.d) model = model.d;
                var options = {
                    model: model.Result,
                    saveTableUrl: '<%= System.Configuration.ConfigurationManager.AppSettings["dotNetReport.apiUrl"] + "/ReportApi/SaveTable"%>',
                    deleteTableUrl: '<%= System.Configuration.ConfigurationManager.AppSettings["dotNetReport.apiUrl"] + "/ReportApi/DeleteTable"%>',
                    getRelationsUrl: '<%= System.Configuration.ConfigurationManager.AppSettings["dotNetReport.apiUrl"] + "/ReportApi/GetRelations"%>',
                    getDataConnectionsUrl: '<%= System.Configuration.ConfigurationManager.AppSettings["dotNetReport.apiUrl"] + "/ReportApi/GetDataConnections"%>',
                    saveRelationsUrl: '<%= System.Configuration.ConfigurationManager.AppSettings["dotNetReport.apiUrl"] + "/ReportApi/SaveRelations"%>',
                    addDataConnectionUrl: '<%= System.Configuration.ConfigurationManager.AppSettings["dotNetReport.apiUrl"] + "/ReportApi/AddDataConnection"%>',
                    saveProcUrl: '<%= System.Configuration.ConfigurationManager.AppSettings["dotNetReport.apiUrl"] + "/ReportApi/SaveProcedure"%>',
                    deleteProcUrl: '<%= System.Configuration.ConfigurationManager.AppSettings["dotNetReport.apiUrl"] + "/ReportApi/DeleteProcedure"%>',
                    reportsApiUrl: '<%= System.Configuration.ConfigurationManager.AppSettings["dotNetReport.apiUrl"]%>',
                    searchProcUrl: '/DotNetReport/ReportService.asmx/SearchProcedure',
                    getUsersAndRoles: '/DotNetReport/ReportService.asmx/GetUsersAndRoles',
                    getSchemaFromSql: '/DotNetReport/ReportService.asmx/GetSchemaFromSql',
                    apiUrl: '/DotNetReport/ReportService.asmx/CallReportApi',
                    onlyApi: queryParams.onlyApi !== 'false'
                };

                var vm = new manageViewModel(options);
                vm.LoadJoins();
                vm.setupManageAccess();
                ko.applyBindings(vm);
                vm.LoadDataConnections();

                vm.customSql.textQuery.setupQuery();
            });
        });

    </script>

</asp:Content>
<asp:Content ID="Content4" ContentPlaceHolderID="body" runat="server">

<div>
    <h2>Manage Database</h2>
    <p>
        You can select and manage display names for the Tables and Columns you would like to allow in Dotnet Report Builder.
    </p>
    <div class="alert alert-danger">
        Do not expose this Setup Tool to end users who should not have access to it. Ideally, only Developers should be given access to this utility.
    </div>
    <div data-bind="visible: false;">
        Loading...
    </div>

    <div>
        <!-- Nav tabs -->
        <ul class="nav nav-tabs" role="tablist">
            <li role="presentation" class="nav-item"><a class="nav-link active" href="#tablesfields" aria-controls="home" role="tab" data-toggle="tab" data-bind="click: function() { customTableMode(false); }">Database Tables</a></li>
            <li role="presentation" class="nav-item"><a class="nav-link" href="#tablesfields" aria-controls="custom" role="tab" data-toggle="tab" data-bind="click: function() { customTableMode(true); }">Custom Tables</a></li>
            <li role="presentation" class="nav-item"><a class="nav-link" href="#relations" aria-controls="profile" role="tab" data-toggle="tab">Relations</a></li>
            <li role="presentation" class="nav-item"><a class="nav-link" href="#procedure" aria-controls="procedure" role="tab" data-toggle="tab">Stored Procs</a></li>
            <li role="presentation" class="nav-item"><a class="nav-link" href="#manageaccess" aria-controls="manageaccess" role="tab" data-toggle="tab">Manage Reports Access</a></li>
            <li role="presentation" class="nav-item"><a class="nav-link" href="#connection" aria-controls="home" role="tab" data-toggle="tab">Data Connection</a></li>
        </ul>
    </div>
    <br />
</div>
<!-- Tab panes -->
<div class="fix-content" style="display: none;" data-bind="visible: true">
    <div class="tab-content">
        <div role="tabpanel" class="tab-pane" id="connection">
            <b>Manage Database Connection</b>
            <p>
                Select and manage Data you would like to allow access to in DNR to use in building Reports and Dashboards
            </p>
            <div class="form-row" data-bind="visible: true">
                <div class="form-group">
                    <div class="control-group">
                        <select class="form-control" data-bind="options: DataConnections, optionsText: 'DataConnectName', optionsValue: 'DataConnectGuid', value: currentConnectionKey"></select>
                        <div class="padded-top"></div>
                        <button class="btn btn-primary btn-sm" data-toggle="modal" data-target="#connection-setup-modal">Manage DB Connection</button>
                        <button class="btn btn-primary btn-sm" data-bind="click: switchConnection, visible: canSwitchConnection">Switch Connection</button>
                        <button class="btn btn-primary btn-sm" data-toggle="modal" data-target="#add-connection-modal">Add New Connection</button>
                        <button class="btn btn-primary btn-sm" data-bind="click: exportAll, visible: false">Export Connection</button>
                        <button class="btn btn-primary btn-sm" data-bind="click: importStart, visible: false">Import Connection</button>
                    </div>
                    <div data-bind="visible: importingFile">
                        <br />
                        <div class="alert alert-info">
                            Please select a file you have previously exported from this utility. The system will load the screen with the Data Connection setting in the file, however, it will <b>NOT</b> be saved until you press the Save All button or Save individual tables
                            <br />
                            <div class="input-group">
                                <input type="file" accept=".json" id="import-file" data-bind="event: { change: function() { importFile($element.files[0]) } }" />
                                <span class="input-group-btn">
                                    <button class="btn btn-link" data-bind="click: importCancel">Cancel</button>
                                </span>
                            </div>

                        </div>
                    </div>
                </div>
            </div>
        </div>

        <div role="tabpanel" class="tab-pane active" id="tablesfields">
            <b data-bind="text: customTableMode() ? 'Custom Tables' : 'Database Tables'"></b>
            <p>
                <span data-bind="visible: $root.customTableMode">
                    Define Custom Tables using SQL Select Query to use in DNR for Reporting and Analytics.
                </span>
                <span data-bind="hidden: $root.customTableMode">
                    Select Tables or Views from your Database to use in DNR for Reporting and Analytics.
                </span>
            </p>

            <div data-bind="with: Tables">
                <input type="text" class="form-control input-sm" placeholder="Filter Tables for..." data-bind="value: tableFilter, valueUpdate: 'afterkeydown'" style="float: left; width: 140px;">
                <button class="btn btn-sm" data-bind="click: clearTableFilter,  visible: tableFilter()!='' && tableFilter()!=null"><span class="fa fa-remove"></span></button>
                <button class="btn btn-sm" onclick="openall()" style="margin-left: 15px;">Open all</button>
                <button class="btn btn-sm" onclick="closeall()">Close all</button>
                <button class="btn btn-sm" data-bind="click: toggleShowAll, hidden: $root.customTableMode() || $root.onlyApi(), text: usedOnly() ? 'Show all' : 'Show used only'">Show used only</button>
                <button class="btn btn-sm btn-primary" data-bind="click: $root.customSql.addNewCustomSqlTable, visible: $root.customTableMode">Add New Custom Table</button>
                <button class="btn btn-sm btn-primary" data-bind="click: $root.loadFromDatabase, hidden: $root.customTableMode() || !$root.onlyApi()">Load all Database Tables</button>
            </div>
            <div class="clearfix"></div>
            <hr />
            <div class="row">
                <div class="form-group form-inline col-md-4" data-bind="with: pager">
                    <div data-bind="template: 'pager-template', data: $data"></div>
                </div>
                <div class="col-md-4" data-bind="ifnot: $root.customTableMode">
                    <div>
                        <span data-bind="text: Tables.model().length"></span> Tables/Views <span data-bind="text: $root.onlyApi() ? 'configured and used' : 'read from database'"></span>
                    </div>
                </div>
            </div>
            <div class="menu row" style="margin-left: 20px;" data-bind="foreach: pagedTables">
                <div class="menu-category card" style="float: left;">

                    <div class="card-header clearfix" style="">
                        <div class="checkbox pull-left">
                            <label>
                                <input type="checkbox" data-bind="checked: Selected">
                                <span data-bind="text: TableName"></span>
                                <span data-bind="visible: IsView" class="label-sm">(view)</span>
                            </label>
                            <button class="btn btn-sm" title="Save this Table" data-bind="click: function(){$data.saveTable($root.keys.AccountApiKey, $root.keys.DatabaseApiKey);}"><i class="fa fa-floppy-o"></i></button>
                            <button class="btn btn-sm" title="Manage Role Access" data-toggle="modal" data-target="#role-access-modal" data-bind="click: $root.selectAllowedRoles">
                                <i class="fa fa-user"></i>
                            </button>
                        </div>
                        <a class="pull-right" data-toggle="collapse" data-bind="attr: {'data-target': '#table'+$index()}">
                            <span class="fa fa-chevron-down"></span>
                        </a>
                    </div>
                    <div data-bind="attr: {id: 'table'+$index()}" class="panel-collapse collapse in">
                        <div class="card-body">
                            <p>
                                <label class="label-sm">Display Name</label><br />
                                <span data-bind="editable: DisplayName"></span>
                            </p>
                            <p>
                                <label class="label-sm">Schema Name</label><br />
                                <span data-bind="editable: SchemaName"></span>
                            </p>
                            <p>
                                <label class="label-sm">Account Id Field</label><br />
                                <span data-bind="editable: AccountIdField"></span>
                            </p>
                            <div class="checkbox">
                                <label>
                                    <input class="check-box" data-val="true" type="checkbox" value="true" data-bind="checked: DoNotDisplay" title="Do not display this table for selecting in reports"> Do Not Display
                                </label>
                            </div>
                            <div data-bind="if: CustomTable">
                                <button data-bind="click: $root.customSql.viewCustomSql.bind($data)" class="btn btn-sm btn-secondary">View Custom Sql</button>
                            </div>
                        </div>

                        <label style="padding-left: 15px;" class="label-sm"><span data-bind="text: Columns().length"></span> Columns</label>
                        <button class="btn btn-sm btn-link label-sm" data-bind="click: autoFormat, visible: Selected">Auto Format</button>
                        <button class="btn btn-sm btn-link label-sm" data-bind="click: unselectAllColumns, visible: Selected">Unselect All</button>
                        <button class="btn btn-sm btn-link label-sm" data-bind="click: selectAllColumns, visible: Selected">Select All</button>
                        <div class="list-group" data-bind="sortable: { data: Columns, options: { handle: '.sortable', cursor: 'move' }, afterMove: $parent.columnSorted }">
                            <div class="list-group-item">
                                <div class="checkbox">
                                    <label>
                                        <input type="checkbox" data-bind="checked: Selected, enable: $parent.Selected()">
                                        <span data-bind="editable: DisplayName, attr: {title: 'DB field is ' + ColumnName()}"></span>
                                        <label data-bind="visible: PrimaryKey" class="badge badge-primary">Primary</label>
                                        <label data-bind="visible: ForeignKey" class="badge badge-info">Foreign</label>
                                    </label>
                                    <button class="btn btn-sm pull-right" data-toggle="modal" data-target="#column-modal" title="All column options" data-bind="click: $root.selectColumn.bind($data, false)">...</button>

                                    <div class="btn btn-sm pull-right sortable">
                                        <span class="fa fa-arrows" aria-hidden="true" title="Drag to reorder"></span>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
        <div role="tabpanel" class="tab-pane" id="relations">
            <b>Relations</b>
            <p>
                Setup your Database Relations for DNR to produce dynamic queries
            </p>
            <button class="btn btn-sm btn-primary" data-bind="click: AddJoin">Add new Join</button>&nbsp;
            <button class="btn btn-sm btn-primary" data-bind="click: SaveJoins">Save Joins</button>
            <br />
            <br />
            <form id="form-joins">
                <table class="table">
                    <thead data-bind="with: JoinFilters">
                        <tr>
                            <th>
                                Primary Table
                                <div class="input-group input-group-sm">
                                    <input type="text" class="form-control input-sm" data-bind="value: primaryTable, valueUpdate: 'afterkeydown'" placeholder="Search...">
                                    <span class="input-group-addon"><i class="fa fa-filter"></i></span>
                                </div>
                            </th>
                            <th>
                                Field
                                <div class="input-group input-group-sm">

                                    <input type="text" class="form-control input-sm" data-bind="value: primaryField, valueUpdate: 'afterkeydown'" placeholder="Search...">
                                    <span class="input-group-addon"><i class="fa fa-filter"></i></span>
                                </div>
                            </th>
                            <th>
                                Join Type
                                <div class="input-group input-group-sm">

                                    <input type="text" class="form-control input-sm" data-bind="value: joinType, valueUpdate: 'afterkeydown'" placeholder="Search...">
                                    <span class="input-group-addon"><i class="fa fa-filter"></i></span>
                                </div>
                            </th>
                            <th>
                                Join Table
                                <div class="input-group input-group-sm">

                                    <input type="text" class="form-control input-sm" data-bind="value: joinTable, valueUpdate: 'afterkeydown'" placeholder="Search...">
                                    <span class="input-group-addon"><i class="fa fa-filter"></i></span>
                                </div>
                            </th>
                            <th>
                                Field<div class="input-group input-group-sm">

                                    <input type="text" class="form-control input-sm" data-bind="value: joinField, valueUpdate: 'afterkeydown'" placeholder="Search...">
                                    <span class="input-group-addon"><i class="fa fa-filter"></i></span>
                                </div>
                            </th>
                            <th></th>
                        </tr>
                    </thead>
                    <tbody data-bind="foreach: filteredJoins">
                        <tr>
                            <td>
                                <select class="form-control input-medium" data-bind="options: $root.Tables.availableTables, optionsText: 'DisplayName', value: JoinTable"></select>
                            </td>
                            <td data-bind="with: JoinTable">
                                <select class="form-control" data-bind="options: availableColumns, optionsText: 'DisplayName', optionsValue: 'ColumnName', value: $parent.FieldName"></select>
                            </td>
                            <td>
                                <select class="form-control input-small" data-bind="options: $root.JoinTypes, value: JoinType"></select>
                            </td>
                            <td>
                                <select class="form-control input-medium" data-bind="options: OtherTables, optionsText: 'DisplayName', value: OtherTable"></select>
                            </td>

                            <td data-bind="with: OtherTable">
                                <select class="form-control" data-bind="options: availableColumns, optionsText: 'DisplayName', optionsValue: 'ColumnName', value: $parent.JoinFieldName"></select>
                            </td>
                            <td>
                                <button class="btn btn-sm btn-secondary" data-bind="click: DeleteJoin">Delete</button>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </form>
            <br />
            <br />
        </div>
        <div id="procedure" class="tab-pane">
            <b>Stored Procedures</b>
            <p>
                Select and manage Stored Procedures to use in DNR for more complex and coded Reports
            </p>
            <hr />
            <button class="btn btn-sm btn-primary" data-toggle="modal" data-target="#procedure-modal">Add Stored Procs from database</button>
            <br />
            <div data-bind="if: Procedures.savedProcedures().length == 0">
                No Stored Procedures have been setup yet.
            </div>
            <div class="menu row" data-bind="foreach: Procedures.savedProcedures" style="margin-left: 20px; padding-top: 20px;">
                <div class="menu-category card" style="float: left;">

                    <div class="card-header clearfix" style="">
                        <div class="pull-left">
                            <label>
                                <span data-bind="text: TableName"></span>
                                <span class="label-xs">Procedure</span>
                            </label>

                            <button class="btn btn-sm" title="Save this Procedure" data-bind="click: function(){$root.saveProcedure($data.TableName, false);}">
                                <span class="fa fa-save"></span>
                            </button>
                            <button class="btn btn-sm" title="Manage Role Access" data-toggle="modal" data-target="#role-access-modal" data-bind="click: $root.selectAllowedRoles">
                                <i class="fa fa-user"></i>
                            </button>
                            <button class="btn btn-sm" title="Delete this Procedure" data-bind="click: function(){$data.deleteTable($root.keys.AccountApiKey, $root.keys.DatabaseApiKey);}">
                                <span class="fa fa-trash"></span>
                            </button>

                        </div>
                        <a class="pull-right" data-toggle="collapse" data-bind="attr: {'data-target': '#table'+$index()}">
                            <span class="fa fa-chevron-down"></span>
                        </a>
                    </div>
                    <div data-bind="attr: {id: 'table'+$index()}" class="panel-collapse collapse in">
                        <div class="card-body">
                            <p>
                                <label class="label-sm">Display Name</label><br />
                                <span data-bind="editable: DisplayName"></span>
                            </p>
                            <p>
                                <label class="label-sm">Schema Name</label><br />
                                <span data-bind="editable: SchemaName"></span>
                            </p>
                            <label class="small">Columns</label>

                            <div class="list-group" data-bind="foreach: Columns">
                                <div class="list-group-item">
                                    <label>
                                        <span data-bind="editable: DisplayName, attr: {title: 'DB field is ' + ColumnName()}"></span>
                                        <span class="badge badge-info" data-bind="text: FieldType"></span>
                                    </label>
                                    <button class="btn btn-sm pull-right" data-toggle="modal" data-target="#column-modal" title="All column options" data-bind="click: $root.selectColumn.bind($data, true)">...</button>

                                </div>
                            </div>
                            <label class="small">Parameters</label>

                            <div class="list-group" data-bind="foreach: Parameters">
                                <div class="list-group-item">
                                    <label>
                                        <span data-bind="editable: DisplayName, attr: {title: 'DB field is ' + ParameterName()}"></span>
                                        <span class="badge badge-info" data-bind="text: ParameterDataTypeString"></span>
                                        &nbsp;
                                        &nbsp;
                                        <button class="btn btn-sm btn-primary pull-right" data-toggle="modal" data-target="#parameter-modal" title="All Parameter options" data-bind="click: $root.editParameter">...</button>
                                    </label>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
        <div id="manageaccess" class="tab-pane">
            <div data-bind="foreach: reportsAndFolders" class="card" style="margin-left: 20px;">
                <div class="card-body">
                    <a class="btn btn-link" role="button" data-toggle="collapse" data-bind="attr: {href: '#folder-' + folderId }">
                        <i class="fa fa-folder"></i>&nbsp;<span data-bind="text: folder"></span>
                    </a>
                    <div class="collapse" data-bind="attr: {id: 'folder-' + folderId }">
                        <div data-bind="if: reports.length == 0">
                            No Reports found in this folder
                        </div>
                        <ul class="list-group" data-bind="foreach: reports">
                            <li class="list-group-item">
                                <div class="row">
                                    <div class="col-md-3">
                                        <div>
                                            <label class="list-group-item-heading">
                                                <span class="fa" data-bind="css: {'fa-file': reportType=='List', 'fa-th-list': reportType=='Summary', 'fa-bar-chart': reportType=='Bar', 'fa-pie-chart': reportType=='Pie',  'fa-line-chart': reportType=='Line', 'fa-globe': reportType =='Map'}" style="font-size: 14pt; color: #808080"></span>
                                                <span data-bind="text: reportName"></span>
                                            </label>
                                        </div>
                                        <p class="list-group-item-text small" data-bind="text: reportDescription"></p>

                                        <div class="small" style="padding-top: 10px;">
                                            <b>Current Report Access</b><br />
                                            Manage by User <span class="badge badge-info" data-bind="text: userId ? userId : 'Any User'"></span>
                                            <br />
                                            View only by User <span class="badge badge-info" data-bind="text: (viewOnlyUserId ? viewOnlyUserId : (userId ? userId : 'Any User'))"></span>
                                            <br />
                                            <div data-bind="if: deleteOnlyUserId">
                                                Delete only by User <span class="badge badge-info" data-bind="text: deleteOnlyUserId"></span>
                                                <br />
                                            </div>
                                            <div data-bind="if: userRole">
                                                Manage by Role <span class="badge badge-info" data-bind="text: userRole ? userRole : 'Any Role'"></span>
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
                                            <div>
                                                For Client <span class="badge badge-info" data-bind="text: clientId ? clientId : 'All Clients'"></span>
                                                <br />
                                            </div>
                                        </div>
                                        <br />
                                        <br />
                                        <button class="btn btn-sm btn-primary" data-bind="click: function() { changeAccess(!changeAccess())}, text: !changeAccess() ? 'Change Access': 'Cancel Changing Access', hidden: changeAccess">Change Access</button>
                                    </div>

                                    <div data-bind="if: changeAccess" class="col-md-9" >
                                        <div style="border-left: 1px solid; padding-left: 10px;">
                                            <div data-bind="template: {name: 'manage-access-template', data: $root }"></div>
                                            <br />
                                            <br />
                                            <button class="btn btn-sm btn-primary" data-bind="click: saveAccessChanges">Save Access Changes</button>
                                            <button class="btn btn-sm btn-primary" data-bind="click: function() { changeAccess(!changeAccess())}, text: !changeAccess() ? 'Change Access': 'Cancel Changing Access'">Change Access</button>

                                        </div>
                                    </div>
                                </div>
                            </li>
                        </ul>
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>

<!-- Modal -->
<div class="modal" id="column-modal" role="dialog" tabindex="-1">
    <div class="modal-dialog" role="document">
        <div class="modal-content" data-bind="with: editColumn()">
            <div class="modal-header">
                <h4 class="modal-title" id="myModalLabel">
                    Manage
                    <label data-bind="text: ColumnName"></label>
                </h4>
                <button type="button" class="close" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button>
            </div>
            <div class="modal-body">
                <p>Choose options that will determine how users interact with this column on Reports</p>
                <div class="form-horizontal">

                    <ul class="nav nav-tabs" role="tablist" data-bind="if: $root.isStoredProcColumn() == false">
                        <li role="presentation" class="nav-item"><a class="nav-link active" href="#column-main" aria-controls="home" role="tab" data-toggle="tab">Main Options</a></li>
                        <li role="presentation" class="nav-item"><a class="nav-link" href="#column-foreign" aria-controls="profile" role="tab" data-toggle="tab">Foreign Key Options</a></li>
                        <li role="presentation" class="nav-item"><a class="nav-link" href="#column-filter" aria-controls="procedure" role="tab" data-toggle="tab">Filter Options</a></li>
                    </ul>
                    <div class="tab-content">
                        <br />
                        <div role="tabpanel" class="tab-pane active" id="column-main">
                            <div class="control-group">
                                <div class="form-group row">
                                    <label class="col-md-3 col-sm-3 control-label" for="Display_Name" data-bind="attr:{placeholder:'Ex First Name'}" placeholder="Ex First Name">Display Name</label>
                                    <div class="col-md-6 col-sm-6">
                                        <input class="form-control text-box single-line" data-val="true" data-val-required="The DisplayName field is required." id="DisplayName" name="DisplayName" type="text" value="First Name" data-bind="value: DisplayName, attr:{placeholder:'Ex First Name'}" placeholder="Ex First Name">
                                    </div>
                                    <div class="col-md-3 col-sm-3">
                                        <span data-toggle="tooltip" data-placement="right" class="fa fa-question-circle helptip" title="This is a friendly name to display to your users"></span>
                                    </div>
                                </div>
                            </div>
                            <div class="control-group" data-bind="if: $root.isStoredProcColumn() == false">
                                <div class="form-group row">
                                    <label class="col-md-3 col-sm-3 control-label" for="Field_Type" data-bind="">Field Type</label>
                                    <div class="col-md-6 col-sm-6">
                                        <select class="form-control" id="FieldType_SelectedId" name="FieldType.SelectedId" data-bind="value: FieldType">
                                            <option value="Boolean">Boolean</option>
                                            <option value="DateTime">Date Time</option>
                                            <option value="Varchar">Varchar</option>
                                            <option value="Money">Money</option>
                                            <option value="Int">Int</option>
                                            <option value="Double">Double</option>
                                            <option value="Json">Json</option>
                                        </select>
                                    </div>
                                    <div class="col-md-3 col-sm-3">
                                        <span class="field-validation-valid help-inline indent" data-valmsg-for="FieldType" data-valmsg-replace="true"></span>
                                    </div>
                                </div>
                            </div>
                            <div class="control-group" data-bind="if: $root.isStoredProcColumn() == false">
                                <div class="form-group row">
                                    <label class="col-md-3 col-sm-3 control-label" for="Primary_Key" data-bind="">Primary Key</label>
                                    <div class="col-md-6 col-sm-6">
                                        <div class="checkbox">
                                            <label>
                                                <input class="check-box" data-val="true" data-val-required="The PrimaryKey field is required." id="PrimaryKey" name="PrimaryKey" type="checkbox" value="true" data-bind="checked:PrimaryKey"><input name="PrimaryKey" type="hidden" value="false">
                                            </label>
                                        </div>
                                    </div>
                                    <div class="col-md-3 col-sm-3">
                                        <span class="field-validation-valid help-inline indent" data-valmsg-for="PrimaryKey" data-valmsg-replace="true"></span>
                                    </div>
                                </div>
                            </div>
                            <div class="control-group">
                                <div class="form-group row">
                                    <label class="col-md-3 col-sm-3 control-label" data-bind="">Do Not Display</label>
                                    <div class="col-md-6 col-sm-6">
                                        <div class="checkbox">
                                            <label>
                                                <input class="check-box" data-val="true" type="checkbox" value="true" data-bind="checked:DoNotDisplay">
                                            </label>
                                        </div>
                                    </div>
                                    <div class="col-md-3 col-sm-3">
                                        <span data-toggle="tooltip" data-placement="right" class="fa fa-question-circle helptip" title="If this is checked, the column will not be displayed on Report Designer, but can still be used on Global Data Filters"></span>
                                    </div>
                                </div>
                            </div>

                            <div class="control-group" data-bind="if: FieldType() == 'Json'">
                                <div class="form-group row">
                                    <label class="col-md-3 col-sm-3 control-label">JSON Data Structure</label>
                                    <div class="col-md-6 col-sm-6">
                                        <textarea class="form-control text-box single-line" data-val="true" data-val-required="The DisplayName field is required." data-bind="value: JsonStructure, attr:{placeholder:'Please paste in Sample Json with all columns for this JSON data field'}" rows="5" ></textarea>
                                    </div>
                                    <div class="col-md-3 col-sm-3">
                                        <span data-toggle="tooltip" data-placement="right" class="fa fa-question-circle helptip" title="You can paste in a sample Json blob with all the columns you want to use in this field"></span>
                                    </div>
                                </div>
                            </div>
                        </div>
                        <div role="tabpanel" class="tab-pane" id="column-filter">
                            <div class="control-group" data-bind="if: $root.isStoredProcColumn() == false">
                                <div class="form-group row">
                                    <label class="col-md-4 col-sm-4 control-label" data-bind="">Require Filter if Column is selected</label>
                                    <div class="col-md-6 col-sm-6">
                                        <div class="checkbox">
                                            <label>
                                                <input class="check-box" data-val="true" type="checkbox" value="true" data-bind="checked:ForceFilter">
                                            </label>
                                        </div>
                                    </div>
                                    <div class="col-md-2 col-sm-2">
                                        <span data-toggle="tooltip" data-placement="right" class="fa fa-question-circle helptip" title="If this is checked, the column will always require the user to pick a filtered value"></span>
                                    </div>
                                </div>
                            </div>
                            <div class="control-group" data-bind="if: $root.isStoredProcColumn() == false">
                                <div class="form-group row">
                                    <label class="col-md-4 col-sm-4 control-label" data-bind="">Require Filter if Table is selected</label>
                                    <div class="col-md-6 col-sm-6">
                                        <div class="checkbox">
                                            <label>
                                                <input class="check-box" data-val="true" type="checkbox" value="true" data-bind="checked:ForceFilterForTable">
                                            </label>
                                        </div>
                                    </div>
                                    <div class="col-md-2 col-sm-2">
                                        <span data-toggle="tooltip" data-placement="right" class="fa fa-question-circle helptip" title="If this is checked, the report will always require the user to pick a filter value if user picks the column in report"></span>
                                    </div>
                                </div>
                            </div>
                            <div class="control-group" data-bind="if: FieldType() == 'DateTime'">
                                <div class="form-group row">
                                    <label class="col-md-4 col-sm-4 control-label" data-bind="">Restrict Filter Date Range</label>
                                    <div class="col-md-1 col-sm-1">
                                        <input class="check-box" type="checkbox" data-bind="checked: restrictDateRangeFilter" />
                                    </div>
                                    <div class="col-md-2 col-sm-2" data-bind="if: restrictDateRangeFilter">
                                        <input class="form-control" type="number" data-bind="value: restrictDateRangeNumber" />
                                    </div>
                                    <div class="col-md-3 col-sm-3" data-bind="if: restrictDateRangeFilter">
                                        <select class="form-control" data-bind="value: restrictDateRangeValue">
                                            <option value="Days">Day(s)</option>
                                            <option value="Months">Month(s)</option>
                                            <option value="Years">Year(s)</option>
                                        </select>
                                    </div>
                                    <div class="col-md-2 col-sm-2">
                                        <span data-toggle="tooltip" data-placement="right" class="fa fa-question-circle helptip" title="If this is selected, user will not be able to pick date range larger than this selection"></span>
                                    </div>
                                </div>
                            </div>
                        </div>
                        <div role="tabpanel" class="tab-pane" id="column-foreign">
                            <div class="control-group" data-bind="if: $root.isStoredProcColumn() == false">
                                <div class="form-group row">
                                    <label class="col-md-3 col-sm-3 control-label" data-bind="">Foreign Key</label>
                                    <div class="col-md-6 col-sm-6">
                                        <div class="checkbox">
                                            <label>
                                                <input class="check-box" data-val="true" data-val-required="The ForeignKey field is required." id="ForeignKey" name="ForeignKey" type="checkbox" value="true" data-bind="checked:ForeignKey"><input name="ForeignKey" type="hidden" value="false">
                                            </label>
                                        </div>
                                    </div>
                                    <div class="col-md-3 col-sm-3">
                                        <span data-toggle="tooltip" data-placement="right" class="fa fa-question-circle helptip" title="Foreign keys allows friendly selection in Filters using a Dropdown List"></span>
                                    </div>
                                </div>
                            </div>
                            <div class="control-group" data-bind="if: $root.isStoredProcColumn() == false">
                                <div class="form-group row">
                                    <label class="col-md-3 col-sm-3 control-label" data-bind="visible:ForeignKey(), attr: {required: ForeignKey()?'True':null, placeholder: 'Ex tblEmployees'}" placeholder="Ex tblEmployees" style="display: none;">Foreign Table</label>
                                    <div class="col-md-6 col-sm-6">
                                        <select class="form-control" data-bind="value: JoinTable, visible:ForeignKey(), options: $root.Tables.model, optionsText: 'TableName'" placeholder="Ex tblEmployees" style="display: none;"></select>

                                    </div>
                                    <div class="col-md-3 col-sm-3">
                                        <span class="field-validation-valid help-inline indent" data-valmsg-for="ForeignTable" data-valmsg-replace="true"></span>
                                    </div>
                                </div>
                            </div>
                            <div class="control-group" data-bind="if: $root.isStoredProcColumn() == false">
                                <div class="form-group row">
                                    <label class="col-md-3 col-sm-3 control-label" data-bind="visible:ForeignKey(), attr: {required: ForeignKey()?'True':null}" style="display: none;">Foreign Join</label>
                                    <div class="col-md-6 col-sm-6">
                                        <select class="form-control" id="ForeignJoin_SelectedId" name="ForeignJoin.SelectedId" data-bind="value: ForeignJoin, visible:ForeignKey(), attr: {required: ForeignKey()?'True':null}" style="display: none;">
                                            <option selected="selected" value="Inner">Inner</option>
                                            <option value="Left">Left</option>
                                            <option value="Right">Right</option>
                                        </select>
                                    </div>
                                    <div class="col-md-3 col-sm-3">
                                        <span class="field-validation-valid help-inline indent" data-valmsg-for="ForeignJoin" data-valmsg-replace="true"></span>
                                    </div>
                                </div>
                            </div>
                            <div class="control-group" data-bind="if: $root.isStoredProcColumn() == false">
                                <div class="form-group row">
                                    <label class="col-md-3 col-sm-3 control-label" data-bind="visible:ForeignKey(), attr: {required: ForeignKey()?'True':null, placeholder: 'Ex EmployeeId'}" placeholder="Ex EmployeeId" style="display: none;">Foreign Key Field</label>
                                    <div class="col-md-6 col-sm-6">
                                        <select class="form-control" id="ForeignKeyField" name="ForeignKeyField" data-bind="options: JoinTable().Columns, optionsText: 'ColumnName', optionsValue: 'ColumnName', value: ForeignKeyField, visible:ForeignKey(), attr: {required: ForeignKey()?'True':null}" placeholder="Ex EmployeeId" style="display: none;"></select>
                                    </div>
                                    <div class="col-md-3 col-sm-3">
                                        <span data-bind="visible:ForeignKey()" data-toggle="tooltip" data-placement="right" class="fa fa-question-circle helptip" style="display: none;" title="Foreign Key field is a Column from the Foreign table which is used as the Key field"></span>
                                    </div>
                                </div>
                            </div>
                            <div class="control-group" data-bind="if: $root.isStoredProcColumn() == false">
                                <div class="form-group row">
                                    <label class="col-md-3 col-sm-3 control-label" data-bind="visible:ForeignKey(), attr: {required: ForeignKey()?'True':null, placeholder: 'Ex EmployeeName'}" placeholder="Ex EmployeeName" style="display: none;">Foreign Value Field</label>
                                    <div class="col-md-6 col-sm-6">
                                        <select class="form-control" id="ForeignValueField" name="ForeignValueField" data-bind="options: JoinTable().Columns, optionsText: 'ColumnName', optionsValue: 'ColumnName', value: ForeignValueField, visible:ForeignKey(), attr: {required: ForeignKey()?'True':null}" placeholder="Ex EmployeeName" style="display: none;"></select>
                                    </div>
                                    <div class="col-md-3 col-sm-3">
                                        <span data-bind="visible:ForeignKey()" data-toggle="tooltip" data-placement="right" class="fa fa-question-circle helptip" style="display: none;" title="Foreign Value field is a Column from the Foreign table which is used to display the value to the User in the Report Designer"></span>
                                    </div>
                                </div>
                            </div>

                            <div data-bind="if: ForeignKey">
                                <div class="alert alert-info">
                                    You can setup a cascading filter by setting up a parent for the foreign key below
                                </div>
                                <div class="control-group">
                                    <div class="form-group row">
                                        <label class="col-md-3 col-sm-3 control-label">Setup Foreign Key Parent?</label>
                                        <div class="col-md-6 col-sm-6">
                                            <div class="checkbox">
                                                <label>
                                                    <input class="check-box" type="checkbox" data-bind="checked:ForeignParentKey">
                                                </label>
                                            </div>
                                        </div>
                                        <div class="col-md-3 col-sm-3">
                                            <span data-toggle="tooltip" data-placement="right" class="fa fa-question-circle helptip" title="Foreign keys parent allows cascading dropdown selection in Filters"></span>
                                        </div>
                                    </div>
                                </div>
                                <div class="control-group">
                                    <div class="form-group row">
                                        <label class="col-md-3 col-sm-3 control-label" data-bind="visible:ForeignParentKey(), attr: {required: ForeignParentKey()?'True':null, placeholder: 'Ex tblEmployees'}" placeholder="Ex tblEmployees" style="display: none;">Parent Table</label>
                                        <div class="col-md-6 col-sm-6">
                                            <select class="form-control" data-bind="value: ForeignJoinTable, visible:ForeignParentKey(), options: $root.Tables.model, optionsText: 'TableName'" placeholder="Ex tblEmployees" style="display: none;"></select>

                                        </div>
                                        <div class="col-md-3 col-sm-3">
                                            <span class="field-validation-valid help-inline indent" data-valmsg-for="ForeignJoinTable" data-valmsg-replace="true"></span>
                                        </div>
                                    </div>
                                </div>

                                <div class="control-group" data-bind="visible:ForeignParentKey()" style="display: none;">
                                    <div class="form-group row">
                                        <label class="col-md-3 col-sm-3 control-label" data-bind="attr: {required: ForeignParentKey()?'True':null, placeholder: 'Ex EmployeeId'}">Parent Key Field</label>
                                        <div class="col-md-6 col-sm-6">
                                            <select class="form-control" data-bind="options: ForeignJoinTable().Columns, optionsText: 'ColumnName', optionsValue: 'ColumnName', value: ForeignParentKeyField, attr: {required: ForeignParentKey()?'True':null}" placeholder="Ex EmployeeId"></select>
                                        </div>
                                        <div class="col-md-3 col-sm-3">
                                            <span data-toggle="tooltip" data-placement="right" class="fa fa-question-circle helptip" title="Parent Key field is a Column from the Parent table which is used as the key in cascading filters"></span>
                                        </div>
                                    </div>
                                </div>
                                <div class="control-group" data-bind="visible:ForeignParentKey()" style="display: none;">
                                    <div class="form-group row">
                                        <label class="col-md-3 col-sm-3 control-label" data-bind="attr: {required: ForeignParentKey()?'True':null, placeholder: 'Ex EmployeeName'}">Parent Value Field</label>
                                        <div class="col-md-6 col-sm-6">
                                            <select class="form-control" data-bind="options: ForeignJoinTable().Columns, optionsText: 'ColumnName', optionsValue: 'ColumnName', value: ForeignParentValueField, attr: {required: ForeignParentKey()?'True':null}" placeholder="Ex EmployeeName"></select>
                                        </div>
                                        <div class="col-md-3 col-sm-3">
                                            <span data-toggle="tooltip" data-placement="right" class="fa fa-question-circle helptip" title="Parent Value field is a Column from the Parent table which is used to display the value to the User in the Report Designer for cascading filters"></span>
                                        </div>
                                    </div>
                                </div>
                                <div class="control-group" data-bind="visible:ForeignParentKey()" style="display: none;">
                                    <div class="form-group row">
                                        <label class="col-md-3 col-sm-3 control-label" data-bind="attr: {required: ForeignParentKey()?'True':null, placeholder: 'Ex EmployeeName'}">Foreign Key Filter Field</label>
                                        <div class="col-md-6 col-sm-6">
                                            <select class="form-control" data-bind="options: JoinTable().Columns, optionsText: 'ColumnName', optionsValue: 'ColumnName', value: ForeignParentApplyTo, attr: {required: ForeignParentKey()?'True':null}" placeholder="Ex EmployeeName"></select>
                                        </div>
                                        <div class="col-md-3 col-sm-3">
                                            <span data-toggle="tooltip" data-placement="right" class="fa fa-question-circle helptip" title="Foreign Key Filter Value field is a Column from the Foreign table where the selected parent filter will be applied for cascading filters"></span>
                                        </div>
                                    </div>
                                </div>
                                <!--<div class="control-group">
                                    <div class="form-group row">
                                        <label class="col-md-3 col-sm-3 control-label">Require Foreign Key Parent?</label>
                                        <div class="col-md-6 col-sm-6">
                                            <div class="checkbox">
                                                <label>
                                                    <input class="check-box" type="checkbox" data-bind="checked:ForeignParentRequired">
                                                </label>
                                            </div>
                                        </div>
                                        <div class="col-md-3 col-sm-3">
                                            <span data-toggle="tooltip" data-placement="right" class="fa fa-question-circle helptip" title="Foreign keys parent can be optional or set to required for cascading filters"></span>
                                        </div>
                                    </div>
                                </div>-->
                            </div>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button class="btn btn-secondary" title="Manage Role Access" data-toggle="modal" data-target="#role-access-modal" data-bind="click: $root.selectAllowedRoles">
                            <i class="fa fa-user"></i>Manage Role Access
                        </button>
                        <button type="button" class="btn btn-secondary" data-dismiss="modal">Done</button>
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>

<div class="modal" id="procedure-modal" role="dialog" tabindex="-2">
    <div class="modal-dialog modal-lg" role="document">
        <div class="modal-content ">
            <div class="modal-header">
                <h4 class="modal-title">Add Stored Proc</h4>
                <button type="button" class="close" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button>
            </div>
            <div class="modal-body">
                <div class="row">
                    <label class="col-md-2 control-label">
                        Search for
                    </label>
                    <div class="col-md-6">
                        <input type="text" class="form-control" data-bind="value: searchProcedureTerm" placeholder="Search stored procedures by name" />
                    </div>
                    <div class="col-md-2">
                        <button class="btn btn-primary" data-bind="click: searchStoredProcedure">Search</button>
                    </div>
                </div>
                <br />
                <div class="menu row" style="margin-left: 20px;" data-bind="foreach: foundProcedures">
                    <div class="menu-category card">
                        <div class="card-header clearfix" style="">
                            <div class="pull-left">
                                <label>
                                    <span data-bind="text: TableName"></span>
                                    <span class="label-xs">Procedure</span>
                                </label>
                                <button class="btn btn-sm" title="Save this Procedure" data-bind="click: function(){$root.saveProcedure($data.TableName, true);}">
                                    <span class="fa fa-save"></span>
                                </button>
                            </div>
                            <a class="pull-right" data-toggle="collapse" data-bind="attr: {'data-target': '#sp_'+$index()}">
                                <span class="fa fa-chevron-down"></span>
                            </a>
                        </div>
                        <div data-bind="attr: {id: 'sp_'+$index()}" class="panel-collapse collapse in">
                            <div class="card-body">
                                Display Name<br />
                                <span data-bind="editable: DisplayName"></span>
                                <label class="small">Columns</label>

                                <div class="list-group" data-bind="foreach: Columns">
                                    <div class="list-group-item">
                                        <span data-bind="editable: DisplayName"></span>
                                        <span class="badge badge-info" data-bind="text: FieldType"></span>
                                    </div>
                                </div>
                                <label class="small">Paramters</label>

                                <div class="list-group" data-bind="foreach: Parameters">
                                    <div class="list-group-item">
                                        <span data-bind="editable: DisplayName"></span>
                                        <span class="badge badge-info" data-bind="text: ParameterDataTypeString"></span><br />
                                        Default Value: <span data-bind="editable: ParameterValue"></span>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-primary" data-dismiss="modal">Done</button>
            </div>
        </div>
    </div>
</div>

<div class="modal" id="parameter-modal" role="dialog" tabindex="-1">
    <div class="modal-dialog" role="document">
        <div class="modal-content" data-bind="with: editParameter()">
            <div class="modal-header">
                <h4 class="modal-title" id="myModalLabel">
                    Manage
                    <label data-bind="text: ParameterName"></label>
                </h4>
                <button type="button" class="close" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button>
            </div>
            <div class="modal-body">
                <p>Choose options that will determine how users interact with this parameter on Reports</p>
                <div class="form-horizontal">
                    <div class="control-group">
                        <div class="form-group row">
                            <label class="col-md-3 col-sm-3 control-label" data-bind="attr:{placeholder:'Ex First Name'}" placeholder="Ex First Name">Display Name</label>
                            <div class="col-md-6 col-sm-6">
                                <input class="form-control text-box single-line" data-val="true" data-val-required="The DisplayName field is required." name="DisplayName" type="text" value="First Name" data-bind="value: DisplayName, attr:{placeholder:'Ex First Name'}" placeholder="Ex First Name">
                            </div>
                            <div class="col-md-3 col-sm-3">
                                <span data-toggle="tooltip" data-placement="right" class="fa fa-question-circle helptip" title="This is a friendly name to display to your users"></span>
                            </div>
                        </div>
                    </div>
                    <div class="control-group">
                        <div class="form-group row">
                            <label class="col-md-3 col-sm-3 control-label">Default Value</label>
                            <div class="col-md-6 col-sm-6">
                                <span data-bind="editable: ParameterValue"></span>
                            </div>
                        </div>
                    </div>
                    <div class="control-group">
                        <div class="form-group row">
                            <label class="col-md-3 col-sm-3 control-label" data-bind="">Hidden Parameter</label>
                            <div class="col-md-6 col-sm-6">
                                <div class="checkbox">
                                    <label>
                                        <input class="check-box" data-val="true" type="checkbox" value="true" data-bind="checked: Hidden, enable: ParameterValue">
                                    </label>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="control-group">
                        <div class="form-group row">
                            <label class="col-md-3 col-sm-3 control-label" data-bind="">Required</label>
                            <div class="col-md-6 col-sm-6">
                                <div class="checkbox">
                                    <label>
                                        <input class="check-box" data-val="true" type="checkbox" value="true" data-bind="checked:Required">
                                    </label>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="control-group">
                        <div class="form-group row">
                            <label class="col-md-3 col-sm-3 control-label" data-bind="">Use Lookup Table</label>
                            <div class="col-md-6 col-sm-6">
                                <div class="checkbox">
                                    <label>
                                        <input class="check-box" data-val="true" name="ForeignKey" type="checkbox" value="true" data-bind="checked:ForeignKey"><input name="ForeignKey" type="hidden" value="false">
                                    </label>
                                </div>
                            </div>
                            <div class="col-md-3 col-sm-3">
                                <span data-toggle="tooltip" data-placement="right" class="fa fa-question-circle helptip" title="Using lookup table allows friendly selection in report using a Dropdown List"></span>
                            </div>
                        </div>
                    </div>
                    <div class="control-group">
                        <div class="form-group row">
                            <label class="col-md-3 col-sm-3 control-label" data-bind="visible:ForeignKey(), attr: {required: ForeignKey()?'True':null, placeholder: 'Ex tblEmployees'}" placeholder="Ex tblEmployees" style="display: none;">Lookup Table</label>
                            <div class="col-md-6 col-sm-6">
                                <select class="form-control" data-bind="value: JoinTable, visible:ForeignKey(), options: $root.Procedures.tables, optionsText: 'TableName'" placeholder="Ex tblEmployees" style="display: none;"></select>

                            </div>
                            <div class="col-md-3 col-sm-3">
                                <span class="field-validation-valid help-inline indent" data-valmsg-for="ForeignTable" data-valmsg-replace="true"></span>
                            </div>
                        </div>
                    </div>
                    <div class="control-group">
                        <div class="form-group row">
                            <label class="col-md-3 col-sm-3 control-label" data-bind="visible:ForeignKey(), attr: {required: ForeignKey()?'True':null, placeholder: 'Ex EmployeeId'}" placeholder="Ex EmployeeId" style="display: none;">Lookup Key Field</label>
                            <div class="col-md-6 col-sm-6">
                                <select class="form-control" name="ForeignKeyField" data-bind="options: JoinTable().Columns, optionsText: 'ColumnName', optionsValue: 'ColumnName', value: ForeignKeyField, visible:ForeignKey(), attr: {required: ForeignKey()?'True':null}" placeholder="Ex EmployeeId" style="display: none;"></select>
                            </div>
                            <div class="col-md-3 col-sm-3">
                                <span data-bind="visible:ForeignKey()" data-toggle="tooltip" data-placement="right" class="fa fa-question-circle helptip" style="display: none;" title="Foreign Key field is a Column from the Foreign table which is used as the Key field"></span>
                            </div>
                        </div>
                    </div>
                    <div class="control-group">
                        <div class="form-group row">
                            <label class="col-md-3 col-sm-3 control-label" data-bind="visible:ForeignKey(), attr: {required: ForeignKey()?'True':null, placeholder: 'Ex EmployeeName'}" placeholder="Ex EmployeeName" style="display: none;">Lookup Value Field</label>
                            <div class="col-md-6 col-sm-6">
                                <select class="form-control" name="ForeignValueField" data-bind="options: JoinTable().Columns, optionsText: 'ColumnName', optionsValue: 'ColumnName', value: ForeignValueField, visible:ForeignKey(), attr: {required: ForeignKey()?'True':null}" placeholder="Ex EmployeeName" style="display: none;"></select>
                            </div>
                            <div class="col-md-3 col-sm-3">
                                <span data-bind="visible:ForeignKey()" data-toggle="tooltip" data-placement="right" class="fa fa-question-circle helptip" style="display: none;" title="Lookup Value field is a Column from the Lookup table which is used to display the value to the User in the Report Designer"></span>
                            </div>
                        </div>
                    </div>

                </div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-dismiss="modal">Done</button>
            </div>
        </div>
    </div>
</div>

<div class="modal fade" id="role-access-modal" role="dialog" tabindex="-2">
    <div class="modal-dialog" role="document">
        <div class="modal-content" data-bind="with: editAllowedRoles">
            <div class="modal-header">
                <h4 class="modal-title">Manage Access by Roles</h4>
                <button type="button" class="close" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button>
            </div>
            <div class="modal-body">
                <p>Choose User Roles that will have Access</p>
                <p class="small">Note: All Users will have access if no restricted roles are setup</p>
                <div class="list-group" data-bind="foreach: AllowedRoles">
                    <div class="list-group-item" style="padding-top: 2px; padding-bottom: 2px;">
                        <span data-bind="text: $data"></span>
                        <button class="btn btn-sm btn-danger pull-right"><i class="fa fa-trash" data-bind="click: $root.removeAllowedRole"></i></button>
                    </div>
                </div>

                <div class="row">
                    <div class="col-10">
                        <input class="form-control" type="text" data-bind="value: $root.newAllowedRole" placeholder="Ex Sales">
                    </div>
                    <div class="col-2">
                        <button class="btn btn-primary" data-bind="click: $root.addAllowedRole">Add</button>
                    </div>
                </div>

            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-dismiss="modal">Done</button>
            </div>
        </div>
    </div>
</div>

<div class="clearfix"></div>

<div class="modal" id="add-connection-modal" role="dialog">
    <div class="modal-dialog" role="document">
        <div class="modal-content" data-bind="with: newDataConnection">
            <div class="modal-header">
                <h4 class="modal-title">Add a new Data Connection</h4>
                <button type="button" class="close" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button>
            </div>
            <div class="modal-body">
                <p>Choose User Roles that will have Access</p>
                <p class="small">Note: All Users will have access if no restricted roles are setup</p>

                <div class="form-group row">
                    <label class="col-md-3 col-sm-3 control-label">Connection Name</label>
                    <div class="col-md-6 col-sm-6">
                        <input class="form-control text-box" data-val="true" data-val-required="Connection Name is required." type="text" data-bind="value: Name" placeholder="" id="add-conn-name">
                    </div>
                </div>
                <div class="form-group row">
                    <label class="col-md-3 col-sm-3 control-label">Connection Key</label>
                    <div class="col-md-6 col-sm-6">
                        <input class="form-control text-box" data-val="true" data-val-required="Connection Key is required." type="text" data-bind="value: ConnectionKey" placeholder="" id="add-conn-key">
                    </div>
                    <div class="col-md-3 col-sm-3">
                        <span data-toggle="tooltip" data-placement="right" class="fa fa-question-circle helptip" title="This is the Connection Key in your web.config for your SQL Connection String"></span>
                    </div>
                </div>
                <div class="form-group row">
                    <div class="check-box col-md-12">
                        <label>
                            <input type="checkbox" data-bind="checked: copySchema">
                            Copy Schema from existing Connection
                        </label>
                    </div>
                </div>
                <div class="form-group row" data-bind="visible: copySchema">
                    <label class="col-md-3 col-sm-3 control-label">Choose Connection</label>
                    <div class="col-md-6 col-sm-6">
                        <select class="form-control" style="max-width: 300px;" data-bind="options: $root.DataConnections, optionsText: 'DataConnectName', optionsValue: 'DataConnectGuid', value: copyFrom"></select>
                    </div>
                </div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-bind="click: $root.addDataConnection">Create</button>
            </div>
        </div>
    </div>
</div>
<div class="clearfix"></div>

<div class="modal" id="custom-sql-modal" tabindex="-1" role="dialog" aria-labelledby="myModalLabel" aria-hidden="true">
    <div class="modal-dialog modal-lg" data-bind="with: customSql">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title">Enter Select SQL for your Data</h5>
                <button type="button" class="close" data-dismiss="modal" aria-label="Close">
                    <span aria-hidden="true">&times;</span>
                </button>
            </div>
            <div class="modal-body">
                <div class="form-group" data-bind="validationElement: customTableName">
                    <label for="custom-table-name">Custom Table Name</label>
                    <input type="text" class="form-control" name="customTableName" placeholder="Enter table name" data-bind="textInput: customTableName" required>
                    <div class="invalid-feedback">Custom Table Name is required.</div>
                </div>
                <div class="checkbox">
                    <label>
                        <input class="check-box" type="checkbox" data-bind="checked: useAi"> Use ChatGPT to build the query?
                    </label>
                </div>
                <div data-bind="visible: useAi">
                    <p id="query-input" class="query-input">
                        Show me&nbsp;
                    </p>
                    <button data-bind="click: buildSqlUsingAi" class="btn btn-primary btn-sm">Process with ChatGPT</button>
                    <button data-bind="click: textQuery.resetQuery" class="btn btn-secondary btn-sm">Start over</button>
                    <hr />
                </div>

                <div class="form-group" data-bind="validationElement: customSql">
                    <label for="custom-sql">Custom SQL</label> | <a href="#" data-bind="click: beautifySql">Beautify Sql</a>
                    <textarea class="form-control" style="height: 240px;" name="customSql" data-bind="textInput: customSql" required></textarea>
                    <div class="invalid-feedback">Custom SQL is required.</div>
                </div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-dismiss="modal">Cancel</button>
                <button type="button" class="btn btn-primary" data-bind="click: executeSql">Save changes</button>
            </div>
        </div>
    </div>
</div>
</asp:Content>