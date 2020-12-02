using IngestTask.Abstraction.Grains;
using IngestTask.Dto;
using IngestTask.Tool;
using IngestTask.Tools.Msv;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IngestTask.Grain
{
    public class VtrBatchTaskHandler : TaskHandlerBase
    {
        public VtrBatchTaskHandler(RestClient rest, MsvClientCtrlSDK msv)
            :base(rest, msv)
        { }

        static public bool IsHandler(TaskFullInfo task)
        {
            if (task.TaskContent.CooperantType == CooperantType.emVTRBackup)
            {
                return true;
            }
            return false;
        }

        public override Task<int> HandleTaskAsync(TaskFullInfo task, ChannelInfo channel)
        {
            Logger.Info("NormalTaskHandler HandleTaskAsync");

           
            return Task.FromResult(0);
        }

        public override Task<int> StartTaskAsync(TaskFullInfo task, ChannelInfo channel)
        {
            throw new NotImplementedException();
        }

        public override Task<int> StopTaskAsync(TaskFullInfo task, ChannelInfo channel)
        {
            throw new NotImplementedException();
        }
    }
}
