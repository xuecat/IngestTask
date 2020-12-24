
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

    

    [Reentrant]
    public class CheckScheduleTaskGrain : Grain<List<CheckTaskContent>>, ICheckSchedule
    {
        private IDisposable _dispoScheduleTimer;
        private readonly RestClient _restClient;
        readonly IGrainFactory _grainFactory;
        private IMapper _mapper;
        private int _resetTimes;

        public CheckScheduleTaskGrain(RestClient client, IGrainFactory grainFactory ,IMapper mapper)
        {
            _grainFactory = grainFactory;
            _restClient = client;
            _mapper = mapper;
            _resetTimes = 0;
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
                _resetTimes = 0;

                List<TaskContent> needsynctasklst = new List<TaskContent>();
                lsttask.ForEach(x => {
                    var findstatetask = State.Find(y => y.TaskId == x.TaskId);
                    if (findstatetask != null)
                    {
                        findstatetask.SyncTimes++;
                        if (findstatetask.SyncTimes>=2)
                        {
                            needsynctasklst.Add(findstatetask);
                        }
                    } 
                    else
                    {
                        State.Add(_mapper.Map<TaskContent,CheckTaskContent>(x, opt => opt.AfterMap((src, dst) => { dst.SyncTimes = 0; })));
                    }
                });

                if (needsynctasklst.Count > 0)
                {
                    foreach (var item in needsynctasklst)
                    {
                        //
                        await _grainFactory.GetGrain<IDispatcherGrain>(0).AddTaskAsync(await _restClient.GetTaskDBAsync(item.TaskId));
                        //await _grainFactory.GetGrain<ITask>(item.ChannelId).AddTaskAsync(item);
                    }
                }
            }
            else
            {
                if (_resetTimes > 10 && State.Count > 0)
                {
                    State.Clear();
                    _resetTimes = 0;
                }
                _resetTimes++;
            }
        }
    }
}
