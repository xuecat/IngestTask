

namespace IngestTask.Grain
{
    using IngestTask.Abstraction.Grains;
    using IngestTask.Dto;
    using IngestTask.Grain;
    using IngestTask.Tool;
    using IngestTask.Tools.Msv;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    public class TaskHandlerFactory : ITaskHandlerFactory
    {
        public TaskHandlerFactory()
        {
            RegisterHandler<NormalTaskHandler>();
            RegisterHandler<VtrBatchTaskHandler>();
        }

        private List<Type> _taskHandlerList = new List<Type>();
        public ITaskHandler CreateInstance(TaskFullInfo task, IServiceProvider services)
        {
            foreach (var item in _taskHandlerList)
            {
                var obj = item.GetMethod("IsHandler")?.Invoke(null, new object[] { task });
                if (obj != null && (bool)obj)
                {
                    
                    return Activator.CreateInstance(item, new object[] { 
                        services.GetRequiredService<RestClient>(),
                        services.GetRequiredService<MsvClientCtrlSDK>(),
                        services.GetRequiredService<IConfiguration>() }) as ITaskHandler;
                }
               
            }
            return null;
        }

        public bool RegisterHandler<T>()
        {
            if (_taskHandlerList != null && _taskHandlerList.Find(a=> a.Name == typeof(T).Name) == null)
            {
                _taskHandlerList.Add(typeof(T));
                return true;
            }
            return false;
        }
    }
}
