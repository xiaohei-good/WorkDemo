using Quartz;
using Quartz.Impl;
using Quartz.Impl.AdoJobStore;
using Quartz.Impl.AdoJobStore.Common;
using Quartz.Impl.Matchers;
using Quartz.Impl.Triggers;
using Quartz.Simpl;
using Quartz.Util;
using QuartzTest.Enums;

namespace QuartzTest;

public class QuartzManager
{
    /// <summary>
    /// 调度器
    /// </summary>
    private IScheduler? _scheduler;

    private IDbProvider _dbProvider;

    public QuartzManager()
    {
        _dbProvider = new DbProvider("SQLite-Microsoft", "Data Source=dbfile/quartztest.db;");
        // "serv?er=127.0.0.1;port=3306;database=quartztest;user=root;password=collenda;Pooling=true");
    }

    public async Task<bool> StartAsync()
    {
        await InitSchedulerAsync();
        if (_scheduler?.InStandbyMode == true)
        {
            await _scheduler.Start();
        }

        return _scheduler?.InStandbyMode ?? false;
    }


    /// <summary>
    /// 初始化Scheduler
    /// </summary>
    private async Task InitSchedulerAsync()
    {
        if (_scheduler == null)
        {
            DBConnectionManager.Instance.AddConnectionProvider("default", _dbProvider);
            var serializer = new JsonObjectSerializer();
            serializer.Initialize();
            var jobStore = new JobStoreTX
            {
                DataSource = "default",
                TablePrefix = "backgroundspooler_",
                InstanceId = "AUTO",
                DriverDelegateType = "Quartz.Impl.AdoJobStore.SQLiteDelegate, Quartz",
                ObjectSerializer = serializer,
            };
            DirectSchedulerFactory.Instance.CreateScheduler("bennyScheduler", "AUTO", new DefaultThreadPool(),
                jobStore);
            _scheduler = await SchedulerRepository.Instance.Lookup("bennyScheduler");
        }
    }

    /// <summary>
    /// 添加一个工作调度
    /// </summary>
    /// <param name="interval"></param>
    /// <param name="entity"></param>
    /// <param name="runNumber"></param>
    /// <returns></returns>
    public async Task AddScheduleJobAsync<T>(TimeSpan interval) where T : IJob
    {
        var jobDetail = JobBuilder.Create<T>().WithIdentity("a_job", "a_group").Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity("a_trigger", "a_group")
            .StartNow()
            .WithSimpleSchedule(x => x.WithInterval(interval).RepeatForever())
            .Build();

        _scheduler?.Start();
        await _scheduler?.ScheduleJob(jobDetail, trigger)!;
        // var result = new BaseResult();
        // try
        // {
        //     //检查任务是否已存在
        //     var jobKey = new JobKey(entity.JobName, entity.JobGroup);
        //     if (await _scheduler.CheckExists(jobKey))
        //     {
        //         result.Code = 500;
        //         result.Msg = "任务已存在";
        //         return result;
        //     }
        //
        //     //http请求配置
        //     var httpDir = new Dictionary<string, string>()
        //     {
        //         { Constant.EndAt, entity.EndTime.ToString() },
        //         { Constant.JobTypeEnum, ((int)entity.JobType).ToString() },
        //     };
        //     if (runNumber.HasValue)
        //         httpDir.Add(Constant.RUNNUMBER, runNumber.ToString());
        //
        //     IJobConfigurator jobConfigurator = null;
        //     if (entity.JobType == JobTypeEnum.TestA)
        //     {
        //         jobConfigurator = JobBuilder.Create<MyJob>();
        //     }
        //
        //     // 定义这个工作，并将其绑定到我们的IJob实现类                
        //     var job = jobConfigurator
        //         .SetJobData(new JobDataMap(httpDir))
        //         .WithDescription(entity.Description)
        //         .WithIdentity(entity.JobName, entity.JobGroup)
        //         .Build();
        //     // 创建触发器
        //     ITrigger trigger;
        //     //校验是否正确的执行周期表达式
        //     if (entity.TriggerType == TriggerTypeEnum.Cron) //CronExpression.IsValidExpression(entity.Cron))
        //     {
        //         trigger = CreateCronTrigger(entity);
        //     }
        //     else
        //     {
        //         trigger = CreateSimpleTrigger(entity);
        //     }
        //
        //     // 告诉Quartz使用我们的触发器来安排作业
        //     await _scheduler.ScheduleJob(job, trigger);
        //     result.Code = 200;
        // }
        // catch (Exception ex)
        // {
        //     result.Code = 505;
        //     result.Msg = ex.Message;
        // }
        //
        // return result;
    }

