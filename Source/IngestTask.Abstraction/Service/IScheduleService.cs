

namespace IngestTask.Abstraction.Service
{
    using Orleans.Services;
    using System.Threading.Tasks;
    public interface IScheduleService : IGrainService
    {
        Task<int> ScheduleTaskAsync();
        Task<int> AddTaskAsync();
    }
}
