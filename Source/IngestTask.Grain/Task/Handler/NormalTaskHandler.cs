using AutoMapper;
using IngestTask.Abstraction.Grains;
using IngestTask.Abstraction.Service;
using IngestTask.Dto;
using IngestTask.Tool;
using IngestTask.Tools;
using IngestTask.Tools.Msv;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IngestTask.Grain
{
    public class NormalTaskHandler : TaskHandlerBase
    {
        public NormalTaskHandler(RestClient rest, MsvClientCtrlSDK msv)
            : base(rest, msv)
        { }

        static public bool IsHandler(TaskFullInfo task)
        {
            if (task.TaskContent.CooperantType == CooperantType.emPureTask)
            {
                return true;
            }
            return false;
        }

        public override async Task<int> HandleTaskAsync(TaskFullInfo task, ChannelInfo channel)
        {
            Logger.Info("NormalTaskHandler HandleTaskAsync");

            if (task.ContentMeta == null || string.IsNullOrEmpty(task.CaptureMeta))
            {
                await UnlockTaskAsync(task.TaskContent.TaskId, taskState.tsNo, dispatchState.dpsRedispatch, syncState.ssSync);
                return 0;
            }

            if (task.StartOrStop)
            {

                if (task.OpType != opType.otDel)
                {

                    if (task.TaskContent.TaskType == TaskType.TT_MANUTASK)
                    {
                        await UnlockTaskAsync(task.TaskContent.TaskId, taskState.tsExecuting, dispatchState.dpsDispatched, syncState.ssSync);
                        return task.TaskContent.TaskId;
                    }
                    else if (task.TaskContent.TaskType == TaskType.TT_TIEUP)
                    {
                        await HandleTieupTaskAsync(task.TaskContent);
                        return task.TaskContent.TaskId;
                    }

                    if (task.OpType == opType.otAdd)
                    {
                        if (channel.CurrentDevState != Device_State.CONNECTED)
                        {
                            return await UnlockTaskAsync(task.TaskContent.TaskId,
                                taskState.tsNo, dispatchState.dpsRedispatch, syncState.ssSync) ? 1 : 0;
                        }

                        return await StartTaskAsync(task, channel);
                    }
                }
            }
            return 0;
        }

        public override async Task<int> StartTaskAsync(TaskFullInfo task, ChannelInfo channel)
        {

            var backinfo = await AutoRetry.BoolRunAsync(async () =>
            {
                var msvtaskinfo = await MsvSdk.QueryTaskInfoAsync(channel.ChannelIndex, channel.Ip, Logger);

                if (msvtaskinfo != null)
                {
                    if (msvtaskinfo.ulID >0)//存在执行任务
                    {
                        if (msvtaskinfo.ulID == task.TaskContent.TaskId)
                        {
                            return true;
                        }
                        else if (msvtaskinfo.ulID < task.TaskContent.TaskId)
                        {
                            Logger.Info($"start msv in stop else {msvtaskinfo.ulID}");
                            await ForceStopTaskAsync(task, channel);
                            return false;//放过本轮，继续下轮
                        }

                        if (task.TaskSource == TaskSource.emStreamMediaUploadTask)
                        {
                            return false;
                        }

                        //前一个任务是手动任务，特别处理无缝任务 bIsStopLastTask
                    }
                    return true;
                }
                else
                {
                    Logger.Error("QueryTaskInfoAsync no task running");
                }

                return false;
            }, 5, 500);

            if (backinfo)
            {
                switch (task.TaskSource)
                {
                    case TaskSource.emMSVUploadTask:
                        {
                            if (!await RestClientApi.SwitchMatrixSignalChannelAsync(task.TaskContent.SignalId, channel.ChannelId))
                            {
                                Logger.Error($"Switchsignalchannel error {task.TaskContent.SignalId} {channel.ChannelId}");
                            }
                        }
                        break;
                    case TaskSource.emRtmpSwitchTask:
                        {
                            if (!await RestClientApi.SwitchMatrixChannelRtmpAsync(channel.ChannelId, task.ContentMeta.SignalRtmpUrl))
                            {
                                Logger.Error($"Switchsignalchannel error {task.TaskContent.SignalId} {channel.ChannelId}");
                            }
                        }
                        break;
                    case TaskSource.emVTRUploadTask:
                        break;
                    case TaskSource.emXDCAMUploadTask:
                        break;
                    case TaskSource.emIPTSUploadTask:
                        break;
                    case TaskSource.emStreamMediaUploadTask:
                        break;
                    case TaskSource.emUnknowTask:
                        break;
                    default:
                        break;
                }
            }
            else
                Logger.Error($"StartTaskAsync QueryTaskInfoAsync error back 0");

            return 0;
        }

        public override Task<int> StopTaskAsync(TaskFullInfo task, ChannelInfo channel)
        {
            throw new NotImplementedException();
        }

        public Task<int> ForceStopTaskAsync(TaskFullInfo task, ChannelInfo channel)
        {
            throw new NotImplementedException();
        }
    }
}
