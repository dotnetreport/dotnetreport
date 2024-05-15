using Quartz;
using Quartz.Impl;
using ReportBuilder.Web.Jobs;
using System.ServiceProcess;

namespace DotnetReportJob.WinService
{
    public partial class ReportScheduleService : ServiceBase
    {
        private IScheduler scheduler;
        public ReportScheduleService()
        {
            InitializeComponent();
        }

        protected override async void OnStart(string[] args)
        {
            var schedulerFactory = new StdSchedulerFactory();
            scheduler = await schedulerFactory.GetScheduler();
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

        protected override async void OnStop()
        {
            if (scheduler != null)
            {
                await scheduler.Shutdown();
            }
        }
    }
}
