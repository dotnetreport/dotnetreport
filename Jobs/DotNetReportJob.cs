using Newtonsoft.Json;
using Quartz;
using Quartz.Impl;
using ReportBuilder.Web.Models;
using System.Globalization;
using System.Net.Mail;

namespace ReportBuilder.Web.Jobs
{
    public class ReportSchedule
    {
        public int Id { get; set; } = 0;
        public string Schedule { get; set; }
        public string EmailTo { get; set; }
        public string LastRun { get; set; }
        public DateTime? NextRun { get; set; }
        public string UserId { get; set; }
        public string Format { get; set; }
        public string DataFilters { get; set; }
        public string TimeZone { get; set; }
        public string SelectedPageSize { get; set; }
        public string SelectedPageOrientation { get; set; }
        public DateTime? ScheduleStart { get; set; }
        public DateTime? ScheduleEnd { get; set; }
        public void NormalizeFormat()
        {
            if (!string.IsNullOrEmpty(Format) && Format.Trim().StartsWith("{"))
            {
                try
                {
                    var formatObj = JsonConvert.DeserializeObject<FormatJson>(Format);
                    if (formatObj != null)
                    {
                        Format = formatObj.exportFormat;
                        SelectedPageSize = formatObj.size;
                        SelectedPageOrientation = formatObj.orientation;
                    }
                }
                catch
                {
                    // ignore if invalid json
                }
            }
        }
    }
    public class ReportWithSchedule
    {
        public int Id { get; set; }
        public int ReportId { get; set; }
        public int DashboardId { get; set; }
        public List<int> DashboardReports { get; set; } = new List<int>();
        public string Name { get; set; }
        public string Description { get; set; }
        public string DataConnectName { get; set; }
        public List<ReportSchedule> Schedules { get; set; }

    }
    public class FormatJson
    {
        public string exportFormat { get; set; }
        public string size { get; set; }
        public string orientation { get; set; }
    }
    public class JobScheduler
    {
        public static string WebAppRootUrl = "";
        public static async void Start()
        {
            var schedulerFactory = new StdSchedulerFactory();
            var scheduler = await schedulerFactory.GetScheduler();
            await scheduler.Start();

            IJobDetail job = JobBuilder.Create<DotNetReportJob>()
                                       .WithIdentity("DotNetReportJob")
                                       .StoreDurably()
                                       .Build();

            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity("DotNetReportJobTrigger")
                .StartNow()
                .WithSimpleSchedule(s => s.WithIntervalInSeconds(60 * 5).RepeatForever())
                .Build();

            await scheduler.ScheduleJob(job, trigger);

        }
    }

    [DisallowConcurrentExecution]
    public class DotNetReportJob : IJob
    {
        private readonly IConfigurationRoot _configuration;
        public readonly static string _configFileName = "appsettings.dotnetreport.json";

        public DotNetReportJob()
        {
            _configuration = DotNetReportHelper.StaticConfig;
        }

        #region Schedule diagnostics
        private static readonly object _diagLock = new object();
        private static bool? _diagEnabled;

        private static bool DiagEnabled
        {
            get
            {
                try { return _diagEnabled ??= DotNetReportHelper.StaticConfig.GetValue<bool>("dotNetReport:scheduleDiagnostics"); }
                catch { _diagEnabled = false; return false; }
            }
        }

        private static void DiagLog(string message)
        {
            if (!DiagEnabled) return;
            try
            {
                var dir = Path.Combine(AppContext.BaseDirectory, "App_Data");
                Directory.CreateDirectory(dir);
                var file = Path.Combine(dir, $"schedule-diagnostics-{DateTime.UtcNow:yyyy-MM-dd}.log");
                var line = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}Z  {message}{Environment.NewLine}";
                lock (_diagLock) { File.AppendAllText(file, line); }
            }
            catch { /* diagnostics must never break the job */ }
        }

