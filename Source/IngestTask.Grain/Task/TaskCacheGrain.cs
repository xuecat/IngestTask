using IngestTask.Abstraction.Grains;
using IngestTask.Dto;
using IngestTask.Tools;
using Orleans;
using Orleans.Concurrency;
using Orleans.Providers;
using OrleansDashboard.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace IngestTask.Grain
{
    [TraceGrain("IngestTask.Grain.TaskCacheGrain", TaskTraceEnum.TaskCache)]
    [Reentrant]
    //[StorageProvider(ProviderName = "MemoryStore")]
    public class TaskCacheGrain : Grain<List<Tuple<DispatchTask, string>>>, ITaskCache
    {
        public TaskCacheGrain()
        {
            
        }
        public override async Task OnActivateAsync()
        {
            await ReadStateAsync();
            await base.OnActivateAsync();
        }
        
        public async Task AddTaskAsync(DispatchTask task, string parsableaddress)
        {
            var tkitem = this.State.Find(x => x.Item1.Taskid == task.Taskid);
            if (tkitem == null)
            {
                this.State.Add(new Tuple<DispatchTask, string>(task, parsableaddress));
                await WriteStateAsync();
            }
            else
            {
                ObjectTool.CopyObjectData(task, tkitem, "", BindingFlags.Public | BindingFlags.Instance);

                await WriteStateAsync();
            }
        }

        public async Task<string> DeleteTaskAsync(int task)
        {
            if (task > 0)
            {
                string parsableaddress = string.Empty;
                int ncount =this.State.RemoveAll(x =>
                {
                    if (x.Item1.Taskid == task)
                    {
                        parsableaddress = x.Item2;
                        return true;
                    }
                    return false;
                });

                if (ncount > 0)
                {
                    
                    await WriteStateAsync();
                    return parsableaddress;
                }
                
            }
            return string.Empty;
        }

        public async Task<string> UpdateTaskAsync(DispatchTask task, string parsableaddress)
        {
            if (task != null)
            {
                var tkitem = this.State.Find(x => x.Item1.Taskid == task.Taskid);
                if (tkitem != null)
                {
                    ObjectTool.CopyObjectData(task, tkitem.Item1, "", BindingFlags.Public | BindingFlags.Instance);
                    await WriteStateAsync();
                    return tkitem.Item2;
                }
                else
                {
                    this.State.Add(new Tuple<DispatchTask, string>(task, parsableaddress));
                    await WriteStateAsync();
                }
            }
            return string.Empty;
        }
        public Task CompleteTaskAsync(List<int> taskid)
        {
            this.State.RemoveAll(x => taskid.Contains(x.Item1.Taskid));
            return Task.CompletedTask;
        }
        public Task<DispatchTask> GetTaskAsync(int taskid)
        {
            var tkitem = this.State.Find(x => x.Item1.Taskid == taskid);
            return Task.FromResult(tkitem.Item1);
        }

        public Task<List<DispatchTask>> GetTaskListAsync(List<int> taskid)
        {
            var lstitem = this.State.Where(x => taskid.Contains(x.Item1.Taskid)).Select(y => y.Item1);
            if (lstitem != null)
            {
                return Task.FromResult(lstitem.ToList());
            }
            return Task.FromResult(default(List<DispatchTask>));
        }

        public Task<List<DispatchTask>> GetTaskListAsync()
        {
            var lstitem = this.State.Select(y => y.Item1);
            if (lstitem != null)
            {
                return Task.FromResult(lstitem.ToList());
            }
            return Task.FromResult(default(List<DispatchTask>));
        }

        public Task<bool> IsCachedAsync(int taskid)
        {
            var tkitem = this.State.Find(x => x.Item1.Taskid == taskid);
            if (tkitem != null && !string.IsNullOrEmpty(tkitem.Item2))
            {
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
    }
}
