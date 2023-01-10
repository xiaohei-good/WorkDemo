namespace QuartzTest;

public class HostedService : IHostedService
{
    private QuartzManager _quartzManager;

    public HostedService(QuartzManager quartzManager)
    {
        _quartzManager = quartzManager;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _quartzManager.StartAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}