
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

    [Reentrant]
    class CheckScheduleTaskGrain : Grain, ICheckSchedule
    {
        private IDisposable _dispoScheduleTimer;
        private readonly RestClient _restClient;

        private readonly IMapper _mapper;
        public CheckScheduleTaskGrain(RestClient client, IMapper mapper)
        {
            _mapper = mapper;
            _restClient = client;
        }
        public Task<bool> CheckSyncAsync()
        {
            _dispoScheduleTimer = RegisterTimer(this.OnCheckTaskAsync, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3));
        }

        private async Task OnCheckTaskAsync(object type)
        { }
    }
}
