namespace IngestTask.Server
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using Orleans;
    using Orleans.Configuration;
    using Orleans.Hosting;
    using Orleans.Runtime;
    using Orleans.Statistics;
    using IngestTask.Abstraction.Constants;
    using IngestTask.Server.Options;

    using Sobey.Core.Log;
    using System.Globalization;
    using IngestTask.Grain;
    using IngestTask.Tool.Msv;
    using IngestTask.Tool;
    using Orleans.Serialization.ProtobufNet;
    using IngestTask.Grain.Service;
    using IngestTask.Abstraction.Grains;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using System.Threading;
    using System.Runtime.Loader;
    using OrleansDashboard;
    using Orleans.Runtime.Placement;
    using System.Net.Http;
    using OrleansDashboard.Abstraction;
    using System.Net;
    using System.Linq;
    using System.Net.Sockets;

    public static class Program
    {
        public static Task<int> Main(string[] args) => LogAndRunAsync(CreateHostBuilder(args).Build());

        private static Sobey.Core.Log.ILogger ExceptionLogger = null;
        private static Sobey.Core.Log.ILogger StartLogger = null;
        private static int siloStopping = 0;
        public static async Task<int> LogAndRunAsync(IHost host)
        {
            if (host is null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;

            host.Services.GetRequiredService<IHostEnvironment>().ApplicationName =
                Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>().Product;

            CreateLogger(host);
            try
            {
               
                var resetEvent = new ManualResetEvent(false);
                ConfigureShutdown(host, resetEvent);

#pragma warning disable CA1303 // 请不要将文本作为本地化参数传递
                StartLogger.Info("Started application");
                Console.WriteLine("Started application");
                Console.WriteLine("dns name:" + Dns.GetHostName());
                await host.RunAsync().ConfigureAwait(false);
                StartLogger.Info("Stopped application");
                Console.WriteLine("Stopped application");
#pragma warning restore CA1303 // 请不要将文本作为本地化参数传递

                resetEvent.WaitOne();
                resetEvent.Dispose();

                return 0;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception exception)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                StartLogger.Error(exception.Message + "Application terminated unexpectedly");
                return 1;
            }

        }

        private static void CurrentDomain_FirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            if (ExceptionLogger != null)
            {
                System.Net.Sockets.SocketException tempExcption = null;
                if (e.Exception is System.Net.Sockets.SocketException)
                {
                    tempExcption = (System.Net.Sockets.SocketException)e.Exception;
                }

                if (tempExcption == null || (tempExcption.ErrorCode != 125 && tempExcption.ErrorCode != 111 && tempExcption.ErrorCode != 104))
                {
                    ExceptionLogger.Error("Exception: {0} ", e.Exception.ToString());
                }
            }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (ExceptionLogger != null)
            {
                System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace();
                System.Diagnostics.StackFrame[] sfs = st.GetFrames();
                //过虑的方法名称,以下方法将不会出现在返回的方法调用列表中
                string _filterdName = "ResponseWrite,ResponseWriteError,";
                string _fullName = string.Empty, _methodName = string.Empty;
                for (int i = 1; i < sfs.Length; ++i)
                {
                    //非用户代码,系统方法及后面的都是系统调用，不获取用户代码调用结束
                    if (System.Diagnostics.StackFrame.OFFSET_UNKNOWN == sfs[i].GetILOffset()) break;
                    _methodName = sfs[i].GetMethod().Name;//方法名称
                                                          //sfs[i].GetFileLineNumber();//没有PDB文件的情况下将始终返回0
                    if (_filterdName.Contains(_methodName)) continue;
                    _fullName = _methodName + "()->" + _fullName;
                }
                st = null;
                sfs = null;
                _filterdName = _methodName = null;

                ExceptionLogger.Fatal("Crash：\r\n{0}", e.ExceptionObject.ToString(), _fullName);
            }
        }

        private static IHostBuilder CreateHostBuilder(string[] args) => new HostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureHostConfiguration(
                    configurationBuilder => configurationBuilder
                        .AddEnvironmentVariables(prefix: "DOTNET_")
                        .AddIf(
                            args is object,
                            x => x.AddCommandLine(args)))
                .ConfigureAppConfiguration((hostingContext, config) =>
                    {
                        AddConfiguration(config, hostingContext.HostingEnvironment, args);
                    })
                .UseDefaultServiceProvider(
                    (context, options) =>
                    {
                        var isDevelopment = context.HostingEnvironment.IsDevelopment();
                        options.ValidateScopes = isDevelopment;
                        options.ValidateOnBuild = isDevelopment;
                    })
                .UseOrleans(ConfigureSiloBuilder)
                .ConfigureWebHost(ConfigureWebHostBuilder)
                .UseConsoleLifetime();

        private static void ConfigureSiloBuilder(
            Microsoft.Extensions.Hosting.HostBuilderContext context,
            ISiloBuilder siloBuilder) =>
            siloBuilder
                .Configure<SerializationProviderOptions>(opt => opt.SerializationProviders.Add(typeof(ProtobufNetSerializer).GetTypeInfo()))
                .Configure<ClusterMembershipOptions>(opt => {
                    opt.DefunctSiloExpiration = TimeSpan.FromHours(1);
                    opt.DefunctSiloCleanupPeriod = TimeSpan.FromHours(1);
                })
