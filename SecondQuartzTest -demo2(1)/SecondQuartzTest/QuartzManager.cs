using System.Collections.Concurrent;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.AdoJobStore;
using Quartz.Impl.AdoJobStore.Common;
using Quartz.Simpl;
using Quartz.Util;

namespace SecondQuartzTest;

public class QuartzManager
{
    /// <summary>
    /// 调度器
    /// </summary>
    private readonly IScheduler _scheduler;

    public static ConcurrentDictionary<string, JobInfo> RunningJobs = new();
    public static ConcurrentDictionary<string, JobInfo> WaitingJobs = new();

    public static bool MonoJobIsRunning => RunningJobs.Values.Any(j => j.IsMono);
 
    public static readonly SemaphoreSlim CheckOrderSemaphore = new(1);
    protected IServiceProvider _serviceProvider;
    public  QuartzManager(IServiceProvider serviceProvide)
    {
        _serviceProvider= serviceProvide;
        DBConnectionManager.Instance.AddConnectionProvider("default", new DbProvider("MySql", "server=localhost;port=3307;database=quartz;user=root;password=1234"));
        var serializer = new JsonObjectSerializer();
        serializer.Initialize();
        var jobStore = new JobStoreTX
        {
            DataSource = "default",
            TablePrefix = "qrtz_",
            InstanceId = "MainScheduler",
            DriverDelegateType = "Quartz.Impl.AdoJobStore.MySQLDelegate, Quartz",
            ObjectSerializer = serializer
        };
        DirectSchedulerFactory.Instance.CreateScheduler("bennyScheduler", "MainScheduler", new DefaultThreadPool(),
            jobStore);
      ///  DirectSchedulerFactory.Instance.CreateScheduler("bennyScheduler", "AUTO", new DefaultThreadPool(),
       //    new RAMJobStore());
        _scheduler = SchedulerRepository.Instance.Lookup("bennyScheduler").Result ?? throw new Exception("获取调度器失败");
       // _scheduler.Start();

        var loggerFactory = _serviceProvider.GetService<ILoggerFactory>();
        if (loggerFactory != null)
        {
            Quartz.Logging.LogContext.SetCurrentLogProvider(loggerFactory);
        }

        _scheduler.JobFactory = new MyJobFactory(_serviceProvider);
        _scheduler.Start();
    }

    public async Task<bool> StartAsync()
    {
        if (_scheduler.InStandbyMode)
        {
            await _scheduler.Start();
        }

        return _scheduler.InStandbyMode;
    }

    public async Task AddTrigger(string name, int priority, bool isMono, int spentTime)
    {
        var now = DateTimeOffset.Now.AddSeconds(5);
        var groupName = "testGroup";
        var triggerName = $"{name}_Trigger";
        var jobName = $"{name}_Job";

        var jobDataMap = new JobDataMap
            { { "name", name }, { "priority", priority }, { "isMono", isMono }, { "spentTime", spentTime } };
        var jobDetail = JobBuilder.Create<MyJob>().WithIdentity(jobName, jobName + "testGroup").SetJobData(jobDataMap).Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity(triggerName, triggerName + "testGroup")
            .WithPriority(priority)
            .StartAt(now)
            // .WithSimpleSchedule(x => x.WithInterval(interval).RepeatForever())
            // .WithCronSchedule("1 0/1 * * * ?", x => x.WithMisfireHandlingInstructionFireAndProceed())
            //.WithCronSchedule("0/10 * * * * ?", x => x.WithMisfireHandlingInstructionFireAndProceed())
            .WithSchedule(CronScheduleBuilder.CronSchedule("0/10 * * * * ?"))
            .Build();
      
        Console.WriteLine(" trigger.StartTimeUtc :" + trigger.StartTimeUtc.ToString());
      //  await _scheduler.Start();
        await _scheduler.ScheduleJob(jobDetail, trigger);
    }

    #region

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

    #endregion
}