using Quartz;
using Quartz.Spi;

namespace SecondQuartzTest
{
    public class MyJobFactory : IJobFactory
    {
        /// &lt;summary&gt;
        /// 容器提供器，
        /// &lt;/summary&gt;
        protected IServiceProvider _serviceProvider;

        /// &lt;summary&gt;
        /// 构造函数
        /// &lt;/summary&gt;
        /// &lt;param name="serviceProvider"&gt;&lt;/param&gt;
        public MyJobFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

       /// <summary>
       /// /
       /// </summary>
       /// <param name="bundle"></param>
       /// <param name="scheduler"></param>
       /// <returns></returns>
        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {          
            //返回jobType对应类型的实例
            return _serviceProvider.GetService<MyJob>()!;

        }

        /// <summary>
        /// 清理销毁IJob
        /// </summary>
        /// <param name="job"></param>
        public void ReturnJob(IJob job)
        {
            var disposable = job as IDisposable;

            disposable?.Dispose();
        }
    }
}
