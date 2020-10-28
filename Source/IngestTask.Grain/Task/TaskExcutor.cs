
using ProtoBuf;

namespace IngestTask.Grain
{
    using AutoMapper;
    using IngestTask.Abstraction.Grains;
    using IngestTask.Abstraction.Service;
    using IngestTask.Dto;
    using IngestTask.Tool;
    using IngestTask.Tools;
    using Orleans;
    using Orleans.Concurrency;
    using Orleans.EventSourcing;
    using Orleans.LogConsistency;
    using Orleans.Runtime;
    using ProtoBuf;
    using Sobey.Core.Log;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

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
        public TaskState ()
        {
            TaskLists = new List<TaskFullInfo>();
        }
        //[ProtoMember(1)]
        public long ChannelId { get; set; }
        //public long ReminderTimer { get; set; }
        public List<TaskFullInfo> TaskLists { get; set; }

        //修改状态对象之外，TransitionState方法不应该有任何副作用，并且应该是确定性的
        public void Apply(TaskEvent @event)
        {
            switch (@event.OpType)
            {
                case opType.otAdd:
                    {
                        if (@event.TaskContentInfo.State == taskState.tsReady 
                            && TaskLists.Find(a => a.TaskContent.TaskId == @event.TaskContentInfo.TaskId) == null)
                        {
                            TaskLists.Add(new TaskFullInfo() { TaskContent = @event.TaskContentInfo, StartOrStop = true});
                        }
                        
                    }
                    break;
                case opType.otDel:
                    break;
                case opType.otMove:
                    break;
                case opType.otModify:
                    break;
                default:
                    break;
            }
        }

        public void Apply(DeviceEvent @event)
        { }
    }


    //要不要存数据库呢
    //[LogConsistencyProvider(ProviderName = "CustomStorage")]
    //[StorageProvider(ProviderName="store1")]
    public class TaskExcutor : JournaledGrain<TaskState,TaskEvent>, ITask
    {
        private readonly ILogger Logger = LoggerManager.GetLogger("TaskExcutor");
        private readonly IScheduleClient _scheduleClient;
        private readonly RestClient _restClient;
        private readonly ITaskHandlerFactory _handlerFactory;
        private readonly IMapper Mapper;
        public TaskExcutor(IGrainActivationContext grainActivationContext,
            IScheduleClient dataServiceClient,
            RestClient rest,
            IMapper mapper,
            ITaskHandlerFactory handlerfac)
        {
            Mapper = mapper;
            _scheduleClient = dataServiceClient;
            _restClient = rest;
            _handlerFactory = handlerfac;
        }

        public override Task OnActivateAsync()
        {
            State.ChannelId = this.GetPrimaryKeyLong();
            Logger.Info($" TaskBase active {State.ChannelId}");
            return base.OnActivateAsync();
        }
        public override Task OnDeactivateAsync()
        {
            return base.OnDeactivateAsync();
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

        public Task<TaskContent> GetCurrentTaskAsync()
        {
            throw new NotImplementedException();
        }

        protected override void OnStateChanged()
        {
            // read state and/or event log and take appropriate action
            var orleansts = TaskScheduler.Current;
            foreach (var item in State.TaskLists)
            {
                _ = Task.Factory.StartNew(async () =>
                {
                    return await HandleTaskAsync(item);
                }, CancellationToken.None, TaskCreationOptions.None, scheduler: orleansts);
            }
        }

        public async Task<int> HandleTaskAsync(TaskFullInfo task)
        {
            Logger.Info($"TaskExcutor HandleTaskAsync {task.TaskContent.TaskId}");

            try
            {
                //如果判断到是周期任务，那么需要对它做分任务的处理
                //这个步骤挪到后台server去做
                if (task.TaskSource == TaskSource.emUnknowTask || task.ContentMeta != null)
                {
                    if (_restClient != null)
                    {
                        ObjectTool.CopyObjectData(await _restClient.GetTaskFullInfoAsync(task.TaskContent.TaskId),
                            task, string.Empty, BindingFlags.Public | BindingFlags.Instance);

                        Logger.Info($"TaskExcutor HandleTaskAsync get {JsonHelper.ToJson(task)}");
                    }
                }

                /*
                * flag katamaki任务检测
                */

                var devicegrain = GrainFactory.GetGrain<IDeviceInspections>(0);
                if (devicegrain != null)
                {
                    return await _handlerFactory.CreateInstance(task)?.HandleTaskAsync(task,
                        await devicegrain.GetChannelInfoAsync(task.TaskContent.ChannelId));

                }

                return 0;
            }
            catch (Exception e)
            {

                Logger.Error($"TaskExcutor HandleTaskAsync error {e.Message}");
            }

            return 0;

        }

        public async Task AddTaskAsync(TaskContent task)
        {
            if (task != null)
            {
                if ((DateTimeFormat.DateTimeFromString(task.Begin) - DateTime.Now).TotalSeconds > 
                    ApplicationContext.Current.TaskPrevious)
                {
                    //提交
                    await _scheduleClient.AddScheduleTaskAsync(task);
                }
                else
                {
                    //归档
                    RaiseEvent(new TaskEvent() { OpType = opType.otAdd, TaskContentInfo = task });
                }
            }
        }

        

        public bool JudgeTaskPriority(TaskContent taskcurrent, TaskContent taskcompare)
        {
            throw new NotImplementedException();
        }

        public Task ModifyTaskAsync(TaskContent task)
        {
            throw new NotImplementedException();
        }

        public Task DeleteTaskAsync(TaskContent task)
        {
            throw new NotImplementedException();
        }

    }
}
