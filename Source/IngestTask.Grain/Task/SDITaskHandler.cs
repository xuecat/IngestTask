using AutoMapper;
using IngestTask.Abstraction.Grains;
using IngestTask.Abstraction.Service;
using IngestTask.Dto;
using IngestTask.Tool;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IngestTask.Grain
{
    public class SDITaskHandler : TaskHandlerBase
    {
        public SDITaskHandler()
            
        {

        }
        static public bool IsHandler(TaskFullInfo task)
        {
            return false;
        }
        public Task<int> HandleTaskAsync(TaskFullInfo task)
        {
            if (task.StartOrStop)
            {

                if (task.OpType != opType.otDel)
                {

                    if (task.TaskContent.TaskType == TaskType.TT_MANUTASK)
                    {
                        await UnlockTaskAsync(task.TaskContent.TaskId, taskState.tsExecuting, dispatchState.dpsDispatched, syncState.ssSync);
                        return task.TaskContent.TaskId;
                    }
                    else if (task.TaskContent.TaskType == TaskType.TT_TIEUP)
                    {
                        await HandleTieupTaskAsync(task.TaskContent);
                        return task.TaskContent.TaskId;
                    }

                    if (task.OpType == opType.otAdd)
                    {

                    }
                }



            }
        }
    }
}