    /// <summary>
    /// 暂停/删除 指定的计划
    /// </summary>
    /// <param name="jobGroup">任务分组</param>
    /// <param name="jobName">任务名称</param>
    /// <param name="isDelete">停止并删除任务</param>
    /// <returns></returns>
    public async Task<BaseResult> StopOrDelScheduleJobAsync(string jobGroup, string jobName, bool isDelete = false)
    {
        BaseResult result;
        try
        {
            await _scheduler.PauseJob(new JobKey(jobName, jobGroup));
            if (isDelete)
            {
                await _scheduler.DeleteJob(new JobKey(jobName, jobGroup));
                result = new BaseResult
                {
                    Code = 200,
                    Msg = "删除任务计划成功！"
                };
            }
            else
            {
                result = new BaseResult
                {
                    Code = 200,
                    Msg = "停止任务计划成功！"
                };
            }
        }
        catch (Exception ex)
        {
            result = new BaseResult
            {
                Code = 505,
                Msg = "停止任务计划失败" + ex.Message
            };
        }

        return result;
    }

    /// <summary>
    /// 恢复运行暂停的任务
    /// </summary>
    /// <param name="jobName">任务名称</param>
    /// <param name="jobGroup">任务分组</param>
    public async Task<BaseResult> ResumeJobAsync(string jobGroup, string jobName)
    {
        BaseResult result = new BaseResult();
        try
        {
            //检查任务是否存在
            var jobKey = new JobKey(jobName, jobGroup);
            if (await _scheduler.CheckExists(jobKey))
            {
                var jobDetail = await _scheduler.GetJobDetail(jobKey);
                var endTime = jobDetail.JobDataMap.GetString("EndAt");
                if (!string.IsNullOrWhiteSpace(endTime) && DateTime.Parse(endTime) <= DateTime.Now)
                {
                    result.Code = 500;
                    result.Msg = "Job的结束时间已过期。";
                }
                else
                {
                    //任务已经存在则暂停任务
                    await _scheduler.ResumeJob(jobKey);
                    result.Msg = "恢复任务计划成功！";
                    Console.WriteLine($"任务“{jobName}”恢复运行");
                }
            }
            else
            {
                result.Code = 500;
                result.Msg = "任务不存在";
            }
        }
        catch (Exception ex)
        {
            result.Msg = "恢复任务计划失败！";
            result.Code = 500;
            await Console.Error.WriteLineAsync($"恢复任务失败！{ex}");
        }

        return result;
    }

    /// <summary>
    /// 查询任务
    /// </summary>
    /// <param name="jobGroup"></param>
    /// <param name="jobName"></param>
    /// <returns></returns>
    public async Task<ScheduleEntity> QueryJobAsync(string jobGroup, string jobName)
    {
        var entity = new ScheduleEntity();
        var jobKey = new JobKey(jobName, jobGroup);
        var jobDetail = await _scheduler.GetJobDetail(jobKey);
        var triggersList = await _scheduler.GetTriggersOfJob(jobKey);
        var triggers = triggersList.AsEnumerable().FirstOrDefault();
        var intervalSeconds = (triggers as SimpleTriggerImpl)?.RepeatInterval.TotalSeconds;
        var endTime = jobDetail.JobDataMap.GetString("EndAt");
        entity.BeginTime = triggers.StartTimeUtc.LocalDateTime;
        if (!string.IsNullOrWhiteSpace(endTime)) entity.EndTime = DateTime.Parse(endTime);
        if (intervalSeconds.HasValue) entity.IntervalSecond = Convert.ToInt32(intervalSeconds.Value);
        entity.JobGroup = jobGroup;
        entity.JobName = jobName;
        entity.Cron = (triggers as CronTriggerImpl)?.CronExpressionString;
        entity.RunTimes = (triggers as SimpleTriggerImpl)?.RepeatCount;
        entity.TriggerType = triggers is SimpleTriggerImpl ? TriggerTypeEnum.Simple : TriggerTypeEnum.Cron;
        entity.Description = jobDetail.Description;
        //旧代码没有保存JobTypeEnum，所以None可以默认为Url。
        entity.JobType = (JobTypeEnum)int.Parse(jobDetail.JobDataMap.GetString(Constant.JobTypeEnum) ?? "1");

        switch (entity.JobType)
        {
            case JobTypeEnum.TestA:
                break;
            default:
                break;
        }

        return entity;
    }

