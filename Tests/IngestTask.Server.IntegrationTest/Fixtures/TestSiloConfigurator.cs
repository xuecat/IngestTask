namespace IngestTask.Server.IntegrationTest.Fixtures
{
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Orleans.Hosting;
    using Orleans.TestingHost;
    using IngestTask.Abstraction.Constants;
    using IngestTask.Tool.Msv;
    using IngestTask.Tool;
    using IngestTask.Abstraction.Grains;
    using IngestTask.Grain.Service;
    using IngestTask.Grain;
    using AutoMapper;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Orleans.Runtime;
    using System;
    using Orleans.Runtime.Placement;

    public class TestSiloConfigurator : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder) =>
#pragma warning disable CA1062 // 验证公共方法的参数
            siloBuilder
#pragma warning restore CA1062 // 验证公共方法的参数
                .ConfigureServices((context, services) =>
                {
                    services.RemoveAll<RestClient>();

                    services.AddScoped<MsvClientCtrlSDK>();
                    var client = new RestClient(null, "http://172.16.0.205:9025", "http://172.16.0.205:10023");
                    services.AddSingleton<RestClient>(client);
                    //services.AddSingleton<IScheduleService, ScheduleTaskService>();
                    services.AddSingleton<IDeviceMonitorClient, DeviceMonitorClient>();

                    services.AddTransient<ITaskHandlerFactory, TaskHandlerFactory>(sp => TaskHandlerFactory.Create(sp));
                    services.AddAutoMapper(typeof(GlobalProfile));
                    services.AddSingletonNamedService<PlacementStrategy, ScheduleTaskPlacementStrategy>(nameof(ScheduleTaskPlacementStrategy));
                    services.AddSingletonKeyedService<Type, IPlacementDirector, ScheduleTaskPlacementSiloDirector>(typeof(ScheduleTaskPlacementStrategy));

                }/*services.AddSingleton<ILoggerFactory>(x => new SerilogLoggerFactory())*/)
                .AddMemoryGrainStorageAsDefault()
                .AddMemoryGrainStorage("PubSubStore")
                .AddGrainService<DeviceMonitorService>()
                .UseInMemoryReminderService()
                .AddSimpleMessageStreamProvider(StreamProviderName.Default);

    }
}
