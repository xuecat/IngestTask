using Orleans;
using System;
using System.Collections.Generic;
using System.Text;

namespace IngestTask.Abstraction.Grains
{
    public interface IDeviceChange : IGrainObserver
    {
        void ReceiveDeveiceChange(int chengetype, string message);
    }
}
