using Microsoft.EntityFrameworkCore;
using Quartz;
using Quartz.Impl.Triggers;
using SecondQuartzTest.Data;

namespace SecondQuartzTest;

// [DisallowConcurrentExecution]
public class MyJob : IJob
{
    private readonly ILogger<MyJob> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    public MyJob(ILogger<MyJob> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }
    public async Task Execute(IJobExecutionContext context)
    {
       
        // get task data
        var jobDetailJobDataMap = context.JobDetail.JobDataMap;
        var name = jobDetailJobDataMap.GetString("name");
        if (name == null) throw new Exception("name is null");
        var priority = jobDetailJobDataMap.GetInt("priority");
        var isMono = jobDetailJobDataMap.GetBooleanValue("isMono");
        var fireTime = context.FireTimeUtc.ToUnixTimeSeconds();
        var spentTime = jobDetailJobDataMap.GetIntValue("spentTime");
         using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetService<SecondQuartzTestContext>()!;        

        if (context.Trigger is CronTriggerImpl && context.NextFireTimeUtc.HasValue)
        {
            DateTimeOffset nextFireTimeUtc = (DateTimeOffset)context.NextFireTimeUtc;
          // nextFireTimeUtc = new DateTimeOffset(Convert.ToDateTime("2022-03-27 01:10:00"));//for test DST switch

            if ((nextFireTimeUtc.Month == 3 && nextFireTimeUtc.Date == GetLastSundayForMonth(nextFireTimeUtc.DateTime).Date
                && nextFireTimeUtc.Hour == 1)
                ||
                (nextFireTimeUtc.Month == 10 && nextFireTimeUtc.Date == GetLastSundayForMonth(nextFireTimeUtc.DateTime).Date
                && nextFireTimeUtc.Hour == 1))       //for Europe/Dublin timezone DST switch
            {
                nextFireTimeUtc = nextFireTimeUtc.AddHours(1);

                //  nextFireTimeUtc = new DateTimeOffset(DateTime.UtcNow).AddMinutes(2);//for test DST switch
                //   DateTime someDate = new DateTime(nextFireTimeUtc.Ticks); //for test DST switch
               // _logger.LogInformation("LOGGER --------------------------------------------");
                //   _logger.LogInformation( " SetNextFireTimeUtc : " + someDate); //for test DST switch
                //_logger.LogInformation("LOGGER --------------------------------------------");

                try
                {
                     dbContext.Database.ExecuteSqlRaw(
                    "update quartz.qrtz_triggers set NEXT_FIRE_TIME = {0} where JOB_NAME = {1}", nextFireTimeUtc.Ticks, name + "_Job");
                }
                catch (MySqlConnector.MySqlException e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }


        // if previous running is not finished or in waiting queue, skip this time
        if (QuartzManager.RunningJobs.ContainsKey(name) || QuartzManager.WaitingJobs.ContainsKey(name))
        {
            return;
        }

        var jobInfo = new JobInfo
        {
            FireTime = fireTime,
            IsMono = isMono,
            Name = name,
            Priority = priority,
        };

        // add task to waiting queue
        QuartzManager.WaitingJobs.AddOrUpdate(name, jobInfo, (_, _) => jobInfo);

        // check if current task should be executed
        while (true)
        {
            await QuartzManager.CheckOrderSemaphore.WaitAsync();
            try
            {
                await Task.Delay(100);

                // sort
                var jobQueue = QuartzManager.WaitingJobs.Values.OrderBy(i => i.FireTime)
                    .ThenByDescending(i => i.Priority).ThenBy(i => i.IsMono).ToArray();

                // if the following conditions are met, break the loop and go to be executed
                if (jobQueue[0].Name.Equals(name, StringComparison.Ordinal) && !QuartzManager.MonoJobIsRunning &&
                    (QuartzManager.RunningJobs.IsEmpty || !jobQueue[0].IsMono))
                {
                    QuartzManager.RunningJobs.AddOrUpdate(name, jobQueue[0], (_, _) => jobQueue[0]);
                    QuartzManager.WaitingJobs.TryRemove(name, out _);
                    break;
                }
            }
            finally
            {
                QuartzManager.CheckOrderSemaphore.Release();
            }
        }

        try
        {
            await JobWork(context, name, priority, isMono, spentTime);
        }
        finally
        {
            QuartzManager.RunningJobs.TryRemove(name, out _);
        }
    }
    private DateTime GetLastSundayForMonth(DateTime dt)
    {
        DateTime d = new DateTime(dt.Year, dt.Month, 1).AddMonths(1);
        while (!(d.DayOfWeek == DayOfWeek.Sunday && d.Month == dt.Month))
        {
            d = d.AddDays(-1);
        }
        return d;
    }
    private async Task JobWork(IJobExecutionContext context, string name, int priority, bool isMono, int spentTime)
    {
        Console.Write($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]Hello, I'm job ");
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($"{name}");
        Console.ResetColor();
        Console.WriteLine(
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]Scheduled fire time: {context.FireTimeUtc:yyyy-MM-dd HH:mm:ss}");
        await Task.Delay(spentTime * 1000);
        Console.Write($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]Job ");
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.Write($"{name}");
        Console.ResetColor();
        Console.WriteLine(" finished!");
    }
}