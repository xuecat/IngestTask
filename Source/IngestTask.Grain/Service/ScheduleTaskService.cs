

namespace IngestTask.Grain.Service
{
    using AutoMapper;
    using IngestTask.Abstraction.Grains;
    using IngestTask.Abstraction.Service;
    using IngestTask.Dto;
    using IngestTask.Tool;
    using IngestTask.Tools;
    using Orleans;
    using Orleans.Concurrency;
    using Orleans.Core;
    using Orleans.Runtime;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading.Tasks;
   
    [Reentrant]
    public class ScheduleTaskService : GrainService, IScheduleService
    {
        readonly IGrainFactory _grainFactory;
        private List<DispatchTask> _lstScheduleTask;
        private IDisposable _dispoScheduleTimer;
        private IMapper _mapper;
        private readonly RestClient _restClient;
        public ScheduleTaskService(IGrainIdentity id, Silo silo,
            Microsoft.Extensions.Logging.ILoggerFactory loggerFactory,
            IGrainFactory grainFactory, IMapper mapper, RestClient restClient)
            : base(id, silo, loggerFactory)
        {
            _dispoScheduleTimer = null;
            _grainFactory = grainFactory;
            _lstScheduleTask = new List<DispatchTask>();
            _mapper = mapper;
            _restClient = restClient;
        }
       
        public Task<int> AddTaskAsync(DispatchTask task)
        {
            if (task != null && _lstScheduleTask.Find(x => x.Taskid == task.Taskid) == null)
            {
                lock (_lstScheduleTask)
                {
                    _lstScheduleTask.Add(task);
                    _lstScheduleTask = _lstScheduleTask.OrderBy(x => x.Starttime).ToList();
                    return Task.FromResult(1);
                }
            }

            return Task.FromResult(0);
        }

        public Task<int> UpdateTaskAsync(DispatchTask task)
        {
            if (task != null)
            {
                var item = _lstScheduleTask.Find(x => x.Taskid == task.Taskid);
                if (item == null)
                {
                    lock (_lstScheduleTask)
                    {
                        _lstScheduleTask.Add(task);
                        _lstScheduleTask = _lstScheduleTask.OrderBy(x => x.Starttime).ToList();
                        return Task.FromResult(1);
                    }
                }
                else
                {
                    lock (_lstScheduleTask)
                    {
                        _lstScheduleTask = _lstScheduleTask.OrderBy(x => x.Starttime).ToList();
                        return Task.FromResult(1);
                    }
                }
            }

            return Task.FromResult(0);
        }

        public Task<int> CheckTaskListAsync(List<DispatchTask> task)
        {
            throw new NotImplementedException();
        }
        public override Task Init(IServiceProvider serviceProvider)
        {
            return base.Init(serviceProvider);
        }

        protected override Task StartInBackground()
        {
            _dispoScheduleTimer = RegisterTimer(this.OnScheduleTaskAsync, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            
            return Task.CompletedTask;
        }

        public override async Task Start()
        {
            await base.Start();
        }

        public override Task Stop()
        {
            return base.Stop();
        }


        private async Task OnScheduleTaskAsync(object type)
        {
            //如果判断到是周期任务，那么需要对它做分任务的处理
            //这个步骤挪到后台server去做

            //任务分发的时候要向请求通道是否存在，不存在提交一个自检请求

            var _lstRemoveTask = new List<DispatchTask>();
            foreach (var task in _lstScheduleTask)
            {
                if (task.StartOrStop <= 0 && task.SyncState == (int)syncState.ssNot )
                {
                    if (task.Tasktype == (int)TaskType.TT_PERIODIC
                        && task.State == (int)taskState.tsReady
                        && task.OldChannelid == 0)
                    {
                        var nowdate = DateTime.Now;
                        DateTime date = new DateTime(nowdate.Year, nowdate.Month, nowdate.Day, task.Starttime.Hour, task.Starttime.Minute, task.Starttime.Second);
                        if ((task.Starttime - DateTime.Now).TotalSeconds <=
                            ApplicationContext.Current.TaskSchedulePrevious)
                        {
                            var info = await _restClient.CreatePeriodcTaskAsync(task.Taskid);

                            if (info != null)
                            {
                                task.StartOrStop++;
                                _lstRemoveTask.Add(task);
                            }
                        }
                    }
                    else
                    {
                        if ((task.Starttime - DateTime.Now).TotalSeconds <=
                            ApplicationContext.Current.TaskSchedulePrevious && task.Endtime > DateTime.Now)
                        {
                            if (await _grainFactory.GetGrain<ITask>(task.Channelid.GetValueOrDefault())?.AddTaskAsync(_mapper.Map<TaskContent>(task)))
                            {
                                task.StartOrStop++;
                            }
                        }
                    }
                }
                else if (task.SyncState == (int)syncState.ssSync)
                {
                    var spansecond = (task.Endtime - DateTime.Now).TotalSeconds;
                    if ( spansecond > 0 && spansecond < ApplicationContext.Current.TaskSchedulePrevious)
                    {
                        if (await _grainFactory.GetGrain<ITask>(task.Channelid.GetValueOrDefault())?.StopTaskAsync(_mapper.Map<TaskContent>(task)))
                        {
                            task.StartOrStop++;
                            _lstRemoveTask.Add(task);
                        }
                    }
                }
                
            }

            if (_lstRemoveTask.Count > 0)
            {
                lock (_lstScheduleTask)
                {
                    foreach (var item in _lstScheduleTask)
                    {
                        _lstScheduleTask.Remove(item);
                    }
                }
            }

        }
    }
}
