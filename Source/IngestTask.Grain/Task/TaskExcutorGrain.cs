
using ProtoBuf;

namespace IngestTask.Grain
{
    using AutoMapper;
    using IngestTask.Abstraction.Grains;
    using IngestTask.Dto;
    using IngestTask.Tool;
    using IngestTask.Tools;
    using Orleans;
    using Orleans.Concurrency;
    using Orleans.EventSourcing;
    using Orleans.LogConsistency;
    using Orleans.Runtime;
    using Orleans.Storage;
    using Orleans.Streams;
    using OrleansDashboard.Abstraction;
    using ProtoBuf;
    using Sobey.Core.Log;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    //using Orleans.Streams;
    //[ProtoContract]
    [Serializable]
    public class TaskEvent
    {
        public TaskContent TaskContentInfo { get; set; }
        public opType OpType { get; set; }

    }

    [Serializable]
    public class DeviceEvent
    {
        public DeviceType DeviceType { get; set; }

    }

    //这些序列化代表基础结构体都要protoc序列化，太麻烦了，我打算只心跳那里做protoc序列化
    //[ProtoContract]
    [Serializable]
    public class TaskState
    {
        
        //[ProtoMember(1)]
        public long ChannelId { get; set; }
        //public long ReminderTimer { get; set; }
        public List<TaskFullInfo> TaskLists { get; set; } = new List<TaskFullInfo>();

