using IngestTask.Abstraction.Grains;
using IngestTask.Dto;
using Orleans.Runtime;
using Orleans.Runtime.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IngestTask.Grain.Service
{
    public class DeviceMonitorClient :GrainServiceClient<IDeviceMonitorService>, IDeviceMonitorClient
    {
        public DeviceMonitorClient(IServiceProvider serviceProvider) :base(serviceProvider)
        {
        }
        public Task RefreshMonnitorDeviceAsync(List<DeviceInfo> info) => GrainService.RefreshMonnitorDeviceAsync(info);
//#pragma warning disable VSTHRD200 // 对异步方法使用“Async”后缀
//        public async Task RefreshAsync(string parsableaddress)
//#pragma warning restore VSTHRD200 // 对异步方法使用“Async”后缀
//        {
//            SiloAddress silo = SiloAddress.FromParsableString(parsableaddress);
//            if (silo != null && _grainfactory != null)
//            {
//                var id = parsableaddress.Split(";");
//                if (id.Length >0)
//                {
//                    SiloAddress siloadd = SiloAddress.FromParsableString(id[0]);
//                    var refgrain = _grainfactory.MakeGrainServiceReference(int.Parse(id[1]), id[2], siloadd);
//                    var service = _grainfactory.CastToGrainServiceReference<IScheduleGrain>(refgrain);
//                    if (service != null)
//                    {
//                        await service.RefreshAsync();
//                    }
//                }
                
//            }
//        }

    }
}
