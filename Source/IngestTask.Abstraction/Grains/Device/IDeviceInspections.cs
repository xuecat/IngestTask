

namespace IngestTask.Abstraction.Grains
{
    using IngestTask.Dto;
    using Orleans;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    public interface IDeviceInspections : IGrainWithIntegerKey
    {
        Task<int> CheckChannelSatetAsync();
        Task<bool> IsChannelInvalidAsync(int channelid);
        Task<ChannelInfo> GetChannelInfoAsync(int channelid);
    }
}
