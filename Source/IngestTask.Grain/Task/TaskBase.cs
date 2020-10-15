
using ProtoBuf;

namespace IngestTask.Grain
{
    using IngestTask.Abstraction.Grains;
    using IngestTask.Dto;
    using Orleans;
    using Orleans.Concurrency;
    using Orleans.EventSourcing;
    using Orleans.LogConsistency;
    using ProtoBuf;
    using Sobey.Core.Log;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    //这些序列化代表基础结构体都要protoc序列化，太麻烦了，我打算只心跳那里做protoc序列化
    //[ProtoContract]
    [Serializable]
    public class TaskState
    {
        //[ProtoMember(1)]
        public int ChannelId { get; set; }
        public long ReminderTimer { get; set; }
        public List<TaskContent> TaskLists { get; set; }
    }

    //[ProtoContract]
    [Serializable]
    public class TaskEvent
    {
        public TaskContent TaskInfo { get; set; }
        public opType OpType { get; set; }

    }
    //要不要存数据库呢
    //[StorageProvider(ProviderName="store1")]
    [Reentrant]
    public class TaskBase : JournaledGrain<TaskState,TaskEvent>, ITask
    {
        private readonly ILogger Logger = LoggerManager.GetLogger("TaskInfo");
        
        public TaskBase()
        {
        }

        public override Task OnActivateAsync()
        {
            Logger.Info(" DeviceInspectionGrain active");
            return base.OnActivateAsync();
        }
        public override Task OnDeactivateAsync()
        {
            return base.OnDeactivateAsync();
        }

        protected override void TransitionState(TaskState state, TaskEvent @event)
        {
            //修改状态对象之外，TransitionState方法不应该有任何副作用，并且应该是确定性的
            switch (@event.OpType)
            {
                case opType.otAdd:
                    {
                        //state.TaskStatus = taskState.tsExecuting;
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

        public Task AddTaskAsync(TaskContent task)
        {
            if (true)
            {

            }
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
