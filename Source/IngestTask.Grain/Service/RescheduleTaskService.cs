using IngestTask.Abstraction.Service;
using Orleans.Concurrency;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IngestTask.Grain.Service
{
    [Reentrant]
    class RescheduleTaskService : GrainService, IRescheduleService
    {
        Task<int> IRescheduleService.RescheduleTaskAsync()
        {
            throw new NotImplementedException();
        }
    }
}