        //修改状态对象之外，TransitionState方法不应该有任何副作用，并且应该是确定性的
        public void Apply(TaskEvent @event)
        {
            //我认为无论任务重复不，都不需要筛选，无非一系列工作交给执行器做而已。后面引入cancletoken保证及时终止就可以了
            switch (@event.OpType)
            {
                case opType.otAdd:
                    {
                        var info = TaskLists.Find(x => x.TaskContent.TaskId == @event.TaskContentInfo.TaskId);
                        if (info == null && DateTimeFormat.DateTimeFromString(@event.TaskContentInfo.End) > DateTime.Now)//防止持久性从数据库加载过期任务执行
                        {
                            TaskLists.Add(new TaskFullInfo() { TaskContent = @event.TaskContentInfo, StartOrStop = true, HandleTask = false });
                        }
                        
                    }
                    break;
                case opType.otDel:
                    {
                        TaskLists.RemoveAll(a => a.TaskContent.TaskId == @event.TaskContentInfo.TaskId);
                    }
                    break;
                case opType.otMove:
                    break;
                case opType.otModify:
                    break;
                case opType.otStop:
                    {
                        //防止持久性从数据库加载过期任务执行
                        if (DateTimeFormat.DateTimeFromString(@event.TaskContentInfo.End) < DateTime.Now.AddSeconds(10))
                        {
                            TaskLists.Add(new TaskFullInfo() { TaskContent = @event.TaskContentInfo, StartOrStop = false, HandleTask = false });
                        }
                        
                    }
                    break;
                case opType.otReDispatch:
                    {
                        var taskitem = TaskLists.Find(a => a.TaskContent.TaskId == @event.TaskContentInfo.TaskId);
                        if (taskitem != null)
                        {
                            taskitem.RetryTimes++;
                            //重调度任务把开始时间滞后看看
                            taskitem.NewBeginTime = DateTime.Now.AddSeconds(2);
                            taskitem.HandleTask = false;
                            DateTime dt = DateTime.Now;
                            if (dt >= DateTimeFormat.DateTimeFromString(taskitem.TaskContent.End))
                            {
                                taskitem.NewEndTime = dt.AddSeconds(2);
                            }
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        //public void Apply(DeviceEvent @event)
        //{ }
    }


    //要不要存数据库呢
    //[LogConsistencyProvider(ProviderName = "CustomStorage")]
    //[StorageProvider(ProviderName="store1")]

    [MultiGrain("IngestTask.Grain.TaskExcutorGrain")]
    [TraceGrain("IngestTask.Grain.TaskExcutorGrain", TaskTraceEnum.TaskExec)]
    [ImplicitStreamSubscription(Abstraction.Constants.StreamName.DeviceReminder)]
    public class TaskExcutorGrain : JournaledGrain<TaskState,TaskEvent>, ITask
    {
        private readonly ILogger Logger = LoggerManager.GetLogger("TaskExcutor");
        
        private readonly RestClient _restClient;
        private readonly ITaskHandlerFactory _handlerFactory;
        private IServiceProvider _services;
        private StreamSubscriptionHandle<ChannelInfo> _streamHandle;
        private IDisposable _timer;
        private IMapper _mapper;
        public TaskExcutorGrain(IGrainActivationContext grainActivationContext,
            RestClient rest,
            ITaskHandlerFactory handlerfac,
            IServiceProvider services,
            IMapper mapper)
        {
            _services = services;
            _timer = null;
            _restClient = rest;
            _handlerFactory = handlerfac;
            _mapper = mapper;
        }

        public override async Task OnActivateAsync()
        {
            State.ChannelId = this.GetPrimaryKeyLong();

            var devicegrain = GrainFactory.GetGrain<IDeviceInspections>(0);
            var streamid = await devicegrain.JoinAsync((int)State.ChannelId);
            var streamProvider = GetStreamProvider(Abstraction.Constants.StreamProviderName.Default)
                                    .GetStream<ChannelInfo>(streamid, Abstraction.Constants.StreamName.DeviceReminder);
            _streamHandle = await streamProvider.SubscribeAsync(new StreamObserver(Logger, OnNextStream)).ConfigureAwait(true);

            if (State.TaskLists.Count > 0)
            {
                State.TaskLists.Clear();
            }
            Logger.Info($" TaskBase active {State.ChannelId}");
            await base.OnActivateAsync();
        }

        public override async Task OnDeactivateAsync()
        {
            if (_streamHandle != null)
            {
                await _streamHandle.UnsubscribeAsync();
            }
            var grainStorage = this.GetGrainStorage(ServiceProvider);
            if (grainStorage != null)
            {
                await grainStorage.ClearStateAsync(this.GetType().FullName, this.GrainReference, grainState: null);
            }
            
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;

            }
            await base.OnDeactivateAsync();
        }

        protected override void OnConnectionIssue(ConnectionIssue issue)
        {
            Logger.Error($"OnConnectionIssue {issue.ToString()}");
        }
        protected override void OnConnectionIssueResolved(ConnectionIssue issue)
        {
            /// handle the resolution of a previously reported issue             
            Logger.Error($"OnConnectionIssueResolved {issue.ToString()}");
        }

        

        public Task<List<DispatchTask>> GetCurrentTaskListAsync()
        {
            return Task.FromResult(_mapper.Map<List<DispatchTask>>(this.State.TaskLists));
        }

        protected override void OnStateChanged()
        {
            // read state and/or event log and take appropriate action
            //好像会有定时读取,所以一定要是调度分发过来的才进行处理

            if (State.TaskLists.Count > 0)
            {
                var orleansts = TaskScheduler.Current;
                foreach (var item in State.TaskLists)
                {
                    if (!item.HandleTask)
                    {
                        item.HandleTask = true;
                        _ = Task.Factory.StartNew(async () =>
                        {
                            return await HandleTaskAsync(item);
                        }, CancellationToken.None, TaskCreationOptions.None, scheduler: orleansts);
                    }
                    
                }
            }
        }

        public async Task<int> HandleTaskAsync(TaskFullInfo task)
        {
            Logger.Info($"TaskExcutor HandleTaskAsync {task.TaskContent.TaskId} {task.StartOrStop}");

            try
            {
                //如果判断到是周期任务，那么需要对它做分任务的处理
                //这个步骤挪到后台server去做
                if (task.TaskSource == TaskSource.emUnknowTask || task.ContentMeta == null)
                {
                    if (_restClient != null)
                    {
                        var fullinfo = await _restClient.GetTaskFullInfoAsync(task.TaskContent.TaskId);

                        if (fullinfo != null)
                        {
                            ObjectTool.CopyObjectData(fullinfo, task, "StartOrStop,RetryTimes,NewBeginTime,NewEndTime", BindingFlags.Public | BindingFlags.Instance);

                            Logger.Info($"TaskExcutor HandleTaskAsync get {JsonHelper.ToJson(task)}");
                        }
                        else
                        {
                            task.HandleTask = false;
                            return 0;
                        }
                    }
                }

                /*
                * flag katamaki任务检测
                */
                var devicegrain = GrainFactory.GetGrain<IDeviceInspections>(0);
                if (devicegrain != null)
                {
                    var chinfo = await devicegrain.GetChannelInfoAsync(task.TaskContent.ChannelId);
                    if (chinfo != null)
                    {
                        var handle = _handlerFactory.CreateInstance(task, _services);
                        if (handle != null)
                        {
                            var taskid = await handle.HandleTaskAsync(task, chinfo);
                            if (taskid > 0)
                            {
                                RaiseEvent(new TaskEvent() { OpType = opType.otDel, TaskContentInfo = task.TaskContent });
                                await ConfirmEvents();

                                if (_timer != null)
                                {
                                    _timer.Dispose();
                                    _timer = null;
                                }

                                if (task.StartOrStop)
                                {
                                    await GrainFactory.GetGrain<ITaskCache>(0).UpdateTaskAsync(await _restClient.GetTaskDBAsync(task.TaskContent.TaskId));
                                    _timer = RegisterTimer(this.OnRunningTaskMonitorAsync, new Tuple<int, int, string, int, int, string>(taskid, (int)task.TaskContent.TaskType, task.TaskContent.Begin, chinfo.ChannelId, chinfo.ChannelIndex, chinfo.Ip), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3));
                                }
                                else
                                {

                                    await GrainFactory.GetGrain<ITaskCache>(0).DeleteTaskAsync(taskid);
                                }
                            }
                            else
                            {
                                RaiseEvent(new TaskEvent() { OpType = opType.otReDispatch, TaskContentInfo = task.TaskContent });
                                await ConfirmEvents();
                            }
                        }
                        else
                            Logger.Error($"CreateInstance error {JsonHelper.ToJson(task)}");
                        
                    }
                    else
                    {
                        Logger.Error($"getgrain channelinfo error {task.TaskContent.ChannelId}");
                    }

                }
                else
                {
                    Logger.Error("not find device grain");
                }

                return 0;
            }
            catch (Exception e)
            {

                Logger.Error($"TaskExcutor HandleTaskAsync error {e.Message}");
            }

            return 0;

        }

        public async Task<bool> AddTaskAsync(TaskContent task)
        {
            if (task != null)
            {
                //归档
                RaiseEvent(new TaskEvent() { OpType = opType.otAdd, TaskContentInfo = task });
                await ConfirmEvents();
                if (_timer != null)
                {
                    _timer.Dispose();
                    _timer = null;
                }
                return true;
            }
            return false;
        }

        public async Task<bool> StopTaskAsync(TaskContent task)
        {
            if (task != null)
            {
                //归档
                RaiseEvent(new TaskEvent() { OpType = opType.otStop, TaskContentInfo = task });
                await ConfirmEvents();
                return true;
            }
            return false;
        }

        public Task<bool> JudgeTaskPriorityAsync(TaskContent taskcurrent, TaskContent taskcompare)
        {
            throw new NotImplementedException();
        }

        public bool OnNextStream(ChannelInfo info)
        {
            //start成功，stop失败，去timer
            return true;
        }

        /*
         * 开始成功就应该监听
         */
        private async Task OnRunningTaskMonitorAsync(object type)
        {
            Tuple<int, int, string, int, int, string> param = (Tuple<int, int, string, int, int, string>)type;

            var taskinfolst = await _restClient.GetChannelCapturingTaskInfoAsync(param.Item4);

            if (taskinfolst != null)
            {
                var devicegrain = GrainFactory.GetGrain<IDeviceInspections>(0);
                if (devicegrain != null)
                {
                    int msvtaskid = await devicegrain.QueryRunningTaskInChannelAsync(param.Item6, param.Item5);
                    if (msvtaskid != param.Item1)
                    {
                        Logger.Info("OnRunningTaskMonitorAsync task need to check");

                        if (param.Item2 == (int)TaskType.TT_VTRUPLOAD)
                        {

                        }
                        else
                        {
                            bool needCreateNewTask = false;
                            if (param.Item2 == (int)TaskType.TT_MANUTASK || param.Item2 == (int)TaskType.TT_OPENEND ||
                                param.Item2 == (int)TaskType.TT_OPENENDEX)
                            {
                                needCreateNewTask = true;
                            }
                            else
                            {
                                // 其它类型的任务，需要检查任务结束时间
                                // 时间都过去了的任务，就不用再创建了
                                if (DateTimeFormat.DateTimeFromString(param.Item3) < DateTime.Now.AddSeconds(5))
                                {
                                    needCreateNewTask = true;
                                }
                                else
                                {
                                    // 其它类型的任务，需要检查是否过时，如果已经过时，则需处理该任务
                                    // 已经过时，则需将任务置为无效状态
                                    await _restClient.SetTaskStateAsync(param.Item1, taskState.tsInvaild);
                                }
                            }

                            if (needCreateNewTask)
                            {
                                await _restClient.SetTaskStateAsync(param.Item1, taskState.tsInvaild);
                                await _restClient.AddReScheduleTaskAsync(param.Item1);
                            }
                        }
                    }
                }
                else
                    Logger.Error("OnRunningTaskMonitorAsync devicegrain null");
            }
           
        }


        public Task DeleteTaskAsync(TaskContent task)
        {
            throw new NotImplementedException();
        }

    }
}