        // Records a swallowed exception with type, message, and stack trace (incl. inner).
        private static void DiagLog(string context, Exception ex)
        {
            if (!DiagEnabled) return;
            var detail = $"{context} EXCEPTION: {ex.GetType().Name}: {ex.Message}\n    {ex.StackTrace}";
            if (ex.InnerException != null)
                detail += $"\n    INNER: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}";
            DiagLog(detail);
        }
        #endregion

        public (DateTime? NextRunLocal, bool ShouldRun, DateTime currentTimeInTargetTz) CalculateNextRun(
            string cron,
            string timeZoneId,
            string lastRunFromDb,
            DateTime? scheduleStart,
            DateTime? scheduleEnd,
            DateTime? currentTimeToTest = null)
        {
            TimeZoneInfo targetTimeZone = !String.IsNullOrEmpty(timeZoneId)
                    ? TimeZoneInfo.FindSystemTimeZoneById(timeZoneId)
                    : TimeZoneInfo.Local;

            var chron = new CronExpression(cron);
            var utcNow = currentTimeToTest.HasValue ? DateTime.SpecifyKind(currentTimeToTest.Value, DateTimeKind.Utc) : DateTime.UtcNow;
            var lastRun = !String.IsNullOrEmpty(lastRunFromDb) ? Convert.ToDateTime(lastRunFromDb) : new DateTimeOffset(utcNow).AddMinutes(-10);
            var nextRun = chron.GetTimeAfter(lastRun);

            if (!String.IsNullOrEmpty(timeZoneId))
            {
                TimeZoneInfo timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                // Convert last run to user's local time zone if it actually came from DB
                if (!string.IsNullOrEmpty(lastRunFromDb))
                    lastRun = TimeZoneInfo.ConvertTime(lastRun, timeZoneInfo);

                nextRun = chron.GetTimeAfter(lastRun);
            }

            // A stale LastRun puts the next occurrence in the past, where it can never fire again.
            var anchor = lastRun;
            if (scheduleStart.HasValue && anchor < scheduleStart.Value) anchor = scheduleStart.Value;
            var nowInTargetTz = TimeZoneInfo.ConvertTime(new DateTimeOffset(utcNow), targetTimeZone);
            if (anchor < nowInTargetTz.AddMinutes(-10)) anchor = nowInTargetTz.AddMinutes(-10);
            if (anchor != lastRun) nextRun = chron.GetTimeAfter(anchor);

            var _nextRun = (nextRun.HasValue ? nextRun.Value.ToLocalTime().DateTime : (DateTime?)null);

            DateTime currentTimeInTargetTz = TimeZoneInfo.ConvertTime(utcNow, targetTimeZone);

            bool shouldRun = false;
            if ((scheduleStart.HasValue && _nextRun.HasValue && _nextRun < scheduleStart.Value) ||
                (scheduleEnd.HasValue && _nextRun.HasValue && _nextRun > scheduleEnd.Value))
            {
                shouldRun = false;
            }
            else if (_nextRun.HasValue && currentTimeInTargetTz >= _nextRun
                && (!String.IsNullOrEmpty(lastRunFromDb) || lastRun <= _nextRun))
            {
                shouldRun = true;
            }

            return (_nextRun, shouldRun, currentTimeInTargetTz);
        }

