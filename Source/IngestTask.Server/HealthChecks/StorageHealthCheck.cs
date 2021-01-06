namespace IngestTask.Server.HealthChecks
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using Orleans;
    using IngestTask.Abstraction.Grains.HealthChecks;
    using Sobey.Core.Log;

    public class StorageHealthCheck : IHealthCheck
    {
        private const string FailedMessage = "Failed storage health check.";
        private readonly IGrainFactory client;
        private readonly ILogger Logger = LoggerManager.GetLogger("StorageHealthCheck");
        public StorageHealthCheck(IGrainFactory client)
        {
            this.client = client;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Call this grain with a random key each time. This grain then deactivates itself, so there is a new
                // instance created and destroyed each time.
                await this.client.GetGrain<IStorageHealthCheckGrain>("ingesttaskstorage").CheckAsync().ConfigureAwait(false);
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
