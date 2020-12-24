

namespace IngestTask.Grain.Service
{
    using AutoMapper;
    using IngestTask.Abstraction.Grains;
    using IngestTask.Abstraction.Service;
    using IngestTask.Dto;
    using IngestTask.Tool;
    using IngestTask.Tools;
    using Microsoft.Extensions.Configuration;
    using NLog;
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
        private readonly Sobey.Core.Log.ILogger Logger;
        public IConfiguration Configuration { get; }

        private int _timerMinutesTimes;
        public ScheduleTaskService(IServiceProvider services, IGrainIdentity id, Silo silo,
            Microsoft.Extensions.Logging.ILoggerFactory loggerFactory,
            IGrainFactory grainFactory, IMapper mapper, RestClient restClient, IConfiguration configuration)
            : base(id, silo, loggerFactory)
        {
            Logger = Sobey.Core.Log.LoggerManager.GetLogger("ScheduleService");
            _dispoScheduleTimer = null;
            _grainFactory = grainFactory;
            _lstScheduleTask = new List<DispatchTask>();
            _mapper = mapper;
            _restClient = restClient;
            Configuration = configuration;
            _timerMinutesTimes = 1;
        }
       
        
        public Task<int> AddScheduleTaskAsync(DispatchTask task)
        {
            if (task != null && _lstScheduleTask.Find(x => x.Taskid == task.Taskid) == null)
            {
                lock (_lstScheduleTask)
                {
                    _lstScheduleTask.Add(task);
                    if (_dispoScheduleTimer == null)
                    {
                        _dispoScheduleTimer = RegisterTimer(this.OnScheduleTaskAsync, null, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(_timerMinutesTimes));
                    }
                    
                    return Task.FromResult(1);
                }
            }

            return Task.FromResult(0);
        }

        public Task<int> RemoveScheduleTaskAsync(DispatchTask task)
        {
            if (task != null)
            {
                lock (_lstScheduleTask)
                {
                    _lstScheduleTask.RemoveAll(x => x.Taskid == task.Taskid);
                    
                }
                if (_lstScheduleTask.Count == 0 && _dispoScheduleTimer != null)
                {
                    _dispoScheduleTimer.Dispose();
                    _dispoScheduleTimer = null;
                }
                return Task.FromResult(1);
            }

            return Task.FromResult(0);
        }

        public override Task Init(IServiceProvider serviceProvider)
        {
            return base.Init(serviceProvider);
        }

        protected override async Task StartInBackground()
        {
           
            var device = _grainFactory.GetGrain<IDeviceInspections>(0);
            await device.CheckChannelSatetAsync();

        }

        public override async Task Start()
        {
            await base.Start();

        }

        public override Task Stop()
        {
            _lstScheduleTask.Clear();
            return base.Stop();
        }


        private async Task OnScheduleTaskAsync(object type)
        {
            //如果判断到是周期任务，那么需要对它做分任务的处理
            //这个步骤挪到后台server去做

            //任务分发的时候要向请求通道是否存在，不存在提交一个自检请求

            int test = Configuration.GetSection("Task:TaskSchedulePrevious").Get<int>();
            var _lstRemoveTask = new List<DispatchTask>();

            if (_lstScheduleTask.Count > 0)
            {
                //获取最新缓存，保证中途修改，删除了也不会更新
                var lsttask = await _grainFactory.GetGrain<ITaskCache>(0).GetTaskListAsync(_lstScheduleTask.Select(x => x.Taskid).ToList());
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
                                var nowdate = DateTime.Now;
                                DateTime date = new DateTime(nowdate.Year, nowdate.Month, nowdate.Day, task.Starttime.Hour, task.Starttime.Minute, task.Starttime.Second);
                                if ((task.Starttime - DateTime.Now).TotalSeconds <=
                                    test)
                                {
                                    var info = await _restClient.CreatePeriodcTaskAsync(task.Taskid);

                                    if (info != null)
                                    {
                                        _lstRemoveTask.Add(task);
                                    }
                                }
                            }
                            else
                            {
                                if ((task.Starttime - DateTime.Now).TotalSeconds <=
                                    test && task.Endtime > DateTime.Now)
                                {
                                    await _grainFactory.GetGrain<ITask>(task.Channelid.GetValueOrDefault())?.AddTaskAsync(_mapper.Map<TaskContent>(task));
                                }
                            }
                        }
                        else if (task.State == (int)taskState.tsExecuting || task.State == (int)taskState.tsManuexecuting)
                        {
                            var spansecond = (task.Endtime - DateTime.Now).TotalSeconds;
                            if (spansecond > 0 && spansecond < test)
                            {
                                if (await _grainFactory.GetGrain<ITask>(task.Channelid.GetValueOrDefault())?.StopTaskAsync(_mapper.Map<TaskContent>(task)))
                                {
                                    _lstRemoveTask.Add(task);
                                }
                            }
                        }

                    }

                    if (_lstRemoveTask.Count > 0)
                    {
                        lock (_lstScheduleTask)
                        {
                            if (_lstScheduleTask.Count >0)
                            {
                                var lst = _lstRemoveTask.Select(x => x.Taskid).ToList();
                                _lstScheduleTask.RemoveAll(x => lst.Contains(x.Taskid));
                            }
                            
                        }

                        await _grainFactory.GetGrain<ITaskCache>(0).CompleteTaskAsync(_lstRemoveTask.Select(x => x.Taskid).ToList());
                    }
                }
                else
                {
                    lock (_lstScheduleTask)
                    {
                        _lstScheduleTask.Clear();
                        Logger.Info("clear all scheduletask");
                    }
                }
                
            }

            if (_lstScheduleTask.Count == 0 && _dispoScheduleTimer != null)
            {
                _dispoScheduleTimer.Dispose();
                _dispoScheduleTimer = null;
            }

        }
    }
}
