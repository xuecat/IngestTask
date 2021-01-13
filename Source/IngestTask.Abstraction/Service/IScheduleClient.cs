using IngestTask.Dto;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IngestTask.Abstraction.Service
{
    public interface IScheduleClient
    {
        Task<string> AddScheduleTaskAsync(DispatchTask task);
        Task<int> RemoveScheduleTaskAsync(DispatchTask task);
        Task RefreshAsync(string parsableaddress);
    }
}
