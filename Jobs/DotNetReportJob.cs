﻿using Newtonsoft.Json;
using Quartz;
using Quartz.Impl;
using ReportBuilder.Web.Controllers;
using ReportBuilder.Web.Models;
using System.Net.Mail;

namespace ReportBuilder.Web.Jobs
{
    public class ReportSchedule
    {
        public int Id { get; set; }
        public string Schedule { get; set; }
        public string EmailTo { get; set; }
        public string LastRun { get; set; }
        public DateTime? NextRun { get; set; }
        public string UserId { get; set; }
        public string Format { get; set; }
        public string DataFilters { get; set; }
        public string TimeZone { get; set; }
    }
    public class ReportWithSchedule
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string DataConnectName { get; set; }
        public List<ReportSchedule> Schedules { get; set; }

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
                .WithSimpleSchedule(s => s.WithIntervalInSeconds(60).RepeatForever())
                .Build();

            await scheduler.ScheduleJob(job, trigger);

        }
    }

    public class DotNetReportJob : IJob
    {
        async Task IJob.Execute(IJobExecutionContext context)
        {
            var apiUrl = Startup.StaticConfig.GetValue<string>("dotNetReport:apiUrl");
            var accountApiKey = Startup.StaticConfig.GetValue<string>("dotNetReport:accountApiToken");
            var databaseApiKey = Startup.StaticConfig.GetValue<string>("dotNetReport:dataconnectApiToken");

            var appsetting = DotNetReportHelper.GetAppSettings();
            var fromEmail = appsetting.emailAddress;
            var fromName = appsetting.emailName;
            var mailServer = appsetting.emailServer;
            var mailUserName =appsetting.emailUserName;
            var mailPassword = appsetting.emailPassword;
            var emailport = appsetting.emailPort;

            var clientId = ""; // you can specify client id here if needed

            // Get all reports with schedule and run the ones that are due
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync($"{apiUrl}/ReportApi/GetScheduledReports?account={accountApiKey}&dataConnect={databaseApiKey}&clientId={clientId}");

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var reports = JsonConvert.DeserializeObject<List<ReportWithSchedule>>(content);

                foreach (var report in reports)
                {
                    foreach (var schedule in report.Schedules)
                    {
                        try
                        {
                            var chron = new CronExpression(schedule.Schedule);
                            var lastRun = !String.IsNullOrEmpty(schedule.LastRun) ? Convert.ToDateTime(schedule.LastRun) : DateTimeOffset.UtcNow.AddMinutes(-10);
                            var nextRun = chron.GetTimeAfter(lastRun);

                            if (!String.IsNullOrEmpty(schedule.TimeZone))
                            {
                                TimeZoneInfo timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(schedule.TimeZone);
                                // Convert last run to user's local time zone
                                lastRun = TimeZoneInfo.ConvertTime(lastRun, timeZoneInfo);
                                nextRun = chron.GetTimeAfter(lastRun);
                                // Get current time in user's time zone
                                DateTime currentTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, timeZoneInfo);
                            }
                            schedule.NextRun = (nextRun.HasValue ? nextRun.Value.ToLocalTime().DateTime : (DateTime?)null);

                            if (schedule.NextRun.HasValue && DateTime.Now >= schedule.NextRun && (!String.IsNullOrEmpty(schedule.LastRun) || lastRun <= schedule.NextRun))
                            {
                                // need to run this report
                                response = await client.GetAsync($"{apiUrl}/ReportApi/RunScheduledReport?account={accountApiKey}&dataConnect={databaseApiKey}&scheduleId={schedule.Id}&reportId={report.Id}&localRunTime={schedule.NextRun.Value.ToShortDateString()} {schedule.NextRun.Value.ToShortTimeString()}&clientId={clientId}&dataFilters={schedule.DataFilters}");
                                response.EnsureSuccessStatusCode();

                                content = await response.Content.ReadAsStringAsync();
                                var reportToRun = JsonConvert.DeserializeObject<DotNetReportModel>(content);

                                response = await client.GetAsync($"{apiUrl}/ReportApi/LoadReportColumnDetails?account={accountApiKey}&dataConnect={databaseApiKey}&reportId={report.Id}&clientId={clientId}");
                                response.EnsureSuccessStatusCode();

                                content = await response.Content.ReadAsStringAsync();
                                var columnDetails = JsonConvert.DeserializeObject<List<ReportHeaderColumn>>(content);

                                byte[] fileData;
                                string fileExt = "";

                                switch ((schedule.Format ?? "Excel").ToUpper())
                                {
                                    case "PDF":
                                        fileData = await DotNetReportHelper.GetPdfFile(JobScheduler.WebAppRootUrl + "/Report/ReportPrint", reportToRun.ReportId, reportToRun.ReportSql, reportToRun.ConnectKey, reportToRun.ReportName, schedule.UserId, clientId, JsonConvert.SerializeObject(schedule.DataFilters));
                                        fileExt = ".pdf"; 
                                        break;

                                    case "CSV": 
                                        fileExt = ".csv";
                                        fileData = await DotNetReportHelper.GetCSVFile(reportToRun.ReportSql, reportToRun.ConnectKey);
                                        break;

                                    case "WORD":
                                        fileExt = ".docx";
                                        fileData = await DotNetReportHelper.GetWordFile(reportToRun.ReportSql,reportToRun.ConnectKey, reportToRun.ReportName, columns: columnDetails, includeSubtotal: reportToRun.IncludeSubTotals, pivot: reportToRun.ReportType == "Pivot");
                                        break;

                                    case "EXCEL-SUB":
                                        fileData = await DotNetReportHelper.GetExcelFile(reportToRun.ReportSql, reportToRun.ConnectKey, reportToRun.ReportName, columns: columnDetails, allExpanded: true, expandSqls: reportToRun.ReportData, includeSubtotal: reportToRun.IncludeSubTotals, pivot: reportToRun.ReportType == "Pivot");
                                        fileExt = ".xlsx";
                                        break;
                                    
                                    case "EXCEL":
                                    default:
                                        fileData = await DotNetReportHelper.GetExcelFile(reportToRun.ReportSql, reportToRun.ConnectKey, reportToRun.ReportName, columns: columnDetails, includeSubtotal: reportToRun.IncludeSubTotals, pivot: reportToRun.ReportType == "Pivot");
                                        fileExt = ".xlsx";
                                        break;
                                }

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
                                else
                                {
                                    var attachment = new Attachment(new MemoryStream(fileData), report.Name + fileExt);
                                    mail.Attachments.Add(attachment);
                                }

                                using (var smtpServer = new SmtpClient(mailServer))
                                {
                                    smtpServer.Port = Convert.ToInt32(emailport);// 587
                                    smtpServer.Credentials = new System.Net.NetworkCredential(mailUserName, mailPassword);
                                    //smtpServer.EnableSsl = true;
                                    smtpServer.Send(mail);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // could not run, ignore error
                        }
                    }
                }
            }
        }
    }
}