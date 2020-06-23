namespace IngestTask.Server.IntegrationTest
{
    using System;
    using System.Threading.Tasks;
    using IngestTask.Abstractions.Grains;
    using IngestTask.Server.IntegrationTest.Fixtures;
    using Xunit;
    using Xunit.Abstractions;

    public class CounterGrainTest : ClusterFixture
    {
        public CounterGrainTest(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
#pragma warning disable CA1707 // 标识符不应包含下划线
        public async Task AddCount_PassValue_ReturnsTotalCountAsync()
#pragma warning restore CA1707 // 标识符不应包含下划线
        {
            var grain = this.Cluster.GrainFactory.GetGrain<ICounterGrain>(Guid.Empty);

            var count = await grain.AddCountAsync(10L).ConfigureAwait(false);

            Assert.Equal(10L, count);
        }

        [Fact]
#pragma warning disable CA1707 // 标识符不应包含下划线
        public async Task GetCount_Default_ReturnsTotalCountAsync()
#pragma warning restore CA1707 // 标识符不应包含下划线
        {
            var grain = this.Cluster.GrainFactory.GetGrain<ICounterGrain>(Guid.Empty);

            var count = await grain.GetCountAsync().ConfigureAwait(false);

            Assert.Equal(0L, count);
        }
    }
}
