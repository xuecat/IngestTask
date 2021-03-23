using IngestTask.Dto;
using Orleans;
using Orleans.Concurrency;
using System;
using System.Text;
using System.Threading.Tasks;

namespace IngestTask.Abstraction.Grains
{
    public interface IDispatcherGrain : IGrainWithIntegerKey
    {
        Task SendAsync(Tuple<int, string>[] messages);
        [OneWay]
        Task AddTaskAsync(DispatchTask task);
        [OneWay]
        Task UpdateTaskAsync(DispatchTask task);
        [OneWay]
        Task DeleteTaskAsync(DispatchTask task);

        
    }
}
