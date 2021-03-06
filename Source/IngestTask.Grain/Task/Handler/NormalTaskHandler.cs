﻿


namespace IngestTask.Grain
{
    using IngestTask.Dto;
    using IngestTask.Tool;
    using IngestTask.Tool.Msv;

    using System;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using Microsoft.Extensions.Configuration;
    public class NormalTaskHandler : TaskHandlerBase
    {
        private int taskStartPrevious;
        private int taskStopBehind;
        public NormalTaskHandler(RestClient rest, MsvClientCtrlSDK msv, IConfiguration configuration)
            : base(rest, msv)
        {
            taskStartPrevious = configuration.GetSection("Task:TaskStartPrevious").Get<int>();
            taskStopBehind = configuration.GetSection("Task:TaskStopBehind").Get<int>();
        }

        /*
         * 判断是否执行这个handle每个继承都必须要写这个
         */
        static public bool IsHandler(TaskFullInfo task)
        {
            if (task.TaskContent.CooperantType == CooperantType.emPureTask)
            {
                return true;
            }
            else if (task.TaskContent.CooperantType == CooperantType.emVTRBackupFailed && !task.StartOrStop)
            {
                return true;
            }
            return false;
        }

        public override int IsNeedRedispatchask(TaskFullInfo taskinfo)
        {
            if (taskinfo.StartOrStop)
            {
                if (DateTime.Now.AddSeconds(5) <=
                DateTimeFormat.DateTimeFromString(taskinfo.TaskContent.End))
                {
                    Logger.Error($"IsNeedRedispatchaskAsync start over {taskinfo.TaskContent.TaskId}");
                    return 0;
                }

                return taskinfo.TaskContent.TaskId;
            }
            else
            {
                if (DateTime.Now <=
                DateTimeFormat.DateTimeFromString(taskinfo.TaskContent.End).AddSeconds(2))
                {
                    Logger.Error($"IsNeedRedispatchaskAsync stop over {taskinfo.TaskContent.TaskId}");
                    return 0;
                }
                return taskinfo.TaskContent.TaskId;
            }
        }

        public override async ValueTask<int> HandleTaskAsync(TaskFullInfo task, ChannelInfo channel)
        {
            Logger.Info($"NormalTaskHandler HandleTaskAsync retrytimes {task.RetryTimes}");

            int taskid = task.TaskContent.TaskId;
            if (task.ContentMeta == null || string.IsNullOrEmpty(task.CaptureMeta))
            {
                await UnlockTaskAsync(taskid, taskState.tsNo, dispatchState.dpsRedispatch, syncState.ssSync);
                return 0;
            }

            if (task.StartOrStop && task.OpType != opType.otDel)
            {
                if (task.TaskContent.TaskType == TaskType.TT_MANUTASK)//已经执行的手动任务不需要执行，未执行的手动任务需要继续执行
                {
                    if (task.TaskContent.State == taskState.tsExecuting || task.TaskContent.State == taskState.tsManuexecuting)
                    {
                        await UnlockTaskAsync(taskid, taskState.tsExecuting, dispatchState.dpsDispatched, syncState.ssSync);
                        return taskid;
                    }
                }
                else if (task.TaskContent.TaskType == TaskType.TT_TIEUP)
                {
                    await HandleTieupTaskAsync(task.TaskContent);
                    return taskid;
                }
                else
                {
                    if (DateTimeFormat.DateTimeFromString(task.TaskContent.End) < DateTime.Now)//普通任务进行时间有效性判断, 
                    {
                        task.StartOrStop = false;//禁止监听任务
                        return taskid;
                    }
                }

                if (channel.CurrentDevState == Device_State.DISCONNECTTED)
                {
                    await UnlockTaskAsync(taskid,
                        taskState.tsNo, dispatchState.dpsRedispatch, syncState.ssSync);
                    return IsNeedRedispatchask(task);
                }

                if (await StartTaskAsync(task, channel) > 0)
                {
                    //成功
                    await UnlockTaskAsync(taskid, taskState.tsExecuting, dispatchState.dpsDispatched, syncState.ssSync);
                    return taskid;
                }
                else
                {
                    //使用备份信号
                    //我擦，居然可以不用写，stop才有
                    Logger.Info("start error. begin to use backupsignal");


                    if (task.TaskContent.TaskType == TaskType.TT_OPENEND ||
                        task.TaskContent.TaskType == TaskType.TT_OPENENDEX)
                    {
                        await UnlockTaskAsync(taskid, taskState.tsInvaild, dispatchState.dpsDispatched, syncState.ssSync);
                    }
                    else
                    {
                        await UnlockTaskAsync(taskid, taskState.tsNo, dispatchState.dpsRedispatch, syncState.ssSync);
                    }

                    //重调度还失败，要看看是否超过了，超过就从列表去了
                    
                    return IsNeedRedispatchask(task);

                }
                
                
            }
            else
            {

                Logger.Info($"task stop timespan {(DateTimeFormat.DateTimeFromString(task.TaskContent.End) - DateTime.Now).TotalSeconds}");
                task.TaskContent.End = DateTimeFormat.DateTimeToString(DateTime.Now);

                if (task.TaskContent.TaskType != TaskType.TT_MANUTASK || 
                    (task.TaskContent.TaskType == TaskType.TT_MANUTASK && task.OpType == opType.otDel))
                {

                    //里面有IsNeedRedispatchask(task);
                    var backinfo = await StopTaskAsync(task, channel);

                    //所有的删除都让入库去做,这里不删除
                    //开始删除素材
                    if (task.OpType == opType.otDel)
                    {
                        await UnlockTaskAsync(task.TaskContent.TaskId, taskState.tsComplete, dispatchState.dpsInvalid, syncState.ssSync);
                        //DeleteClip();
                    }
                    return backinfo;
                }
            }
            return 0;
        }

