﻿

namespace IngestTask.Grain
{
    using AutoMapper;
    using IngestTask.Abstraction.Grains;
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
        public int taskSchedulePrevious { get; }

        private IMapper _mapper;
        public DispatcherGrain(
            IConfiguration configuration, IMapper mapper)
        {
            //_mapper = new Mapper();
            _mapper = mapper;
            taskSchedulePrevious = configuration.GetSection("Task:TaskSchedulePrevious").Get<int>();
        }
        public Task SendAsync(Tuple<int, string>[] messages)
        {
            return Task.CompletedTask;
        }

        public async Task AddTaskAsync(DispatchTask task)
        {
            if (task != null)
            {
                if ((task.Starttime - DateTime.Now).TotalSeconds >
                    taskSchedulePrevious || (task.Endtime - DateTime.Now).TotalSeconds < taskSchedulePrevious)//开始时间之前，结束时间之后，有效范围外的
                {
                    //提交开始监听(监听会做有效时间判断)
                    await GrainFactory.GetGrain<IReminderTask>(0).AddTaskAsync(task);
                }
                else
                {
                    var add = await GrainFactory.GetGrain<ITask>((long)task.Channelid).AddTaskAsync(_mapper.Map<TaskContent>(task));
                    if (!add)
                    {
                        await GrainFactory.GetGrain<IReminderTask>(0).AddTaskAsync(task);
                    }
                    else//添加结束的监听
                    {
                        task.State = (int)taskState.tsExecuting;
                        await GrainFactory.GetGrain<IReminderTask>(0).AddTaskAsync(task);
                    }
                }

            }
        }

        //修改或者stop
        public async Task UpdateTaskAsync(DispatchTask task)
        {
            if (task != null)
            {
               
                if ((task.Starttime - DateTime.Now).TotalSeconds >
                    taskSchedulePrevious || (task.Endtime - DateTime.Now).TotalSeconds < taskSchedulePrevious)
                {
                    //提交开始监听
                    await GrainFactory.GetGrain<IReminderTask>(0).UpdateTaskAsync(task);
                }
                else
                {
                    var add = await GrainFactory.GetGrain<ITask>((long)task.Channelid).AddTaskAsync(_mapper.Map<TaskContent>(task));
                    if (!add)
                    {
                        await GrainFactory.GetGrain<IReminderTask>(0).UpdateTaskAsync(task);
                    }
                    else//添加结束的监听
                    {
                        task.State = (int)taskState.tsExecuting;
                        await GrainFactory.GetGrain<IReminderTask>(0).UpdateTaskAsync(task);
                    }
                }
            }
        }

        public async Task DeleteTaskAsync(DispatchTask task)
        {
            if (task != null)
            {
                await GrainFactory.GetGrain<IReminderTask>(0).DeleteTaskAsync(task.Taskid);
                await GrainFactory.GetGrain<ITask>((long)task.Channelid).StopTaskAsync(_mapper.Map<TaskContent>(task));
            }
        }

    }
}