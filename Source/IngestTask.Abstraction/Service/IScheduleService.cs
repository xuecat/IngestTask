

namespace IngestTask.Abstraction.Service
{
    using IngestTask.Dto;
    using Orleans.Concurrency;
    using Orleans.Services;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    public interface IScheduleService : IGrainService
    {
        Task<string> AddScheduleTaskAsync(DispatchTask task);
        Task<int> RemoveScheduleTaskAsync(DispatchTask task);
        [OneWay]
        Task RefreshAsync();
    }

   
}
