using Quartz;

namespace QuartzApp
{

    [DisallowConcurrentExecution]
    public class TestJob : IJob
    {
        public System.Threading.Tasks.Task Execute(IJobExecutionContext context)
        {
            Console.WriteLine("hello quartz!");
            Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            return System.Threading.Tasks.Task.CompletedTask;
        }
    }
}
