using IngestTask.Abstraction.Service;
using IngestTask.Dto;
using Orleans.Runtime.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IngestTask.Grain.Service
{
    public class ScheduleTaskClient :GrainServiceClient<IScheduleService> ,IScheduleClient
    {
        public ScheduleTaskClient(IServiceProvider serviceProvider) :base(serviceProvider)
        {

        }
        public Task<int> AddScheduleTaskAsync(DispatchTask task) => GrainService.AddScheduleTaskAsync(task);
        public Task<int> RemoveScheduleTaskAsync(DispatchTask task) => GrainService.RemoveScheduleTaskAsync(task);

    }
}
