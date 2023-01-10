using Quartz;
using QuartzTest;

namespace SecondQuartzTest;

// [DisallowConcurrentExecution]
public class MyJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {

        IScheduler sche = context.Scheduler;
        var jobDetailJobDataMap = context.JobDetail.JobDataMap;
        var name = jobDetailJobDataMap.GetString("name");
        if (name == null) throw new Exception("name is null");
        var priority = jobDetailJobDataMap.GetInt("priority");
        var isMono = jobDetailJobDataMap.GetBooleanValue("isMono");
        var fireTime = context.FireTimeUtc.ToUnixTimeSeconds();

        var jobInfo = new JobInfo
        {
            FireTime = fireTime,
            IsMono = isMono,
            Name = name,
            Priority = priority,
        };
        QuartzManager.WaitingJobs.AddOrUpdate(name, jobInfo, (_, _) => jobInfo);
        while (true)
        {
            await QuartzManager.CheckOrderSemaphore.WaitAsync();
            try
            {
                 Console.WriteLine($"Job {name} is checking");

                var jobQueue = QuartzManager.WaitingJobs.Values.OrderBy(i => i.FireTime)
                    .ThenByDescending(i => i.Priority).ToArray();
                 Console.WriteLine($"{name}: {System.Text.Json.JsonSerializer.Serialize(jobQueue)}");
                 Console.WriteLine($"{name}: {QuartzManager.MonoJobIsRunning}-{QuartzManager.RunningJobs.IsEmpty}");

                 Console.WriteLine($"First of queue is {jobQueue[0].Name}");

                if (jobQueue[0].Name.Equals(name, StringComparison.Ordinal) && !QuartzManager.MonoJobIsRunning &&
                    (QuartzManager.RunningJobs.IsEmpty || !jobQueue[0].IsMono))
                {
                    Console.WriteLine($"Job {name} will be run");
                    QuartzManager.RunningJobs.AddOrUpdate(name, priority, (_, _) => priority);
                    if (isMono)
                        QuartzManager.MonoJobIsRunning = true;
                    QuartzManager.WaitingJobs.TryRemove(name, out _);
                    break;
                }
            }
            finally
            {
                QuartzManager.CheckOrderSemaphore.Release();
            }

            await Task.Delay(1000);
        }

        if (QuartzManager.MonoJobIsRunning)
        {
            await QuartzManager.RunningMonoSemaphore.WaitAsync();
            try
            {
                await JobWork(name, priority, isMono);
            }
            finally
            {
                QuartzManager.RunningJobs.TryRemove(name, out _);
                QuartzManager.MonoJobIsRunning = false;
                QuartzManager.RunningMonoSemaphore.Release();
            }
        }
        else
        {
            await JobWork(name, priority, isMono);
            QuartzManager.RunningJobs.TryRemove(name, out _);
            QuartzManager.MonoJobIsRunning = false;
        }
    }

    private async Task JobWork(string name, int priority, bool isMono)
    {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}]Hello, I'm job {name}!");
        Console.WriteLine($"Data: {name}, {priority}, {isMono}");
        await Task.Delay(3000);
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}]Job {name} finished!");
    }
}