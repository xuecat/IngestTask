

namespace IngestTask.Abstraction.Service
{
    using IngestTask.Dto;
    using Orleans.Services;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    public interface IScheduleService : IGrainService
    {
        Task<int> AddScheduleTaskAsync(DispatchTask task);
        Task<int> RemoveScheduleTaskAsync(DispatchTask task);
    }

   
}
