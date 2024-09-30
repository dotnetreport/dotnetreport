<%@ Page Title="" Language="C#" MasterPageFile="~/DotNetReport/ReportLayout.Master" AutoEventWireup="true" CodeBehind="Query.aspx.cs" Inherits="ReportBuilder.Web.DotNetReport.Query" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <link href="../Content/tribute.css" rel="stylesheet" />
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="scripts" runat="server">
     <script src="../Scripts/tribute.min.js"></script>
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
                     reportWizard: $("#modal-reportbuilder"),
                     linkModal: $("#linkModal"),
                     fieldOptionsModal: $("#fieldOptionsModal"),
                     lookupListUrl: svc + "GetLookupList",
                     apiUrl: svc + "CallReportApi",
                     runReportApiUrl: svc + "RunReportApi",
                     getUsersAndRolesUrl: svc + "GetUsersAndRoles",
                     reportId: queryParams.reportId || 0,
                     userSettings: data,
                     dataFilters: data.dataFilters,
                     getTimeZonesUrl: svc + "GetAllTimezones",
                     samePageOnRun: true
                 });

                 vm.init(0, data.noAccount);
                 ko.applyBindings(vm);

                 $(window).resize(function () {
                     vm.DrawChart();
                 });

                 vm.textQuery.setupQuery();
             });
         });

     </script>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="body" runat="server">
    <div>
    <div data-bind="template: {name: 'admin-mode-template'}, visible: allowAdmin" style="display: none;"></div>
    <h4>Smarter Report Builder <small style="color: red">(Beta)</small> </h4>

    <p>
        What would you like to know? Try typing in below to search your data
    </p>
    <p id="query-input" class="query-input">
        Show me&nbsp;
    </p>
    <p>
        <button data-bind="click: function() {runQuery(false);}" class="btn btn-primary btn-sm">Process your Query</button>&nbsp;
        <button data-bind="click: function() {runQuery(true);}" class="btn btn-primary btn-sm">Process with ChatGPT</button>&nbsp;
        <button data-bind="click: resetQuery" class="btn btn-secondary btn-sm">Start over</button>
        <button data-bind="click: openDesigner, hidden: usingAi" class="btn btn-secondary btn-sm">Design/Save Report</button>
    </p>
</div>

<!-- Report Builder -->
<div class="modal modal-fullscreen" id="modal-reportbuilder" role="dialog" aria-labelledby="myModalLabel" aria-hidden="true" style="padding-right: 0px !important;">
    <div data-bind="template: {name: 'report-designer', data: $data}"></div>
</div>


<div data-bind="with: ReportResult">
    <div data-bind="ifnot: HasError, visible: ReportData()">
        <div class="report-canvas" data-bind="with: $root">
            <div class="report-container">
                <div class="report-inner">
                    <h2 data-bind="text: ReportName"></h2>
                    <p data-bind="html: ReportDescription">
                    </p>
                    <div data-bind="with: ReportResult">
                        <div data-bind="template: 'report-template', data: $data"></div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <div data-bind="if: HasError">
        <h3>An unexpected error occured while running the Report</h3>
        <hr />
        <b>Error Details</b>
        <p>
            <div data-bind="text: Exception"></div>
        </p>
    </div>

    <div data-bind="if: $root.adminMode()">
        <br />
        <br />
        <code data-bind="html: ReportSql">
        </code>
    </div>

</div>
</asp:Content>
