

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
    using IngestTask.Tool.Msv;
    using IngestTask.Tool;
    using AutoMapper;
    using ProtoBuf;
    using Orleans.Streams;
    using System.Linq;
    using OrleansDashboard.Abstraction;
    using System.Reflection;
    using AutoMapper.Internal;

    [Reentrant]
    [TraceGrain("IngestTask.Grain.DeviceInspectionGrain", TaskTraceEnum.Device)]
    public class DeviceInspectionGrain : Grain<List<ChannelInfo>>, IDeviceInspections
    {
        private readonly ILogger Logger = LoggerManager.GetLogger("DeviceInfo");
        
        private readonly RestClient _restClient;
        private readonly MsvClientCtrlSDK _msvClient;

        private readonly IMapper Mapper;
        private IAsyncStream<ChannelInfo> _stream;

        private readonly List<int> _onlineMembers;
        
        private Dictionary<string, int> _monitorMember;
        public DeviceInspectionGrain(RestClient client, MsvClientCtrlSDK msv, IMapper mapper)
        {
            Mapper = mapper;
            _restClient = client;
            _msvClient = msv;

            _onlineMembers = new List<int>();
            _monitorMember = new Dictionary<string, int>();
        }

        public override async Task OnActivateAsync()
        {
            await InitLoadAsync();
            
            Logger.Info(" DeviceInspectionGrain active");

            var streamProvider = GetStreamProvider(Abstraction.Constants.StreamProviderName.Default);
            _stream = streamProvider.GetStream<ChannelInfo>(Guid.NewGuid(), Abstraction.Constants.StreamName.DeviceReminder);

            await base.OnActivateAsync();
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

                lock (State)
                {
                    State = Mapper.Map<List<ChannelInfo>>(lstdevice);

                    State.ForEach(x =>
                    {
                        var info = channelstate.Find(y => y.ChannelId == x.ChannelId);
                        if (info != null)
                        {
                            x.CurrentDevState = info.DevState;
                            x.LastMsvMode = info.MsvMode;
                            x.VtrId = info.VtrId;
                        }
                    });
                }
                
            }
            catch (Exception e)
            {
                Logger.Error($"DeviceInspectionGrain InitLoadAsync {e.Message}");
                return false;
            }
            
            return true;
        }
        public Task NotifyDeviceDeleteAsync(int deviceid)
        {
            lock (State)
            {
                State.RemoveAll(x => x.Id == deviceid);
            }
            return Task.CompletedTask;
        }

        public Task NotifyChannelDeleteAsync(int channelid)
        {
            lock (State)
            {
                State.RemoveAll(x => x.ChannelId == channelid);
            }
            return Task.CompletedTask;
        }

        public async Task NotifyDeviceChangeAsync()
        {
            await InitLoadAsync();
        }

        public Task NotifyDeviceChangeAsync(ChannelInfo info)
        {
            var iteminfo = State.Find(x => x.Id == info.Id);
            if (iteminfo != null)
            {
                ObjectTool.CopyObjectData(info, iteminfo, string.Empty, BindingFlags.Public | BindingFlags.Instance);
            }
            else
                State.Add(info);

            return Task.CompletedTask;
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


        public Task<List<ChannelInfo>> GetChannelInfosAsync()
        {
            return Task.FromResult(this.State);
        }

        public Task<bool> IsChannelInvalidAsync(int channelid)
        {
            foreach (var item in State)
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
            foreach (var item in State)
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
            var taskinfo = await _msvClient.QueryTaskInfoAsync(index, ip, Logger);
            if (taskinfo != null)
            {
                return (int)taskinfo.ulID;
            }

            return 0;
        }

        public async Task<bool> SubmitChannelInfoAsync(string serverid, List<ChannelInfo> infos, bool notify)
        {
            foreach (var itm in infos)
            {
                var item = State.Find(x => x.Id == itm.Id);
                lock (State)
                {
                    if (item == null)
                    {
                        State.Add(item);
                    }
                    else
                    {
                        ObjectTool.CopyObjectData(itm, item, string.Empty, BindingFlags.Public | BindingFlags.Instance);
                    }
                }
                

                if (notify && itm.NeedStopFlag)//通知执行器
                {
                    await _stream.OnNextAsync(infos.First());
                }
            }

            int count = 0;
            if (!_monitorMember.TryGetValue(serverid, out count))
            {
                return true;
            }
            else
            {
                if (count != _monitorMember.Count)
                {
                    return true;
                }
            }
            return false;
        }

        public Task<int> QuitServiceAsync(string serviceid)
        {
            _monitorMember.Remove(serviceid);

            //其它的service强制更新，这样他们会重新申请
            if (_monitorMember.Count > 0)
            {
                foreach (var item in _monitorMember)
                {
                    _monitorMember[item.Key] = 0;
                }
            }
            return Task.FromResult(_monitorMember.Count);
        }

        public Task<List<ChannelInfo>> RequestChannelInfoAsync(string serverid)
        {
            int count = 0;
            if (!_monitorMember.TryGetValue(serverid, out count))
            {
                _monitorMember.Add(serverid, _monitorMember.Count);
            }
            else
            {
                _monitorMember[serverid] = _monitorMember.Count;
            }

            var lst = State.FindAll(x => x.Id%_monitorMember.Count == 0);
            return Task.FromResult(lst);
        }

       
    }
}
