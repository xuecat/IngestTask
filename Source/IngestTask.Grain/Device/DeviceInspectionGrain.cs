

namespace IngestTask.Grain
{
    using Orleans.Concurrency;
    using IngestTask.Abstraction.Grains;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using Orleans;
    using IngestTask.Dto;
    using Sobey.Core.Log;
    using IngestTask.Tools.Msv;
    using IngestTask.Tool;
    using AutoMapper;
    using ProtoBuf;

    //[ProtoContract]
    [Serializable]
    class DeviceState
    {
        //[ProtoMember(1)]
        public int ActionType { get; set; }
        //[ProtoMember(2)]
        public List<ChannelInfo> ChannelInfos { get; set; }
        
    }

    
    [Reentrant]
    [StatelessWorker(1)]
    class DeviceInspectionGrain : Grain<DeviceState>, IDeviceInspections
    {
        private readonly ILogger Logger = LoggerManager.GetLogger("DeviceInfo");
        private readonly GrainObserverManager<IDeviceChange> _observerManager = new GrainObserverManager<IDeviceChange>();
        private readonly MsvClientCtrlSDK _msvClient;
        private readonly RestClient _restClient;

        private readonly IMapper Mapper;

        DeviceInspectionGrain(MsvClientCtrlSDK msv, RestClient client, IMapper mapper)
        {
            Mapper = mapper;
            _restClient = client;
            _msvClient = msv;
        }

        public override Task OnActivateAsync()
        {
            RegisterTimer(this.OnCheckAllChannelsAsync, State.ActionType, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
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

                State.ChannelInfos = Mapper.Map<List<ChannelInfo>>(lstdevice);
                State.ChannelInfos = Mapper.Map<List<MsvChannelState>, List<ChannelInfo>>(channelstate, State.ChannelInfos);
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
                        State.ActionType = 1;
                    } break;
                case 1:
                    {
                        foreach (var item in State.ChannelInfos)
                        {
                            _ = Task.Run(async () =>
                            {
                                var state = await _msvClient.QueryDeviceStateAsync(item.ChannelIndex, item.Ip, Logger);

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
                                    var taskinfo = await _msvClient.QueryTaskInfoAsync(item.ChannelIndex, item.Ip, Logger);
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

        public Task<bool> IsChannelInvalidAsync(int channelid)
        {
            foreach (var item in State.ChannelInfos)
            {
                if (item.ChannelId == channelid &&item.CurrentDevState == Device_State.CONNECTED)
                {
                    return Task.FromResult(false);
                }
            }
            return Task.FromResult(true);
        }

        public Task<ChannelInfo> GetChannelInfoAsync(int channelid)
        {
            foreach (var item in State.ChannelInfos)
            {
                if (item.ChannelId == channelid)
                {
                    return Task.FromResult(item);
                }
            }
            return null;
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
