using IngestTask.Abstraction.Grains;
using IngestTask.Dto;
using IngestTask.Grain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IngestTask.Server.Dispatcher
{
    public class TaskHandlerFactory : ITaskHandlerFactory
    {
        public TaskHandlerFactory()
        {
            RegisterHandler<SDITaskHandler>();
            RegisterHandler<VtrBatchTaskHandler>();
        }

        private List<Type> _taskHandlerList = new List<Type>();
        public ITaskHandler CreateInstance(TaskFullInfo task)
        {
            foreach (var item in _taskHandlerList)
            {
                var obj = item.GetMethod("IsHandler")?.Invoke(null, new object[] { task });
                if (obj != null && (bool)obj)
                {
                    return Activator.CreateInstance(item) as ITaskHandler;
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
