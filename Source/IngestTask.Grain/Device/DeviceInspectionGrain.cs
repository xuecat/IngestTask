

namespace IngestTask.Grain.Device
{
    using Orleans.Concurrency;
    using IngestTask.Abstraction.Grains;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using Orleans;
    using IngestTask.Tools.Dto;

    [StatelessWorker(1)]
    class DeviceInspectionGrain : Grain, IDeviceInspections
    {

        private readonly List<MsvChannelState> ChannelInfoList;

        DeviceInspectionGrain()
        {
            ChannelInfoList = new List<MsvChannelState>();
        }

        public override Task OnActivateAsync()
        {
            RegisterTimer(this.OnCheckAllChannelsAsync, ChannelInfoList, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            return base.OnActivateAsync();
        }

        private Task OnCheckAllChannelsAsync(object Channels)
        {
            throw new NotImplementedException();
        }
       
        public override Task OnDeactivateAsync()
        {
            return base.OnDeactivateAsync();
        }

        public Task<int> CheckSiloChannelAsync() 
        {
            throw new NotImplementedException();
        }                                                                                                                                                                                                                                                                                                                                                                                                                                          
    }
}
