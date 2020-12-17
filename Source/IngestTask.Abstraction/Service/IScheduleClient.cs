using IngestTask.Dto;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IngestTask.Abstraction.Service
{
    public interface IScheduleClient
    {
        Task<int> AddScheduleTaskAsync(DispatchTask task);
        Task<int> RemoveScheduleTaskAsync(DispatchTask task);
    }
}