        public override async ValueTask<int> StartTaskAsync(TaskFullInfo task, ChannelInfo channel)
        {

            var backinfo = await msvClient.QueryTaskInfoAsync(channel.ChannelIndex, channel.Ip, Logger);


            if (backinfo != null)
            {
                if (backinfo.ulID > 0)//存在执行任务
                {
                    if (backinfo.ulID == task.TaskContent.TaskId)
                    {
                        return task.TaskContent.TaskId;
                    }
                    else if (backinfo.ulID < task.TaskContent.TaskId)
                    {
                        Logger.Info($"start msv in stop else {backinfo.ulID}");
                        await ForceStopTaskAsync(task, channel);
                    }

                    if (task.TaskSource == TaskSource.emStreamMediaUploadTask)
                    {
                        return 0;
                    }
                    //前一个任务是手动任务，特别处理无缝任务 bIsStopLastTask
                }

                switch (task.TaskSource)
                {
                    case TaskSource.emMSVUploadTask:
                        {
                            if (!await restClient.SwitchMatrixSignalChannelAsync(task.TaskContent.SignalId, channel.ChannelId))
                            {
                                Logger.Error($"Switchsignalchannel error {task.TaskContent.SignalId} {channel.ChannelId}");
                            }
                        }
                        break;
                    case TaskSource.emRtmpSwitchTask:
                        {
                            if (!await restClient.SwitchMatrixChannelRtmpAsync(channel.ChannelId, task.ContentMeta.SignalRtmpUrl))
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


                string capparam = await GetCaptureParmAsync(task, channel);

                DateTime dtcurrent;
                DateTime dtbegin = (task.RetryTimes > 0 && task.NewBeginTime != DateTime.MinValue)?task.NewBeginTime :
                    DateTimeFormat.DateTimeFromString(task.TaskContent.Begin).AddSeconds(-1* taskStartPrevious);

                while(true)
                {
                    dtcurrent = DateTime.Now;
                    if (dtcurrent >= dtbegin)
                    {
                        var recordinfo = msvClient.RecordReady(channel.ChannelIndex, channel.Ip, CreateTaskParam(task.TaskContent), "", capparam, Logger);

                        bool backrecord = await msvClient.RecordAsync(recordinfo, channel.ChannelIndex, channel.Ip, Logger);

                        if (backrecord)
                        {
                           
                            var state = await AutoRetry.RunSyncAsync(() =>
                                                            msvClient.QueryDeviceStateAsync(channel.ChannelIndex, channel.Ip, true, Logger),
                                                                                            (e) =>
                                                                                            {
                                                                                                if (e == Device_State.WORKING)
                                                                                                {
                                                                                                    return true;
                                                                                                }
                                                                                                return false;
                                                                                            }, 4, 500).ConfigureAwait(true);

                            if (state == Device_State.WORKING)
                            {
                                return task.TaskContent.TaskId;
                            }
                            else
                                Logger.Error($"start task error {task.TaskContent.TaskId}");
                        }
                        return 0;
                    }
                    else
                    {
                        await Task.Delay(dtbegin - dtcurrent);
                    }
                }
                
            }
            else
                Logger.Error($"StartTaskAsync QueryTaskInfoAsync error back 0");

            return 0;
        }

        public override async ValueTask<int> StopTaskAsync(TaskFullInfo task, ChannelInfo channel)
        {
            DateTime end = (task.RetryTimes > 0 && task.NewEndTime != DateTime.MinValue) ? task.NewEndTime
                : DateTimeFormat.DateTimeFromString(task.TaskContent.End).AddSeconds(taskStopBehind);

            while (true)
            {
                if (DateTime.Now >= end)
                {
                    var msvtaskinfo = await msvClient.QueryTaskInfoAsync(channel.ChannelIndex, channel.Ip, Logger);

                    if (msvtaskinfo != null)
                    {
                        if (msvtaskinfo.ulID == task.TaskContent.TaskId)
                        {
                            var stopback = await msvClient.StopAsync(channel.ChannelIndex, channel.Ip, task.TaskContent.TaskId, Logger);

                            if (stopback > 0)
                            {
                                await restClient.SetTaskStateAsync(task.TaskContent.TaskId, taskState.tsComplete);
                                await UnlockTaskAsync(task.TaskContent.TaskId, taskState.tsNo, dispatchState.dpsDispatched, syncState.ssSync);
                                return task.TaskContent.TaskId;
                            }
                            else
                            {
                                return IsNeedRedispatchask(task);
                            }
                        }
                        else
                        {
                            if (msvtaskinfo.ulID >0)
                            {
                                // 停止的任务不是MSV当前正在采集的任务
                                // 需要将该任务标记为无效任务
                                await UnlockTaskAsync(task.TaskContent.TaskId, taskState.tsInvaild,
                                    dispatchState.dpsDispatched, syncState.ssSync);
                            }
                            
                            Logger.Error($"stop task not same {msvtaskinfo.ulID} {task.TaskContent.TaskId}");
                            return task.TaskContent.TaskId;
                        }

                    }
                    else
                    {
                        return IsNeedRedispatchask(task);
                    }

                }
                else
                    await Task.Delay(end - DateTime.Now);
            }
        }

        public async Task<int> ForceStopTaskAsync(TaskFullInfo task, ChannelInfo channel)
        {
            var stopback = await msvClient.StopAsync(channel.ChannelIndex, channel.Ip, task.TaskContent.TaskId, Logger);

            if (stopback > 0)
            {
                return task.TaskContent.TaskId;
            }
            return 0;
        }
    }
}
