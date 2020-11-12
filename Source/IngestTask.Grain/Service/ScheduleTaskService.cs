

namespace IngestTask.Grain.Service
{
    using IngestTask.Abstraction.Service;
    using IngestTask.Dto;
    using Orleans;
    using Orleans.Concurrency;
    using Orleans.Core;
    using Orleans.Runtime;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;

    [Reentrant]
    public class ScheduleTaskService : GrainService, IScheduleService
    {
        readonly IGrainFactory GrainFactory;
        public ScheduleTaskService(IGrainIdentity id, Silo silo, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, IGrainFactory grainFactory)
            : base(id, silo, loggerFactory)
        {
            GrainFactory = grainFactory;
        }
        public Task<int> ScheduleTaskAsync()
        {
            throw new NotImplementedException();
            //如果判断到是周期任务，那么需要对它做分任务的处理
            //这个步骤挪到后台server去做

            //任务分发的时候要向请求通道是否存在，不存在提交一个自检请求
        }

        public Task<int> AddTaskAsync(TaskContent task)
        {
            throw new NotImplementedException();
        }

        public override Task Init(IServiceProvider serviceProvider)
        {
            return base.Init(serviceProvider);
        }

        public override async Task Start()
        {
            await base.Start();
        }

        public override Task Stop()
        {
            return base.Stop();
        }

    }
}
