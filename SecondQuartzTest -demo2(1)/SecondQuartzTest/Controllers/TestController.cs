using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace SecondQuartzTest.Controllers;

[Route("[controller]/[action]")]
public class TestController : Controller
{
    private readonly QuartzManager _quartzManager;
    private readonly ILogger<TestController> _logger;
    public TestController (QuartzManager quartzManager, ILogger<TestController> logger)
    {
        _quartzManager = quartzManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task TestCron()
    {
        await _quartzManager.AddTrigger(name: "a", priority: 4, isMono: false, spentTime: 3);
        await _quartzManager.AddTrigger(name: "b", priority: 1, isMono: true, spentTime: 3);
        await _quartzManager.AddTrigger(name: "c", priority: 10, isMono: false, spentTime: 3);
        await _quartzManager.AddTrigger(name: "d", priority: 2, isMono: false, spentTime: 3);
        await _quartzManager.AddTrigger(name: "e", priority: 2, isMono: true, spentTime: 3);
    }


    [HttpPost]
    public IActionResult ConvertPdf([FromBody] string pdfArgs)
    {
        try
        {
            PdfArg toPdfArgs = JsonConvert.DeserializeObject<PdfArg>(pdfArgs)!;
        }
        catch (Exception e)
        {
            var exceptionMessage = e.InnerException?.Message ?? e.Message;
            _logger.LogError(
                "An error occured during the convertPdf api deserializeObject:  Error message is: {error}",
                exceptionMessage);
            return Problem(exceptionMessage);
        }



        //TODO convert pdf...


        return Ok();
    }
}