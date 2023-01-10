using System.Collections.Concurrent;
using Quartz;
using Quartz.Impl;
using Quartz.Simpl;
using SecondQuartzTest;

namespace QuartzTest;

public class QuartzManager
{
    /// <summary>
    /// 调度器
    /// </summary>
    private readonly IScheduler _scheduler;

    public static ConcurrentDictionary<string, int> RunningJobs = new();
    public static ConcurrentDictionary<string, JobInfo> WaitingJobs = new();
    public static bool MonoJobIsRunning = false;
    
    public static SemaphoreSlim CheckOrderSemaphore = new(1);
    public static SemaphoreSlim RunningMonoSemaphore = new(1);

    public QuartzManager()
    {
        DirectSchedulerFactory.Instance.CreateScheduler("bennyScheduler", "AUTO", new DefaultThreadPool(),
            new RAMJobStore());
        _scheduler = SchedulerRepository.Instance.Lookup("bennyScheduler").Result ?? throw new Exception("获取调度器失败");
    }

    public async Task<bool> StartAsync()
    {
        if (_scheduler.InStandbyMode)
        {
            await _scheduler.Start();
        }

        return _scheduler.InStandbyMode;
    }

    public async Task AddTrigger(string name, int priority, bool isMono)
    {
        var now = DateTimeOffset.Now.AddSeconds(5);
        var groupName = "testGroup";
        var triggerName = $"{name}_Trigger";
        var jobName = $"{name}_Job";

        var jobDataMap = new JobDataMap { { "name", name }, { "priority", priority }, { "isMono", isMono } };
        var jobDetail = JobBuilder.Create<MyJob>().WithIdentity(jobName, groupName).SetJobData(jobDataMap).Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity(triggerName, groupName)
            .WithPriority(priority)
            .StartAt(now)
            // .WithSimpleSchedule(x => x.WithInterval(interval).RepeatForever())
            // .WithCronSchedule("1 0/1 * * * ?", x => x.WithMisfireHandlingInstructionFireAndProceed())
            .WithCronSchedule("0/10 * * * * ?", x => x.WithMisfireHandlingInstructionFireAndProceed())
            //.WithCronSchedule("0/10 0/1 * 1/1 * ? *", x => x.WithMisfireHandlingInstructionFireAndProceed())
            .Build();

        await _scheduler.Start();
        await _scheduler.ScheduleJob(jobDetail, trigger)!;
    }

    public async Task<bool> DeleteJobAndTrigger(string name)
    {
        var groupName = "testGroup";
        var jobName = $"{name}_Job";
        var jobKey = new JobKey(jobName, groupName);
        var triggersList = await _scheduler.GetTriggersOfJob(jobKey);
        var trigger = triggersList.AsEnumerable().FirstOrDefault();
        if (trigger != null)
        {
            await _scheduler.PauseTrigger(trigger.Key);
            await _scheduler.UnscheduleJob(trigger.Key);
        }

        return await _scheduler.DeleteJob(jobKey);
    }

    public async Task<DateTimeOffset?> GetNextFireTime(string triggerKey)
    {
        var trigger = await _scheduler!.GetTrigger(new TriggerKey(triggerKey));
        return trigger?.GetNextFireTimeUtc();
    }
}