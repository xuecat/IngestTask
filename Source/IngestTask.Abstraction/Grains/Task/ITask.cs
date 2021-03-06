﻿using IngestTask.Dto;
using Orleans;
using Orleans.CodeGeneration;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IngestTask.Abstraction.Grains
{
    public interface ITask : IGrainWithIntegerKey
    {
        Task<List<DispatchTask>> GetCurrentTaskListAsync();

        
        Task<bool> AddTaskAsync(TaskContent task);
        Task<bool> StopTaskAsync(TaskContent task);
        Task<bool> StopTaskAsync(int taskid);
        Task<bool> JudgeTaskPriorityAsync(TaskContent taskcurrent, TaskContent taskcompare);
    }

    public interface ICheckSchedule : IGrainWithIntegerKey
    {
        Task<bool> StartCheckSyncAsync();
    }
}
