using System.Collections.Specialized;
using Quartz;
using Quartz.Impl;

namespace QuartzTest;

public static class QuartzHelper
{
    private static IScheduler? _scheduler;

    private static async Task InitialQuartz(this WebApplicationBuilder builder)
    {
        if (_scheduler == null)
        {
            var configuration = builder.Configuration;
            var properties = new NameValueCollection();
            //存储类型
            properties["quartz.jobStore.type"] = configuration.GetSection("Quartz:quartz.jobStore.type").Value;
            //表明前缀
            properties["quartz.jobStore.tablePrefix"] = configuration.GetSection("Quartz:quartz.jobStore.tablePrefix").Value;
            //驱动类型
            properties["quartz.jobStore.driverDelegateType"] = configuration.GetSection("Quartz:quartz.jobStore.driverDelegateType").Value;
            //数据源名称
            var sourceName = configuration.GetSection("Quartz:quartz.jobStore.dataSource").Value;
            ;
            properties["quartz.jobStore.dataSource"] = sourceName;
            //连接字符串
            properties[$"quartz.dataSource.{sourceName}.connectionString"] = configuration.GetSection("quartz.dataSource.connectionString").Value;
            //sqlserver版本
            properties[$"quartz.dataSource.{sourceName}.provider"] = configuration.GetSection("quartz.dataSource.provider").Value;
            
            var factory = new StdSchedulerFactory(properties);
            _scheduler = await factory.GetScheduler();
        }
    }

    private static async Task AddAJobWithSimpleTrigger<T>(TimeSpan interval) where T : IJob
    {
        var jobDetail = JobBuilder.Create<T>().WithIdentity("a_job", "a_group").Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity("a_trigger", "a_group")
            .StartNow()
            .WithSimpleSchedule(x => x.WithInterval(interval).RepeatForever())
            .Build();

        _scheduler?.Start();
        await _scheduler?.ScheduleJob(jobDetail, trigger)!;
    }

    private static async Task DeleteJob(string name)
    {
        //触发器的key
        var triggerKey = new TriggerKey($"{name}_trigger", $"{name}_group");
        //Job的Key
        var jobKey = new JobKey($"{name}_job", $"{name}_group");
        await _scheduler.PauseTrigger(triggerKey); //暫停觸發器
        await _scheduler.UnscheduleJob(triggerKey); //移除觸發器
        await _scheduler.DeleteJob(jobKey);
    }

    /// <summary>
    /// 這種辦法可以根據job名稱找到觸發器，也可以找到Job，這樣就可以在任何地方修改Job頻次，不再限於IJob的實現方法Execute內
    /// 以此實現了：即時修改執行頻次即時生效
    /// </summary>
    /// <param name="scheduler"></param>
    /// <param name="name"></param>
    private static async Task ModifyJob(string name)
    {
        //触发器的key
        var triggerKey = new TriggerKey($"{name}_trigger", $"{name}_group");
        var trigger = await _scheduler.GetTrigger(triggerKey);

        // var scheduleBuilder = CronScheduleBuilder.CronSchedule(cronExpression);
        var scheduleBuilder = SimpleScheduleBuilder.RepeatSecondlyForever(1);
        trigger = trigger?.GetTriggerBuilder().WithIdentity(triggerKey).WithSchedule(scheduleBuilder).Build();

        if (trigger != null) await _scheduler.RescheduleJob(triggerKey, trigger);
    }
}