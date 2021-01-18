

namespace IngestTask.Grain
{
    using IngestTask.Abstraction.Grains;
    using IngestTask.Dto;
    using Orleans;
    using Orleans.Concurrency;
    using Orleans.Runtime;
    using OrleansDashboard.Abstraction;
    using OrleansDashboard.Abstraction.Model;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    [StatelessWorker]
    public class DashboardTaskGrain : Grain, IDashboardTaskGrain
    {
     
        public DashboardTaskGrain()
        {
        }

        public async Task<object> GetTaskTrace(string grain, TaskTraceEnum type)
        {
            switch (type)
            {
                case TaskTraceEnum.No:
                    break;
                case TaskTraceEnum.TaskExec:
                    return await GetExcuterTaskTraceAsync(grain).ConfigureAwait(true);
                case TaskTraceEnum.TaskCache:
                    return await GetChacheTaskTraceAsync();
                case TaskTraceEnum.Device:
                    return await GetDeviceTraceAsync();
                default:
                    break;
            }
            return null;
        }

        public async Task<List<DispatchTask>> GetChacheTaskTraceAsync()
        {
            var graininfo = GrainFactory.GetGrain<IReminderTask>(0);
            if (graininfo != null)
            {
                return await graininfo.GetTaskListAsync();
            }
            return null;
        }

        public async Task<List<ChannelInfo>> GetDeviceTraceAsync()
        {
            var graininfo = GrainFactory.GetGrain<IDeviceInspections>(0);
            if (graininfo != null)
            {
                return await graininfo.GetChannelInfosAsync();
            }
            return null;
        }

        public async Task<List<DispatchTask>> GetExcuterTaskTraceAsync(string grain)
        {
            var info = grain.Split(":");
            if (info.Length > 1 && GrainFactory != null)
            {
                var graininfo = GrainFactory.GetGrain<ITask>(int.Parse(info.ElementAt(1)));
                if (graininfo != null)
                {
                    return await graininfo.GetCurrentTaskListAsync();
                }
            }
            return null;
        }
    }
}
