using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OrleansDashboard.Abstraction
{
    public interface IGrainServiceDataBack
    {
        object GetGrainServiceData();
    }

    public class ServiceDataBackTest : IGrainServiceDataBack
    {
        public ServiceDataBackTest()
        { }

        public object GetGrainServiceData()
        {
            return 1;
        }
    }
}
