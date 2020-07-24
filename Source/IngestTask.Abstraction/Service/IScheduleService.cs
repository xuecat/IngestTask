

namespace IngestTask.Abstraction.Service
{
    using Orleans.Services;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    public interface IScheduleService : IGrainService
    {
        Task<int> ScheduleTaskAsync();
    }
}
