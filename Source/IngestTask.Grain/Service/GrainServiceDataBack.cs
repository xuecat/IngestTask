

namespace IngestTask.Grain.Service
{
    using Microsoft.Extensions.DependencyInjection;
    using Orleans.Services;
    using OrleansDashboard.Abstraction;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;

    public class GrainServiceDataBack : IGrainServiceDataBack
    {
#pragma warning disable IDE0044 // 添加只读修饰符
        private IServiceProvider _serviceprovider;
#pragma warning restore IDE0044 // 添加只读修饰符
        public GrainServiceDataBack(IServiceProvider services)
        {
            _serviceprovider = services;
            
        }
        public object GetGrainServiceData()
        {
            var _service = _serviceprovider.GetServices<IGrainService>();
            foreach (var item in _service)
            {
                if (item.GetType() == typeof(ScheduleTaskService))
                {
                    object info = ((ScheduleTaskService)item).GetDataBack();
                    return info;
                }
            }

            return null;
        }
    }
}
