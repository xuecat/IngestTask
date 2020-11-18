using IngestTask.Dto;
using Orleans;
using System;
using System.Text;
using System.Threading.Tasks;

namespace IngestTask.Abstraction.Grains
{
    public interface IDispatcherGrain : IGrain
    {
        Task SendAsync(Tuple<int, string>[] messages);
        Task AddTaskAsync(DispatchTask task);
    }
}
