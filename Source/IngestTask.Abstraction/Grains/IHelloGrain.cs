namespace IngestTask.Abstraction.Grains
{
    using System.Threading.Tasks;
    using Orleans;

    public interface IHelloGrain : IGrainWithGuidKey
    {
        Task<string> SayHelloAsync(string name);
    }
}
