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
using System.Xml.Linq;

namespace IngestTask.Grain
{
    public class NormalTaskHandler : TaskHandlerBase
    {
        public NormalTaskHandler(RestClient rest, MsvClientCtrlSDK msv)
            : base(rest, msv)
        { }

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
                if (DateTime.Now.AddSeconds(2) <=
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

        public override async Task<int> HandleTaskAsync(TaskFullInfo task, ChannelInfo channel)
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

                if (task.OpType == opType.otAdd)
                {
                    if (channel.CurrentDevState != Device_State.CONNECTED)
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


                string capparam = await GetCaptureParmAsync(task, channel);

                DateTime dtcurrent;
                DateTime dtbegin = (task.RetryTimes > 0 && task.NewBeginTime != DateTime.MinValue)?task.NewBeginTime :
                    DateTimeFormat.DateTimeFromString(task.TaskContent.Begin).AddSeconds(-1* ApplicationContext.Current.TaskStartPrevious);

                while(true)
                {
                    dtcurrent = DateTime.Now;
                    if (dtcurrent >= dtbegin)
                    {
                        MsvSdk.RecordReady(channel.ChannelIndex, channel.Ip, CreateTaskParam(task.TaskContent), "", task.CaptureMeta, Logger);

                        bool backrecord = await AutoRetry.BoolRunAsync( async () => {
                            if (await MsvSdk.RecordAsync(channel.ChannelIndex, channel.Ip, Logger))
                            {
                                return true;
                            }
                            return false;
                        }, 3, 200);


                        if (backrecord)
                        {
                            backrecord = await AutoRetry.BoolRunAsync(async () => {
                                if ( await MsvSdk.QueryDeviceStateAsync(channel.ChannelIndex, channel.Ip, true, Logger) 
                                            == Device_State.WORKING)
                                {
                                    return true;
                                }
                                return false;
                            });

                            if (backrecord)
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

        public override async Task<int> StopTaskAsync(TaskFullInfo task, ChannelInfo channel)
        {
            DateTime end = (task.RetryTimes > 0 && task.NewEndTime != DateTime.MinValue) ? task.NewEndTime
                : DateTimeFormat.DateTimeFromString(task.TaskContent.End).AddSeconds(ApplicationContext.Current.TaskStopBehind);

            while (true)
            {
                if (DateTime.Now >= end)
                {
                    var msvtaskinfo = await AutoRetry.RunSyncAsync(async () =>
                    {
                        return await MsvSdk.QueryTaskInfoAsync(channel.ChannelIndex, channel.Ip, Logger);
                    },
                    (e) =>
                    {
                        if (e != null)
                        {
                            return true;
                        }
                        return false;
                    },
                    5, 300);

                    if (msvtaskinfo != null)
                    {
                        if (msvtaskinfo.ulID == task.TaskContent.TaskId)
                        {
                            var stopback = await AutoRetry.BoolRunAsync(async () =>
                            {
                                var backlong = await MsvSdk.StopAsync(channel.ChannelIndex, channel.Ip, task.TaskContent.TaskId, Logger);
                                if (backlong > 0)
                                {
                                    return true;
                                }
                                return false;
                            });

                            if (stopback)
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

        public Task<int> ForceStopTaskAsync(TaskFullInfo task, ChannelInfo channel)
        {
            throw new NotImplementedException();
        }
    }
}
