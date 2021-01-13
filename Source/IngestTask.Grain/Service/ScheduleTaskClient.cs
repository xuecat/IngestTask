using IngestTask.Abstraction.Service;
using IngestTask.Dto;
using Orleans.Runtime;
using Orleans.Runtime.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IngestTask.Grain.Service
{
    public class ScheduleTaskClient :GrainServiceClient<IScheduleService> ,IScheduleClient
    {
        private IGrainServiceFactory _grainfactory;
        public ScheduleTaskClient(IServiceProvider serviceProvider, IGrainServiceFactory factory) :base(serviceProvider)
        {
            _grainfactory = factory;
        }
        public Task<string> AddScheduleTaskAsync(DispatchTask task) => GrainService.AddScheduleTaskAsync(task);
        public Task<int> RemoveScheduleTaskAsync(DispatchTask task) => GrainService.RemoveScheduleTaskAsync(task);
#pragma warning disable VSTHRD200 // 对异步方法使用“Async”后缀
        public async Task RefreshAsync(string parsableaddress)
#pragma warning restore VSTHRD200 // 对异步方法使用“Async”后缀
        {
            SiloAddress silo = SiloAddress.FromParsableString(parsableaddress);
            if (silo != null && _grainfactory != null)
            {
                var id = parsableaddress.Split(";");
                if (id.Length >0)
                {
                    SiloAddress siloadd = SiloAddress.FromParsableString(id[0]);
                    var refgrain = _grainfactory.MakeGrainServiceReference(int.Parse(id[1]), id[2], siloadd);
                    var service = _grainfactory.CastToGrainServiceReference<IScheduleService>(refgrain);
                    if (service != null)
                    {
                        await service.RefreshAsync();
                    }
                }
                
            }
        }

    }
}
