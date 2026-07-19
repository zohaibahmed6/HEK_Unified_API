using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HekCoreApi.Api.HealthChecks;

/// <summary>
/// Liveness check - always healthy if the process can respond at all. No dependency checks here by
/// design; readiness (dependency-aware) is a separate tagged registration - see Program.cs.
/// </summary>
public sealed class SelfHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(HealthCheckResult.Healthy("API process is running."));
    }
}
