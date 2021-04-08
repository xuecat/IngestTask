

namespace IngestTask.Grain.Service
{
    using IngestTask.Abstraction.Grains;
    using IngestTask.Dto;
    using IngestTask.Tool;
    using IngestTask.Tool.Msv;
    using Microsoft.Extensions.Configuration;
    using NLog;
    using Orleans;
    using Orleans.Concurrency;
    using Orleans.Core;
    using Orleans.Runtime;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading.Tasks;

    [Reentrant]
    public class DeviceMonitorService : GrainService, IDeviceMonitorService
    {
        private List<ChannelInfo> _lstTimerScheduleDevice;
        
        private readonly Sobey.Core.Log.ILogger Logger;

        private IDisposable _timer;
        private IDisposable _synctimer;

        private readonly MsvClientCtrlSDK _msvClient;
        private readonly RestClient _restClient;

        private readonly IGrainFactory _grainFactory;
        private string _grainKey;

        //private readonly IMembershipTable membershipTableProvider;
        private readonly ILocalSiloDetails localsilo;

        public DeviceMonitorService(/*IMembershipTable member*/ ILocalSiloDetails localsilo, IConfiguration configuration, IGrainIdentity id, Silo silo,
            Microsoft.Extensions.Logging.ILoggerFactory loggerFactory,
            IGrainFactory grainFactory, MsvClientCtrlSDK msv, RestClient client)
            : base(id, silo, loggerFactory)
        {
            Logger = Sobey.Core.Log.LoggerManager.GetLogger("MonitorService");
            _lstTimerScheduleDevice = new List<ChannelInfo>();
            _timer = null;
            _synctimer = null;
            _msvClient = msv;
            _restClient = client;
            _grainFactory = grainFactory;
            _grainKey = string.Empty;
            //this.membershipTableProvider = member;
            this.localsilo = localsilo;
            //SiloName = configuration.GetSection("SiloName").Get<string>();
        }

        public override async Task Init(IServiceProvider serviceProvider)
        {
            
            await base.Init(serviceProvider);

        }

        public List<ChannelInfo> GetDataBack()
        {
            return _lstTimerScheduleDevice;
        }
        protected override Task StartInBackground()
        {
            _timer = RegisterTimer(this.OnCheckAllDeviceAsync, _lstTimerScheduleDevice, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3));

            _synctimer = RegisterTimer(this.OnSyncTaskAsync, null, TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(3));
            return Task.CompletedTask;
        }

        public override async Task Start()
        {
           
            string extrakey = string.Empty;

            var refgrain = GetGrainReference();
            refgrain.GetPrimaryKey(out extrakey);

            _grainKey = refgrain.GrainServiceSiloAddress.ToParsableString() + ";" + refgrain.GrainIdentity.TypeCode.ToString() + ";" + extrakey;

            _lstTimerScheduleDevice = await _grainFactory.GetGrain<IDeviceInspections>(0).RequestChannelInfoAsync(_grainKey);
            //await _grainFactory.GetGrain<ICheckSchedule>(0).StartCheckSyncAsync();
            Logger.Info("decice monitor start");
            await base.Start();
        }

        public override async Task Stop()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
            await _grainFactory.GetGrain<IDeviceInspections>(0).QuitServiceAsync(_grainKey);
            Logger.Info("deice monitor stop");
            await base.Stop();
        }

        /*
         * 主动更新暂时没想到场景
         */
        public Task RefreshMonnitorDeviceAsync(List<DeviceInfo> info)
        {
            throw new NotImplementedException();
        }

        private async Task OnSyncTaskAsync(object type)
        {
            var lst = await _restClient.GetNeedSyncScheduleTaskListAsync();
            if (lst != null && lst.Count >0)
            {
                Logger.Info("device syncscheduletask: " + string.Join(",", lst.Select(x => x.Taskid)));
                await _grainFactory.GetGrain<IReminderTask>(0).SyncScheduleTaskAsync(lst);
            }

            if (_synctimer != null)
            {
                _synctimer.Dispose();
                _synctimer = null;
            }
        }

        private async Task OnCheckAllDeviceAsync(object type)
        {
            bool request = false;
            if (_lstTimerScheduleDevice.Count > 0)
            {
                foreach (var item in _lstTimerScheduleDevice)
                {
                    var state = await _msvClient.QueryDeviceStateAsync(item.ChannelIndex, item.Ip, false, capstate:  CAPTURE_STATE.CS_STANDBY, Logger);

                    MSV_Mode msvmode = MSV_Mode.NETWORK;
                    if (state == Device_State.DISCONNECTTED)
                    {
                        msvmode = MSV_Mode.ERROR;
                        Logger.Warn($"QueryDeviceState : {item.Ip}：{item.ChannelIndex}, {state}");
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

                    Logger.Info($"OnCheckAllDeviceAsync one : {item.Ip}：{item.ChannelIndex}, {item.LastDevState}, {item.CurrentDevState}, {changedstate}");
                    if (item.LastDevState == Device_State.DISCONNECTTED
                            && item.CurrentDevState == Device_State.WORKING
                            && changedstate)
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

                            Logger.Info($"OnCheckAllDeviceAsync two : {item.Ip}：{item.ChannelIndex}, {taskinfo.ulID}, {cptaskinfo?.TaskId}, {needstop}");
                            if (needstop)
                            {
                                /*
                                    * 应该不用通知出去，任务那边监听可以管理
                                    */
                                item.NeedStopFlag = true;

                                await _msvClient.StopAsync(item.ChannelIndex, item.Ip, cptaskinfo.TaskId, Logger);
                            }
                            else
                                item.NeedStopFlag = false;
                        }
                    }
                }

                request = await _grainFactory.GetGrain<IDeviceInspections>(0).SubmitChannelInfoAsync(_grainKey, _lstTimerScheduleDevice, false);
                
            }

            if (request)
            {
                _lstTimerScheduleDevice = await _grainFactory.GetGrain<IDeviceInspections>(0).RequestChannelInfoAsync(_grainKey);
            }
        }
    }
}
