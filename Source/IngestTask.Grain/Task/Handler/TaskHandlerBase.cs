

namespace IngestTask.Grain
{
    using IngestTask.Abstraction.Grains;
    using IngestTask.Dto;
    using IngestTask.Tool;
    using IngestTask.Tools.Msv;
    using Sobey.Core.Log;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;

    public class TaskHandlerBase : ITaskHandler
    {
        protected ILogger Logger { get; set; }
        protected RestClient RestClientApi { get; set; }

        protected MsvClientCtrlSDK MsvSdk { get; set; }
        public TaskHandlerBase(RestClient rest, MsvClientCtrlSDK msv)
        {
            Logger = LoggerManager.GetLogger("TaskHandlerBase");
            RestClientApi = rest;
            MsvSdk = msv;
        }

        public virtual Task<int> HandleTaskAsync(TaskFullInfo task, ChannelInfo channel)
        {
            throw new NotImplementedException();
        }

        public virtual Task<int> StartTaskAsync(TaskFullInfo task, ChannelInfo channel)
        {
            throw new NotImplementedException();
        }

        public virtual Task<int> StopTaskAsync(TaskFullInfo task, ChannelInfo channel)
        {
            throw new NotImplementedException();
        }

        public virtual async Task<bool> UnlockTaskAsync(int taskid, taskState tkstate, dispatchState dpstate, syncState systate)
        {
            if (await RestClientApi.CompleteSynTasksAsync(taskid, tkstate, dpstate, systate))
            {
                Logger.Info($"taskbase UnlockTaskAsync success {taskid} {tkstate} {dpstate} {systate}");
            }
            else
            {
                Logger.Info($"taskbase UnlockTaskAsync failed {taskid} {tkstate} {dpstate} {systate}");
            }

            if (dpstate == dispatchState.dpsRedispatch
                || dpstate == dispatchState.dpsDispatchFailed)
            {
                //同步planing的状态为 PlanState.emPlanFailed
                //但是现在代码没有可以先不用写
                //SetPlanSourceListState(PluginsMgr.PlanState.emPlanFailed)
            }

            return false;
        }

        public virtual async Task<bool> HandleTieupTaskAsync(TaskContent info)
        {
            await UnlockTaskAsync(info.TaskId, taskState.tsNo, dispatchState.tsNo, syncState.ssSync);
            var lsttask = await RestClientApi.GetChannelCapturingTaskInfoAsync(info.ChannelId);
            if (lsttask != null && lsttask.TaskType == TaskType.TT_MANUTASK)
            {
                await RestClientApi.DeleteTaskAsync(info.TaskId).ConfigureAwait(true);

                //同步planing的状态为 PlanState.emPlanDeleted
                //但是现在代码没有可以先不用写
                //SetPlanSourceListState(PluginsMgr.PlanState.emPlanDeleted)
            }
            return false;
        }
    }
}
