
namespace IngestTask.Server
{
    using K4os.Compression.LZ4.Internal;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Orleans;
    using Orleans.Hosting;
    using Orleans.Runtime;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;


    /*暂时去掉*/
    public static class IngestTaskSiloLifeExtensions
    {
        public static ISiloBuilder AddSartIngestTask(
       this ISiloBuilder builder,
       int stage = ServiceLifecycleStage.RuntimeServices)
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
        private readonly IMembershipTable membershipTableProvider;
        private readonly int _stage;
        private readonly int _mode;
        public IngestTaskSiloLife(
            IServiceProvider serviceProvider,
            int stage)
        {
            this.serviceProvider = serviceProvider;
            this._stage = stage;
            this.membershipTableProvider = serviceProvider.GetRequiredService<IMembershipTable>();
            _mode = serviceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>().GetSection("BalanceMode").Get<int>();
        }



        public void Participate(ISiloLifecycle lifecycle)
        {
            lifecycle.Subscribe<IngestTaskSiloLife>(
                this._stage,
                cancellation => this.OnSiloStopAsync());
        }

        private async Task OnSiloStopAsync()
        {
            if (this.membershipTableProvider != null)
            {
                try
                {
                    switch (_mode)
                    {
                        case 1:
                            {
                                await this.membershipTableProvider.InitializeMembershipTable(true).ConfigureAwait(true);
                                var info = await this.membershipTableProvider.ReadAll().ConfigureAwait(true);
                                if (info != null && info.Members.Any(x => x.Item1.Status != SiloStatus.Dead))
                                {
                                    await membershipTableProvider.DeleteMembershipTableEntries(Abstraction.Constants.Cluster.ClusterId).ConfigureAwait(true);
                                }
                            }
                            break;
                        default:
                            break;
                    }
                    
                }
                catch (Exception)
                {

                }
                
            }
            

        }
    }
}