    /// <summary>
    /// 立即执行
    /// </summary>
    /// <param name="jobKey"></param>
    /// <returns></returns>
    public async Task<bool> TriggerJobAsync(JobKey jobKey)
    {
        await _scheduler.TriggerJob(jobKey);
        return true;
    }

    /// <summary>
    /// 获取job日志
    /// </summary>
    /// <param name="jobKey"></param>
    /// <returns></returns>
    public async Task<List<string>> GetJobLogsAsync(JobKey jobKey)
    {
        var jobDetail = await _scheduler.GetJobDetail(jobKey);
        return jobDetail.JobDataMap[Constant.LOGLIST] as List<string>;
    }

    /// <summary>
    /// 获取运行次数
    /// </summary>
    /// <param name="jobKey"></param>
    /// <returns></returns>
    public async Task<long> GetRunNumberAsync(JobKey jobKey)
    {
        var jobDetail = await _scheduler.GetJobDetail(jobKey);
        return jobDetail.JobDataMap.GetLong(Constant.RUNNUMBER);
    }

    /// <summary>
    /// 获取所有Job（详情信息 - 初始化页面调用）
    /// </summary>
    /// <returns></returns>
    public async Task<List<JobInfoEntity>> GetAllJobAsync()
    {
        List<JobKey> jboKeyList = new List<JobKey>();
        List<JobInfoEntity> jobInfoList = new List<JobInfoEntity>();
        var groupNames = await _scheduler.GetJobGroupNames();
        foreach (var groupName in groupNames.OrderBy(t => t))
        {
            jboKeyList.AddRange(await _scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals(groupName)));
            jobInfoList.Add(new JobInfoEntity() { GroupName = groupName });
        }

        foreach (var jobKey in jboKeyList.OrderBy(t => t.Name))
        {
            var jobDetail = await _scheduler.GetJobDetail(jobKey);
            var triggersList = await _scheduler.GetTriggersOfJob(jobKey);
            var triggers = triggersList.AsEnumerable().FirstOrDefault();

            var interval = string.Empty;
            if (triggers is SimpleTriggerImpl)
                interval = (triggers as SimpleTriggerImpl)?.RepeatInterval.ToString();
            else
                interval = (triggers as CronTriggerImpl)?.CronExpressionString;

            foreach (var jobInfo in jobInfoList)
            {
                if (jobInfo.GroupName == jobKey.Group)
                {
                    //旧代码没有保存JobTypeEnum，所以None可以默认为Url。
                    var jobType = (JobTypeEnum)jobDetail.JobDataMap.GetLong(Constant.JobTypeEnum);

                    var triggerAddress = string.Empty;

                    //Constant.MailTo
                    jobInfo.JobInfoList.Add(new JobInfo()
                    {
                        Name = jobKey.Name,
                        LastErrMsg = jobDetail.JobDataMap.GetString(Constant.EXCEPTION),
                        TriggerAddress = triggerAddress,
                        TriggerState = await _scheduler.GetTriggerState(triggers.Key),
                        PreviousFireTime = triggers.GetPreviousFireTimeUtc()?.LocalDateTime,
                        NextFireTime = triggers.GetNextFireTimeUtc()?.LocalDateTime,
                        BeginTime = triggers.StartTimeUtc.LocalDateTime,
                        Interval = interval,
                        EndTime = triggers.EndTimeUtc?.LocalDateTime,
                        Description = jobDetail.Description,
                        RequestType = jobDetail.JobDataMap.GetString(Constant.REQUESTTYPE),
                        RunNumber = jobDetail.JobDataMap.GetLong(Constant.RUNNUMBER),
                        JobType = (long)jobType
                        //(triggers as SimpleTriggerImpl)?.TimesTriggered
                        //CronTriggerImpl 中没有 TimesTriggered 所以自己RUNNUMBER记录
                    });
                    continue;
                }
            }
        }

