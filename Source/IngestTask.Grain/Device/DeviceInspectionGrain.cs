

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
        private readonly GrainObserverManager<IDeviceChange> _observerManager;
        private readonly MsvClientCtrlSDK _msvClient;
        private readonly RestClient _restClient;
        private List<DeviceInfo> _deviceInfoList;//不存在并发问题，没有锁
        private int _actionType;

        DeviceInspectionGrain(MsvClientCtrlSDK msv, RestClient client)
        {
            _restClient = client;
            _msvClient = msv;
            _deviceInfoList = new List<DeviceInfo>();
            _observerManager = new GrainObserverManager<IDeviceChange>();
        }

        public override Task OnActivateAsync()
        {
            RegisterTimer(this.OnCheckAllChannelsAsync, _actionType, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
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
             * flag 重新更新内存数据并通知外面
             */

            _observerManager.Notify(s => s.ReceiveDeveiceChange(type, message));
            return Task.FromResult(0);
        }
        private async Task OnCheckAllChannelsAsync(object type)
        {

            switch (type)
            {
                case 0:
                    {
                        _actionType = 1;
                    } break;
                case 1:
                    {
                        foreach (var item in _deviceInfoList)
                        {
                            await Task.Run(async () => {
                                var state = _msvClient.QueryDeviceState(item.ChannelIndex, item.Ip, Logger);

                                MSV_Mode msvmode = MSV_Mode.NETWORK;
                                if (state == Device_State.DISCONNECTTED)
                                {
                                    msvmode = MSV_Mode.ERROR;
                                    Logger.Warn($"QueryDeviceState {state}");
                                }

                                if (item.CurrentDevState != state || msvmode != item.LastMsvMode)
                                {
                                    item.LastDevState = item.CurrentDevState;
                                    item.LastMsvMode = msvmode;
                                    item.CurrentDevState = state;

                                    if (!await _restClient.UpdateMSVChannelStateAsync(item.ChannelId, item.LastMsvMode, item.CurrentDevState))
                                    {
                                        Logger.Error("OnCheckAllChannelsAsync UpdateMSVChannelStateAsync error");
                                    }
                                }

                                if (item.LastDevState == Device_State.DISCONNECTTED
                                     && item.CurrentDevState == Device_State.DISCONNECTTED
                                     && item.NeedStopFlag)
                                {
                                    /*
                                     * flag 需要用流通知到各个通道，通道异常
                                     */
                                }




                            }).ConfigureAwait(true);
                        }
                    }
                    break;
                default:
                    break;
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
