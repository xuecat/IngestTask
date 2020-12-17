

namespace IngestTask.Grain
{
    using AutoMapper;
    using IngestTask.Abstraction.Grains;
    using IngestTask.Abstraction.Service;
    using IngestTask.Dto;
    using Microsoft.Extensions.Configuration;
    using Orleans;
    using Orleans.Concurrency;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;

    [StatelessWorker]
    public class DispatcherGrain : Grain, IDispatcherGrain
    {
        public int _taskSchedulePrevious { get; }
        private readonly IScheduleClient _scheduleClient;
        readonly IGrainFactory _grainFactory;
        private IMapper _mapper;
        public DispatcherGrain(IScheduleClient dataServiceClient,
            IConfiguration configuration, IGrainFactory grainFactory, IMapper mapper)
        {
            //_mapper = new Mapper();
            _mapper = mapper;
            _grainFactory = grainFactory;
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
                //记录缓存
                await _grainFactory.GetGrain<ITaskCache>(0).AddTaskAsync(task);

                if ((task.Starttime - DateTime.Now).TotalSeconds >
                    _taskSchedulePrevious || (task.Endtime - DateTime.Now).TotalSeconds < _taskSchedulePrevious)
                {
                    //提交开始监听
                    await _scheduleClient.AddScheduleTaskAsync(task);
                }
                else
                {
                    var add = await _grainFactory.GetGrain<ITask>((long)task.Channelid).AddTaskAsync(_mapper.Map<TaskContent>(task));
                    if (!add)
                    {
                        await _scheduleClient.AddScheduleTaskAsync(task);
                    }
                    else//添加结束的监听
                    {
                        task.SyncState = (int)syncState.ssSync;
                        await _scheduleClient.AddScheduleTaskAsync(task);
                    }
                }
                

            }
        }

        //修改或者stop
        public async Task UpdateTaskAsync(DispatchTask task)
        {
            if (task != null)
            {
                await _grainFactory.GetGrain<ITaskCache>(0).UpdateTaskAsync(task);

                if ((task.Starttime - DateTime.Now).TotalSeconds >
                    _taskSchedulePrevious || (task.Endtime - DateTime.Now).TotalSeconds < _taskSchedulePrevious)
                {
                    //提交开始监听
                    await _scheduleClient.AddScheduleTaskAsync(task);
                }
                else
                {
                    var add = await _grainFactory.GetGrain<ITask>((long)task.Channelid).AddTaskAsync(_mapper.Map<TaskContent>(task));
                    if (!add)
                    {
                        await _scheduleClient.AddScheduleTaskAsync(task);
                    }
                    else//添加结束的监听
                    {
                        task.SyncState = (int)syncState.ssSync;
                        await _scheduleClient.AddScheduleTaskAsync(task);
                    }
                }
            }
        }

        public async Task DeleteTaskAsync(DispatchTask task)
        {
            if (task != null)
            {
                await _grainFactory.GetGrain<ITaskCache>(0).DeleteTaskAsync(task);
                var add = await _grainFactory.GetGrain<ITask>((long)task.Channelid).StopTaskAsync(_mapper.Map<TaskContent>(task));
            }
        }

    }
}