        async Task IJob.Execute(IJobExecutionContext context)
        {
            var apiUrl = _configuration.GetValue<string>("dotNetReport:apiUrl");
            var accountApiKey = _configuration.GetValue<string>("dotNetReport:accountApiToken");
            var databaseApiKey = _configuration.GetValue<string>("dotNetReport:dataconnectApiToken");

            var fromEmail = _configuration.GetValue<string>("email:fromemail");
            var fromName = _configuration.GetValue<string>("email:fromname");
            var mailServer = _configuration.GetValue<string>("email:server");
            var mailUserName = _configuration.GetValue<string>("email:username");
            var mailPassword = _configuration.GetValue<string>("email:password");

            var clientId = ""; // you can specify client id here if needed

            // Get all reports with schedule and run the ones that are due
            using (var client = new HttpClient())
            {
                DotNetReportHelper.defaultDateFormat = "United States";
                try
                {
                    var settingsResp = await client.GetAsync($"{apiUrl}/ReportApi/GetAccountSettings?account={accountApiKey}&dataConnect={databaseApiKey}&clientId={clientId}");
                    if (settingsResp.IsSuccessStatusCode)
                    {
                        var settings = JsonConvert.DeserializeObject<Dictionary<string, object>>(await settingsResp.Content.ReadAsStringAsync());
                        var ddf = settings?.GetValueOrDefault("defaultDateFormat")?.ToString();
                        if (!string.IsNullOrWhiteSpace(ddf)) DotNetReportHelper.defaultDateFormat = ddf;
                    }
                }
                catch (Exception ex) { DiagLog("GetAccountSettings", ex); }

                var response = await client.GetAsync($"{apiUrl}/ReportApi/GetScheduledReportsAndDashboards?account={accountApiKey}&dataConnect={databaseApiKey}&clientId={clientId}");

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var reports = JsonConvert.DeserializeObject<List<ReportWithSchedule>>(content);

                foreach (var report in reports)
                {
                    foreach (var schedule in report.Schedules)
                    {
                        try
                        {
                            schedule.NormalizeFormat();
                            TimeZoneInfo targetTimeZone = !String.IsNullOrEmpty(schedule.TimeZone)
                                ? TimeZoneInfo.FindSystemTimeZoneById(schedule.TimeZone)
                                : TimeZoneInfo.Local;

                            var (nextRun, shouldRun, currentTimeInTargetTz) = CalculateNextRun(
                                 schedule.Schedule,
                                 schedule.TimeZone,
                                 schedule.LastRun ?? DateTime.UtcNow.AddMinutes(-10).ToString(CultureInfo.InvariantCulture),
                                 schedule.ScheduleStart,
                                 schedule.ScheduleEnd);

                            schedule.NextRun = nextRun;

                            DiagLog($"[{report.Name}] schedule={schedule.Id} shouldRun={shouldRun}"
                                  + $"\n    cron='{schedule.Schedule}' scheduleTz='{schedule.TimeZone}' serverTz='{TimeZoneInfo.Local.Id}'"
                                  + $"\n    utcNow={DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} nowInScheduleTz={currentTimeInTargetTz:yyyy-MM-dd HH:mm:ss} serverLocalNow={DateTime.Now:yyyy-MM-dd HH:mm:ss}"
                                  + $"\n    lastRunFromDb={(string.IsNullOrEmpty(schedule.LastRun) ? "(null)" : schedule.LastRun)}"
                                  + $"\n    computedNextRun(localRunTime to be sent)={nextRun:yyyy-MM-ddTHH:mm:ss}");

                            if (shouldRun)
                            {
                                var isDashboard = report.DashboardId > 0;
                                var itemId = isDashboard ? report.DashboardId : report.ReportId;
                                DotNetReportHelper.CurrentDataFilters = schedule.DataFilters ?? "";

                                DiagLog($"[{report.Name}] dataFilters(schedule)={schedule.DataFilters ?? "(null)"}"
                                      + $"\n    CurrentDataFilters(after set)={DotNetReportHelper.CurrentDataFilters}");

                                // The header is resolved per-report just before the Word export (see
                                // ResolveScheduledReportHeader); PDF/Html get it from the rendered ReportPrint page.
                                string hfFooterHtml = null;
                                bool hfFooterEveryPage = false;
                                try
                                {
                                    var ftrResp = await client.GetAsync($"{apiUrl}/ReportApi/GetReportFooter?account={accountApiKey}&dataConnect={databaseApiKey}&clientId={clientId}&userId={schedule.UserId}");
                                    if (ftrResp.IsSuccessStatusCode)
                                    {
                                        var ftrJson = await ftrResp.Content.ReadAsStringAsync();
                                        var ftr = JsonConvert.DeserializeObject<dynamic>(ftrJson);
                                        if (ftr != null && (bool?)ftr.useReportFooter == true)
                                        {
                                            string rawFooter = (string)ftr.footerJson ?? "";
                                            hfFooterHtml = System.Web.HttpUtility.UrlDecode(rawFooter);
                                            hfFooterEveryPage = (bool?)ftr.includeOnEveryPage == true;
                                        }
                                    }
                                }
                                catch (Exception ex) { DiagLog("GetReportFooter", ex); /* not critical */ }

                                response = await client.GetAsync($"{apiUrl}/ReportApi/RunScheduledItem?account={accountApiKey}&dataConnect={databaseApiKey}&scheduleId={schedule.Id}&id={itemId}&localRunTime={schedule.NextRun:yyyy-MM-ddTHH:mm:ss}&isDashboard={isDashboard}&clientId={clientId}&dataFilters={schedule.DataFilters}");
                                response.EnsureSuccessStatusCode();                            

                                content = await response.Content.ReadAsStringAsync();

                                DotNetReportScheduleModel reportToRun = null;
                                List<DotNetReportScheduleModel> reportsToRun = null;

                                if (isDashboard)
                                {
                                    reportsToRun = JsonConvert.DeserializeObject<List<DotNetReportScheduleModel>>(content);
                                }
                                else
                                {
                                    reportToRun = JsonConvert.DeserializeObject<DotNetReportScheduleModel>(content);
                                }

                                DiagLog($"[{report.Name}] RunScheduledItem sent localRunTime={schedule.NextRun:yyyy-MM-ddTHH:mm:ss} dataFilters={schedule.DataFilters ?? "(null)"}"
                                      + $"\n    responseChars={content?.Length ?? 0}"
                                      + $"\n    reportSql={(isDashboard ? string.Join(" || ", (reportsToRun ?? new List<DotNetReportScheduleModel>()).Select(x => x.ReportSql)) : reportToRun?.ReportSql) ?? "(null)"}");

                                var files = new List<byte[]>();
                                byte[] fileData;
                                string fileExt = "";
                                string imageData = "";

                                (string PivotColumn, string PivotFunction) pivotInfo;
                                switch ((schedule.Format ?? "Excel").ToUpper())
                                {
                                    case "PDF":
                                        if (report.DashboardId > 0)
                                        {
                                            foreach (var r in reportsToRun)
                                            {
                                                pivotInfo = PreparePivotData(r.Columns);
                                                fileData = await DotNetReportHelper.GetPdfFile(JobScheduler.WebAppRootUrl + "/DotnetReport/ReportPrint", r.ReportId, r.ReportSql, r.ConnectKey, r.ReportName, schedule.UserId, clientId, dataFilters: schedule.DataFilters ?? "", expandSqls: r.ReportData, pivotColumn: pivotInfo.PivotColumn, pivotFunction: pivotInfo.PivotFunction,pageSize:schedule.SelectedPageSize,pageOrientation:schedule.SelectedPageOrientation);
                                                files.Add(fileData);
                                            }

                                            fileData = DotNetReportHelper.GetCombinePdfFile(files);
                                        }
                                        else
                                        {
                                            pivotInfo = PreparePivotData(reportToRun.Columns);
                                            fileData = await DotNetReportHelper.GetPdfFile(JobScheduler.WebAppRootUrl + "/DotnetReport/ReportPrint", reportToRun.ReportId, reportToRun.ReportSql, reportToRun.ConnectKey, reportToRun.ReportName, schedule.UserId, clientId, dataFilters: schedule.DataFilters ?? "", expandSqls: reportToRun.ReportData, pivotColumn: pivotInfo.PivotColumn, pivotFunction: pivotInfo.PivotFunction, pageSize: schedule.SelectedPageSize, pageOrientation: schedule.SelectedPageOrientation);
                                        }
                                        fileExt = ".pdf"; 
                                        break;

                                    case "CSV":
                                        pivotInfo = PreparePivotData(reportToRun.Columns);
                                        fileExt = ".csv";
                                        fileData = await DotNetReportHelper.GetCSVFile(reportToRun.ReportSql, reportToRun.ConnectKey, reportToRun.Columns, reportToRun.IncludeSubTotals, expandSqls: reportToRun.ReportData, pivotColumn: pivotInfo.PivotColumn, pivotFunction: pivotInfo.PivotFunction);
                                        break;

                                    case "WORD":
                                        if (report.DashboardId > 0)
                                        {
                                            foreach (var r in reportsToRun)
                                            {
                                                pivotInfo = PreparePivotData(r.Columns);
                                                try
                                                {
                                                    imageData = Convert.ToBase64String(await DotNetReportHelper.GetPdfFile(JobScheduler.WebAppRootUrl + "/DotnetReport/ReportPrint", r.ReportId, r.ReportSql, r.ConnectKey, r.ReportName, schedule.UserId, clientId, dataFilters: schedule.DataFilters ?? "", expandSqls: r.ReportData, pivotColumn: pivotInfo.PivotColumn, pivotFunction: pivotInfo.PivotFunction, imageOnly: true));
                                                }
                                                catch (Exception __ex) { imageData = ""; DiagLog("chart image (imageOnly)", __ex); }
                                                string customHtmlR = null;
                                                if (string.Equals(r.ReportType, "Html", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    try
                                                    {
                                                        customHtmlR = await DotNetReportHelper.GetReportRenderedHtml(JobScheduler.WebAppRootUrl + "/DotnetReport/ReportPrint", r.ReportId, r.ReportSql, r.ConnectKey, r.ReportName, schedule.UserId, clientId, dataFilters: schedule.DataFilters ?? "", expandSqls: r.ReportData, pivotColumn: pivotInfo.PivotColumn, pivotFunction: pivotInfo.PivotFunction);
                                                    }
                                                    catch (Exception ex) { customHtmlR = null; DiagLog("GetReportRenderedHtml(dashboard)", ex); }
                                                }
                                                var rHdr = await ResolveScheduledReportHeader(client, apiUrl, accountApiKey, databaseApiKey, clientId, schedule.UserId, r);
                                                fileData = await DotNetReportHelper.GetWordFile(r.ReportSql, r.ConnectKey, r.ReportName, columns: r.Columns, includeSubtotal: r.IncludeSubTotals, pivot: r.ReportType == "Pivot", chartData: imageData, expandSqls: r.ReportData, pivotColumn: pivotInfo.PivotColumn, pivotFunction: pivotInfo.PivotFunction, pageSize: schedule.SelectedPageSize, pageOrientation: schedule.SelectedPageOrientation,
                                                    headerHtml: rHdr.html, footerHtml: hfFooterHtml, headerEveryPage: rHdr.everyPage, footerEveryPage: hfFooterEveryPage, currentUserName: schedule.UserId, currentUserRoles: null,
                                                    customHtml: customHtmlR);
                                                files.Add(fileData);
                                            }

                                            fileData = DotNetReportHelper.GetCombineWordFile(files);
                                        }
                                        else
                                        {
                                            pivotInfo = PreparePivotData(reportToRun.Columns);
                                            fileExt = ".docx";
                                            try
                                            {
                                                imageData = Convert.ToBase64String(await DotNetReportHelper.GetPdfFile(JobScheduler.WebAppRootUrl + "/DotnetReport/ReportPrint", reportToRun.ReportId, reportToRun.ReportSql, reportToRun.ConnectKey, reportToRun.ReportName, schedule.UserId, clientId, dataFilters: schedule.DataFilters ?? "", expandSqls: reportToRun.ReportData, pivotColumn: pivotInfo.PivotColumn, pivotFunction: pivotInfo.PivotFunction, imageOnly: true));
                                            }
                                            catch (Exception __ex) { imageData = ""; DiagLog("chart image (imageOnly)", __ex); }
                                            string customHtml = null;
                                            if (string.Equals(reportToRun.ReportType, "Html", StringComparison.OrdinalIgnoreCase))
                                            {
                                                try
                                                {
                                                    customHtml = await DotNetReportHelper.GetReportRenderedHtml(JobScheduler.WebAppRootUrl + "/DotnetReport/ReportPrint", reportToRun.ReportId, reportToRun.ReportSql, reportToRun.ConnectKey, reportToRun.ReportName, schedule.UserId, clientId, dataFilters: schedule.DataFilters ?? "", expandSqls: reportToRun.ReportData, pivotColumn: pivotInfo.PivotColumn, pivotFunction: pivotInfo.PivotFunction);
                                                }
                                                catch (Exception ex) { customHtml = null; DiagLog("GetReportRenderedHtml", ex); }
                                            }
                                            var singleHdr = await ResolveScheduledReportHeader(client, apiUrl, accountApiKey, databaseApiKey, clientId, schedule.UserId, reportToRun);
                                            fileData = await DotNetReportHelper.GetWordFile(reportToRun.ReportSql, reportToRun.ConnectKey, reportToRun.ReportName, columns: reportToRun.Columns, includeSubtotal: reportToRun.IncludeSubTotals, pivot: reportToRun.ReportType == "Pivot", chartData: imageData, expandSqls: reportToRun.ReportData, pivotColumn: pivotInfo.PivotColumn, pivotFunction: pivotInfo.PivotFunction, pageSize: schedule.SelectedPageSize, pageOrientation: schedule.SelectedPageOrientation,
                                                headerHtml: singleHdr.html, footerHtml: hfFooterHtml, headerEveryPage: singleHdr.everyPage, footerEveryPage: hfFooterEveryPage, currentUserName: schedule.UserId, currentUserRoles: null,
                                                customHtml: customHtml);
                                        }
                                        break;

                                    case "EXCEL-SUB":
                                        pivotInfo = PreparePivotData(reportToRun.Columns);
                                        fileData = await DotNetReportHelper.GetExcelFile(reportToRun.ReportSql, reportToRun.ConnectKey, reportToRun.ReportName, columns: reportToRun.Columns, allExpanded: true, expandSqls: reportToRun.ReportData, includeSubtotal: reportToRun.IncludeSubTotals, pivot: reportToRun.ReportType == "Pivot", pivotColumn: pivotInfo.PivotColumn, pivotFunction: pivotInfo.PivotFunction);
                                        fileExt = ".xlsx";
                                        break;
                                    
                                    case "EXCEL":
                                    default:
                                        if (report.DashboardId > 0)
                                        {
                                            foreach (var r in reportsToRun)
                                            {
                                                pivotInfo = PreparePivotData(r.Columns);
                                                try
                                                {
                                                    imageData = Convert.ToBase64String(await DotNetReportHelper.GetPdfFile(JobScheduler.WebAppRootUrl + "/DotnetReport/ReportPrint", r.ReportId, r.ReportSql, r.ConnectKey, r.ReportName, schedule.UserId, clientId, dataFilters: schedule.DataFilters ?? "", expandSqls: r.ReportData, pivotColumn: pivotInfo.PivotColumn, pivotFunction: pivotInfo.PivotFunction, imageOnly: true));
                                                }
                                                catch (Exception __ex) { imageData = ""; DiagLog("chart image (imageOnly)", __ex); }
                                                fileData = await DotNetReportHelper.GetExcelFile(r.ReportSql, r.ConnectKey, r.ReportName, columns: r.Columns, expandSqls: r.ReportData, includeSubtotal: r.IncludeSubTotals, pivot: r.ReportType == "Pivot", chartData: imageData, pivotColumn: pivotInfo.PivotColumn, pivotFunction: pivotInfo.PivotFunction);
                                                files.Add(fileData);
                                            }

                                            fileData = DotNetReportHelper.GetCombineExcelFile(files, reportsToRun.Select(r => r.ReportName).ToList());
                                            fileExt = ".xlsx";
                                        }
                                        else
                                        {
                                            pivotInfo = PreparePivotData(reportToRun.Columns);
                                            try
                                            {
                                                imageData = Convert.ToBase64String(await DotNetReportHelper.GetPdfFile(JobScheduler.WebAppRootUrl + "/DotnetReport/ReportPrint", reportToRun.ReportId, reportToRun.ReportSql, reportToRun.ConnectKey, reportToRun.ReportName, schedule.UserId, clientId, dataFilters: schedule.DataFilters ?? "", expandSqls: reportToRun.ReportData, pivotColumn: pivotInfo.PivotColumn, pivotFunction: pivotInfo.PivotFunction, imageOnly: true));
                                            }
                                            catch (Exception __ex) { imageData = ""; DiagLog("chart image (imageOnly)", __ex); }
                                            fileData = await DotNetReportHelper.GetExcelFile(reportToRun.ReportSql, reportToRun.ConnectKey, reportToRun.ReportName, columns: reportToRun.Columns, expandSqls: reportToRun.ReportData, includeSubtotal: reportToRun.IncludeSubTotals, pivot: reportToRun.ReportType == "Pivot", chartData: imageData, pivotColumn: pivotInfo.PivotColumn, pivotFunction: pivotInfo.PivotFunction);
                                            fileExt = ".xlsx";
                                        }
                                        break;
                                }

                                DiagLog($"[{report.Name}] format={schedule.Format} builtFileBytes={(fileData?.Length ?? 0)}"
                                      + $"\n    CurrentDataFilters(before email)={DotNetReportHelper.CurrentDataFilters}");

                                // send email
                                var mail = new MailMessage
                                {
                                    From = new MailAddress(fromEmail, fromName),
                                    Subject = report.Name,
                                    Body = $"Your scheduled report is attached.<br><br>{report.Description}",
                                    IsBodyHtml = true
                                };
                                mail.To.Add(schedule.EmailTo);


                                if (schedule.Format == "Link")
                                {
                                    mail.Body = $"Please click on the link below to Run your Report:<br><br><a href=\"{JobScheduler.WebAppRootUrl}/DotnetReport/Report?linkedreport=true&noparent=true&reportId={reportToRun.ReportId}\">{report.Description}</a>";
                                }
                                else if (fileData != null)
                                {
                                    var attachment = new Attachment(new MemoryStream(fileData), report.Name + fileExt);
                                    mail.Attachments.Add(attachment);
                                }

                                using (var smtpServer = new SmtpClient(mailServer))
                                {
                                    smtpServer.Port = 587;
                                    smtpServer.Credentials = new System.Net.NetworkCredential(mailUserName, mailPassword);
                                    //smtpServer.EnableSsl = true;
                                    smtpServer.Send(mail);
                                }

                                await LogScheduleSent(client, apiUrl, accountApiKey, databaseApiKey, schedule, report, isDashboard, isError: false, message: "Sent");
                            }
                        }
                        catch (Exception ex)
                        {
                            DiagLog($"[{report.Name}] schedule={schedule.Id} RUN FAILED", ex);
                            await LogScheduleSent(client, apiUrl, accountApiKey, databaseApiKey, schedule, report, report.DashboardId > 0, isError: true, message: ex.Message);
                            // could not run, ignore error
                        }
                    }
                }
            }
        }

        private static async Task LogScheduleSent(HttpClient client, string apiUrl, string accountApiKey, string databaseApiKey, ReportSchedule schedule, ReportWithSchedule report, bool isDashboard, bool isError, string message)
        {
            try
            {
                var itemId = isDashboard ? report.DashboardId : report.ReportId;
                var itemName = System.Web.HttpUtility.UrlEncode(report.Name ?? "");
                var format = System.Web.HttpUtility.UrlEncode(schedule.Format ?? "");
                var sentTo = System.Web.HttpUtility.UrlEncode(schedule.EmailTo ?? "");
                var msg = System.Web.HttpUtility.UrlEncode(message ?? "");
                await client.GetAsync($"{apiUrl}/ReportApi/LogScheduleSent?account={accountApiKey}&dataConnect={databaseApiKey}&scheduleId={schedule.Id}&itemId={itemId}&isDashboard={isDashboard}&itemName={itemName}&format={format}&sentTo={sentTo}&isError={isError}&message={msg}");
            }
            catch { /* logging failure should not break the job */ }
        }

        public (string PivotColumn, string PivotFunction) PreparePivotData(List<ReportHeaderColumn> columns)
        {
            var pivotColumn = columns.FirstOrDefault(x => x.aggregateFunction == "Pivot");
            string pivotFunction = string.Empty;

            if (pivotColumn != null)
            {
                int pivotColumnIndex = columns.FindIndex(x => x.aggregateFunction == "Pivot");

                if (pivotColumnIndex >= 0 && pivotColumnIndex < columns.Count - 1)
                {
                    var nextValue = columns[pivotColumnIndex + 1];
                    pivotFunction = nextValue.aggregateFunction;
                }
            }

            return (
                 pivotColumn?.fieldName ?? string.Empty,
                 pivotColumn != null && !string.IsNullOrEmpty(pivotFunction) ? pivotFunction : string.Empty
             );
        }

        // Resolves the header html a scheduled report should use, honoring its per-report selection:
        // "don't use" (HideReportHeader / ReportHeaderId == -1) -> none; a custom per-report header ->
        // used verbatim; otherwise the chosen named header id is resolved server-side (chosen ->
        // client default -> global default) via GetReportHeader.
        private async Task<(string html, bool everyPage)> ResolveScheduledReportHeader(HttpClient client, string apiUrl, string accountApiKey, string databaseApiKey, string clientId, string userId, DotNetReportScheduleModel r)
        {
            try
            {
                if (r == null || r.HideReportHeader) return (null, false);

                int reportHeaderId = 0;
                bool useCustom = false;
                string customHtml = null;
                if (!string.IsNullOrEmpty(r.ReportSettings))
                {
                    try
                    {
                        var rs = JsonConvert.DeserializeObject<Dictionary<string, object>>(r.ReportSettings);
                        if (rs != null)
                        {
                            if (rs.TryGetValue("ReportHeaderId", out var rid) && rid != null) int.TryParse(rid.ToString(), out reportHeaderId);
                            if (rs.TryGetValue("UseCustomReportHeader", out var uc) && uc != null) bool.TryParse(uc.ToString(), out useCustom);
                            if (rs.TryGetValue("CustomReportHeaderHtml", out var ch) && ch != null) customHtml = ch.ToString();
                        }
                    }
                    catch { /* malformed ReportSettings -> fall back to default header */ }
                }

                if (reportHeaderId == -1) return (null, false); // don't use a header
                if (useCustom) return (System.Web.HttpUtility.UrlDecode(customHtml ?? ""), false);

                var resp = await client.GetAsync($"{apiUrl}/ReportApi/GetReportHeader?account={accountApiKey}&dataConnect={databaseApiKey}&clientId={clientId}&userId={userId}&reportHeaderId={reportHeaderId}");
                if (resp.IsSuccessStatusCode)
                {
                    var j = await resp.Content.ReadAsStringAsync();
                    var h = JsonConvert.DeserializeObject<dynamic>(j);
                    if (h != null && (bool?)h.useReportHeader == true)
                        return (System.Web.HttpUtility.UrlDecode((string)h.headerJson ?? ""), (bool?)h.includeOnEveryPage == true);
                }
            }
            catch (Exception ex) { DiagLog("ResolveScheduledReportHeader", ex); }
            return (null, false);
        }
    }
}