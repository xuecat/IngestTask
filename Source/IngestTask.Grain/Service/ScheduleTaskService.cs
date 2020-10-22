

namespace IngestTask.Grain.Service
{
    using IngestTask.Abstraction.Service;
    using IngestTask.Dto;
    using Orleans.Concurrency;
    using Orleans.Runtime;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;

    [Reentrant]
    public class ScheduleTaskService : GrainService, IScheduleService
    {
        public Task<int> ScheduleTaskAsync()
        {
            throw new NotImplementedException();
            //如果判断到是周期任务，那么需要对它做分任务的处理
            //这个步骤挪到后台server去做
        }

        public Task<int> AddTaskAsync(TaskContent task)
        {
            throw new NotImplementedException();
        }

    }
}
