using Orleans;
using System;
using System.Collections.Generic;
using System.Text;

namespace IngestTask.Abstraction.Service
{
    public interface ITaskViewer :IGrainObserver
    {
        void NewTask();
    }
}
