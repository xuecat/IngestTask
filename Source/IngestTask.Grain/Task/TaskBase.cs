
using ProtoBuf;

namespace IngestTask.Grain
{
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
    using System.Threading.Tasks;

    //[ProtoContract]
    [Serializable]
    public class TaskEvent
    {
        public TaskInfo TaskInfo { get; set; }
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
            TaskLists = new List<TaskInfo>();
        }
        //[ProtoMember(1)]
        public int ChannelId { get; set; }
        public long ReminderTimer { get; set; }
        public List<TaskInfo> TaskLists { get; set; }

        //修改状态对象之外，TransitionState方法不应该有任何副作用，并且应该是确定性的
        public void Apply(TaskEvent @event)
        {
            switch (@event.OpType)
            {
                case opType.otAdd:
                    {
                        if (@event.TaskInfo.Content.State == taskState.tsReady 
                            && TaskLists.Find(a => a.Content.TaskId == @event.TaskInfo.Content.TaskId) == null)
                        {
                            TaskLists.Add(@event.TaskInfo);
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
    [Reentrant]
    //[StorageProvider(ProviderName="store1")]
    public class TaskBase : JournaledGrain<TaskState,TaskEvent>, ITask
    {
        private readonly ILogger Logger = LoggerManager.GetLogger("TaskInfo");
        private readonly IScheduleClient _scheduleClient;
        public TaskBase(IGrainActivationContext grainActivationContext, IScheduleClient dataServiceClient)
        {
            _scheduleClient = dataServiceClient;
        }

        public override Task OnActivateAsync()
        {
            Logger.Info(" TaskBase active");
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

        protected override void OnStateChanged()
        {
            // read state and/or event log and take appropriate action
        }

        public Task<TaskContent> GetCurrentTaskAsync()
        {
            throw new NotImplementedException();
        }

        public Task HandleTaskAsync(TaskInfo task)
        {
            if (task.StartOrStop)
            {
                //如果判断到是周期任务，那么需要对它做分任务的处理
                //这个步骤挪到后台server去做


            }
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
                    RaiseEvent(new TaskEvent() { OpType = opType.otAdd, TaskInfo = new TaskInfo() { Content = task, StartOrStop = true} });
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
