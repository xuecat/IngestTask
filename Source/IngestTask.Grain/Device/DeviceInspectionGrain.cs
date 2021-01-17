

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
    using OrleansDashboard.Abstraction;
    using System.Reflection;

    [Reentrant]
    [TraceGrain("IngestTask.Grain.DeviceInspectionGrain", TaskTraceEnum.Device)]
    public class DeviceInspectionGrain : Grain<List<ChannelInfo>>, IDeviceInspections
    {
        private readonly ILogger Logger = LoggerManager.GetLogger("DeviceInfo");
        
        private readonly RestClient _restClient;

        private readonly IMapper Mapper;
        private IAsyncStream<ChannelInfo> _stream;

        private readonly List<int> _onlineMembers;
        
        private List<string> _monitorMember;
        public DeviceInspectionGrain(RestClient client, IMapper mapper)
        {
            Mapper = mapper;
            _restClient = client;
            
            _onlineMembers = new List<int>();
            _monitorMember = new List<string>();
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

                State = Mapper.Map<List<ChannelInfo>>(lstdevice);

                State.ForEach(x => {
                    var info = channelstate.Find(y => y.ChannelId == x.ChannelId);
                    if (info != null)
                    {
                        x.CurrentDevState = info.DevState;
                        x.LastMsvMode = info.MsvMode;
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
        public Task<int> QueryRunningTaskInChannelAsync(string ip, int index)
        {
            //通道状态校验，
           

            return Task.FromResult(0);
        }

        public async Task<int> SubmitChannelInfoAsync(ChannelInfo info, bool notify)
        {
            var item = State.Find(x => x.Id == info.Id);
            if (item == null)
            {
                State.Add(item);
            }
            else
            {
                ObjectTool.CopyObjectData(info, item, string.Empty, BindingFlags.Public | BindingFlags.Instance);
            }

            if (notify)//通知执行器
            {
                await _stream.OnNextAsync(info);
            }


            return _monitorMember.Count;
        }

        public Task<int> QuitServiceAsync(string serviceid)
        {
            _monitorMember.RemoveAll(x => x == serviceid);
            return Task.FromResult(_monitorMember.Count);
        }

        public Task<List<ChannelInfo>> RequestChannelInfoAsync(string serverid)
        {
            var item = _monitorMember.Find(x => x == serverid);
            if (item == null)
            {
                _monitorMember.Add(serverid);
            }

            var lst = State.FindAll(x => x.Id%_monitorMember.Count == 0);
            return Task.FromResult(lst);
        }

        public Task<int> CheckChannelSatetAsync()
        {
            //throw new NotImplementedException();
            return Task.FromResult(0);
        }

       
    }
}
