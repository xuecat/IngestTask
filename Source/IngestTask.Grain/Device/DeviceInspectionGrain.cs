

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
    using Orleans.Streams;
    using IngestTask.Tools;
    using System.Linq;

    //[ProtoContract]
    [Serializable]
    public class DeviceState
    {
        //[ProtoMember(1)]
        public int ActionType { get; set; }
        //[ProtoMember(2)]
        public List<ChannelInfo> ChannelInfos { get; set; }
        
    }

    
    [Reentrant]
    public class DeviceInspectionGrain : Grain<DeviceState>, IDeviceInspections
    {
        private readonly ILogger Logger = LoggerManager.GetLogger("DeviceInfo");
        private readonly MsvClientCtrlSDK _msvClient;
        private readonly RestClient _restClient;

        private readonly IMapper Mapper;
        private IAsyncStream<ChannelInfo> _stream;

        private readonly List<int> _onlineMembers;
        private IDisposable _timer;

        public DeviceInspectionGrain(MsvClientCtrlSDK msv, RestClient client, IMapper mapper)
        {
            _timer = null;
            Mapper = mapper;
            _restClient = client;
            _msvClient = msv;
            _onlineMembers = new List<int>();
        }

        public override async Task OnActivateAsync()
        {
            await InitLoadAsync();
            _timer = RegisterTimer(this.OnCheckAllChannelsAsync, State.ActionType, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            Logger.Info(" DeviceInspectionGrain active");

            var streamProvider = GetStreamProvider(Abstraction.Constants.StreamProviderName.Default);
            _stream = streamProvider.GetStream<ChannelInfo>(Guid.NewGuid(), Abstraction.Constants.StreamName.DeviceReminder);

            await base.OnActivateAsync();
        }
        public override Task OnDeactivateAsync()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
            
            return base.OnDeactivateAsync();
        }

        public async Task<bool> InitLoadAsync()
        {
            try
            {
                var lstdevice = await _restClient.GetAllDeviceInfoAsync();
                var channelstate = await _restClient.GetAllChannelStateAsync();

                State.ChannelInfos = Mapper.Map<List<ChannelInfo>>(lstdevice);

                State.ChannelInfos.ForEach(x => {
                    var info = channelstate.Find(y => y.ChannelId == x.ChannelId);
                    if (info != null)
                    {
                        x.CurrentDevState = info.DevState;
                        x.KamatakiInfo = info.KamatakiInfo;
                        x.LastMsvMode = info.MsvMode;
                        x.UploadMode = info.UploadMode;
                        x.VtrId = info.VtrId;
                    }
                });
            }
            catch (Exception e)
            {
                Logger.Error($"DeviceInspectionGrain InitLoadAsync {e.Message}");
                return false;
            }
            
            return true;
        }
        
        private async Task OnCheckAllChannelsAsync(object type)
        {

            switch (State.ActionType)
            {
                case 0:
                    {
                        State.ActionType = 1;
                    } break;
                case 1:
                    {
                        if (State.ChannelInfos != null && State.ChannelInfos.Count >0)
                        {
                            foreach (var item in State.ChannelInfos)
                            {
                                var state = await _msvClient.QueryDeviceStateAsync(item.ChannelIndex, item.Ip, false, Logger);

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
                            }
                        }
                        
                    }
                    break;
                default:
                    break;
            }

        }
        public Task<Guid> JoinAsync(int nickname)
        {
            _onlineMembers.Add(nickname);

           
            return Task.FromResult(_stream.Guid);
        }

        public Task<Guid> LeaveAsync(int nickname)
        {
            _onlineMembers.Remove(nickname);

            return Task.FromResult(_stream.Guid);
        }

        public async Task<int> OnDeviceChangeAsync(ChannelInfo info)
        {
            /*
             * flag 重新更新内存数据并通知外面
             */
            await _stream.OnNextAsync( info);

            return 0;
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
        public async Task<int> QueryRunningTaskInChannelAsync(string ip, int index)
        {
            //通道状态校验，
            var backinfo = await  _msvClient.QueryTaskInfoAsync(index, ip, Logger);
            if (backinfo != null)
            {
                return (int)backinfo.ulID;
            }

            return 0;
        }

        public Task<int> CheckChannelSatetAsync()
        {
            //throw new NotImplementedException();
            return Task.FromResult(0);
        }

       
    }
}
