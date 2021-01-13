

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
        Task AddTaskAsync(DispatchTask task, string parsableaddress);
        Task<string> UpdateTaskAsync(DispatchTask task, string parsableaddress);
        Task<string> DeleteTaskAsync(int task);
        Task<DispatchTask> GetTaskAsync(int taskid);
        Task<List<DispatchTask>> GetTaskListAsync(List<int> taskid);
        Task<List<DispatchTask>> GetTaskListAsync();
        Task CompleteTaskAsync(List<int> taskid);
        Task<bool> IsCachedAsync(int taskid);
    }
}