#if DEBUG
#else
                .Configure<EndpointOptions>(options =>
                {
                    var lst = Dns.GetHostEntry("appnode").AddressList;
                    foreach (var item in lst)
                    {
                        Console.WriteLine(item.ToString());
                    }

                    options.SiloPort = 11111;
                    options.GatewayPort = 30000;
                    options.AdvertisedIPAddress = Dns.GetHostAddresses("appnode").First();
                    options.GatewayListeningEndpoint = new IPEndPoint(IPAddress.Any, 40000);
                    options.SiloListeningEndpoint = new IPEndPoint(IPAddress.Any, 50000);
                })

#endif
                .ConfigureServices(
                    (context, services) =>
                    {
                        var CircuitBreakerOpenTriggerCount = context.Configuration.GetSection("PollySetting:CircuitBreakerOpenTriggerCount").Get<int>();
                        //通用策略
                        services.AddHttpClientPolly(PollyHttpClientServiceCollectionExtensions.HttpclientName, options =>
                        {
                            options.TimeoutTime = context.Configuration.GetSection("PollySetting:TimeoutTime").Get<int>();
                            options.RetryCount = context.Configuration.GetSection("PollySetting:RetryCount").Get<int>();
                            options.CircuitBreakerOpenFallCount = context.Configuration.GetSection("PollySetting:CircuitBreakerOpenFallCount").Get<int>();
                            options.CircuitBreakerDownTime = context.Configuration.GetSection("PollySetting:CircuitBreakerDownTime").Get<int>();
                            options.CircuitBreakerAction = (p =>
                            {
                                int rcount = (int)p;
                                if (rcount == CircuitBreakerOpenTriggerCount)
                                    Environment.Exit(0);//断路器触发超过CircuitBreakerOpenTriggerCount次-退出程序
                            });
                        });

                        services.AddSingleton<MsvClientCtrlSDK>();
                        services.AddSingleton<RestClient>(pd =>
                        {
                            return new RestClient(pd.GetService<IHttpClientFactory>().CreateClient(PollyHttpClientServiceCollectionExtensions.HttpclientName),
                                context.Configuration.GetSection("IngestDBSvr").Value, context.Configuration.GetSection("CMServer").Value);
                        });

                        //services.AddSingleton<IDeviceMonitorService, DeviceMonitorService>();
                        services.AddSingleton<IDeviceMonitorClient, DeviceMonitorClient>();
                        services.AddSingleton<IGrainServiceDataBack, GrainServiceDataBack>();
                        services.AddTransient<ITaskHandlerFactory, TaskHandlerFactory>(sp => TaskHandlerFactory.Create(sp));

                        services.Configure<ApplicationOptions>(context.Configuration);
                        services.Configure<ClusterOptions>(opt => { opt.ClusterId = Cluster.ClusterId; opt.ServiceId = Cluster.ServiceId; });
                        services.AddSingletonNamedService<PlacementStrategy, ScheduleTaskPlacementStrategy>(nameof(ScheduleTaskPlacementStrategy));
                        services.AddSingletonKeyedService<Type, IPlacementDirector, ScheduleTaskPlacementSiloDirector>(typeof(ScheduleTaskPlacementStrategy));
                        //services.BuildServiceProvider();
                    })
                .UseSiloUnobservedExceptionsHandler()
                .UseAdoNetClustering(
                    options =>
                    {
                        options.Invariant = "MySql.Data.MySqlClient";
                        options.ConnectionString = context.Configuration.GetSection("ConnectDB").Value;
                    })
