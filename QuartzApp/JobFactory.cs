using Quartz;
using Quartz.Spi;

namespace QuartzApp
{
    public class JobFactory : IJobFactory
    {
        protected readonly IServiceProvider _serviceProvider;

        public JobFactory(IServiceProvider container)
        {
            _serviceProvider = container;
        }

        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            using var serviceScope = _serviceProvider.CreateScope();
            return serviceScope.ServiceProvider.GetService<TestJob>()!;
        }

        public void ReturnJob(IJob job)
        {
            (job as IDisposable)?.Dispose();
        }
    }
}