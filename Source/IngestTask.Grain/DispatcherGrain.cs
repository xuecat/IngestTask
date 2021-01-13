

namespace IngestTask.Grain
{
    using AutoMapper;
    using IngestTask.Abstraction.Grains;
    using IngestTask.Abstraction.Service;
    using IngestTask.Dto;
    using Microsoft.Extensions.Configuration;
    using Orleans;
    using Orleans.Concurrency;
    using Orleans.Internal;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;

    [StatelessWorker]
    public class DispatcherGrain : Grain, IDispatcherGrain
    {
        public int _taskSchedulePrevious { get; }
        private readonly IScheduleClient _scheduleClient;
        private IMapper _mapper;
        public DispatcherGrain(IScheduleClient dataServiceClient,
            IConfiguration configuration, IMapper mapper)
        {
            //_mapper = new Mapper();
            _mapper = mapper;
            _scheduleClient = dataServiceClient;
            _taskSchedulePrevious = configuration.GetSection("Task:TaskSchedulePrevious").Get<int>();
        }
        public Task SendAsync(Tuple<int, string>[] messages)
        {
            return Task.CompletedTask;
        }

        public async Task AddTaskAsync(DispatchTask task)
        {
            if (task != null)
            {
                string parsableaddress = string.Empty;

                if ((task.Starttime - DateTime.Now).TotalSeconds >
                    _taskSchedulePrevious || (task.Endtime - DateTime.Now).TotalSeconds < _taskSchedulePrevious)
                {
                    //提交开始监听
                    parsableaddress = await _scheduleClient.AddScheduleTaskAsync(task);
                }
                else
                {
                    var add = await GrainFactory.GetGrain<ITask>((long)task.Channelid).AddTaskAsync(_mapper.Map<TaskContent>(task));
                    if (!add)
                    {
                        parsableaddress = await _scheduleClient.AddScheduleTaskAsync(task);
                    }
                    else//添加结束的监听
                    {
                        task.SyncState = (int)syncState.ssSync;
                        parsableaddress = await _scheduleClient.AddScheduleTaskAsync(task);
                    }
                }

                //记录缓存
                await GrainFactory.GetGrain<ITaskCache>(0).AddTaskAsync(task, parsableaddress);
            }
        }

        //修改或者stop
        public async Task UpdateTaskAsync(DispatchTask task)
        {
            if (task != null)
            {
                string parsableaddress = string.Empty;
                var cached = await GrainFactory.GetGrain<ITaskCache>(0).IsCachedAsync(task.Taskid);//如果缓存过了说明有分发的server监听了，不能再分发了

                if ((task.Starttime - DateTime.Now).TotalSeconds >
                    _taskSchedulePrevious || (task.Endtime - DateTime.Now).TotalSeconds < _taskSchedulePrevious)
                {
                    //提交开始监听
                    if (!cached)
                        parsableaddress = await _scheduleClient.AddScheduleTaskAsync(task);
                }
                else
                {
                    var add = await GrainFactory.GetGrain<ITask>((long)task.Channelid).AddTaskAsync(_mapper.Map<TaskContent>(task));
                    if (!add)
                    {
                        if (!cached)
                            parsableaddress = await _scheduleClient.AddScheduleTaskAsync(task);
                    }
                    else//添加结束的监听
                    {
                        task.SyncState = (int)syncState.ssSync;
                        if (!cached)
                            parsableaddress = await _scheduleClient.AddScheduleTaskAsync(task);
                    }
                }

                string bakcparsableaddress = await GrainFactory.GetGrain<ITaskCache>(0).UpdateTaskAsync(task, parsableaddress);
                if (!string.IsNullOrEmpty(bakcparsableaddress))
                {
                    await _scheduleClient.RefreshAsync(bakcparsableaddress);
                }
            }
        }

        public async Task DeleteTaskAsync(DispatchTask task)
        {
            if (task != null)
            {
                string bakcparsableaddress = await GrainFactory.GetGrain<ITaskCache>(0).DeleteTaskAsync(task.Taskid);
                if (!string.IsNullOrEmpty(bakcparsableaddress))
                {
                    await _scheduleClient.RefreshAsync(bakcparsableaddress);
                }
                var add = await GrainFactory.GetGrain<ITask>((long)task.Channelid).StopTaskAsync(_mapper.Map<TaskContent>(task));
            }
        }

    }
}
