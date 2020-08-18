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

    public static class Program
    {
        public static Task<int> Main(string[] args) => LogAndRunAsync(CreateHostBuilder(args).Build());

        public static async Task<int> LogAndRunAsync(IHost host)
        {
            if (host is null)
            {
                throw new ArgumentNullException(nameof(host));
            }

           
            host.Services.GetRequiredService<IHostEnvironment>().ApplicationName =
                Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>().Product;

            CreateLogger(host);
            Sobey.Core.Log.ILogger logger = LoggerManager.GetLogger(host.ToString());

            try
            {
#pragma warning disable CA1303 // 请不要将文本作为本地化参数传递
                logger.Info("Started application");
                await host.RunAsync().ConfigureAwait(false);
                logger.Info("Stopped application");
#pragma warning restore CA1303 // 请不要将文本作为本地化参数传递
                
                return 0;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception exception)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                logger.Error(exception.Message + "Application terminated unexpectedly");
                return 1;
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
                    AddConfiguration(config, hostingContext.HostingEnvironment, args))
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
                .ConfigureServices(
                    (context, services) =>
                    {
                        services.AddScoped<MsvClientCtrlSDK>();
                        services.AddSingleton<RestClient>();
                        services.Configure<ApplicationOptions>(context.Configuration);
                        services.Configure<ClusterOptions>(context.Configuration.GetSection(nameof(ApplicationOptions.Cluster)));
                        services.Configure<StorageOptions>(context.Configuration.GetSection(nameof(ApplicationOptions.Storage)));
                    })
                .UseSiloUnobservedExceptionsHandler()
                .UseAdoNetClustering(
                    options => {
                        options.Invariant = "MySql.Data.MySqlClient";
                        options.ConnectionString = GetStorageOptions(context.Configuration).ConnectionString;
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
                        options.ConnectionString = GetStorageOptions(context.Configuration).ConnectionString;
                        options.ConfigureJsonSerializerSettings = ConfigureJsonSerializerSettings;
                        options.UseJsonFormat = true;
                    }
                )
               
                .UseAdoNetReminderService(
                      options => {
                          options.Invariant = "MySql.Data.MySqlClient";
                          options.ConnectionString = GetStorageOptions(context.Configuration).ConnectionString;
                      })
                
                .AddSimpleMessageStreamProvider(StreamProviderName.Default)
               
                .AddAdoNetGrainStorage(
                    "PubSubStore",
                    options =>
                    {
                        options.Invariant = "MySql.Data.MySqlClient";
                        options.ConnectionString = GetStorageOptions(context.Configuration).ConnectionString;
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

        private static StorageOptions GetStorageOptions(IConfiguration configuration) =>
            configuration.GetSection(nameof(ApplicationOptions.Storage)).Get<StorageOptions>();
    }
}
