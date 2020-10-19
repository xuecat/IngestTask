using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IngestTask.Abstraction.Service
{
    public interface IScheduleClient
    {
        Task AddScheduleTaskAsync();
    }
}
