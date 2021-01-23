namespace IngestTask.Server
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Diagnostics.HealthChecks;
    using Microsoft.Extensions.DependencyInjection;
    using IngestTask.Server.HealthChecks;
    using Microsoft.Extensions.Logging;
    using AutoMapper;
    using IngestTask.Tool;
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using System;
#pragma warning disable CA1724 // The type name conflicts with the namespace name 'Orleans.Runtime.Startup'
    public class Startup
#pragma warning restore CA1724 // The type name conflicts with the namespace name 'Orleans.Runtime.Startup'
    {
        public virtual void ConfigureServices(IServiceCollection services) =>
            services
                .Configure<HealthCheckPublisherOptions>(opt => { opt.Period = TimeSpan.FromSeconds(3); })
                .AddAutoMapper(typeof(GlobalProfile))
                .AddRouting()
                .AddHealthChecks()
                .AddCheck<ClusterHealthCheck>(nameof(ClusterHealthCheck))
                .AddCheck<GrainHealthCheck>(nameof(GrainHealthCheck))
                .AddCheck<SiloHealthCheck>(nameof(SiloHealthCheck))
                .AddCheck<StorageHealthCheck>(nameof(StorageHealthCheck));

        public virtual void Configure(IApplicationBuilder application,
                      ILoggerFactory loggerFactory)
        {
            
            if (loggerFactory != null)
            {
                application.UseCustomSerilogRequestLogging(loggerFactory);
            }
            application
                .UseHealthChecks("/status")
                .UseRouting();

            
        }
    }
}
