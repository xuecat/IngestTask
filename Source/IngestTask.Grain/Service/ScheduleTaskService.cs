

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
        private List<TaskContent> _lstScheduleTask;
        public ScheduleTaskService(IGrainIdentity id, Silo silo, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, IGrainFactory grainFactory)
            : base(id, silo, loggerFactory)
        {
            GrainFactory = grainFactory;
            _lstScheduleTask = new List<TaskContent>();
        }
      
        public Task<int> AddTaskAsync(TaskContent task)
        {
            throw new NotImplementedException();
        }

        public override Task Init(IServiceProvider serviceProvider)
        {
            return base.Init(serviceProvider);
        }

        protected override Task StartInBackground()
        {
            RegisterTimer(this.OnScheduleTaskAsync, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            
            return Task.CompletedTask;
        }

        public override async Task Start()
        {
            await base.Start();
        }

        public override Task Stop()
        {
            return base.Stop();
        }


        private Task OnScheduleTaskAsync(object type)
        {
            //如果判断到是周期任务，那么需要对它做分任务的处理
            //这个步骤挪到后台server去做

            //任务分发的时候要向请求通道是否存在，不存在提交一个自检请求

            lock (_lstScheduleTask)
            {
                foreach (var item in _lstScheduleTask)
                {

                }
            }

        }
    }
}
