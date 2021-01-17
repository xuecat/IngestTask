

namespace IngestTask.Abstraction.Grains
{
    using IngestTask.Dto;
    using Orleans;
    using Orleans.Concurrency;
    using Orleans.Services;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    public interface IScheduleTaskGrain : IGrainWithIntegerKey
    {
        Task<int> AddScheduleTaskAsync(DispatchTask task);
        Task<int> UpdateScheduleTaskAsync(DispatchTask task);
        Task<int> RemoveScheduleTaskAsync(DispatchTask task);
    }

   
}
