using Quartz;
using Quartz.Impl;
using Quartz.Impl.AdoJobStore;
using Quartz.Impl.AdoJobStore.Common;
using Quartz.Simpl;
using Quartz.Util;
namespace QuartzApp
{
    public class QuartzManager : IQuartzManager
    {
        private readonly IDbProvider _dbProvider;
        private readonly IServiceProvider _serviceProvider;
        private IScheduler? _scheduler;

        public QuartzManager(IConfiguration configuration, IServiceProvider serviceProvider)
        {
            _dbProvider = new DbProvider("MySql", configuration.GetConnectionString("ConnectionString"));
            _serviceProvider = serviceProvider;
        }

        public async Task<bool> StartAsync()
        {
            await InitSchedulerAsync();
            if (_scheduler?.InStandbyMode == true) await _scheduler.Start();

            return _scheduler?.InStandbyMode ?? false;
        }

        public async Task<bool> SaveTrigger(string task)
        {
            Guid id = Guid.NewGuid();
            var newTriggerName = id + "Trigger";
            var groupName = id + "Group";
            var jobName = id + "Job";
            ITrigger newTrigger;
     
                newTrigger = CreateImmediateTrigger(groupName, jobName, newTriggerName);
           
               // newTrigger = CreateScheduledTrigger(groupName, jobName, newTriggerName, task.ScheduledTimestamp!.Value);
            
              //  newTrigger = CreateCronTrigger(groupName, jobName, newTriggerName, task.CronExpression);
  
           // if ()
           // {
                var job = JobBuilder.Create<TestJob>()
                    .WithIdentity(jobName, groupName)
                    .Build();
                job.JobDataMap.Put("taskId", id);
                await _scheduler!.ScheduleJob(job, newTrigger);
          //  }
          //  else //update task's TriggerKey
          //  {
            //    var jobKey = new JobKey(jobName, groupName);
            //    var triggersList = await _scheduler!.GetTriggersOfJob(jobKey);
            //    var oldTrigger = triggersList.AsEnumerable().FirstOrDefault();
            //    if (oldTrigger != null)
            //    {
            //        await _scheduler!.RescheduleJob(oldTrigger.Key, newTrigger);
            //    }
            //    else
            //    {
            //        return false;
            //    }
            //}

           
            return true;
        }

        public async Task<bool> DeleteJobAndTrigger(Guid id)
        {
            var groupName = id + "Group";
            var jobName = id + "Job";
            var jobKey = new JobKey(jobName, groupName);
            var triggersList = await _scheduler!.GetTriggersOfJob(jobKey);
            var trigger = triggersList.AsEnumerable().FirstOrDefault();
            if (trigger != null)
            {
                await _scheduler.PauseTrigger(trigger.Key);
                await _scheduler.UnscheduleJob(trigger.Key);
            }

            return await _scheduler.DeleteJob(jobKey);
        }

        public async Task<DateTimeOffset?> GetNextFireTime(string triggerKey)
        {
            var trigger = await _scheduler!.GetTrigger(new TriggerKey(triggerKey));
            return trigger?.GetNextFireTimeUtc();
        }

        private async System.Threading.Tasks.Task InitSchedulerAsync()
        {
            if (_scheduler == null)
            {
                DBConnectionManager.Instance.AddConnectionProvider("default", _dbProvider);
                var serializer = new JsonObjectSerializer();
                serializer.Initialize();
                var jobStore = new JobStoreTX
                {
                    DataSource = "default",
                    TablePrefix = "BSP_QUARTZ_",
                    InstanceId = "MainScheduler",
                    DriverDelegateType = "Quartz.Impl.AdoJobStore.MySQLDelegate, Quartz",
                    ObjectSerializer = serializer
                };
                DirectSchedulerFactory.Instance.CreateScheduler("BspScheduler", "MainScheduler", new DefaultThreadPool(),
                    jobStore);
                _scheduler = await SchedulerRepository.Instance.Lookup("BspScheduler");
                _scheduler!.JobFactory = new JobFactory(_serviceProvider);
            }
        }

        private ITrigger CreateImmediateTrigger(string groupName, string jobName, string triggerName)
        {
            return TriggerBuilder.Create()
                .WithIdentity(triggerName, groupName)
                .StartNow()
                .ForJob(jobName, groupName)
                .Build();
        }

        private ITrigger CreateScheduledTrigger(string groupName, string jobName, string triggerName,
            long scheduledTimestamp)
        {
            return TriggerBuilder.Create()
                .WithIdentity(triggerName, groupName)
                .StartAt(DateTimeOffset.FromUnixTimeMilliseconds(scheduledTimestamp))
                .ForJob(jobName, groupName)
                .Build();
        }

        private ITrigger CreateCronTrigger(string groupName, string jobName, string triggerName, string cronExpression)
        {
            return TriggerBuilder.Create()
                .WithIdentity(triggerName, groupName)
                .WithCronSchedule(cronExpression)
                .ForJob(jobName, groupName)
                .Build();
        }
    }
}
