
namespace IngestTask.Grain
{
    using IngestTask.Abstraction.Grains;
    using IngestTask.Tools.Dto;
    using Orleans;
    using Orleans.Concurrency;
    using Orleans.EventSourcing;
    using Orleans.LogConsistency;
    using Sobey.Core.Log;
    using System;
    using System.Threading.Tasks;

    //[StorageProvider(ProviderName="store1")]
    [Reentrant]
    public class TaskBase : JournaledGrain<TaskState,TaskEvent>, ITask
    {
        private readonly ILogger Logger = LoggerManager.GetLogger("TaskInfo");
        //private int _channelID;
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
                        state.TaskStatus = taskState.tsExecuting;
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
