using AutoMapper;
using IngestTask.Abstraction.Grains;
using IngestTask.Dto;
using IngestTask.Tool;
using IngestTask.Tools;
using IngestTask.Tools.Msv;

using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace IngestTask.Grain
{
    using Microsoft.Extensions.Configuration;
    public class NormalTaskHandler : TaskHandlerBase
    {
        public IConfiguration Configuration { get; }
        public NormalTaskHandler(RestClient rest, MsvClientCtrlSDK msv, IConfiguration configuration)
            : base(rest, msv)
        {
            Configuration = configuration;
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
                if (task.TaskContent.TaskType == TaskType.TT_MANUTASK)
                {
                    await UnlockTaskAsync(taskid, taskState.tsExecuting, dispatchState.dpsDispatched, syncState.ssSync);
                    return taskid;
                }
                else if (task.TaskContent.TaskType == TaskType.TT_TIEUP)
                {
                    await HandleTieupTaskAsync(task.TaskContent);
                    return taskid;
                }

                if (channel.CurrentDevState == Device_State.DISCONNECTTED)
                {
                    return await UnlockTaskAsync(taskid,
                        taskState.tsNo, dispatchState.dpsRedispatch, syncState.ssSync) ? 1 : 0;
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
                    if (task.RetryTimes > 0)
                    {
                        return IsNeedRedispatchask(task);
                    }

                    return 0;
                }
                
                
            }
            else
            {
                Logger.Info($"task stop timespan {(DateTimeFormat.DateTimeFromString(task.TaskContent.End) - DateTime.Now).TotalSeconds}");
                task.TaskContent.End = DateTimeFormat.DateTimeToString(DateTime.Now);


                
                if (task.TaskContent.TaskType != TaskType.TT_MANUTASK || 
                    (task.TaskContent.TaskType == TaskType.TT_MANUTASK && task.OpType == opType.otDel))
                {
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

            var backinfo = await _msvClient.QueryTaskInfoAsync(channel.ChannelIndex, channel.Ip, Logger);


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


                string capparam = await GetCaptureParmAsync(task, channel);

                int test = Configuration.GetSection("Task:TaskStartPrevious").Get<int>();

                DateTime dtcurrent;
                DateTime dtbegin = (task.RetryTimes > 0 && task.NewBeginTime != DateTime.MinValue)?task.NewBeginTime :
                    DateTimeFormat.DateTimeFromString(task.TaskContent.Begin).AddSeconds(-1* test);

                while(true)
                {
                    dtcurrent = DateTime.Now;
                    if (dtcurrent >= dtbegin)
                    {
                        _msvClient.RecordReady(channel.ChannelIndex, channel.Ip, CreateTaskParam(task.TaskContent), "", capparam, Logger);

                        bool backrecord = await _msvClient.RecordAsync(channel.ChannelIndex, channel.Ip, Logger);

                        if (backrecord)
                        {
                            var state = await _msvClient.QueryDeviceStateAsync(channel.ChannelIndex, channel.Ip, true, Logger);

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
            int test = Configuration.GetSection("Task:TaskStopBehind").Get<int>();

            DateTime end = (task.RetryTimes > 0 && task.NewEndTime != DateTime.MinValue) ? task.NewEndTime
                : DateTimeFormat.DateTimeFromString(task.TaskContent.End).AddSeconds(test);

            while (true)
            {
                if (DateTime.Now >= end)
                {
                    var msvtaskinfo = await _msvClient.QueryTaskInfoAsync(channel.ChannelIndex, channel.Ip, Logger);

                    if (msvtaskinfo != null)
                    {
                        if (msvtaskinfo.ulID == task.TaskContent.TaskId)
                        {
                            var stopback = await _msvClient.StopAsync(channel.ChannelIndex, channel.Ip, task.TaskContent.TaskId, Logger);

                            if (stopback > 0)
                            {
                                await RestClientApi.SetTaskStateAsync(task.TaskContent.TaskId, taskState.tsComplete);
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
                            // 停止的任务不是MSV当前正在采集的任务
                            // 需要将该任务标记为无效任务
                            await UnlockTaskAsync(task.TaskContent.TaskId, taskState.tsInvaild,
                                dispatchState.dpsDispatched, syncState.ssSync);
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
            var stopback = await _msvClient.StopAsync(channel.ChannelIndex, channel.Ip, task.TaskContent.TaskId, Logger);

            if (stopback > 0)
            {
                return task.TaskContent.TaskId;
            }
            return 0;
        }
    }
}