        return jobInfoList;
    }

    /// <summary>
    /// 获取所有Job信息（简要信息 - 刷新数据的时候使用）
    /// </summary>
    /// <returns></returns>
    public async Task<List<JobBriefInfoEntity>> GetAllJobBriefInfoAsync()
    {
        List<JobKey> jboKeyList = new List<JobKey>();
        List<JobBriefInfoEntity> jobInfoList = new List<JobBriefInfoEntity>();
        var groupNames = await _scheduler.GetJobGroupNames();
        foreach (var groupName in groupNames.OrderBy(t => t))
        {
            jboKeyList.AddRange(await _scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals(groupName)));
            jobInfoList.Add(new JobBriefInfoEntity() { GroupName = groupName });
        }

        foreach (var jobKey in jboKeyList.OrderBy(t => t.Name))
        {
            var jobDetail = await _scheduler.GetJobDetail(jobKey);
            var triggersList = await _scheduler.GetTriggersOfJob(jobKey);
            var triggers = triggersList.AsEnumerable().FirstOrDefault();

            foreach (var jobInfo in jobInfoList)
            {
                if (jobInfo.GroupName == jobKey.Group)
                {
                    jobInfo.JobInfoList.Add(new JobBriefInfo()
                    {
                        Name = jobKey.Name,
                        LastErrMsg = jobDetail?.JobDataMap.GetString(Constant.EXCEPTION),
                        TriggerState = await _scheduler.GetTriggerState(triggers.Key),
                        PreviousFireTime = triggers.GetPreviousFireTimeUtc()?.LocalDateTime,
                        NextFireTime = triggers.GetNextFireTimeUtc()?.LocalDateTime,
                        RunNumber = jobDetail?.JobDataMap.GetLong(Constant.RUNNUMBER) ?? 0
                    });
                    continue;
                }
            }
        }

        return jobInfoList;
    }

    /// <summary>
    /// 停止任务调度
    /// </summary>
    public async Task<bool> StopScheduleAsync()
    {
        //判断调度是否已经关闭
        if (!_scheduler.InStandbyMode)
        {
            //等待任务运行完成
            await _scheduler.Standby(); //TODO  注意：Shutdown后Start会报错，所以这里使用暂停。
            Console.WriteLine("任务调度暂停！");
        }

        return !_scheduler.InStandbyMode;
    }

    /// <summary>
    /// 创建类型Simple的触发器
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    private ITrigger CreateSimpleTrigger(ScheduleEntity entity)
    {
        //作业触发器
        if (entity.RunTimes.HasValue && entity.RunTimes > 0)
        {
            return TriggerBuilder.Create()
                .WithIdentity(entity.JobName, entity.JobGroup)
                .StartAt(entity.BeginTime) //开始时间
                //.EndAt(entity.EndTime)//结束数据
                .WithSimpleSchedule(x =>
                {
                    x.WithIntervalInSeconds(entity.IntervalSecond.Value) //执行时间间隔，单位秒
                        .WithRepeatCount(entity.RunTimes.Value) //执行次数、默认从0开始
                        .WithMisfireHandlingInstructionNextWithRemainingCount();
                })
                .ForJob(entity.JobName, entity.JobGroup) //作业名称
                .Build();
        }
        else
        {
            return TriggerBuilder.Create()
                .WithIdentity(entity.JobName, entity.JobGroup)
                .StartAt(entity.BeginTime) //开始时间
                //.EndAt(entity.EndTime)//结束数据
                .WithSimpleSchedule(x =>
                {
                    x.WithIntervalInSeconds(entity.IntervalSecond.Value) //执行时间间隔，单位秒
                        .RepeatForever() //无限循环
                        .WithMisfireHandlingInstructionFireNow();
                })
                .ForJob(entity.JobName, entity.JobGroup) //作业名称
                .Build();
        }
    }

    /// <summary>
    /// 创建类型Cron的触发器
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    private ITrigger CreateCronTrigger(ScheduleEntity entity)
    {
        // 作业触发器
        return TriggerBuilder.Create()
            .WithIdentity(entity.JobName, entity.JobGroup)
            .StartAt(entity.BeginTime) //开始时间
            //.EndAt(entity.EndTime)//结束时间
            .WithCronSchedule(entity.Cron,
                cronScheduleBuilder => cronScheduleBuilder.WithMisfireHandlingInstructionFireAndProceed()) //指定cron表达式
            .ForJob(entity.JobName, entity.JobGroup) //作业名称
            .Build();
    }
}