#if DEBUG
                 .ConfigureEndpoints(
                        EndpointOptions.DEFAULT_SILO_PORT,
                        EndpointOptions.DEFAULT_GATEWAY_PORT
                    )
#endif
                .AddAdoNetGrainStorageAsDefault(
                    options =>
                    {
                        options.Invariant = "MySql.Data.MySqlClient";
                        options.ConnectionString = context.Configuration.GetSection("ConnectDB").Value;
                        options.ConfigureJsonSerializerSettings = ConfigureJsonSerializerSettings;
                        options.UseJsonFormat = true;
                    }
                )

                .UseAdoNetReminderService(
                      options =>
                      {
                          options.Invariant = "MySql.Data.MySqlClient";
                          options.ConnectionString = context.Configuration.GetSection("ConnectDB").Value;
                      })

                .AddSimpleMessageStreamProvider(StreamProviderName.Default)

                .AddAdoNetGrainStorage(
                    "PubSubStore",
                    options =>
                    {
                        options.Invariant = "MySql.Data.MySqlClient";
                        options.ConnectionString = context.Configuration.GetSection("ConnectDB").Value;
                        options.ConfigureJsonSerializerSettings = ConfigureJsonSerializerSettings;
                        options.UseJsonFormat = true;
                    })
                .AddGrainService<DeviceMonitorService>()
                .UseIf(
                    RuntimeInformation.IsOSPlatform(OSPlatform.Linux),
                    x => x.UseLinuxEnvironmentStatistics())
                .UseIf(
                    RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
                    x => x.UsePerfCounterEnvironmentStatistics())
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(HelloGrain).Assembly).WithReferences())
                .ConfigureApplicationParts(parts => parts.AddFromApplicationBaseDirectory())
                .UseDashboard(config =>
                {
                    config.Port = int.Parse(context.Configuration.GetSection("Port").Value, CultureInfo.CurrentCulture);
                });

        private static void ConfigureWebHostBuilder(IWebHostBuilder webHostBuilder) =>
            webHostBuilder
                .UseKestrel((builderContext, options) =>
                {
                    options.AddServerHeader = false;
                    options.ListenAnyIP(int.Parse(builderContext.Configuration.GetSection("HealthCheckPort").Value, CultureInfo.CurrentCulture));
                })
                .UseStartup<Startup>();

        private static IConfigurationBuilder AddConfiguration(
            IConfigurationBuilder configurationBuilder,
            IHostEnvironment hostEnvironment,
            string[] args) =>
            configurationBuilder
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddJsonFile($"appsettings.{hostEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: false)
                .AddInMemoryCollection(GetMemoryOptions())
                .AddKeyPerFile(Path.Combine(Directory.GetCurrentDirectory(), "configuration"), optional: true)
                .AddIf(
                    hostEnvironment.IsDevelopment() && !string.IsNullOrEmpty(hostEnvironment.ApplicationName),
                    x => x.AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true))
                .AddEnvironmentVariables()
                // Add command line options. These take the highest priority.
                .AddIf(
                    args is object,
                    x => x.AddCommandLine(args));


        private static void CreateLogger(IHost host)
        {
            var logConfig = host.Services.GetRequiredService<IConfiguration>().GetSection("Logging");

            int maxDays = 10;
            string maxFileSize = "10MB";
            LogLevels logLevel = LogLevels.Info;
            if (logConfig != null)
            {
                _ = Enum.TryParse(logConfig["Level"] ?? "", out logLevel)
                    && int.TryParse(logConfig["SaveDays"], out maxDays);

                maxFileSize = logConfig["MaxFileSize"];
                if (string.IsNullOrEmpty(maxFileSize))
                {
                    maxFileSize = "10MB";
                }
            }
            LoggerManager.InitLogger(new LogConfig()
            {
                LogBaseDir = logConfig["Path"],
                MaxFileSize = maxFileSize,
                LogLevels = logLevel,
                IsAsync = true,
                LogFileTemplate = LogFileTemplates.PerDayDirAndLogger,
                LogContentTemplate = LogLayoutTemplates.SimpleLayout,
                DeleteDay = maxDays.ToString(CultureInfo.CurrentCulture),
                TargetConsole = true
            });
            LoggerManager.SetLoggerAboveLevels(logLevel);

            ExceptionLogger = LoggerManager.GetLogger("Exception");
            StartLogger = LoggerManager.GetLogger("Main");
        }

        private static void ConfigureJsonSerializerSettings(JsonSerializerSettings jsonSerializerSettings)
        {
            jsonSerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            jsonSerializerSettings.DateParseHandling = DateParseHandling.DateTimeOffset;
        }
        private static string CreateConfigURI(string str)
        {
            if (str.IndexOf("http:") >= 0 || str.IndexOf("https:") >= 0)
            {
                return str;
            }
            else
                return "http://" + str;
        }

        private static Dictionary<string, string> GetMemoryOptions()
        {
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

            Console.WriteLine($"path : {path}, {File.Exists(path)} ");
            if (File.Exists(path))
            {
                try
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
                    dic.Add("ConnectDB", GetConnectOptions(ps, vip));
                    Console.WriteLine($"path : {path}, {File.Exists(path)} ");
                    return dic;
                }
                catch (Exception)
                {
                }

            }
            return null;
        }
        private static string GetConnectOptions(XElement config, string vip)
        {
            var dblist = config.Element("DBConfig");
            foreach (var item in dblist.Elements())
            {
                if (item.Attribute("module").Value.CompareTo("INGESTDB") == 0)
                {
                    return string.Format(
                "Server={0};Port={4};Database={1};Uid={2};Pwd={3};Pooling=true;minpoolsize=0;maxpoolsize=40;SslMode=none;",
                vip, item.Element("Instance").Value,
                item.Element("Username").Value,
                Base64SQL.Base64_Decode(item.Element("Password").Value),
                item.Element("Port").Value);
                }
            }
            return string.Empty;
        }

        private static void ConfigureShutdown(IHost siloHost, ManualResetEvent resetEvent)
        {
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                StartLogger.Info($"Received shutdown via {nameof(Console.CancelKeyPress)}.");
                Console.WriteLine($"Received shutdown via {nameof(Console.CancelKeyPress)}.");

                eventArgs.Cancel = true;
                Shutdown(siloHost, resetEvent);
            };

            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) =>
            {
                StartLogger.Info($"Received shutdown via {nameof(AppDomain.CurrentDomain.ProcessExit)}.");
                Console.WriteLine($"Received shutdown via {nameof(AppDomain.CurrentDomain.ProcessExit)}.");
                Shutdown(siloHost, resetEvent);
            };

            AssemblyLoadContext.Default.Unloading += context =>
            {
                StartLogger.Info("Assembly unloading...");
                Console.WriteLine("Assembly unloading...");
                Shutdown(siloHost, resetEvent);
            };
        }

        private static void Shutdown(IHost siloHost, ManualResetEvent resetEvent)
        {
            if (Interlocked.Exchange(ref siloStopping, 1) == 0)
            {
                StartLogger.Info($"Shutting down silohost from thread ID:'{Thread.CurrentThread.ManagedThreadId}' name:'{Thread.CurrentThread.Name}'");

                try
                {
#pragma warning disable VSTHRD002 // 避免有问题的同步等待
                    Dashboard.Stop();

                    siloHost.StopAsync().Wait();
#pragma warning restore VSTHRD002 // 避免有问题的同步等待
                }
                finally
                {
                    StartLogger.Info($"SiloHost stopped at {DateTime.UtcNow} UTC.");
                    resetEvent.Set();
                }
            }
            else
            {
                StartLogger.Info("Shutdown in progress. Ignoring shutdown request.");
            }
        }
    }
}
