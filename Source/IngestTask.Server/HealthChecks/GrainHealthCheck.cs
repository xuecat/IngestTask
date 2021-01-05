namespace IngestTask.Server.HealthChecks
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using Orleans;
    using IngestTask.Abstraction.Grains.HealthChecks;
    using Sobey.Core.Log;

    public class GrainHealthCheck : IHealthCheck
    {
        private const string FailedMessage = "Failed local health check.";
        private readonly IClusterClient client;
        private readonly ILogger Logger = LoggerManager.GetLogger("GrainHealthCheck");

        public GrainHealthCheck(IClusterClient client)
        {
            this.client = client;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await this.client.GetGrain<ILocalHealthCheckGrain>(Guid.Empty).CheckAsync().ConfigureAwait(false);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception exception)
#pragma warning restore CA1031 // Do not catch general exception types
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                Logger.Error(exception.Message+ FailedMessage);
#pragma warning restore CA1303 // Do not pass literals as localized parameters
                return HealthCheckResult.Unhealthy(FailedMessage, exception);
            }

            return HealthCheckResult.Healthy();
        }
    }
}
