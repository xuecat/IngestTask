

namespace IngestTask.Grain
{
    using AutoMapper;
    using IngestTask.Abstraction.Grains;
    using IngestTask.Dto;
    using IngestTask.Tool;
    using IngestTask.Tools;
    using Microsoft.Extensions.Configuration;
    using Orleans;
    using Orleans.Concurrency;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    [Reentrant]
    [ScheduleTaskPlacementStrategy]
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
            await base.OnActivateAsync();
        }

        public override async Task OnDeactivateAsync()
        {
            await base.OnDeactivateAsync();
        }

        public async Task<int> AddScheduleTaskAsync(DispatchTask task)
        {
            if (task != null && this.State.Find(x => x.Taskid == task.Taskid) == null)
            {
                lock (this.State)
                {
                    this.State.Add(task);
                    if (_dispoScheduleTimer == null)
                    {
                        _dispoScheduleTimer = RegisterTimer(this.OnScheduleTaskAsync, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
                    }
                }
                await WriteStateAsync();
                return task.Taskid;
            }

            return 0;
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
                lock (this.State)
                {
                    removecount = this.State.RemoveAll(x => x.Taskid == task.Taskid);
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
                                && task.State == (int)taskState.tsReady
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

                                        //周期母任务更新后，再加进去，进行下一个周期
                                        var taskperiod = await _restClient.GetTaskDBAsync(task.Taskid);
                                        if (taskperiod.Endtime > DateTime.Now && taskperiod.NewBegintime > DateTime.Now)
                                        {
                                            await GrainFactory.GetGrain<IReminderTask>(0).AddTaskAsync(taskperiod);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if ((task.Starttime - DateTime.Now).TotalSeconds <=
                                    _taskSchedulePreviousSeconds && task.Endtime > DateTime.Now)
                                {
                                    await GrainFactory.GetGrain<ITask>(task.Channelid.GetValueOrDefault())?.AddTaskAsync(_mapper.Map<TaskContent>(task));
                                    Logger.Info($"add task excuter {task.Taskid}");
                                }
                            }
                        }
                        else if (task.State == (int)taskState.tsExecuting || task.State == (int)taskState.tsManuexecuting)
                        {
                            var spansecond = (task.Endtime - DateTime.Now).TotalSeconds;
                            if (spansecond > 0 && spansecond < _taskSchedulePreviousSeconds)
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
