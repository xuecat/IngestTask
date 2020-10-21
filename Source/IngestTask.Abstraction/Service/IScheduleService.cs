﻿

namespace IngestTask.Abstraction.Service
{
    using IngestTask.Dto;
    using Orleans.Services;
    using System.Threading.Tasks;
    public interface IScheduleService : IGrainService
    {
        Task<int> ScheduleTaskAsync();
        Task<int> AddTaskAsync(TaskContent task);
    }
}
