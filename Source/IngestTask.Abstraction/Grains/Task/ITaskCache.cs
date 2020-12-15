

namespace IngestTask.Abstraction.Grains
{
    using IngestTask.Dto;
    using Orleans;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;

    public interface ITaskCache : IGrainWithIntegerKey
    {
        Task AddTaskAsync(DispatchTask task);
        Task UpdateTaskAsync(DispatchTask task);
        Task DeleteTaskAsync(DispatchTask task);
        Task<DispatchTask> GetTaskAsync(int taskid);
        Task<List<DispatchTask>> GetTaskListAsync(List<int> taskid);
        Task CompleteTaskAsync(List<int> taskid);
    }
}
