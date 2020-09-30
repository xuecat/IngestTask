

namespace IngestTask.Grain
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
    using AutoMapper;

    [StatelessWorker(1)]
    class DeviceInspectionGrain : Grain, IDeviceInspections
    {
        private readonly ILogger Logger = LoggerManager.GetLogger("DeviceInfo");
        private readonly GrainObserverManager<IDeviceChange> _observerManager;
        private readonly MsvClientCtrlSDK _msvClient;
        private readonly RestClient _restClient;
        private List<ChannelInfo> _channelInfoList;//不存在并发问题，没有锁
        private int _actionType;

        private readonly IMapper Mapper;

        DeviceInspectionGrain(MsvClientCtrlSDK msv, RestClient client, IMapper mapper)
        {
            Mapper = mapper;
            _restClient = client;
            _msvClient = msv;
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

        public async Task<bool> InitLoadAsync()
        {
            try
            {
                var lstdevice = await _restClient.GetAllDeviceInfoAsync();
                var channelstate = await _restClient.GetAllChannelStateAsync();

                _channelInfoList = Mapper.Map<List<ChannelInfo>>(lstdevice);
                _channelInfoList = Mapper.Map<List<MsvChannelState>, List<ChannelInfo>>(channelstate, _channelInfoList);
            }
            catch (Exception e)
            {
                Logger.Error($"DeviceInspectionGrain InitLoadAsync {e.Message}");
                return false;
            }
            
            return true;
        }
        
        private Task OnCheckAllChannelsAsync(object type)
        {

            switch (type)
            {
                case 0:
                    {
                        _actionType = 1;
                    } break;
                case 1:
                    {
                        foreach (var item in _channelInfoList)
                        {
                            _ = Task.Run(async () =>
                            {
                                var state = _msvClient.QueryDeviceState(item.ChannelIndex, item.Ip, Logger);

                                MSV_Mode msvmode = MSV_Mode.NETWORK;
                                if (state == Device_State.DISCONNECTTED)
                                {
                                    msvmode = MSV_Mode.ERROR;
                                    Logger.Warn($"QueryDeviceState {state}");
                                }

                                bool changedstate = false;
                                if (item.CurrentDevState != state || msvmode != item.LastMsvMode)
                                {
                                    changedstate = true;
                                    item.LastDevState = item.CurrentDevState;
                                    item.LastMsvMode = msvmode;
                                    item.CurrentDevState = state;

                                    if (!await _restClient.UpdateMSVChannelStateAsync(item.ChannelId, item.LastMsvMode, item.CurrentDevState).ConfigureAwait(true))
                                    {
                                        Logger.Error("OnCheckAllChannelsAsync UpdateMSVChannelStateAsync error");
                                    }
                                }

                                if (item.LastDevState == Device_State.DISCONNECTTED
                                     && item.CurrentDevState == Device_State.CONNECTED
                                     && item.NeedStopFlag)
                                {
                                    item.NeedStopFlag = false;
                                }

                                if (item.LastDevState == Device_State.DISCONNECTTED
                                     && item.CurrentDevState == Device_State.WORKING
                                     && changedstate
                                     && item.NeedStopFlag)
                                {
                                    var taskinfo = _msvClient.QueryTaskInfo(item.ChannelIndex, item.Ip, Logger);
                                    if (taskinfo != null && taskinfo.ulID > 0)
                                    {
                                        var cptaskinfo = await _restClient.GetChannelCapturingTaskInfoAsync(item.ChannelId);

                                        bool needstop = true;
                                        if (cptaskinfo != null && cptaskinfo.TaskId == taskinfo.ulID)
                                        {
                                            Logger.Info("OnCheckAllChannelsAsync not needstop");
                                            needstop = false;
                                        }

                                        if (needstop)
                                        {
                                            /*
                                             * flag 通知出去走正常流程stop，并任务complete状态
                                             */
                                        }

                                        item.NeedStopFlag = false;
                                    }
                                }


                            });
                        }
                    }
                    break;
                default:
                    break;
            }


            return Task.CompletedTask;
        }

        public Task<int> OnDeviceChangeAsync(int type, string message)
        {
            /*
             * flag 重新更新内存数据并通知外面
             */

            _observerManager.Notify(s => s.ReceiveDeveiceChange(type, message));
            return Task.FromResult(0);
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
