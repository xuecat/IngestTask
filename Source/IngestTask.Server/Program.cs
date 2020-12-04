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
    using IngestTask.Tools.Msv;
    using IngestTask.Tool;
    using Orleans.Serialization.ProtobufNet;
    using IngestTask.Grain.Service;
    using IngestTask.Abstraction.Service;
    using IngestTask.Server.Dispatcher;
    using IngestTask.Abstraction.Grains;

    public static class Program
    {
        public static Task<int> Main(string[] args) => LogAndRunAsync(CreateHostBuilder(args).Build());
        private static Sobey.Core.Log.ILogger ExceptionLogger = LoggerManager.GetLogger("Exception");
        private static Sobey.Core.Log.ILogger StartLogger = LoggerManager.GetLogger("Startup");
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
#pragma warning disable CA1303 // 请不要将文本作为本地化参数传递
                StartLogger.Info("Started application");
                await host.RunAsync().ConfigureAwait(false);
                StartLogger.Info("Stopped application");
#pragma warning restore CA1303 // 请不要将文本作为本地化参数传递
                
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

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            new HostBuilder()
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
                .AddGrainService<ScheduleTaskService>()
                .ConfigureServices(
                    (context, services) =>
                    {
                        services.AddScoped<MsvClientCtrlSDK>();
                        services.AddSingleton<RestClient>(new RestClient(context.Configuration.GetSection("IngestDBSvr").Value, context.Configuration.GetSection("CMServer").Value));
                        services.AddSingleton<IScheduleService, ScheduleTaskService>();
                        services.AddSingleton<IScheduleClient, ScheduleTaskClient>();

                        services.AddSingleton<ITaskHandlerFactory, TaskHandlerFactory>();

                        services.Configure<ApplicationOptions>(context.Configuration);
                        services.Configure<ClusterOptions>(context.Configuration.GetSection(nameof(ApplicationOptions.Cluster)));
                        services.Configure<StorageOptions>(context.Configuration.GetSection(nameof(ApplicationOptions.Storage)));
                    })
                .UseSiloUnobservedExceptionsHandler()
                .UseAdoNetClustering(
                    options => {
                        options.Invariant = "MySql.Data.MySqlClient";
                        options.ConnectionString = GetConnectOptions(context.Configuration);
                    })
                
                .ConfigureEndpoints(
                    EndpointOptions.DEFAULT_SILO_PORT,
                    EndpointOptions.DEFAULT_GATEWAY_PORT,
                    listenOnAnyHostAddress: !context.HostingEnvironment.IsDevelopment())
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(HelloGrain).Assembly).WithReferences())
                .AddAdoNetGrainStorageAsDefault(
                    options =>
                    {
                        options.Invariant = "MySql.Data.MySqlClient";
                        options.ConnectionString = GetConnectOptions(context.Configuration);
                        options.ConfigureJsonSerializerSettings = ConfigureJsonSerializerSettings;
                        options.UseJsonFormat = true;
                    }
                )
               
                .UseAdoNetReminderService(
                      options => {
                          options.Invariant = "MySql.Data.MySqlClient";
                          options.ConnectionString = GetConnectOptions(context.Configuration);
                      })
                
                .AddSimpleMessageStreamProvider(StreamProviderName.Default)
               
                .AddAdoNetGrainStorage(
                    "PubSubStore",
                    options =>
                    {
                        options.Invariant = "MySql.Data.MySqlClient";
                        options.ConnectionString = GetConnectOptions(context.Configuration);
                        options.ConfigureJsonSerializerSettings = ConfigureJsonSerializerSettings;
                        options.UseJsonFormat = true;
                    })
                .UseIf(
                    RuntimeInformation.IsOSPlatform(OSPlatform.Linux),
                    x => x.UseLinuxEnvironmentStatistics())
                .UseIf(
                    RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
                    x => x.UsePerfCounterEnvironmentStatistics())
                .ConfigureApplicationParts(parts => parts.AddFromApplicationBaseDirectory())
                .UseDashboard(config => {
                    config.Port = int.Parse(context.Configuration.GetSection("Port").Value, CultureInfo.CurrentCulture);
                });

        private static void ConfigureWebHostBuilder(IWebHostBuilder webHostBuilder) =>
            webHostBuilder
                .UseKestrel((builderContext, options) => options.AddServerHeader = false)
                .UseStartup<Startup>();

        private static IConfigurationBuilder AddConfiguration(
            IConfigurationBuilder configurationBuilder,
            IHostEnvironment hostEnvironment,
            string[] args) =>
            configurationBuilder
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                // Add configuration from an optional appsettings.development.json, appsettings.staging.json or
                // appsettings.production.json file, depending on the environment. These settings override the ones in
                // the appsettings.json file.
                .AddJsonFile($"appsettings.{hostEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: false)
                // Add configuration from files in the specified directory. The name of the file is the key and the
                // contents the value.
                .AddXmlFile("publicsetting.xml")
                
                .AddKeyPerFile(Path.Combine(Directory.GetCurrentDirectory(), "configuration"), optional: true)
                // This reads the configuration keys from the secret store. This allows you to store connection strings
                // and other sensitive settings, so you don't have to check them into your source control provider.
                // Only use this in Development, it is not intended for Production use. See
                // http://docs.asp.net/en/latest/security/app-secrets.html
                .AddIf(
                    hostEnvironment.IsDevelopment() && !string.IsNullOrEmpty(hostEnvironment.ApplicationName),
                    x => x.AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true))
                // Add configuration specific to the Development, Staging or Production environments. This config can
                // be stored on the machine being deployed to or if you are using Azure, in the cloud. These settings
                // override the ones in all of the above config files. See
                // http://docs.asp.net/en/latest/security/app-secrets.html
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
                //TargetConsole = false
            });
            LoggerManager.SetLoggerAboveLevels(logLevel);
        }

        private static void ConfigureJsonSerializerSettings(JsonSerializerSettings jsonSerializerSettings)
        {
            jsonSerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            jsonSerializerSettings.DateParseHandling = DateParseHandling.DateTimeOffset;
        }

        private static string GetConnectOptions(IConfiguration configuration)
        {
            var dbinfo = configuration.GetSection("PublicSetting:DBConfig");

            if (dbinfo.GetChildren() != null)
            {
                foreach (var item in dbinfo.GetChildren())
                {
                    if (item.GetSection("Instance").Value == "ingestdb")
                    {
                        var connectinfo = string.Format($"Server={configuration.GetSection("PublicSetting:System:Sys_VIP")};" +
                            $"Port={item.GetSection("Port").Value};Database={item.GetSection("Instance").Value};" +
                            $"Uid={item.GetSection("Username").Value};Pwd={Base64SQL.Base64_Decode(item.GetSection("Password").Value)};" +
                            $"Pooling=true;minpoolsize=0;maxpoolsize=40;SslMode=none;" +
                            $"ConnectionReset=True;ConnectionLifeTime=120");

                        return connectinfo;
                    }
                }
            }
            return string.Empty;
        }
            //configuration.GetSection(nameof(ApplicationOptions.Storage)).Get<StorageOptions>();
    }
}
