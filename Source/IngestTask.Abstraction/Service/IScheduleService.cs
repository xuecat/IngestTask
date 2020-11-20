

namespace IngestTask.Abstraction.Service
{
    using IngestTask.Dto;
    using Orleans.Services;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    public interface IScheduleService : IGrainService
    {
        Task<int> AddTaskAsync(DispatchTask task);
        Task<int> UpdateTaskAsync(DispatchTask task);
        Task<int> CheckTaskListAsync(List<DispatchTask> task);
    }

    public interface ICheckScheduleService : IGrainService
    {
    }
}
