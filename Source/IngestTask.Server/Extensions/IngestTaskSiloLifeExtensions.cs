
namespace IngestTask.Server
{
    using Microsoft.Extensions.DependencyInjection;
    using Orleans;
    using Orleans.Hosting;
    using Orleans.Runtime;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

   

    public static class IngestTaskSiloLifeExtensions
    {
        public static ISiloHostBuilder AddStopTask(
       this ISiloHostBuilder builder,
       int stage = ServiceLifecycleStage.Last)
        {
            builder.ConfigureServices(services =>
                services.AddTransient<ILifecycleParticipant<ISiloLifecycle>>(sp =>
                    new IngestTaskSiloLife(
                        sp,
                        stage)));
            return builder;
        }
    }
    public class IngestTaskSiloLife : ILifecycleParticipant<ISiloLifecycle>
    {
        private readonly IServiceProvider serviceProvider;
        private readonly int stage;

        public IngestTaskSiloLife(
            IServiceProvider serviceProvider,
            int stage)
        {
            this.serviceProvider = serviceProvider;
            this.stage = stage;
        }

        

        public void Participate(ISiloLifecycle lifecycle)
        {
            lifecycle.Subscribe<IngestTaskSiloLife>(
                this.stage,
                cancellation => this.OnSiloStopAsync(this.serviceProvider, cancellation));
        }

        public Task OnSiloStopAsync(IServiceProvider p, CancellationToken cancle)
        {
            return Task.CompletedTask;
        }
    }
}
