using System.Text.Json;
using Quartz;

namespace QuartzTest;

[DisallowConcurrentExecution]
public class MyJob : IJob
{
    // private readonly ILogger<MyJob> _logger;
    //
    // public MyJob(ILogger<MyJob> logger)
    // {
    //     _logger = logger;
    // }

    public Task Execute(IJobExecutionContext context)
    {
        // _logger.LogInformation(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        var jobKey = context.JobDetail.Key;
        var jobDataMap = context.JobDetail.JobDataMap;
        Console.WriteLine($"JobKey: {jobKey}, JobDataMap: {JsonSerializer.Serialize(jobDataMap)}");
        return Task.CompletedTask;
    }
}