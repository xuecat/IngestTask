namespace IngestTask.Grain
{
    using System.Threading.Tasks;
    using Orleans;
    using Orleans.Concurrency;
    using IngestTask.Abstraction.Grains.HealthChecks;

    [StatelessWorker(1)]
    public class LocalHealthCheckGrain : Grain, ILocalHealthCheckGrain
    {
        public Task CheckAsync() => Task.CompletedTask;
    }
}
