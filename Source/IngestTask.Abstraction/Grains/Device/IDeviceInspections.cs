

namespace IngestTask.Abstraction.Grains
{
    using IngestTask.Dto;
    using Orleans;
    using Orleans.Concurrency;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    public interface IDeviceInspections : IGrainWithIntegerKey
    {
        Task<int> CheckChannelSatetAsync();
        Task<List<ChannelInfo>> GetChannelInfosAsync();
        Task<bool> IsChannelInvalidAsync(int channelid);
        Task<ChannelInfo> GetChannelInfoAsync(int channelid);
        Task<int> QueryRunningTaskInChannelAsync(string ip, int index);
        Task<Guid> JoinAsync(int nickname);
        Task<Guid> LeaveAsync(int nickname);

        Task<int> SubmitChannelInfoAsync(List<ChannelInfo> infos, bool notify);
        Task<List<ChannelInfo>> RequestChannelInfoAsync(string serverid);

        Task<int> QuitServiceAsync(string serviceid);
    }
}
