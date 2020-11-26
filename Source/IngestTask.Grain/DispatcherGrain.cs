using IngestTask.Abstraction.Grains;
using IngestTask.Abstraction.Service;
using IngestTask.Dto;
using IngestTask.Tool;
using IngestTask.Tools;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IngestTask.Grain
{
    [StatelessWorker]
    class DispatcherGrain : IDispatcherGrain
    {
        private readonly IScheduleClient _scheduleClient;
        public DispatcherGrain(IScheduleClient dataServiceClient)
        {
            _scheduleClient = dataServiceClient;
        }
        public Task SendAsync(Tuple<int, string>[] messages)
        {

            return Task.CompletedTask;
        }

        public async Task AddTaskAsync(DispatchTask task)
        {
            if (task != null)
            {
                if ((task.Starttime - DateTime.Now).TotalSeconds >
                   ApplicationContext.Current.TaskSchedulePrevious)
                {
                    //提交
                    await _scheduleClient.AddScheduleTaskAsync(task);
                }
                else
                {
                }
                //归档
                //RaiseEvent(new TaskEvent() { OpType = opType.otAdd, TaskContentInfo = task });

            }
        }

    }
}
