

namespace IngestTask.Grain
{
    using IngestTask.Abstraction.Grains;
    using IngestTask.Dto;
    using IngestTask.Grain;
    using IngestTask.Tool;
    using IngestTask.Tool.Msv;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Orleans;
    using Orleans.Runtime;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    public class TaskHandlerFactory : ITaskHandlerFactory, ILifecycleParticipant<IGrainLifecycle>
    {
        private IServiceProvider _serviceProvider;
        private List<Type> _taskHandlerList = new List<Type>();

        public static TaskHandlerFactory Create(IServiceProvider services)
        {
            var taskfactory = new TaskHandlerFactory();
            taskfactory.Participate(services.GetRequiredService<IGrainActivationContext>().ObservableLifecycle);
            taskfactory._serviceProvider = services;
            return taskfactory;
        }

        public ITaskHandler CreateInstance(TaskFullInfo task)
        {
            foreach (var item in _taskHandlerList)
            {
                var obj = item.GetMethod("IsHandler")?.Invoke(null, new object[] { task });
                if (obj != null && (bool)obj)
                {
                    return Activator.CreateInstance(item, new object[] {
                        _serviceProvider.GetRequiredService<RestClient>(),
                        _serviceProvider.GetRequiredService<MsvClientCtrlSDK>(),
                        _serviceProvider.GetRequiredService<IConfiguration>() }) as ITaskHandler;
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
        private Task OnActivateAsync(CancellationToken ct)
        {
            RegisterHandler<NormalTaskHandler>();
            RegisterHandler<VtrBatchTaskHandler>();
            return Task.CompletedTask;
            // Do stuff
        }
        public void Participate(IGrainLifecycle lifecycle)
        {
            lifecycle.Subscribe<TaskHandlerFactory>(GrainLifecycleStage.Activate, OnActivateAsync);
        }
    }
}
