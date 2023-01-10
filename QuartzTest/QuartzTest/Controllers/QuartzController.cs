using Microsoft.AspNetCore.Mvc;
using Quartz;
using Quartz.Impl;

namespace QuartzTest.Controllers;

[Route("[controller]/[action]")]
public class QuartzController : Controller
{
    private QuartzManager _quartzManager;

    public QuartzController(QuartzManager quartzManager)
    {
        _quartzManager = quartzManager;
    }
    
    [HttpGet]
    public async Task AddJob()
    {
        await _quartzManager.AddScheduleJobAsync<MyJob>(TimeSpan.FromSeconds(3));
    }

    [HttpDelete]
    public async Task DeleteJob()
    {
        await _quartzManager.StopOrDelScheduleJobAsync("a_group", "a_job", true);
    }

    // [HttpPost]
    // public async Task ModifyJob()
    // {
    //     await QuartzHelper.ModifyJob("a");
    // }
}