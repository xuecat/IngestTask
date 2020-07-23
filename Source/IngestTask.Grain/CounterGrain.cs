namespace IngestTask.Grain
{
    using System.Threading.Tasks;
    using Orleans;
    using IngestTask.Abstraction.Grains;

    public class CounterGrain : Grain<long>, ICounterGrain
    {
        public async Task<long> AddCountAsync(long value)
        {
            this.State += value;
            try
            {
                await this.WriteStateAsync().ConfigureAwait(true);
            }
            catch (System.Exception e)
            {
                string s = e.Message;
                this.State++;
            }
            
            return this.State;
        }

        public Task<long> GetCountAsync() => Task.FromResult(this.State);
    }
}
