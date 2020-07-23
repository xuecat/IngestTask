namespace IngestTask.Abstraction.Grains.HealthChecks
{
    using System.Threading.Tasks;
    using Orleans;

    public interface IStorageHealthCheckGrain : IGrainWithGuidKey
    {
        Task CheckAsync();
    }
}
