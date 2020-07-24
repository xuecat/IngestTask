

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
    class ScheduleTaskService : GrainService, IScheduleService
    {
        public Task<int> ScheduleTaskAsync()
        {
            throw new NotImplementedException();
        }

    }
}
