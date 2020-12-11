namespace IngestTask.Server.IntegrationTest.Fixtures
{
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Orleans.Hosting;
    using Orleans.TestingHost;
    using IngestTask.Abstraction.Constants;
    using IngestTask.Tools.Msv;
    using IngestTask.Tool;
    using IngestTask.Abstraction.Service;
    using IngestTask.Abstraction.Grains;
    using IngestTask.Grain.Service;
    using IngestTask.Grain;
    using AutoMapper;
    using IngestTask.Tools;

    public class TestSiloConfigurator : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder) =>
#pragma warning disable CA1062 // 验证公共方法的参数
            siloBuilder
#pragma warning restore CA1062 // 验证公共方法的参数
                .ConfigureServices((context, services) =>
                {
                    services.AddScoped<MsvClientCtrlSDK>();
                    var client = new RestClient("http://172.16.0.205:9025", "http://172.16.0.205:10023");
                    services.AddSingleton<RestClient>(client);
                    //services.AddSingleton<IScheduleService, ScheduleTaskService>();
                    services.AddSingleton<IScheduleClient, ScheduleTaskClient>();

                    services.AddSingleton<ITaskHandlerFactory, TaskHandlerFactory>();
                    services.AddAutoMapper(typeof(GlobalProfile));

                }/*services.AddSingleton<ILoggerFactory>(x => new SerilogLoggerFactory())*/)
                .AddMemoryGrainStorageAsDefault()
                .AddMemoryGrainStorage("PubSubStore")
                .AddSimpleMessageStreamProvider(StreamProviderName.Default);

    }
}
