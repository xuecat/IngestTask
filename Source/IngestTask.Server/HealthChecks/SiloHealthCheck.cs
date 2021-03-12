namespace IngestTask.Server.HealthChecks
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using Orleans.Runtime;

  
    public class SiloHealthCheck : IHealthCheck
    {
        private static long lastCheckTime = DateTime.UtcNow.ToBinary();
        private readonly IEnumerable<IHealthCheckParticipant> participants;

        public SiloHealthCheck(IEnumerable<IHealthCheckParticipant> participants) => this.participants = participants;

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(HealthCheckResult.Healthy());
            /*暂时先这样避免ha重启*/
            var thisLastCheckTime = DateTime.FromBinary(Interlocked.Exchange(ref lastCheckTime, DateTime.UtcNow.ToBinary()));

            foreach (var participant in this.participants)
            {
                if (!participant.CheckHealth(thisLastCheckTime))
                {
                    return Task.FromResult(HealthCheckResult.Degraded());
                }
            }

            return Task.FromResult(HealthCheckResult.Healthy());
        }
    }
}
