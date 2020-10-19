

namespace IngestTask.Grain.Service
{
    using IngestTask.Abstraction.Service;
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
        }

        public Task<int> AddTaskAsync()
        {
            throw new NotImplementedException();
        }

    }
}
