﻿<%@ Page Title="Report Setup" Language="C#" MasterPageFile="~/DotNetReport/ReportLayout.Master" AutoEventWireup="true" CodeBehind="Setup.aspx.cs" Inherits="ReportBuilder.WebForms.DotNetReport.Setup" Async="true" %>

<asp:Content ID="Content2" ContentPlaceHolderID="head" runat="server">
    <link href="../Content/bootstrap-editable.css" rel="stylesheet" />    
    <link href="../Content/tribute.css" rel="stylesheet" />
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
        .table-selected rect.main-rect {
        filter: drop-shadow(3px 3px 5px #999);
        }
        .column-selected {
        font-weight: bold;
        }
        .line-highlight {
        stroke: #33f !important;
        stroke-width: 2 !important;
        }
    </style>
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="scripts" runat="server">    
    <script>$.fn.popover = { Constructor: {} };</script>
    <script src="../Scripts/tribute.min.js"></script>
    <script src="../Scripts/dotnetreport-setup.js"></script>
    <script src="../Scripts/knockout.mapping-latest.js"></script>
    <script src="../Scripts/bootstrap-editable.min.js"></script>
    <script src="../Scripts/knockout.x-editable.min.js"></script>
    <script type="text/javascript">

        $.fn.editable.defaults.mode = 'inline';

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
                if (model.Result) model = model.Result;
                var options = {
                    model: model,
                    saveTableUrl: '/ReportApi/SaveTable',
                    deleteTableUrl: '/ReportApi/DeleteTable',
                    getRelationsUrl: '/ReportApi/GetRelations',
                    getDataConnectionsUrl: '/ReportApi/GetDataConnections',
                    updateDataConnectionUrl: '/ReportApi/UpdateDataConnection',
                    saveRelationsUrl: '/ReportApi/SaveRelations',
                    addDataConnectionUrl: '/ReportApi/AddDataConnection',
                    saveProcUrl: '/ReportApi/SaveProcedure',
                    deleteProcUrl: '/ReportApi/DeleteProcedure',
                    saveCustomFuncUrl: '/ReportApi/SaveCustomFunction',
                    deleteCustomFuncUrl: '/ReportApi/DeleteCustomFunction',
                    getSchedulesUrl: '/ReportApi/GetScheduledReportsAndDashboards',
                    deleteScheduleUrl: '/ReportApi/DeleteSchedule',
                    saveCategoriesUrl: '/ReportApi/SaveCategories',
                    getCategoriesUrl: '/ReportApi/GetCategories',
                    reportsApiUrl: '/DotNetReport/ReportService.asmx/CallReportApi',
                    getUsersAndRoles: '/DotNetReport/ReportService.asmx/GetUsersAndRoles',
                    searchProcUrl: '/DotNetReport/ReportService.asmx/SearchProcedure',                    
                    getSchemaFromSql: '/DotNetReport/ReportService.asmx/GetSchemaFromSql',
                    apiUrl: '/DotNetReport/ReportService.asmx/CallReportApi',
                    getPreviewFromSqlUrl: '/DotNetReport/ReportService.asmx/GetPreviewFromSql',
                    onlyApi: queryParams.onlyApi !== 'false'
                };

                var vm = new manageViewModel(options);
                vm.LoadJoins();
                vm.setupManageAccess();
                vm.LoadSchedules();
                vm.LoadCategories();
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
            <li role="presentation" class="nav-item">
                <a class="nav-link active" href="#tablesfields" aria-controls="home" role="tab" data-bs-toggle="tab" data-bind="click: function() { customTableMode(false); activeTable(null);}">
                    <i class="fa fa-database"></i> Database Tables
                </a>
            </li>
            <li role="presentation" class="nav-item">
                <a class="nav-link" href="#tablesfields" aria-controls="custom" role="tab" data-bs-toggle="tab" data-bind="click: function() { customTableMode(true); activeTable(null); }">
                    <i class="fa fa-table"></i> Custom Tables
                </a>
            </li>
            <li role="presentation" class="nav-item">
                <a class="nav-link" href="#relations" aria-controls="profile" role="tab" data-bs-toggle="tab">
                    <i class="fa fa-link"></i> Relations
                </a>
            </li>
            <li role="presentation" class="nav-item">
                <a class="nav-link" href="#procedure" aria-controls="procedure" role="tab" data-bs-toggle="tab">
                    <i class="fa fa-code"></i> Stored Procs
                </a>
            </li>
            <li role="presentation" class="nav-item">
                <a class="nav-link" href="#schedules" aria-controls="schedules" role="tab" data-bs-toggle="tab">
                    <i class="fa fa-clock-o"></i> Schedules
                </a>
            </li>
            <li role="presentation" class="nav-item" style="display: none;">
                <a class="nav-link" href="#functions" aria-controls="functions" role="tab" data-bs-toggle="tab">
                    <i class="fa fa-cogs"></i> Custom Functions
                </a>
            </li>
            <li role="presentation" class="nav-item">
                <a class="nav-link" href="#manageaccess" aria-controls="manageaccess" role="tab" data-bs-toggle="tab">
                    <i class="fa fa-user"></i> Manage Access
                </a>
            </li>
            <li role="presentation" class="nav-item">
                <a class="nav-link" href="#connection" aria-controls="home" role="tab" data-bs-toggle="tab">
                    <i class="fa fa-plug"></i> Data Connection
                </a>
            </li>
        </ul>

    </div>
    <br />
</div>
<!-- Tab panes -->
<div class="fix-content" style="display: none;" data-bind="visible: true">
    <div class="tab-content">
        <div role="tabpanel" class="tab-pane" id="connection">
            <b>Manage Data Connection</b>
            <p>
                Data Connection groups data schemas (including tables and stored procedures and other configuration), Reports, and Dashboards into a single environment. You can create multiple Data Connections and easily switch between them for separate environment management.
            </p>
            <div class="form-row" data-bind="visible: true">
                <div class="form-group">
                    <div class="control-group">
                        <select class="form-select" style="width:25%" data-bind="options: DataConnections, optionsText: 'DataConnectName', optionsValue: 'DataConnectGuid', value: currentConnectionKey"></select>
                        <div class="padded-top"></div>
                        <button class="btn btn-primary btn-sm" data-bind="visible: false" data-bs-toggle="modal" data-bs-target="#connection-setup-modal">Manage DB Connection</button>
                        <button class="btn btn-primary btn-sm" data-bind="click: switchConnection, visible: canSwitchConnection">Switch Connection</button>
                        <button class="btn btn-primary btn-sm" data-bind="click: editDataConnectionModal, hidden: canSwitchConnection" data-bs-toggle="modal" data-bs-target="#add-connection-modal">Edit Connection</button>
                        <button class="btn btn-primary btn-sm" data-bind="click: newDataConnectionModal" data-bs-toggle="modal" data-bs-target="#add-connection-modal">Add New Connection</button>
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
                    Define Custom Tables using SQL Select Query to use in Dotnet Report for Reporting and Analytics.
                </span>
                <span data-bind="hidden: $root.customTableMode">
                    Select Tables or Views from your Database to use in Dotnet Report for Reporting and Analytics.
                </span>
            </p>

            <div data-bind="with: Tables">
                <input type="text" class="form-control input-sm" placeholder="Filter Tables for..." data-bind="value: tableFilter, valueUpdate: 'afterkeydown'" style="float: left; width: 180px; margin-right: 5px;">
                <button class="btn btn-sm" data-bind="click: clearTableFilter,  visible: tableFilter()!='' && tableFilter()!=null"><span class="fa fa-remove"></span></button>
                <button class="btn btn-sm" data-bind="click: toggleShowAll, hidden: $root.customTableMode() || $root.onlyApi(), text: usedOnly() ? 'Show all' : 'Show used only'">Show used only</button>
                <button class="btn btn-sm btn-primary" data-bind="click: $root.customSql.addNewCustomSqlTable, visible: $root.customTableMode">Add New Custom Table</button>
                <button class="btn btn-sm btn-primary" data-bind="click: $root.loadFromDatabase, hidden: $root.customTableMode() || !$root.onlyApi()">
                    <span class="fa fa-database"></span> Load all Database Tables
                </button>
                <button class="btn btn-sm btn-primary" data-bind="click: $root.visualizeJoins">Visualize Joins</button>&nbsp;
                <button class="btn btn-sm btn-primary" data-bind="hidden: $root.customTableMode()" data-bs-toggle="modal" data-bs-target="#uploadTablesFileModal" aria-haspopup="true" aria-expanded="false">
                    <span class="fa fa-file"></span> Import Tables/Views
                </button>
                <button class="btn btn-sm btn-primary" title="Manage Categories" data-bs-toggle="modal" data-bs-target="#category-modal">
                    <span class="fa fa-server"></span> Manage Categories
                </button>
            </div>
            <div class="clearfix"></div>
            <hr />
           
            <div class="row">
                <div class="col-3 border-end" style="height: 100vh; overflow-y: auto;">
                    <div class="row">                        
                        <div data-bind="ifnot: $root.customTableMode">
                            <div class="alert alert-info">
                                <span data-bind="text: Tables.model().length"></span> Tables/Views
                                <span data-bind="text: $root.onlyApi() ? 'configured and used' : 'read from database'"></span>
                            </div>
                        </div>
                    </div>
                    <div class="menu g-3" data-bind="foreach: pagedTables">
                        <div class="menu-category card">
                            <div class="card-header clearfix">
                                <div class="checkbox pull-left">
                                    <label>
                                        <input type="checkbox" data-bind="checked: Selected" title="Check to use in Dotnet Report, uncheck to Remove">
                                        <span data-bind="text: TableName"></span>
                                        <span data-bind="visible: IsView" class="label-sm">(view)</span>
                                    </label>
                                    <button class="btn btn-sm" title="Save this Table" data-bind="click: function() { saveTable($root.keys.AccountApiKey, $root.keys.DatabaseApiKey); }">
                                        <i class="fa fa-floppy-o"></i>
                                    </button>
                                    <button class="btn btn-sm" title="Preview this Table" data-bind="click: function() { previewTable($root.keys.AccountApiKey, $root.keys.DatabaseApiKey); }">
                                        <i class="fa fa-table"></i>
                                    </button>
                                </div>
                                <a class="pull-right" data-bind="click: function() { $root.activeTable($data); }">
                                    <span class="fa fa-chevron-right"></span>
                                </a>
                            </div>
                        </div>
                    </div>
                    <div class="row mt-3">
                        <div class="d-flex flex-row align-items-center col-md-12" data-bind="with: pager">
                            <div data-bind="template: 'pager-template', data: $data"></div>
                        </div>
                    </div>

                </div>

                <div class="col-9">
                    <div data-bind="visible: pagedTables().length === 0">
                        <br />
                        <svg style="width:40px;float:left;transform:rotate(-45deg) translateY(6px);transform-origin:center center;" fill="#000000" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg" class="icon flat-line"><g stroke-width="0"></g><g stroke-linecap="round" stroke-linejoin="round" stroke="#CCCCCC" stroke-width="0.048"></g><g><path d="M18,21A13.17,13.17,0,0,1,9,8.51V3" style="fill:none;stroke:#000000;stroke-linecap:round;stroke-linejoin:round;stroke-width:2;"></path><polyline points="12 6 9 3 6 6" style="fill:none;stroke:#000000;stroke-linecap:round;stroke-linejoin:round;stroke-width:2;"></polyline></g></svg>
                        <div class="mt-3 fw-bold" style="float:left; padding-left: 5px;">No <span data-bind="visible: $root.customTableMode">Custom </span> Tables added yet, click "<span data-bind="text: customTableMode() ? 'Add New Custom Table' : 'Load All Tables'"></span>" to get started!</div>
                    </div>

                    <div data-bind="visible: !activeTable()">
                        <br />
                        <br />
                        <svg style="width:40px;float:left;transform:rotate(-45deg) translateY(6px);transform-origin:center center;" fill="#000000" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg" class="icon flat-line"><g stroke-width="0"></g><g stroke-linecap="round" stroke-linejoin="round" stroke="#CCCCCC" stroke-width="0.048"></g><g><path d="M18,21A13.17,13.17,0,0,1,9,8.51V3" style="fill:none;stroke:#000000;stroke-linecap:round;stroke-linejoin:round;stroke-width:2;"></path><polyline points="12 6 9 3 6 6" style="fill:none;stroke:#000000;stroke-linecap:round;stroke-linejoin:round;stroke-width:2;"></polyline></g></svg>
                        <div class="mt-3 fw-bold" style="float:left; padding-left: 5px;">Select a <span data-bind="visible: $root.customTableMode">Custom </span> Table, or "<span data-bind="text: customTableMode() ? 'Add New Custom Table' : 'Load All Tables'"></span>"</div>
                    </div>

                    <div data-bind="with: activeTable">
                        <div class="card my-3">
                            <div class="card-header">
                                <h5>
                                    <input type="checkbox" data-bind="checked: Selected" title="Check to use in Dotnet Report, uncheck to Remove">
                                    <span data-bind="text: TableName"></span>
                                    <span data-bind="visible: IsView" class="label-sm">(view)</span>
                                </h5>
                                <button class="btn btn-sm" title="Save this Table" data-bind="click: function() { saveTable($root.keys.AccountApiKey, $root.keys.DatabaseApiKey); }">
                                    <i class="fa fa-floppy-o"></i> Save Changes
                                </button>
                                <button class="btn btn-sm" title="Preview this Table" data-bind="click: function() { previewTable($root.keys.AccountApiKey, $root.keys.DatabaseApiKey); }">
                                    <i class="fa fa-table"></i> Preview Data
                                </button>
                            </div>
                            <div class="card-body">
                                <p class="pull-right">
                                    <button class="btn btn-sm" title="Manage Category" data-bs-toggle="modal" data-bs-target="#category-manage-modal" data-bind="click: $root.selectedCategory">
                                        <i class="fa fa-gear"></i>
                                    </button>
                                    <button class="btn btn-sm" title="Manage Role Access" data-bs-toggle="modal" data-bs-target="#role-access-modal" data-bind="click: $root.selectAllowedRoles">
                                        <i class="fa fa-user"></i>
                                    </button>
                                    <button class="btn btn-sm" title="Export this Table" data-bind="click: function() { exportTableJson(); }">
                                        <i class="fa fa-file"></i>
                                    </button>
                                </p>
                                <p>
                                    <label class="label-sm">Display Name</label><br />
                                    <span data-bind="editable: DisplayName"></span>
                                </p>
                                <p>
                                    <label class="label-sm">Schema Name</label><br />
                                    <span data-bind="editable: SchemaName"></span>
                                </p>
                                <p style="display: none;"><!-- deprecated -->
                                    <label class="label-sm">Account Id Field</label><br />
                                    <span data-bind="editable: AccountIdField"></span>
                                </p>
                                <div class="checkbox">
                                    <label>
                                        <input class="check-box" data-val="true" type="checkbox" value="true" data-bind="checked: DoNotDisplay">
                                        Do Not Display
                                    </label>
                                </div>
                                <div data-bind="if: CustomTable">
                                    <button class="btn btn-sm btn-secondary" data-bind="click: $root.customSql.viewCustomSql.bind($data)">View Custom SQL</button>
                                </div>

                                <div class="d-flex align-items-center justify-content-end gap-2">
                                    <label class="label-sm m-0"><span data-bind="text: Columns().length"></span> Columns</label>
                                    <button class="btn btn-sm btn-outline-secondary" data-bind="click: autoFormat, visible: Selected">Auto Format</button>
                                    <button class="btn btn-sm btn-outline-secondary" data-bind="click: unselectAllColumns, visible: Selected">Unselect All</button>
                                    <button class="btn btn-sm btn-outline-secondary" data-bind="click: selectAllColumns, visible: Selected">Select All</button>
                                </div>
                            </div>

                            <div class="list-group" data-bind="sortable: { data: Columns, options: { handle: '.sortable', cursor: 'move' }, afterMove: $root.columnSorted }">
                                <div class="list-group-item">
                                    <div data-bind="if: $parent.DynamicColumns()">
                                        <span data-bind="html: DisplayName, attr: { title: 'DB field is ' + ColumnName() }"></span>
                                    </div>
                                    <div class="checkbox" data-bind="if: !$parent.DynamicColumns()">
                                        <label>
                                            <input type="checkbox" data-bind="checked: Selected, enable: $parent.Selected()">
                                            <span data-bind="editable: DisplayName, attr: { title: 'DB field is ' + ColumnName() }"></span>
                                            <span class="badge text-bg-info text-white" data-bind="text: FieldType"></span>
                                            <label data-bind="visible: PrimaryKey" class="badge text-bg-primary">Primary</label>
                                            <label data-bind="visible: ForeignKey" class="badge text-bg-info text-white">Foreign</label>
                                        </label>
                                        <button class="btn btn-sm pull-right" data-bs-toggle="modal" data-bs-target="#column-modal" title="All column options" data-bind="click: $root.selectColumn.bind($data, false)">...</button>
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
        </div>
        <div role="tabpanel" class="tab-pane" id="relations">
            <b>Relations</b>
            <p>
                Setup your Database Relations for Dotnet Report to produce dynamic queries
            </p>
            <button class="btn btn-sm btn-primary" data-bind="click: AddAllRelations">Auto Add Joins</button>
            <button class="btn btn-sm btn-primary" data-bind="click: AddJoin">Add new Join</button>&nbsp;
            <button class="btn btn-sm btn-primary" data-bind="click: SaveJoins">Save Joins</button>&nbsp;
            <button class="btn btn-sm btn-primary" data-bind="click: visualizeJoins">Visualize Joins</button>&nbsp;
            <button class="btn btn-sm btn-primary" data-bind="click: ExportJoins">Export Joins</button>&nbsp;
            <button class="btn btn-sm btn-primary" data-bs-toggle="modal" data-bs-target="#uploadJoinsFileModal" aria-haspopup="true" aria-expanded="false">
                <span class="fa fa-file"></span> Import Joins
            </button>
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
                                    <span class="btn btn-sm" data-bind="click: $root.sortByPrimaryTable">
                                        <i class="fa" data-bind="css: $root.sortDirection.primaryTable() ? 'fa-sort-asc' : 'fa-sort-desc'"></i>
                                    </span>
                                </div>
                            </th>
                            <th>
                                Field
                                <div class="input-group input-group-sm">
                                    <input type="text" class="form-control input-sm" data-bind="value: primaryField, valueUpdate: 'afterkeydown'" placeholder="Search...">
                                    <span class="input-group-addon"><i class="fa fa-filter"></i></span>
                                    <span class="btn btn-sm" data-bind="click: $root.sortByField">
                                        <i class="fa" data-bind="css: $root.sortDirection.primaryField() ? 'fa-sort-asc' : 'fa-sort-desc'"></i>
                                    </span>
                                </div>
                            </th>
                            <th>
                                Join Type
                                <div class="input-group input-group-sm">
                                    <input type="text" class="form-control input-sm" data-bind="value: joinType, valueUpdate: 'afterkeydown'" placeholder="Search...">
                                    <span class="input-group-addon"><i class="fa fa-filter"></i></span>
                                    <span class="btn btn-sm" data-bind="click: $root.sortByJoinType">
                                        <i class="fa" data-bind="css: $root.sortDirection.joinType() ? 'fa-sort-asc' : 'fa-sort-desc'"></i>
                                    </span>
                                </div>
                            </th>
                            <th>
                                Join Table
                                <div class="input-group input-group-sm">
                                    <input type="text" class="form-control input-sm" data-bind="value: joinTable, valueUpdate: 'afterkeydown'" placeholder="Search...">
                                    <span class="input-group-addon"><i class="fa fa-filter"></i></span>
                                    <span class="btn btn-sm" data-bind="click: $root.sortByJoinTable">
                                        <i class="fa" data-bind="css: $root.sortDirection.joinTable() ? 'fa-sort-asc' : 'fa-sort-desc'"></i>
                                    </span>
                                </div>
                            </th>
                            <th>
                                Field<div class="input-group input-group-sm">
                                    <input type="text" class="form-control input-sm" data-bind="value: joinField, valueUpdate: 'afterkeydown'" placeholder="Search...">
                                    <span class="input-group-addon"><i class="fa fa-filter"></i></span>
                                    <span class="btn btn-sm" data-bind="click: $root.sortByJoinField">
                                        <i class="fa" data-bind="css: $root.sortDirection.joinField() ? 'fa-sort-asc' : 'fa-sort-desc'"></i>
                                    </span>
                                </div>
                            </th>
                            <th></th>
                        </tr>
                    </thead>
                    <tbody data-bind="foreach: pagedJoins">
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
                 <div class="row mt-3">
                     <div class="d-flex flex-row align-items-center col-md-12" data-bind="with: joinsPager">
                         <div data-bind="template: 'pager-template', data: $data"></div>
                     </div>
                 </div>
            </form>
            <br />
            <br />
        </div>
        <div id="procedure" class="tab-pane">
            <b>Stored Procedures</b>
            <p>
                Select and manage Stored Procedures to use in Dotnet Report for more complex and coded Reports
            </p>
            <hr />
            <button class="btn btn-sm btn-primary" data-bs-toggle="modal" data-bs-target="#procedure-modal">Add Stored Procs from database</button>
            <button class="btn btn-sm btn-primary" data-bs-toggle="modal" data-bs-target="#uploadStoredProceduresFileModal" aria-haspopup="true" aria-expanded="false">
                <span class="fa fa-file"></span> Import Stored Procedures
            </button>
            <br />
            <div data-bind="if: Procedures.savedProcedures().length == 0">
                <br />
                No Stored Procedures have been setup yet.
            </div>
            <div class="row" data-bind="if: Procedures.savedProcedures().length > 0">
                <!-- Left Panel for Stored Procedure Names -->
                <div class="col-3 border-end" style="height: 100vh; overflow-y: auto;">
                    <div class="menu g-3" data-bind="foreach: Procedures.savedProcedures" style="margin-left: 20px; padding-top: 20px;">
                        <div class="menu-category card">
                            <div class="card-header clearfix">
                                <div class="pull-left">
                                    <label>
                                        <span data-bind="text: TableName"></span>
                                    </label>
                                    <button class="btn btn-sm" title="Save this Procedure" data-bind="click: function() { $root.saveProcedure($data.TableName, false); }">
                                        <span class="fa fa-save"></span>
                                    </button>
                                    <button class="btn btn-sm" title="Delete this Procedure" data-bind="click: function() { $data.deleteTable($root.keys.AccountApiKey, $root.keys.DatabaseApiKey); }">
                                        <span class="fa fa-trash"></span>
                                    </button>
                                </div>
                                <!-- Click to activate procedure -->
                                <a class="pull-right" data-bind="click: function() { $root.activeProcedure($data); }">
                                    <span class="fa fa-chevron-right"></span>
                                </a>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Right Panel for Stored Procedure Details -->
                <div class="col-9">
                    <div data-bind="with: activeProcedure">
                        <div class="card my-3">
                            <div class="card-header">
                                <h5 data-bind="text: TableName"></h5>
                                <button class="btn btn-sm" title="Save this Procedure" data-bind="click: function() { $root.saveProcedure($data.TableName, false); }">
                                    <span class="fa fa-save"></span> Save Changes
                                </button>
                                <button class="btn btn-sm" title="Delete this Procedure" data-bind="click: function() { $data.deleteTable($root.keys.AccountApiKey, $root.keys.DatabaseApiKey); }">
                                    <span class="fa fa-trash"></span> Delete
                                </button>
                            </div>
                            <div class="card-body">
                                <p class="pull-right">
                                    <button class="btn btn-sm" title="Manage Role Access" data-bs-toggle="modal" data-bs-target="#role-access-modal" data-bind="click: $root.selectAllowedRoles">
                                        <i class="fa fa-user"></i>
                                    </button>
                                    <button class="btn btn-sm" title="Export this Procedure" data-bind="click: function() { $root.exportProcedureJson($data.TableName); }">
                                        <i class="fa fa-file"></i>
                                    </button>
                                </p>
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
                                            <span data-bind="editable: DisplayName, attr: { title: 'DB field is ' + ColumnName() }"></span>
                                            <span class="badge text-bg-info text-white" data-bind="text: FieldType"></span>
                                        </label>
                                        <button class="btn btn-sm pull-right" data-bs-toggle="modal" data-bs-target="#column-modal" title="All column options" data-bind="click: $root.selectColumn.bind($data, true)">...</button>
                                    </div>
                                </div>
                                <label class="small">Parameters</label>
                                <div class="list-group" data-bind="foreach: Parameters">
                                    <div class="list-group-item">
                                        <label>
                                            <span data-bind="editable: DisplayName, attr: { title: 'DB field is ' + ParameterName() }"></span>
                                            <span class="badge text-bg-info text-white" data-bind="text: ParameterDataTypeString"></span>
                                            &nbsp;&nbsp;
                                            <button class="btn btn-sm btn-primary pull-right" data-bs-toggle="modal" data-bs-target="#parameter-modal" title="All Parameter options" data-bind="click: $root.editParameter">...</button>
                                        </label>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

        </div>
        <div id="manageaccess" class="tab-pane">
            <div data-bind="foreach: reportsAndFolders" class="card">
                <div class="card-body">
                    <a class="btn btn-link" role="button" data-bs-toggle="collapse" data-bind="attr: {href: '#folder-' + folderId }">
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
                                            Manage by User <span class="badge text-bg-info text-white" data-bind="text: userId ? userId : 'Any User'"></span>
                                            <br />
                                            View only by User <span class="badge text-bg-info text-white" data-bind="text: (viewOnlyUserId ? viewOnlyUserId : (userId ? userId : 'Any User'))"></span>
                                            <br />
                                            <div data-bind="if: deleteOnlyUserId">
                                                Delete only by User <span class="badge text-bg-info text-white" data-bind="text: deleteOnlyUserId"></span>
                                                <br />
                                            </div>
                                            <div data-bind="if: userRole">
                                                Manage by Role <span class="badge text-bg-info text-white" data-bind="text: userRole ? userRole : 'Any Role'"></span>
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
                                            <div>
                                                For Client <span class="badge text-bg-info text-white" data-bind="text: clientId ? clientId : 'All Clients'"></span>
                                                <br />
                                            </div>
                                        </div>
                                        <br />
                                        <br />
                                        <button class="btn btn-sm btn-primary" data-bind="click: function() { changeAccess(!changeAccess())}, text: !changeAccess() ? 'Change Access': 'Cancel Changing Access', hidden: changeAccess">Change Access</button>
                                    </div>

                                    <div data-bind="if: changeAccess" class="col-md-9">
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
        <div role="tabpanel" class="tab-pane" id="functions" data-bind="with: Functions">
            <b>Custom Functions</b>
            <p>
                Create and manage Custom Function that you can use in building Reports and Dashboards
            </p>
            <button class="btn btn-sm btn-primary" data-bind="click: createNewFunction">Add new Function</button>
            <br />
            <hr />

            <div class="container-fluid card card-body">
                <div class="row">
                    <div class="col-md-3">
                        <input id="searchInput" class="form-control mb-2" type="text" placeholder="Search..." data-bind="value: search, valueUpdate: 'input', visible: functions().length > 0">
                        <ul class="list-group" data-bind="foreach: filteredFunctions">
                            <li class="list-group-item d-flex justify-content-between align-items-center">
                                <span data-bind="text: name"></span>
                                <div>
                                    <button class="btn btn-primary btn-sm" data-bind="click: $parent.selectFunction">Edit</button>
                                    <button class="btn btn-danger btn-sm" data-bind="click: function() { $parent.deleteFunction($data) }">Delete</button>
                                </div>
                            </li>
                        </ul>
                        <div data-bind="if: functions().length == 0">
                            No Functions found
                        </div>
                    </div>
                    <div class="col-md-9" data-bind="with: selectedFunction">
                        <div style="border-left: 1px solid; padding-left: 10px;">
                            <div class="mb-3" data-bind="validationElement: functionName">
                                <label for="functionName" class="form-label">Function Name</label>
                                <input id="functionName" class="form-control" required data-bind="value: name, valueUpdate: 'input'" />
                                <div class="invalid-feedback">Function Name is required.</div>
                            </div>
                            <div class="mb-3">
                                <label for="functionType" class="form-label">Function Type</label>
                                <select id="functionType" class="form-control" data-bind="value: functionType, event: { change: $parent.updateCodeEditorMode }">
                                    <option value="javascript">JavaScript</option>
                                    <option value="csharp">C#</option>
                                    <option value="sql">SQL</option>
                                </select>
                            </div>
                            <div class="mb-3">
                                <label for="functionDescription" class="form-label">Description</label>
                                <textarea id="functionDescription" class="form-control" data-bind="value: description, valueUpdate: 'input'" placeholder="Add any helpful description here for the user..."></textarea>
                            </div>

                            <div>
                                <h4>Parameters</h4>
                                <table class="table table-bordered">
                                    <thead>
                                        <tr>
                                            <th>Name</th>
                                            <th>Display Name</th>
                                            <th>Description</th>
                                            <th>Required</th>
                                            <th></th>
                                        </tr>
                                    </thead>
                                    <tbody data-bind="foreach: parameters">
                                        <tr>
                                            <td>
                                                <input class="form-control" data-bind="value: parameterName, valueUpdate: 'input'" />
                                                <span class="text-danger" data-bind="visible: name.hasError, text: name.validationMessage"></span>
                                            </td>
                                            <td><input class="form-control" data-bind="value: displayName, valueUpdate: 'input'" /></td>
                                            <td><input class="form-control" data-bind="value: description, valueUpdate: 'input'" placeholder="Friendly description for user..." /></td>
                                            <td><input type="checkbox" data-bind="checked: required" /></td>
                                            <td><button class="btn btn-sm btn-danger" data-bind="click: $parent.removeParameter">Remove</button></td>
                                        </tr>
                                    </tbody>
                                </table>
                                <button class="btn btn-sm btn-primary" data-bind="click: addParameter">Add Parameter</button>
                            </div>
                            <br />
                            <div class="mb-3">
                                <label for="codeEditor" class="form-label">Write the <b>Code</b> for the function below:</label>
                                <div style="border: 1px solid">
                                    <textarea id="codeEditor" class="code-editor form-control" data-bind="value: code"></textarea>
                                </div>
                            </div>
                            <button class="btn btn-primary" data-bind="click: $parent.saveFunction">Save</button>
                            <button class="btn btn-secondary" data-bind="click: $parent.cancelEdit">Cancel</button>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <div role="tabpanel" class="tab-pane" id="schedules">
            <b>Scheduled Reports</b>
            <p>
                View Schedules setup for sending Reports and Dashboards
            </p>
            <hr />

            <div class="row">
                <div class="col-md-12">
                    <div data-bind="visible: schedules().length === 0">                            
                        <i class="fa fa-exclamation-circle"></i> No scheduled Reports or Dashboards available.
                    </div>
                    <div class="table-responsive" data-bind="if: schedules().length > 0">
                        <table class="table table-condensed table-striped table-hover">
                            <thead>
                                <tr>
                                    <th style="border-top: none;"><i class="fa fa-clock-o"></i> Schedule</th>
                                    <th style="border-top: none;"><i class="fa fa-envelope"></i> Email To</th>
                                    <th style="border-top: none;"><i class="fa fa-file"></i> Format</th>
                                    <th style="border-top: none;"><i class="fa fa-history"></i> Last Run</th>
                                    <th style="border-top: none;"><i class="fa fa-user"></i> User ID</th>
                                    <th style="border-top: none;"><i class="fa fa-globe"></i> Time Zone</th>
                                    <th style="border-top: none;"><i class="fa fa-cogs"></i> Actions</th>
                                </tr>
                            </thead>
                            <tbody data-bind="foreach: schedules">                                    
                                <tr >
                                    <td colspan="7">                                        
                                        For <span class="text-muted" data-bind="text: DashboardId === 0 ? 'Report' : 'Dashboard'"></span> <b><span data-bind="text: Name"></span></b>
                                    </td>
                                </tr>
                                <!-- ko foreach: Schedules -->
                                <tr>
                                    <td data-bind="text: ScheduleDisplay"></td>
                                    <td data-bind="text: EmailTo"></td>
                                    <td data-bind="text: Format"></td>
                                    <td data-bind="text: LastRun"></td>
                                    <td data-bind="text: UserId ? UserId : 'N/A'"></td>
                                    <td data-bind="text: Timezone ? Timezone : 'N/A'"></td>
                                    <td>
                                        <button class="btn btn-sm btn-danger" data-bind="click: $root.deleteSchedule">
                                            <i class="fa fa-trash"></i> Delete
                                        </button>
                                    </td>
                                </tr>
                                <!-- /ko -->
                            </tbody>
                        </table>
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
                <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
            </div>
            <div class="modal-body">
                <p>Choose options that will determine how users interact with this column on Reports</p>
                <div class="form-horizontal">

                    <ul class="nav nav-tabs" role="tablist" data-bind="if: $root.isStoredProcColumn() == false">
                        <li role="presentation" class="nav-item"><a class="nav-link active" href="#column-main" aria-controls="home" role="tab" data-bs-toggle="tab">Main Options</a></li>
                        <li role="presentation" class="nav-item"><a class="nav-link" href="#column-foreign" aria-controls="profile" role="tab" data-bs-toggle="tab">Foreign Key Options</a></li>
                        <li role="presentation" class="nav-item"><a class="nav-link" href="#column-filter" aria-controls="procedure" role="tab" data-bs-toggle="tab">Filter Options</a></li>
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
                                        <span data-bs-toggle="tooltip" data-bs-placement="right" class="fa fa-question-circle helptip" title="This is a friendly name to display to your users"></span>
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
                                        <span data-bs-toggle="tooltip" data-bs-placement="right" class="fa fa-question-circle helptip" title="If this is checked, the column will not be displayed on Report Designer, but can still be used on Global Data Filters"></span>
                                    </div>
                                </div>
                            </div>

                            <div class="control-group" data-bind="if: FieldType() == 'Json'">
                                <div class="form-group row">
                                    <label class="col-md-3 col-sm-3 control-label">JSON Data Structure</label>
                                    <div class="col-md-6 col-sm-6">
                                        <textarea class="form-control text-box single-line" data-val="true" data-val-required="The DisplayName field is required." data-bind="value: JsonStructure, attr:{placeholder:'Please paste in Sample Json with all columns for this JSON data field'}" rows="5"></textarea>
                                    </div>
                                    <div class="col-md-3 col-sm-3">
                                        <span data-bs-toggle="tooltip" data-bs-placement="right" class="fa fa-question-circle helptip" title="You can paste in a sample Json blob with all the columns you want to use in this field"></span>
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
                                        <span data-bs-toggle="tooltip" data-bs-placement="right" class="fa fa-question-circle helptip" title="If this is checked, the column will always require the user to pick a filtered value"></span>
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
                                        <span data-bs-toggle="tooltip" data-bs-placement="right" class="fa fa-question-circle helptip" title="If this is checked, the report will always require the user to pick a filter value if user picks the column in report"></span>
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
                                        <span data-bs-toggle="tooltip" data-bs-placement="right" class="fa fa-question-circle helptip" title="If this is selected, user will not be able to pick date range larger than this selection"></span>
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
                                        <span data-bs-toggle="tooltip" data-bs-placement="right" class="fa fa-question-circle helptip" title="Foreign keys allows friendly selection in Filters using a Dropdown List"></span>
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
                                        <span data-bind="visible:ForeignKey()" data-bs-toggle="tooltip" data-bs-placement="right" class="fa fa-question-circle helptip" style="display: none;" title="Foreign Key field is a Column from the Foreign table which is used as the Key field"></span>
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
                                        <span data-bind="visible:ForeignKey()" data-bs-toggle="tooltip" data-bs-placement="right" class="fa fa-question-circle helptip" style="display: none;" title="Foreign Value field is a Column from the Foreign table which is used to display the value to the User in the Report Designer"></span>
                                    </div>
                                </div>
                            </div>

                            <div data-bind="if: ForeignKey">
                                <div class="control-group">
                                    <div class="form-group row">
                                        <label class="col-md-3 col-sm-3 control-label">Only for Filter?</label>
                                        <div class="col-md-6 col-sm-6">
                                            <div class="checkbox">
                                                <label>
                                                    <input class="check-box" type="checkbox" data-bind="checked:ForeignFilterOnly">
                                                </label>
                                            </div>
                                        </div>
                                        <div class="col-md-3 col-sm-3">
                                            <span data-bs-toggle="tooltip" data-bs-placement="right" class="fa fa-question-circle helptip" title="Check this option if you would like to use this for Filtering only, and not for SQL Joins"></span>
                                        </div>
                                    </div>
                                </div>

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
                                            <span data-bs-toggle="tooltip" data-bs-placement="right" class="fa fa-question-circle helptip" title="Foreign keys parent allows cascading dropdown selection in Filters"></span>
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
                                            <span data-bs-toggle="tooltip" data-bs-placement="right" class="fa fa-question-circle helptip" title="Parent Key field is a Column from the Parent table which is used as the key in cascading filters"></span>
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
                                            <span data-bs-toggle="tooltip" data-bs-placement="right" class="fa fa-question-circle helptip" title="Parent Value field is a Column from the Parent table which is used to display the value to the User in the Report Designer for cascading filters"></span>
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
                                            <span data-bs-toggle="tooltip" data-bs-placement="right" class="fa fa-question-circle helptip" title="Foreign Key Filter Value field is a Column from the Foreign table where the selected parent filter will be applied for cascading filters"></span>
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
                                            <span data-bs-toggle="tooltip" data-bs-placement="right" class="fa fa-question-circle helptip" title="Foreign keys parent can be optional or set to required for cascading filters"></span>
                                        </div>
                                    </div>
                                </div>-->
                            </div>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button class="btn btn-secondary" title="Manage Role Access" data-bs-toggle="modal" data-bs-target="#role-access-modal" data-bind="click: $root.selectAllowedRoles">
                            <i class="fa fa-user"></i>Manage Role Access
                        </button>
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Done</button>
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>

    
<div class="modal" id="joinModal" tabindex="-1" role="dialog">
  <div class="modal-dialog modal-lg" role="document">
    <div class="modal-content">
      <div class="modal-header">
        <h5 class="modal-title">Joins Diagram</h5>
        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
      </div>
      <div class="modal-body" style="max-height:800px;overflow:auto;">
        <div id="joinDiagram" style="width:2000px;height:1200px;position:relative;border:1px solid #ccc;"></div>
      </div>
    </div>
  </div>
</div>


<div class="modal" id="procedure-modal" role="dialog" tabindex="-2">
    <div class="modal-dialog modal-lg" role="document">
        <div class="modal-content ">
            <div class="modal-header">
                <h4 class="modal-title">Add Stored Proc</h4>
                <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
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
                <div class="menu g-3" style="margin-left: 20px;" data-bind="foreach: foundProcedures">
                    <div class="menu-category card" style="float:left">
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
                            <a class="pull-right" data-bs-toggle="collapse" data-bind="attr: {'data-bs-target': '#sp_'+$index()}">
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
                                        <span class="badge text-bg-info text-white" data-bind="text: FieldType"></span>
                                    </div>
                                </div>
                                <label class="small">Paramters</label>

                                <div class="list-group" data-bind="foreach: Parameters">
                                    <div class="list-group-item">
                                        <span data-bind="editable: DisplayName"></span>
                                        <span class="badge text-bg-info text-white" data-bind="text: ParameterDataTypeString"></span><br />
                                        Default Value: <span data-bind="editable: ParameterValue"></span>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-primary" data-bs-dismiss="modal">Done</button>
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
                <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
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
                                <span data-bs-toggle="tooltip" data-bs-placement="right" class="fa fa-question-circle helptip" title="This is a friendly name to display to your users"></span>
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
                                <span data-bs-toggle="tooltip" data-bs-placement="right" class="fa fa-question-circle helptip" title="Using lookup table allows friendly selection in report using a Dropdown List"></span>
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
                                <span data-bind="visible:ForeignKey()" data-bs-toggle="tooltip" data-bs-placement="right" class="fa fa-question-circle helptip" style="display: none;" title="Foreign Key field is a Column from the Foreign table which is used as the Key field"></span>
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
                                <span data-bind="visible:ForeignKey()" data-bs-toggle="tooltip" data-bs-placement="right" class="fa fa-question-circle helptip" style="display: none;" title="Lookup Value field is a Column from the Lookup table which is used to display the value to the User in the Report Designer"></span>
                            </div>
                        </div>
                    </div>

                </div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Done</button>
            </div>
        </div>
    </div>
</div>

<div class="modal" id="role-access-modal" role="dialog" tabindex="-2">
    <div class="modal-dialog" role="document">
        <div class="modal-content" data-bind="with: editAllowedRoles">
            <div class="modal-header">
                <h4 class="modal-title">Manage Access by Roles</h4>
                <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
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
                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Done</button>
            </div>
        </div>
    </div>
</div>
<div class="modal" id="category-manage-modal" role="dialog" tabindex="-2">
    <div class="modal-dialog" role="document">
        <div class="modal-content" data-bind="with: manageCategories">
            <div class="modal-header">
                <b class="modal-title">Manage Categories for <span data-bind="text: TableName"></span></b>
                <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
            </div>
            <div class="modal-body">
                <div data-bind="visble: $root.Categories().lenth == 0">
                    No categories have been set up yet.
                </div>
                <div class="list-group" data-bind="foreach: $root.Categories">
                    <div class="checkbox list-group-item" style="padding-top: 2px; padding-bottom: 2px;">
                        <label>
                            <input type="checkbox" data-bind="checked: $parent.Categories, checkedValue: $data" />
                            <span data-bind="text: Name"></span> <!-- Display category name -->
                        </label>
                    </div>
                </div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Done</button>
            </div>
        </div>
    </div>
</div>


<div class="modal" id="category-modal" role="dialog" tabindex="-2">
    <div class="modal-dialog" role="document">
        <div class="modal-content">
            <div class="modal-header">
                <b class="modal-title">Manage Categories</b>
                <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
            </div>
            <div class="modal-body">
                <p>
                    You can organize the display of tables in the Report Designer by assigning categories here.
                </p>
                <!-- Table for Categories -->
                <table class="table table-bordered">
                    <thead>
                        <tr>
                            <th>Category Name</th>
                            <th>Description</th>
                            <th>Actions</th>
                        </tr>
                    </thead>
                    <tbody data-bind="foreach: $root.Categories">
                        <tr>
                            <td>
                                <input type="text" data-bind="value: Name, visible: $root.editingCategoryIndex() === $index()" class="form-control" />
                                <span id="cat-name" data-bind="text: Name, visible: $root.editingCategoryIndex() !== $index(), attr: { id: Id ? 'cat-name-' + Id :undefined }" style="color: #6c757d;"></span>
                            </td>
                            <td>
                                <input type="text" data-bind="value: Description, visible: $root.editingCategoryIndex() === $index()" class="form-control" />
                                <span id="cat-desc" data-bind="text: Description, visible: $root.editingCategoryIndex() !== $index(), attr: { id: Id ? 'cat-desc-' + Id :undefined }" style="color: #6c757d;"></span>
                            </td>
                            <td class="d-flex">
                                <button class="btn btn-primary btn-sm" data-bind="visible: $root.editingCategoryIndex() !== $index() && Id !== 0, click: function() { $root.toggleEdit($index()) }">
                                    <i class="fa fa-edit"></i>
                                </button>
                                <button class="btn btn-success btn-sm" data-bind="visible: $root.editingCategoryIndex() === $index() && Id !== 0, click: function() { $root.saveCategory($index()) }" style="margin-left: 5px;">
                                    <i class="fa fa-save"></i>
                                </button>
                                <button class="btn btn-danger btn-sm" data-bind="click: $root.removeCategory">
                                    <i class="fa fa-trash"></i>
                                </button>
                            </td>
                        </tr>
                    </tbody>
                    <tfoot>
                        <tr>
                            <td>
                                <input class="form-control" type="text" data-bind="value: $root.newCategoryName" placeholder="Category Name">
                            </td>
                            <td>
                                <input class="form-control" type="text" data-bind="value: $root.newCategoryDescription" placeholder="Description">
                            </td>
                            <td>
                                <button class="btn btn-primary btn-sm" data-bind="click: $root.addCategory">Add</button>
                            </td>
                        </tr>
                    </tfoot>
                </table>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-primary" data-bind="click: $root.saveCategories">Save</button>
                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Done</button>
            </div>
        </div>
    </div>
</div>

<div class="clearfix"></div>

<div class="modal" id="add-connection-modal" role="dialog">
    <div class="modal-dialog" role="document">
        <div class="modal-content" data-bind="with: newDataConnection">
            <div class="modal-header">
                <h4 class="modal-title"><span data-bind="text: $root.editingDataConnection() ? 'Edit' : 'Add a new'"></span> Data Connection</h4>
                <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
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
                        <span data-bs-toggle="tooltip" data-bs-placement="right" class="fa fa-question-circle helptip" title="This is the Connection Key in your application settings for your SQL Connection String"></span>
                    </div>
                </div>
                <div class="form-group row" data-bind="visible: $root.editingDataConnection">
                    <div class="check-box col-md-9 col-sm-9">
                        <label>
                            <input type="checkbox" data-bind="checked: UseSchema">
                            Use Schema
                        </label>
                    </div>
                    <div class="col-md-3 col-sm-3">
                        <span data-bs-toggle="tooltip" data-bs-placement="right" class="fa fa-question-circle helptip" title="Use Schema in SQL when building query"></span>
                    </div>
                </div>
                <div class="form-group row" data-bind="hidden: $root.editingDataConnection">
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
                <button type="button" class="btn btn-secondary" data-bind="click: $root.updateDataConnection, visible: $root.editingDataConnection()">Update</button>
                <button type="button" class="btn btn-secondary" data-bind="click: $root.addDataConnection, visible: !$root.editingDataConnection()">Create</button>
            </div>
        </div>
    </div>
</div>
<div class="clearfix"></div>

<div class="modal" id="data-preview-modal" tabindex="-1" role="dialog" aria-hidden="true">
    <div class="modal-dialog modal-lg">
        <div class="modal-content" data-bind="with: previewData">
            <div class="modal-header">
                <h4 class="modal-title">Preview Table</h4>
                <button type="button" class="btn-close pull-right" data-bs-dismiss="modal" aria-label="Close"></button>
            </div>
            <div class="modal-body">
                <div style="overflow-x: auto; max-height: 500px; overflow-y: auto;">
                    <table class="table table-striped table-hover table-condensed">
                        <thead style="position: sticky; top: -1px; z-index: 2; background-color: #f8f9fa;">
                            <tr>
                                <!-- ko foreach: Columns -->
                                <th>
                                    <span data-bind="text: ColumnName, attr: { title: DataType.replace('System.', '') }"></span>
                                </th>
                                <!-- /ko -->
                            </tr>
                        </thead>
                        <tbody>
                            <!-- ko foreach: Rows  -->
                            <tr>
                                <!-- ko foreach: Items -->
                                <td>
                                    <span data-bind="html: FormattedValue"></span>
                                </td>
                                <!-- /ko-->
                            </tr>
                            <!-- /ko -->
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    </div>
</div>

<div class="modal" id="custom-sql-modal" tabindex="-1" role="dialog" aria-labelledby="myModalLabel" aria-hidden="true">
    <div class="modal-dialog modal-lg" data-bind="with: customSql">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title">Enter Select SQL for your Data</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
            </div>
            <div class="modal-body">
                <div class="form-group" data-bind="validationElement: customTableName">
                    <label for="custom-table-name">Custom Table Name</label>
                    <input type="text" class="form-control" name="customTableName" placeholder="Enter table name" data-bind="textInput: customTableName" required>
                    <div class="invalid-feedback">Custom Table Name is required.</div>
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
                    <label for="custom-sql">Custom SQL</label> |
                    <label>
                        <input type="checkbox" data-bind="checked: dynamicColumns">
                        <span title="Setup a table that returns dynamic columns to pick from">Table with Dynamic Columns</span>
                    </label> |
                    <a href="#" data-bind="click: beautifySql">Beautify Sql</a>
                    <textarea class="form-control" style="height: 240px;" name="customSql" data-bind="textInput: customSql" required></textarea>
                    <div class="invalid-feedback">Custom SQL is required.</div>

                    <div data-bind="if: dynamicColumns">
                        <label>Add SQL code to use the dynamic column</label>
                        <textarea class="form-control" style="height: 240px;" name="columnTranslation" data-bind="textInput: columnTranslation" required></textarea>
                    </div>
                </div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                <button type="button" class="btn btn-primary" data-bind="click: executeSql">Save changes</button>
            </div>
        </div>
    </div>
</div>

<!-- Import Tables/Views Json File Modal -->
<div class="modal" id="uploadTablesFileModal" tabindex="-1" aria-labelledby="uploadTablesFileModalLabel" aria-hidden="true">
    <div class="modal-dialog">
        <div class="modal-content" data-bind="with: ManageTablesJsonFile">
            <div class="modal-header">
                <h5 class="modal-title" id="uploadTablesFileModalLabel">Import Tables/Views JSON File Upload</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
            </div>
            <div class="modal-body">
                <div id="dropzone" class="dropzone"
                     data-bind="event: {click: triggerTablesFileInput }"
                     style="border: 2px dashed #007bff; border-radius: 5px; padding: 30px; text-align: center; color: #007bff; cursor: pointer;">
                    click to select Json file
                </div>
                <input type="file" id="tablesFileInputJson" accept=".json" style="display: none;" data-bind="event: { change: handleTablesFileSelect }">
                <div data-bind="visible: fileName">
                    <p>Selected Tables/Views File: <span data-bind="text: fileName"></span></p>
                </div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
                <button type="button" class="btn btn-primary" data-bind="click: uploadTablesFile">Upload</button>
            </div>
        </div>
    </div>
</div>

<!-- Import Joins Json File Modal -->
<div class="modal" id="uploadJoinsFileModal" tabindex="-1" aria-labelledby="uploadJoinsFileModalLabel" aria-hidden="true">
    <div class="modal-dialog">
        <div class="modal-content" data-bind="with: ManageJoinsJsonFile">
            <div class="modal-header">
                <h5 class="modal-title" id="uploadJoinsFileModalLabel">Import Joins JSON File Upload</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
            </div>
            <div class="modal-body">
                <div id="dropzone" class="dropzone"
                     data-bind="event: {click: triggerJoinsFileInput }"
                     style="border: 2px dashed #007bff; border-radius: 5px; padding: 30px; text-align: center; color: #007bff; cursor: pointer;">
                    click to select Json file
                </div>
                <input type="file" id="joinsFileInputJson" accept=".json" style="display: none;" data-bind="event: { change: handleJoinsFileSelect }">
                <div data-bind="visible: fileName">
                    <p>Selected Joins File: <span data-bind="text: fileName"></span></p>
                </div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
                <button type="button" class="btn btn-primary" data-bind="click: uploadJoinsFile">Upload</button>
            </div>
        </div>
    </div>
</div>
<!-- Import StoredProcedures Json File Modal -->
<div class="modal" id="uploadStoredProceduresFileModal" tabindex="-1" aria-labelledby="uploadStoredProceduresFileModalLabel" aria-hidden="true">
    <div class="modal-dialog">
        <div class="modal-content" data-bind="with: ManageStoredProceduresJsonFile">
            <div class="modal-header">
                <h5 class="modal-title" id="uploadStoredProceduresFileModalLabel">Import Stored Procedures JSON File Upload</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
            </div>
            <div class="modal-body">
                <div id="dropzone" class="dropzone"
                     data-bind="event: {click: triggerStoredProceduresFileInput }"
                     style="border: 2px dashed #007bff; border-radius: 5px; padding: 30px; text-align: center; color: #007bff; cursor: pointer;">
                    click to select Json file
                </div>
                <input type="file" id="storedProceduresFileInputJson" accept=".json" style="display: none;" data-bind="event: { change: handleStoredProceduresFileSelect }">
                <div data-bind="visible: fileName">
                    <p>Selected Stored Procedures File: <span data-bind="text: fileName"></span></p>
                </div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
                <button type="button" class="btn btn-primary" data-bind="click: uploadStoredProceduresFile">Upload</button>
            </div>
        </div>
    </div>
</div>

</asp:Content>