using IngestTask.Abstraction.Grains;
using IngestTask.Server.IntegrationTest.Fixtures;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace IngestTask.Server.IntegrationTest
{
    public class DeviceGrainTest : ClusterFixture
    {
        public DeviceGrainTest(ITestOutputHelper testOutputHelper)
           : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task AddDeviceAsync()
        {
            var devicegrain = this.Cluster.GrainFactory.GetGrain<IDeviceInspections>(0);
            var streamid = await devicegrain.IsChannelInvalidAsync(1).ConfigureAwait(true);

            await devicegrain.NotifyDeviceChangeAsync().ConfigureAwait(true);
        }
    }
}
