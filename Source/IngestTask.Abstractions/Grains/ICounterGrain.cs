namespace IngestTask.Abstractions.Grains
{
    using System.Threading.Tasks;
    using Orleans;

  
    public interface ICounterGrain : IGrainWithGuidKey
    {
        Task<long> AddCountAsync(long value);

        Task<long> GetCountAsync();
    }
}
