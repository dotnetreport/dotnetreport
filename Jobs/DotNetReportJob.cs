using Newtonsoft.Json;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace ReportBuilder.Web.Jobs
{
    public class ReportSchedule
    {
        public string Schedule { get; set; }
        public string EmailTo { get; set; }
        public DateTime? LastRun { get; set; }
        public DateTime? NextRun { get; set; }
        public string UserId { get; set; }
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
            var apiUrl = ConfigurationManager.AppSettings["dotNetReport.apiUrl"];
            var accountApiKey = ConfigurationManager.AppSettings["dotNetReport.accountApiToken"];
            var databaseApiKey = ConfigurationManager.AppSettings["dotNetReport.dataconnectApiToken"];
            var clientId = ""; // you can specify client id here if needed

            // Get all reports with schedule and run the ones that are due
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync($"{apiUrl}/ReportApi/GetScheduledReports?account={accountApiKey}&dataConnect={databaseApiKey}&clientId={clientId}");

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var reports = JsonConvert.DeserializeObject<List<ReportWithSchedule>>(content);

                foreach(var report in reports)
                {
                    foreach(var schedule in report.Schedules)
                    {
                        try
                        {
                            var chron = new CronExpression(schedule.Schedule);
                            var nextRun = chron.GetTimeAfter(DateTimeOffset.UtcNow);

                            schedule.NextRun = null;
                        }
                        catch(Exception ex)
                        {
                            schedule.NextRun = null;
                        }
                    }
                }
            }
        }
    }
}