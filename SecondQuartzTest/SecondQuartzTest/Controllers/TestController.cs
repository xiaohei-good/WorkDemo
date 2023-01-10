using Microsoft.AspNetCore.Mvc;
using QuartzTest;

namespace SecondQuartzTest.Controllers;

[Route("[controller]/[action]")]
public class TestController : Controller
{
    private readonly QuartzManager _quartzManager;
    
    public TestController(QuartzManager quartzManager)
    {
        _quartzManager = quartzManager;
    }
    
    [HttpGet]
    public async Task TestCron()
    {
        await _quartzManager.AddTrigger("a", 1, true);
        await _quartzManager.AddTrigger("b", 3, true);
        await _quartzManager.AddTrigger("c", 2, false);
        await _quartzManager.AddTrigger("d", 1, false);
        await _quartzManager.AddTrigger("e", 2, false);
    }
}