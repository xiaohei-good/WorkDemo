namespace SecondQuartzTest; 
public class QuartzHostedService : IHostedService
{
    private readonly QuartzManager _quartzManager;

        public QuartzHostedService(QuartzManager quartzManager)
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