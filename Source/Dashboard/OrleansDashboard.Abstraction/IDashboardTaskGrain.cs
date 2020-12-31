using Orleans;
using Orleans.Concurrency;
using System;
using OrleansDashboard.Abstraction.Model;
using System.Threading.Tasks;

namespace OrleansDashboard.Abstraction
{

    public enum TaskTraceEnum
    {
        No,
        TaskExec,
        TaskCache,
        Device,
    }
    public interface IDashboardTaskGrain : IGrainWithIntegerKey
    {
        Task<object> GetTaskTrace(string grain, TaskTraceEnum type);
        
    }
}
