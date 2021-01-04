using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Xml.Linq;
using AutoMapper;
using IngestTask.Abstraction.Constants;
using IngestTask.Abstraction.Grains;
using IngestTask.Abstraction.Service;
using IngestTask.Tool;
using IngestTask.Tools;
using IngestTask.Tools.Msv;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Runtime;
using TestGrains;

// ReSharper disable MethodSupportsCancellation

namespace TestHost
{
    public static class Program
    {
        private static string CreateConfigURI(string str)
        {
            if (str.IndexOf("http:") >= 0 || str.IndexOf("https:") >= 0)
            {
                return str;
            }
            else
                return "http://" + str;
        }
        public static void Main(string[] args)
        {
            var siloPort = 11111;
            int gatewayPort = 30000;
            var siloAddress = IPAddress.Loopback;

            var silo =
                new SiloHostBuilder()
                    .UseDashboard(options =>
                    {
                        options.HostSelf = true;
                        options.HideTrace = false;
                    })
                     .ConfigureServices((context, services) =>
                     {
                         services.RemoveAll<RestClient>();

                         services.AddScoped<MsvClientCtrlSDK>();
                         var client = new RestClient("http://172.16.0.205:9025", "http://172.16.0.205:10023");
                         services.AddSingleton<RestClient>(client);
                         //services.AddSingleton<IScheduleService, ScheduleTaskService>();
                         //services.AddSingleton<IScheduleClient, ScheduleTaskClient>();

                         //services.AddSingleton<ITaskHandlerFactory, TaskHandlerFactory>();
                         services.AddAutoMapper(typeof(GlobalProfile));

                     })
                    .UseDevelopmentClustering(options => options.PrimarySiloEndpoint = new IPEndPoint(siloAddress, siloPort))
                    .UseInMemoryReminderService()
                    .ConfigureEndpoints(siloAddress, siloPort, gatewayPort)
                    .Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = "helloworldcluster";
                        options.ServiceId = "1";
                    })
                    .ConfigureApplicationParts(appParts => appParts.AddApplicationPart(typeof(TestCalls).Assembly))
                    .ConfigureApplicationParts(appParts => appParts.AddApplicationPart(typeof(IngestTestGrain.TestCalls).Assembly))
                    .ConfigureApplicationParts(parts => parts.AddFromApplicationBaseDirectory())
                    .ConfigureLogging(builder =>
                    {
                        builder.AddConsole();
                    })
                    .ConfigureAppConfiguration((config) =>
                    {
                        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);

                        var dic = new Dictionary<string, string>();

                        string fileName = "publicsetting.xml";
                        string path = string.Empty;
                        if ((Environment.OSVersion.Platform == PlatformID.Unix) || (Environment.OSVersion.Platform == PlatformID.MacOSX))
                        {
                            //str = string.Format(@"{0}/{1}", System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase, fileName);
                            path = '/' + fileName;
                        }
                        else
                        {
                            path = AppDomain.CurrentDomain.BaseDirectory.Replace("\\", "/") + "/" + fileName;
                        }

                        if (File.Exists(path))
                        {
                            XDocument xd = new XDocument();
                            xd = XDocument.Load(path);
                            XElement ps = xd.Element("PublicSetting");
                            XElement sys = ps.Element("System");

                            string vip = sys.Element("Sys_VIP").Value;
                            dic.Add("VIP", vip);
                            dic.Add("IngestDBSvr", CreateConfigURI(sys.Element("IngestDBSvr").Value));
                            dic.Add("IngestDEVCTL", CreateConfigURI(sys.Element("IngestDEVCTL").Value));
                            dic.Add("CMWindows", CreateConfigURI(sys.Element("CMserver_windows").Value));
                            dic.Add("CMServer", CreateConfigURI(sys.Element("CMServer").Value));
                            config.AddInMemoryCollection(dic);
                        }
                    })
                    .AddMemoryGrainStorageAsDefault()
                .AddMemoryGrainStorage("PubSubStore")
                //.AddGrainService<ScheduleTaskService>()
                .AddSimpleMessageStreamProvider(StreamProviderName.Default)
                    .Build();

            silo.StartAsync().Wait();

            var client =
                new ClientBuilder()
                    .UseStaticClustering(options => options.Gateways.Add((new IPEndPoint(siloAddress, gatewayPort)).ToGatewayUri()))
                    .Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = "helloworldcluster";
                        options.ServiceId = "1";
                    })
                    .ConfigureApplicationParts(appParts => appParts.AddApplicationPart(typeof(TestCalls).Assembly))
                    .ConfigureApplicationParts(appParts => appParts.AddApplicationPart(typeof(IngestTestGrain.TestCalls).Assembly))
                    .ConfigureApplicationParts(appParts => appParts.AddApplicationPart(typeof(IngestTestGrain.TestCalls).Assembly))
                    .ConfigureApplicationParts(parts => parts.AddFromApplicationBaseDirectory())
                    .ConfigureLogging(builder =>
                    {
                        builder.AddConsole();
                    })
                    .Build();

            client.Connect().Wait();

            var cts = new CancellationTokenSource();

            TestCalls.Make(client, cts);
            IngestTestGrain.TestCalls.Make(client, cts);

            Console.WriteLine("Press key to exit...");
            Console.ReadLine();

            cts.Cancel();

            silo.StopAsync().Wait();
        }
    }
}