namespace QuartzApp
{
    public class QuartzHostedService : IHostedService
    {
        private IQuartzManager _quartzManager;

        public QuartzHostedService(IQuartzManager quartzManager)
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
}
