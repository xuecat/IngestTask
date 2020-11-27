
namespace IngestTask.Grain
{
    using AutoMapper;
    using IngestTask.Abstraction.Grains;
    using IngestTask.Tool;
    using Orleans.Concurrency;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using Orleans;
    using IngestTask.Dto;
    using System.Linq;

    [Serializable]
    public class CheckTaskContent : TaskContent
    {
        public int SyncTimes { get; set; }
    }

    [Reentrant]
    class CheckScheduleTaskGrain : Grain<List<CheckTaskContent>>, ICheckSchedule
    {
        private IDisposable _dispoScheduleTimer;
        private readonly RestClient _restClient;
        readonly IGrainFactory _grainFactory;

        public CheckScheduleTaskGrain(RestClient client, IGrainFactory grainFactory /*IMapper mapper*/)
        {
            _grainFactory = grainFactory;
            _restClient = client;
        }
        public Task<bool> StartCheckSyncAsync()
        {
            _dispoScheduleTimer = RegisterTimer(this.OnCheckTaskAsync, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3));
            return Task.FromResult(true);
        }

        /*
         * 如果俩次同步检测到，都还没变化状态，说明调度模式肯定延时或者丢失数据了，因为第一次检测到需要同步就应该被执行器修改状态的
         * 它应该直接向执行器请求。
         */
        private async Task OnCheckTaskAsync(object type)
        {
            var lsttask = await _restClient.GetNeedSyncTaskListAsync();
            if (lsttask != null && lsttask.Count > 0)
            {
                var lst = lsttask.Select(x => x.TaskId).ToList();
                
                var findlst = State.FindAll(a => lst.Contains(a.TaskId));
                findlst.ForEach(x => x.SyncTimes++);


            }
        }
    }
}
