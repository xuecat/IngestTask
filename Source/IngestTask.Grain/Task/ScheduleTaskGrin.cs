

namespace IngestTask.Grain
{
    using AutoMapper;
    using IngestTask.Abstraction.Grains;
    using IngestTask.Dto;
    using IngestTask.Tool;
    using Microsoft.Extensions.Configuration;
    using Orleans;
    using Orleans.Concurrency;
    using OrleansDashboard.Abstraction;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    //[Reentrant]
    [ScheduleTaskPlacementStrategy]
    [MultiGrain("IngestTask.Grain.ScheduleTaskGrin")]
    [TraceGrain("IngestTask.Grain.ScheduleTaskGrin", TaskTraceEnum.TaskSchedule)]
    public class ScheduleTaskGrin : Grain<List<DispatchTask>>, IScheduleTaskGrain
    {
        private IDisposable _dispoScheduleTimer;
        private int _taskSchedulePreviousSeconds;

        private readonly Sobey.Core.Log.ILogger Logger;
        private readonly RestClient _restClient;

        private IMapper _mapper;

        public ScheduleTaskGrin(IMapper mapper, RestClient restClient, IConfiguration configuration)
        {
            Logger = Sobey.Core.Log.LoggerManager.GetLogger("ScheduleTask");
            _taskSchedulePreviousSeconds = configuration.GetSection("Task:TaskSchedulePrevious").Get<int>();
            _restClient = restClient;
            _mapper = mapper;
            _dispoScheduleTimer = null;
        }

