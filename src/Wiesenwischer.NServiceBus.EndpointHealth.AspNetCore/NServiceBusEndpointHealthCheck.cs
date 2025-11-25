using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Wiesenwischer.NServiceBus.EndpointHealth.AspNetCore;

/// <summary>
/// ASP.NET Core health check that verifies the NServiceBus endpoint is healthy.
/// </summary>
/// <remarks>
/// The health check returns:
/// <list type="bullet">
/// <item><see cref="HealthStatus.Unhealthy"/> if a critical error has been detected</item>
/// <item><see cref="HealthStatus.Unhealthy"/> if no health ping has been processed within the configured threshold</item>
/// <item><see cref="HealthStatus.Healthy"/> if the endpoint is operating normally</item>
/// </list>
/// </remarks>
public class NServiceBusEndpointHealthCheck : IHealthCheck
{
    private readonly IEndpointHealthState _state;
    private readonly EndpointHealthOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="NServiceBusEndpointHealthCheck"/> class.
    /// </summary>
    /// <param name="state">The endpoint health state to check.</param>
    /// <param name="options">The health options containing the unhealthy threshold.</param>
    public NServiceBusEndpointHealthCheck(
        IEndpointHealthState state,
        EndpointHealthOptions options)
    {
        _state = state;
        _options = options;
    }

    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (_state.HasCriticalError)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"Critical error detected: {_state.CriticalErrorMessage}"));
        }

        var now = DateTime.UtcNow;
        var lastPing = _state.LastHealthPingProcessedUtc;

        if (lastPing is null)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "No health ping has been processed yet. Endpoint may not have started."));
        }

        var timeSinceLastPing = now - lastPing.Value;
        if (timeSinceLastPing > _options.UnhealthyAfter)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"No health ping for {timeSinceLastPing.TotalSeconds:F0}s (threshold: {_options.UnhealthyAfter.TotalSeconds:F0}s). Message pump may be stuck."));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            $"Last health ping: {timeSinceLastPing.TotalSeconds:F0}s ago"));
    }
}
