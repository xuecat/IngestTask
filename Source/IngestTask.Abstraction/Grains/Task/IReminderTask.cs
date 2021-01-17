

namespace IngestTask.Abstraction.Grains
{
    using IngestTask.Dto;
    using Orleans;
    using Orleans.Concurrency;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;

    public interface IReminderTask : IGrainWithIntegerKey
    {
        Task<int> AddTaskAsync(DispatchTask task);
        Task<int> UpdateTaskAsync(DispatchTask task);
        Task<int> DeleteTaskAsync(int task);
        Task<DispatchTask> GetTaskAsync(int taskid);
        Task<List<DispatchTask>> GetTaskListAsync(List<int> taskid);
        Task<List<DispatchTask>> GetTaskListAsync();
        Task CompleteTaskAsync(List<int> taskid);
        Task<bool> IsCachedAsync(int taskid);
    }
}
