

namespace IngestTask.Grain
{
    using IngestTask.Abstraction.Constants;
    using IngestTask.Abstraction.Grains;
    using IngestTask.Dto;
    using IngestTask.Tools;
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

    [TraceGrain("IngestTask.Grain.TaskCacheGrain", TaskTraceEnum.TaskCache)]
    //[StorageProvider(ProviderName = "MemoryStore")]
    public class ReminderTaskGrain : Grain<List<DispatchTask>>, IReminderTask, IRemindable
    {
        private readonly int _reminderTimerMinutes;

        private IGrainReminder _grainReminder;

        public ReminderTaskGrain(IConfiguration configuration)
        {
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

        private async Task<List<DispatchTask>> RecalculateReminderAsync()
        {
            DateTime mintime = DateTime.MaxValue;
            var lstTask = this.State;

            List<DispatchTask> lstbacktask = new List<DispatchTask>();
            for (int i = lstTask.Count - 1; i >= 0; i--)
            {
                bool bneddtimer = false;
                
                if (lstTask[i].Tasktype == (int)TaskType.TT_PERIODIC)
                {
                    if (lstTask[i].NewBegintime.AddMinutes(_reminderTimerMinutes) >= DateTime.Now)
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
                    if (lstTask[i].Starttime.AddMinutes(_reminderTimerMinutes) >= DateTime.Now)
                    {
                        bneddtimer = true;
                    }
                    else if (lstTask[i].Starttime < mintime)
                    {
                        mintime = lstTask[i].NewBegintime;
                    }
                }

                if (bneddtimer)
                {
                    lstbacktask.Add(lstTask[i]);
                    lstTask.Remove(lstTask[i]);
                }
            }

            if (mintime != DateTime.MaxValue)
            {
                var minspan = DateTime.Now - mintime.AddMinutes(-1 * _reminderTimerMinutes);
                _grainReminder = await RegisterOrUpdateReminder(Cluster.TaskReminder, TimeSpan.FromSeconds(1), minspan);
            }
            return lstbacktask;
        }

        public async Task ReceiveReminder(string reminderName, TickStatus status)
        {
            switch (reminderName)
            {
                case Cluster.TaskReminder:
                    {
                        var membership = await GrainFactory.GetGrain<IManagementGrain>(0).GetHosts(true);
                        var lsttimer = await RecalculateReminderAsync();
                        if (lsttimer != null && lsttimer.Count > 0)
                        {
                            foreach (var item in lsttimer)
                            {
                                await GrainFactory.GetGrain<IScheduleTaskGrain>(item.Taskid % membership.Count).AddScheduleTaskAsync(item);
                            }
                        }
                    } break;
            }
        }

        public async Task<int> AddTaskAsync(DispatchTask task)
        {
            var tkitem = this.State.Find(x => x.Taskid == task.Taskid);
            if (tkitem == null)
            {
                this.State.Add(task);
                await WriteStateAsync();
            }
            else
            {
                ObjectTool.CopyObjectData(task, tkitem, "", BindingFlags.Public | BindingFlags.Instance);

                await WriteStateAsync();
            }

            return task.Taskid;
        }

        public async Task<int> DeleteTaskAsync(int task)
        {
            if (task > 0)
            {
                string parsableaddress = string.Empty;
                int ncount =this.State.RemoveAll(x =>x.Taskid == task);

                if (ncount > 0)
                {
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
                    ObjectTool.CopyObjectData(task, tkitem, "", BindingFlags.Public | BindingFlags.Instance);
                    await WriteStateAsync();
                    return tkitem.Taskid;
                }
                else
                {
                    this.State.Add(task);
                    await WriteStateAsync();
                }
            }
            return 0;
        }
        public Task CompleteTaskAsync(List<int> taskid)
        {
            this.State.RemoveAll(x => taskid.Contains(x.Taskid));
            return Task.CompletedTask;
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
