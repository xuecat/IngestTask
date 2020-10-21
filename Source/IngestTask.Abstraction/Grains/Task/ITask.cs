using IngestTask.Dto;
using Orleans;
using Orleans.CodeGeneration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IngestTask.Abstraction.Grains
{
    public interface ITask : IGrainWithIntegerKey
    {
        Task<TaskContent> GetCurrentTaskAsync();

        Task HandleTaskAsync(TaskContent task);
        Task AddTaskAsync(TaskContent task);
        Task ModifyTaskAsync(TaskContent task);
        Task DeleteTaskAsync(TaskContent task);

        bool JudgeTaskPriority(TaskContent taskcurrent, TaskContent taskcompare);
    }
}
