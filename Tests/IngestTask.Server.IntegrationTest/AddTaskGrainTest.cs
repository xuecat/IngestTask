namespace IngestTask.Server.IntegrationTest
{
    using System;
    using System.Threading.Tasks;
    using IngestTask.Abstraction.Grains;
    using IngestTask.Server.IntegrationTest.Fixtures;
    using Xunit;
    using Xunit.Abstractions;

    public class AddTaskGrainTest : ClusterFixture
    {
        public AddTaskGrainTest(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task AddTaskAsync()
        {
            var devicegrain = this.Cluster.GrainFactory.GetGrain<IDeviceInspections>(0);
            var streamid = await devicegrain.JoinAsync(1).ConfigureAwait(true);

            var grain = this.Cluster.GrainFactory.GetGrain<IDispatcherGrain>(0);
            Dto.DispatchTask task = new Dto.DispatchTask();
            task.Backtype = 0;
            task.Backupvtrid = -1;
            task.Category = "A";
            task.Channelid = 2;
            task.DispatchState = 2;
            task.Starttime = DateTime.Now;
            task.Endtime = DateTime.Now.AddMinutes(5);
            task.OldChannelid = 0;
            task.OpType = 0;
            task.Recunitid = 0;
            task.Signalid = 10;
            task.State = 0;
            task.SyncState = 0;
            task.Taskguid = Guid.NewGuid().ToString("N");
            task.Taskid = 41;
            task.Taskname = "test";
            task.Tasktype = 1;
            task.Usercode = "2afee3ce8dd64d81afea0fa59841941c";
            await grain.AddTaskAsync(task).ConfigureAwait(true);
            //var grain = this.Cluster.GrainFactory.GetGrain<ICounterStatelessGrain>(0L);
            //var counterGrain = this.Cluster.GrainFactory.GetGrain<ICounterGrain>(Guid.Empty);

            //await grain.IncrementAsync().ConfigureAwait(false);
            //var countBefore = await counterGrain.GetCountAsync().ConfigureAwait(false);

            //Assert.Equal(0L, countBefore);

            //await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);

            //var countAfter = await counterGrain.GetCountAsync().ConfigureAwait(false);

            //Assert.Equal(1L, countAfter);
        }
    }
}
