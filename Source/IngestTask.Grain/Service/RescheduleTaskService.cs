﻿

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
    class RescheduleTaskService : GrainService, IRescheduleService
    {
        public Task<int> RescheduleTaskAsync()
        {
            throw new NotImplementedException();
        }
    }
}
