namespace IngestTask.Abstraction.Grains
{
    using System.Threading.Tasks;
    using Orleans;

   
    public interface ICounterStatelessGrain : IGrainWithIntegerKey
    {
        Task IncrementAsync();
    }
}
