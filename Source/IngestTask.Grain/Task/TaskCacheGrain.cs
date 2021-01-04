using IngestTask.Abstraction.Grains;
using IngestTask.Dto;
using IngestTask.Tools;
using Orleans;
using Orleans.Concurrency;
using Orleans.Providers;
using OrleansDashboard.Abstraction;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace IngestTask.Grain
{
    [TraceGrain("IngestTask.Grain.TaskCacheGrain", TaskTraceEnum.TaskCache)]
    [Reentrant]
    //[StorageProvider(ProviderName = "MemoryStore")]
    public class TaskCacheGrain : Grain<List<DispatchTask>>, ITaskCache
    {
        public TaskCacheGrain()
        {
            
        }
        public override async Task OnActivateAsync()
        {
            await ReadStateAsync();
            await base.OnActivateAsync();
        }
        
        public async Task AddTaskAsync(DispatchTask task)
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
        }

        public async Task DeleteTaskAsync(int task)
        {
            if (task > 0)
            {
                int ncount =this.State.RemoveAll(x => x.Taskid == task);
                if (ncount > 0)
                {
                    await WriteStateAsync();
                }
                
            }
        }

        public async Task UpdateTaskAsync(DispatchTask task)
        {
            if (task != null)
            {
                var tkitem = this.State.Find(x => x.Taskid == task.Taskid);
                if (tkitem != null)
                {
                    ObjectTool.CopyObjectData(task, tkitem, "", BindingFlags.Public | BindingFlags.Instance);

                    await WriteStateAsync();
                }
            }
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
            var lstitem = this.State.FindAll(x => taskid.Contains(x.Taskid));
            return Task.FromResult(lstitem);
        }

        public Task<List<DispatchTask>> GetTaskListAsync()
        {
            return Task.FromResult(this.State);
        }
    }
}
