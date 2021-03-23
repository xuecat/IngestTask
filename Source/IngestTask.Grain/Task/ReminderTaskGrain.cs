

namespace IngestTask.Grain
{
    using IngestTask.Abstraction.Constants;
    using IngestTask.Abstraction.Grains;
    using IngestTask.Dto;
    using IngestTask.Tool;
    using Microsoft.Extensions.Configuration;
    using Orleans;
    using Orleans.Concurrency;
    using Orleans.Providers;
    using Orleans.Runtime;
    using OrleansDashboard.Abstraction;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    [TraceGrain("IngestTask.Grain.ReminderTaskGrain", TaskTraceEnum.TaskCache)]
    //[StorageProvider(ProviderName = "MemoryStore")]
    public class ReminderTaskGrain : Grain<List<DispatchTask>>, IReminderTask, IRemindable
    {
        private readonly int _reminderTimerMinutes;

        private TimeSpan _reminderCurPeriod;
        private IGrainReminder _grainReminder;

        private bool _syncScheduled;
        public ReminderTaskGrain(IConfiguration configuration)
        {
            _syncScheduled = false;
            _reminderCurPeriod = TimeSpan.MaxValue;
            _grainReminder = null;
            _reminderTimerMinutes = configuration.GetSection("Task:TaskSchedulePreviousTimer").Get<int>();
        }
        public override async Task OnActivateAsync()
        {
            await ReadStateAsync();
            await base.OnActivateAsync();
        }

        public override async Task OnDeactivateAsync()
        {
            if (_grainReminder != null)
            {
                await UnregisterReminder(_grainReminder);
            }
            await base.OnDeactivateAsync();
        }

        private async ValueTask<bool> RecalculateReminderAsync()
        {
            DateTime mintime = DateTime.MaxValue;
            var lstTask = this.State;

            List<DispatchTask> lstbacktask = new List<DispatchTask>();
            for (int i = lstTask.Count - 1; i >= 0; i--)
            {
                bool bneddtimer = false;
                
                if (lstTask[i].Tasktype == (int)TaskType.TT_PERIODIC)
                {
                    if (lstTask[i].NewBegintime <= DateTime.Now.AddMinutes(_reminderTimerMinutes))
                    {
                        bneddtimer = true;
                    }
                    else if (lstTask[i].NewBegintime < mintime)
                    {
                        mintime = lstTask[i].NewBegintime;
                    }
                }
                else
                {
                    if (lstTask[i].Starttime <= DateTime.Now.AddMinutes(_reminderTimerMinutes))
                    {
                        bneddtimer = true;
                    }
                    else if (lstTask[i].Starttime < mintime)
                    {
                        mintime = lstTask[i].Starttime;
                    }
                }

                if (bneddtimer)
                {
                    lstbacktask.Add(lstTask[i]);
                    lock (this.State)
                    {
                        this.State.Remove(lstTask[i]);
                    }
                    
                }
            }

            if (mintime != DateTime.MaxValue)
            {
                var nowtime = DateTime.Now;
                TimeSpan minspan ;
                if (nowtime < mintime.AddMinutes(-1 * _reminderTimerMinutes))
                {
                    minspan = mintime.AddMinutes(-1 * (_reminderTimerMinutes-1)) - nowtime;
                    if (minspan != _reminderCurPeriod)
                    {
                        _grainReminder = await RegisterOrUpdateReminder(Cluster.TaskReminder, TimeSpan.FromSeconds(1), minspan);
                    }
                }
            }
            
            if (lstbacktask != null && lstbacktask.Count > 0)
            {
                var membership = await GrainFactory.GetGrain<IManagementGrain>(0).GetHosts(true);
                foreach (var item in lstbacktask)
                {
                    await GrainFactory.GetGrain<IScheduleTaskGrain>(item.Taskid % membership.Count).AddScheduleTaskAsync(item);
                }

                await WriteStateAsync();
                return true;
            }
            return false;
        }

        public async Task ReceiveReminder(string reminderName, TickStatus status)
        {
            switch (reminderName)
            {
                case Cluster.TaskReminder:
                    {
                        _reminderCurPeriod = status.Period;
                        await RecalculateReminderAsync();
                    } break;
            }
        }

        public async Task<int> AddTaskAsync(DispatchTask task)
        {
            var tkitem = this.State.Find(x => x.Taskid == task.Taskid);
            if (tkitem == null)
            {
                lock (this.State)
                {
                    this.State.Add(task);
                }
                if (!await RecalculateReminderAsync())
                    await WriteStateAsync();
            }
            else
            {
                lock (this.State)
                {
                    ObjectTool.CopyObjectData(task, tkitem, "", BindingFlags.Public | BindingFlags.Instance);
                }
                if (!await RecalculateReminderAsync())
                    await WriteStateAsync();
            }

            return task.Taskid;
        }

        public async Task<int> DeleteTaskAsync(int task)
        {
            if (task > 0)
            {
                string parsableaddress = string.Empty;
                int ncount = 0;
                lock (this.State)
                {
                    ncount = this.State.RemoveAll(x => x.Taskid == task);
                }

                if (ncount > 0)
                {
                    if (!await RecalculateReminderAsync())
                        await WriteStateAsync();
                    return task;
                }
                
            }
            return 0;
        }

        public async Task<int> UpdateTaskAsync(DispatchTask task)
        {
            if (task != null)
            {
                var tkitem = this.State.Find(x => x.Taskid == task.Taskid);
                if (tkitem != null)
                {
                    lock (this.State)
                    {
                        ObjectTool.CopyObjectData(task, tkitem, "", BindingFlags.Public | BindingFlags.Instance);
                    }
                    if (!await RecalculateReminderAsync())
                        await WriteStateAsync();
                    return tkitem.Taskid;
                }
                else
                {
                    lock (this.State)
                    {
                        this.State.Add(task);
                    }
                    if (!await RecalculateReminderAsync())
                        await WriteStateAsync();
                }
            }
            return 0;
        }

        
        public Task<DispatchTask> GetTaskAsync(int taskid)
        {
            var tkitem = this.State.Find(x => x.Taskid == taskid);
            return Task.FromResult(tkitem);
        }

        public Task<List<DispatchTask>> GetTaskListAsync(List<int> taskid)
        {
            var lstitem = this.State.Where(x => taskid.Contains(x.Taskid));
            if (lstitem != null)
            {
                return Task.FromResult(lstitem.ToList());
            }
            return Task.FromResult(default(List<DispatchTask>));
        }

        public async Task SyncScheduleTaskAsync(List<DispatchTask> lsttask)
        {
            if (_syncScheduled)
            {
                return;
            }

            _syncScheduled = true;//多节点只同步一次
            if (this.State.Count > 0)
            {
                var lsttaskid = this.State.Select(x => x.Taskid);
                lsttask.RemoveAll(x => lsttaskid.Contains(x.Taskid));
            }

            if (lsttask.Count > 0)
            {
                lock (this.State)
                {
                    this.State.AddRange(lsttask);
                }

                if (!await RecalculateReminderAsync())
                    await WriteStateAsync();

            }
        }

        [NoProfiling]
        public Task<List<DispatchTask>> GetTaskListAsync()
        {
            return Task.FromResult(this.State.ToList());
        }

        public Task<bool> IsCachedAsync(int taskid)
        {
            var tkitem = this.State.Find(x => x.Taskid == taskid);
            if (tkitem != null)
            {
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
    }
}
