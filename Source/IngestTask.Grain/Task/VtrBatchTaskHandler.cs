using IngestTask.Abstraction.Grains;
using IngestTask.Dto;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IngestTask.Grain
{
    public class VtrBatchTaskHandler : TaskHandlerBase
    {
        static public bool IsHandler(TaskFullInfo task)
        {
            return false;
        }
        public Task<int> HandleTaskAsync(TaskFullInfo task)
        {
            throw new NotImplementedException();
        }
    }
}
