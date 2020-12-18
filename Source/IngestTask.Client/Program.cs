namespace IngestTask.Client
{
    using System;
    using System.Threading.Tasks;
    using Orleans;
    using Orleans.Configuration;
    using Orleans.Hosting;
    using Orleans.Runtime;
    using Orleans.Streams;
    using IngestTask.Abstraction.Constants;
    using IngestTask.Abstraction.Grains;
    using Microsoft.Extensions.Logging;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net;

    public static class Program
    {
        public static async Task<int> Main()
        {
            try
            {
                var clusterClient = CreateClientBuilder()
                    //.UseLocalhostClustering()
                    //.UseAdoNetClustering(
                    //options =>
                    //{
                    //    options.Invariant = "MySql.Data.MySqlClient";
                    //    options.ConnectionString = "Server=172.16.0.205;Port=3307;Database=ingestdb;Uid=ingestdba;Pwd=sobeydba1;Pooling=true;minpoolsize=0;maxpoolsize=40;SslMode=none;";
                    //})
                    .UseStaticClustering(new IPEndPoint[] { new IPEndPoint(IPAddress.Parse("172.16.135.116"), 30000) })
                    .Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = "IngestTaskClusterId";
                        options.ServiceId = "IngestTaskServiceId";
                    })
                    .ConfigureLogging(logging => logging.AddConsole())
                    .Build();
                var namefaa = Console.ReadLine();
                await clusterClient.Connect().ConfigureAwait(false);

                // Set a trace ID, so that requests can be identified.
                RequestContext.Set("TraceId", Guid.NewGuid());

                using (var _httpClient = new RestClient("http://172.16.0.205:9025", "http://172.16.0.205:10023"))
                {
                    //var lsttask = await _httpClient.GetNeedSyncTaskListAsync().ConfigureAwait(true);
                    //if (lsttask != null && lsttask.Count >0)
                    //{
                        var taskitem = await _httpClient.GetTaskDBAsync(5203).ConfigureAwait(true);
                        var grain = clusterClient.GetGrain<IDispatcherGrain>(0);

                        await grain.AddTaskAsync(taskitem).ConfigureAwait(true);
                        var namefa = Console.ReadLine();
                    //}
                }

                var reminderGrain = clusterClient.GetGrain<IReminderGrain>(Guid.Empty);
                await reminderGrain.SetReminderAsync("Don't forget to say hello!").ConfigureAwait(false);

                var streamProvider = clusterClient.GetStreamProvider(StreamProviderName.Default);
                var saidHelloStream = streamProvider.GetStream<string>(Guid.Empty, StreamName.SaidHello);
                var saidHelloSubscription = await saidHelloStream.SubscribeAsync(OnSaidHelloAsync).ConfigureAwait(false);
                var reminderStream = streamProvider.GetStream<string>(Guid.Empty, StreamName.Reminder);
                var reminderSubscription = await reminderStream.SubscribeAsync(OnReminderAsync).ConfigureAwait(false);

#pragma warning disable CA1303 // Do not pass literals as localized parameters
                Console.WriteLine("What is your name?");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
                var name = Console.ReadLine();
                var helloGrain = clusterClient.GetGrain<IHelloGrain>(Guid.NewGuid());
                Console.WriteLine(await helloGrain.SayHelloAsync(name).ConfigureAwait(false));

                await saidHelloSubscription.UnsubscribeAsync().ConfigureAwait(false);
                await reminderSubscription.UnsubscribeAsync().ConfigureAwait(false);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception exception)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                Console.WriteLine(exception.ToString());
                return -1;
            }

            return 0;
        }

        private static Task OnSaidHelloAsync(string name, StreamSequenceToken token)
        {
            Console.WriteLine($"{name} said hello.");
            return Task.CompletedTask;
        }

        private static Task OnReminderAsync(string reminder, StreamSequenceToken token)
        {
            Console.WriteLine(reminder);
            return Task.CompletedTask;
        }

        private static IClientBuilder CreateClientBuilder() =>
            new ClientBuilder()
                .UseAzureStorageClustering(options => options.ConnectionString = "UseDevelopmentStorage=true")
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = Cluster.ClusterId;
                    options.ServiceId = Cluster.ServiceId;
                })
                .ConfigureApplicationParts(
                    parts => parts
                        .AddApplicationPart(typeof(ICounterGrain).Assembly)
                        .WithReferences())
                .AddSimpleMessageStreamProvider(StreamProviderName.Default);
    }
}
