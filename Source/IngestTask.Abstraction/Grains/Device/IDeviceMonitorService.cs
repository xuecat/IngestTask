

namespace IngestTask.Abstraction.Grains
{
    using IngestTask.Dto;
    using Orleans;
    using Orleans.Services;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    public interface IDeviceMonitorService : IGrainService
    {
        Task RefreshMonnitorDeviceAsync(List<DeviceInfo> info);
    }
}
