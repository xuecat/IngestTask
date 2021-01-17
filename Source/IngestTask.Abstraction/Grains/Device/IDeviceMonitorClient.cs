using IngestTask.Abstraction.Grains;
using IngestTask.Dto;
using Orleans.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IngestTask.Abstraction.Grains
{
    public interface IDeviceMonitorClient : IGrainServiceClient<IDeviceMonitorService>, IDeviceMonitorService
    {
        //Task RefreshAsync(string parsableaddress);
    }
}
