using IngestTask.Dto;
using System;
using System.Collections.Generic;
using System.Text;

namespace IngestTask.Abstraction.Grains
{
    public interface ITaskHandlerFactory
    {
        ITaskHandler CreateInstance(TaskFullInfo task, IServiceProvider services);
        bool RegisterHandler<T>();
    }
}
