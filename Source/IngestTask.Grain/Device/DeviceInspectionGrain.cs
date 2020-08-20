

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
    using Sobey.Core.Log;
    using IngestTask.Tools.Msv;
    using IngestTask.Tool;

    [StatelessWorker(1)]
    class DeviceInspectionGrain : Grain, IDeviceInspections
    {
        private readonly ILogger Logger = LoggerManager.GetLogger("DeviceInfo");
        private readonly List<MsvChannelState> _channelInfoList;
        private readonly GrainObserverManager<IDeviceChange> _observerManager;
        private readonly MsvClientCtrlSDK _msvClient;
        private readonly RestClient _restClient;

        DeviceInspectionGrain(MsvClientCtrlSDK msv, RestClient client)
        {
            _restClient = client;
            _msvClient = msv;
            _channelInfoList = new List<MsvChannelState>();
            _observerManager = new GrainObserverManager<IDeviceChange>();
        }

        public override Task OnActivateAsync()
        {
            RegisterTimer(this.OnCheckAllChannelsAsync, _channelInfoList, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            Logger.Info(" DeviceInspectionGrain active");
            return base.OnActivateAsync();
        }
        public override Task OnDeactivateAsync()
        {
            return base.OnDeactivateAsync();
        }
        public Task<int> OnDeviceChangeAsync(int type, string message)
        {
            /*
             * 重新更新内存数据并通知外面
             */

            _observerManager.Notify(s => s.ReceiveDeveiceChange(type, message));
            return Task.FromResult(0);
        }
        private Task OnCheckAllChannelsAsync(object Channels)
        {
            if (Channels != null)
            {
                foreach (var item in _channelInfoList)
                {

                }
            }
        }
       
        public Task<int> CheckChannelSatetAsync() 
        {
            throw new NotImplementedException();
        }

        public Task SubscribeAsync(IDeviceChange observer)
        {
            _observerManager.Subscribe(observer);
            return Task.CompletedTask;
        }

        public Task UnSubscribeAsync(IDeviceChange observer)
        {
            _observerManager.Unsubscribe(observer);
            return Task.CompletedTask;
        }
    }
}
