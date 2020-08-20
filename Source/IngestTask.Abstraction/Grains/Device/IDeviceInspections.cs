

namespace IngestTask.Abstraction.Grains
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    public interface IDeviceInspections
    {
        Task<int> CheckChannelSatetAsync();
    }
}
