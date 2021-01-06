
using IngestTask.Abstraction.Grains;
using IngestTask.Dto;
using Orleans;
using Orleans.Concurrency;
using OrleansDashboard.Abstraction;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IngestTestGrain
{
    public static class TestCalls
    {
       
        public static Task Make(IClusterClient client, CancellationTokenSource tokenSource)
        {
            return Task.Run(async () =>
            {
                var devicegrain = client.GetGrain<ITaskCache>(0);
                await devicegrain.AddTaskAsync(new IngestTask.Dto.DispatchTask() { 
                    Taskid = 11,
                    Taskguid = "wagnqiu",
                    Taskname = "wangqiu",
                    Tasktype = 0,
                    Channelid = 1,
                    Starttime = DateTime.Now,
                    Endtime = DateTime.Now,
                    OpType = 1,
                    DispatchState = 2,
                    State = 1,
                    SyncState = 2
                });

                var taskgrain = client.GetGrain<IDeviceInspections>(0);
                await taskgrain.GetChannelInfosAsync();

            });
        }
    }
}
