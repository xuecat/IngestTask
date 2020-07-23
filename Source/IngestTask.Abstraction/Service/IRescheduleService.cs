
using Orleans.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IngestTask.Abstraction.Service
{
    public interface IRescheduleService : IGrainService
    {
        Task<int> RescheduleTaskAsync();
    }
}
