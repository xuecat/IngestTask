namespace IngestTask.Grain
{
    using System;
    using System.Threading.Tasks;
    using Orleans;
    using Orleans.Concurrency;
    using IngestTask.Abstraction.Grains;

   
    [StatelessWorker]
    public class CounterStatelessGrain : Grain, ICounterStatelessGrain
    {
        private long count = 0;

        public Task IncrementAsync()
        {
            this.count += 1;
            return Task.CompletedTask;
        }

        public override Task OnActivateAsync()
        {
            // Timers are stored in-memory so are not resilient to nodes going down. They should be used for short
            // high-frequency timers their period should be measured in seconds.
            this.RegisterTimer(this.OnTimerTickAsync, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            return base.OnActivateAsync();
        }

        private Task OnTimerTickAsync(object arg)
        {
            var count = this.count;
            this.count = 0;
            var counter = this.GrainFactory.GetGrain<ICounterGrain>(Guid.Empty);
            return counter.AddCountAsync(count);
        }
    }
}