        public override async Task OnActivateAsync()
        {
            await ReadStateAsync();
            if (State.Count > 0)
            {
                if (_dispoScheduleTimer == null)
                {
                    _dispoScheduleTimer = RegisterTimer(this.OnScheduleTaskAsync, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
                }
            }
            await base.OnActivateAsync();
        }

        public override async Task OnDeactivateAsync()
        {
            await base.OnDeactivateAsync();
        }

        [NoProfiling]
        public Task<List<DispatchTask>> GetTaskListAsync()
        {
            return Task.FromResult(this.State);
        }

        private bool TaskIsInvalid(DispatchTask task)
        {
            if (task != null && task.Tasktype != null
                && task.Taskid > 0 && task.State != null && task.Endtime != null
                && task.Starttime != null && task.Endtime > DateTime.MinValue)
            {
                return false;
            }
            return true;
        }

        public async Task<int> AddScheduleTaskAsync(DispatchTask task)
        {
            Logger.Info($"add scheduletask {task.Taskid} {task.Tasktype} {task.State} {task.Starttime} {task.Endtime}");
            var finditem = this.State.Find(x => x.Taskid == task.Taskid);
            if (task != null && finditem == null)
            {
                lock (this.State)
                {
                    this.State.Add(task);
                    if (_dispoScheduleTimer == null)
                    {
                        _dispoScheduleTimer = RegisterTimer(this.OnScheduleTaskAsync, null, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(1));
                    }

                    this.State.RemoveAll(x => TaskIsInvalid(x));
                }

                await WriteStateAsync();
                return task.Taskid;
            }
            else
            {
                lock (this.State)
                {
                    ObjectTool.CopyObjectData(task, finditem, string.Empty, BindingFlags.Public | BindingFlags.Instance);

                    if (_dispoScheduleTimer == null)
                    {
                        _dispoScheduleTimer = RegisterTimer(this.OnScheduleTaskAsync, null, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(1));
                    }
                    this.State.RemoveAll(x => TaskIsInvalid(x));
                }
                await WriteStateAsync();
                return task.Taskid;
            }
        }

        public async Task<int> UpdateScheduleTaskAsync(DispatchTask task)
        {
            if (task != null)
            {
                var finditem = this.State.Find(x => x.Taskid == task.Taskid);
                if (finditem == null)
                {
                    lock (this.State)
                    {
                        this.State.Add(task);
                        if (_dispoScheduleTimer == null)
                        {
                            _dispoScheduleTimer = RegisterTimer(this.OnScheduleTaskAsync, null, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(1));
                        }
                        this.State.RemoveAll(x => TaskIsInvalid(x));
                    }
                    await WriteStateAsync();
                    return task.Taskid;
                }
                else
                {
                    lock (this.State)
                    {
                        ObjectTool.CopyObjectData(task, finditem, string.Empty, BindingFlags.Public | BindingFlags.Instance);

                        if (_dispoScheduleTimer == null)
                        {
                            _dispoScheduleTimer = RegisterTimer(this.OnScheduleTaskAsync, null, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(1));
                        }
                        this.State.RemoveAll(x => TaskIsInvalid(x));
                    }
                    await WriteStateAsync();
                    return task.Taskid;
                }
            }
            return 0;
        }

        public async Task<int> RemoveScheduleTaskAsync(DispatchTask task)
        {
            if (task != null)
            {
                int removecount = 0;
                bool stopnow = false;

                lock (this.State)
                {
                    var item = this.State.Find(x => x.Taskid == task.Taskid);
                    if (item != null)
                    {
                        this.State.Remove(item);
                        if (item.State == (int)taskState.tsExecuting)
                        {
                            stopnow = true;
                        }
                        removecount = 1;
                    }
                }

                if (this.State.Count == 0 && _dispoScheduleTimer != null)
                {
                    _dispoScheduleTimer.Dispose();
                    _dispoScheduleTimer = null;
                }
                if (removecount > 0)
                {
                    await WriteStateAsync();
                }

                if (stopnow)
                {
                    await GrainFactory.GetGrain<ITask>(task.Channelid.GetValueOrDefault())?.StopTaskAsync(_mapper.Map<TaskContent>(task));
                }

                return task.Taskid;
            }

            return 0;
        }

        private async Task OnScheduleTaskAsync(object type)
        {
            //如果判断到是周期任务，那么需要对它做分任务的处理
            //这个步骤挪到后台server去做

            //任务分发的时候要向请求通道是否存在，不存在提交一个自检请求

            var _lstRemoveTask = new List<DispatchTask>();

            if (this.State.Count > 0)
            {
                var lsttask = this.State;
                
                if (lsttask != null && lsttask.Count > 0)
                {
                    foreach (var task in lsttask)
                    {
                        if (task.State == (int)taskState.tsReady)
                        {
                            if (task.Tasktype == (int)TaskType.TT_PERIODIC
                                && task.OldChannelid == 0)
                            {
                                if ((task.NewBegintime - DateTime.Now).TotalSeconds <=
                                    _taskSchedulePreviousSeconds)
                                {
                                    var info = await _restClient.CreatePeriodcTaskAsync(task.Taskid);

                                    Logger.Info($"create period task {task.Taskid}");
                                    if (info != null)
                                    {
                                        _lstRemoveTask.Add(task);
                                        Logger.Info($"perod new info {JsonHelper.ToJson(info)}");
                                        //周期母任务更新后，再加进去，进行下一个周期
                                        //var taskperiod = await _restClient.GetTaskDBAsync(task.Taskid);
                                        if (info.Endtime > DateTime.Now && info.NewBegintime > DateTime.Now)
                                        {
                                            await GrainFactory.GetGrain<IDispatcherGrain>(0).AddTaskAsync(info);
                                        }
                                    }
                                    else
                                    {
                                        _lstRemoveTask.Add(task);
                                        Logger.Info($"create period failed begin to remove {task.Taskid}");
                                    }
                                }
                            }
                            else
                            {
                                if ((task.Starttime - DateTime.Now).TotalSeconds <=
                                    _taskSchedulePreviousSeconds)
                                {
                                    if (task.Endtime > DateTime.Now)
                                    {
                                        await GrainFactory.GetGrain<ITask>(task.Channelid.GetValueOrDefault())?.AddTaskAsync(_mapper.Map<TaskContent>(task));
                                        task.State = (int)taskState.tsExecuting;
                                        Logger.Info($"add task excuter {task.Taskid}");
                                    }
                                    else
                                        _lstRemoveTask.Add(task);

                                }
                            }
                        }
                        else if (task.State == (int)taskState.tsExecuting || task.State == (int)taskState.tsManuexecuting)
                        {
                            var spansecond = (task.Endtime - DateTime.Now).TotalSeconds;
                            if (spansecond < _taskSchedulePreviousSeconds)
                            {
                                if (await GrainFactory.GetGrain<ITask>(task.Channelid.GetValueOrDefault())?.StopTaskAsync(_mapper.Map<TaskContent>(task)))
                                {
                                    _lstRemoveTask.Add(task);
                                    Logger.Info($"stop task {task.Taskid}");
                                }
                            }
                        }

                    }

                    if (_lstRemoveTask.Count > 0)
                    {
                        lock (this.State)
                        {
                            if (this.State.Count > 0)
                            {
                                var lst = _lstRemoveTask.Select(x => x.Taskid).ToList();
                                this.State.RemoveAll(x => lst.Contains(x.Taskid));
                            }
                            if (this.State.Count == 0 && _dispoScheduleTimer != null)
                            {
                                _dispoScheduleTimer.Dispose();
                                _dispoScheduleTimer = null;
                            }
                        }
                        await WriteStateAsync();
                    }
                }
                else
                {
                    lock (this.State)
                    {
                        this.State.Clear();

                        if (this.State.Count == 0 && _dispoScheduleTimer != null)
                        {
                            _dispoScheduleTimer.Dispose();
                            _dispoScheduleTimer = null;
                        }
                        Logger.Info("clear all scheduletask");
                    }
                }

            }



        }
    }
}
