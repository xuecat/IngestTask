namespace IngestTask.Server.HealthChecks
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using Orleans;
    using Orleans.Runtime;
    using Sobey.Core.Log;

    public class ClusterHealthCheck : IHealthCheck
    {
        private const string DegradedMessage = " silo(s) unavailable.";
        private const string FailedMessage = "Failed cluster status health check.";
        private readonly IGrainFactory client;
        private readonly ILogger Logger = LoggerManager.GetLogger("ClusterHealthCheck");

        public ClusterHealthCheck(IGrainFactory grainFactory)
        {
            this.client = grainFactory;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var manager = this.client.GetGrain<IManagementGrain>(0);

            try
            {
                var hosts = await manager.GetHosts().ConfigureAwait(false);
                var count = hosts.Values.Count(x => x.IsUnavailable());
                return count > 0 ? HealthCheckResult.Degraded(count + DegradedMessage) : HealthCheckResult.Healthy();
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception exception)
#pragma warning restore CA1031 // Do not catch general exception types
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                Logger.Error(exception.Message+FailedMessage);
#pragma warning restore CA1303 // Do not pass literals as localized parameters
                return HealthCheckResult.Unhealthy(FailedMessage, exception);
            }
        }
    }
}
