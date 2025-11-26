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

    /// <summary>
    /// Data key for the transport key in health check results.
    /// </summary>
    public const string DataKeyTransportKey = "transportKey";

    /// <summary>
    /// Data key for the last health ping timestamp in health check results.
    /// </summary>
    public const string DataKeyLastHealthPingProcessedUtc = "lastHealthPingProcessedUtc";

    /// <summary>
    /// Data key for the time since last ping in health check results.
    /// </summary>
    public const string DataKeyTimeSinceLastPing = "timeSinceLastPing";

    /// <summary>
    /// Data key for the ping interval in health check results.
    /// </summary>
    public const string DataKeyPingInterval = "pingInterval";

    /// <summary>
    /// Data key for the unhealthy threshold in health check results.
    /// </summary>
    public const string DataKeyUnhealthyAfter = "unhealthyAfter";

    /// <summary>
    /// Data key for the critical error flag in health check results.
    /// </summary>
    public const string DataKeyHasCriticalError = "hasCriticalError";

    /// <summary>
    /// Data key for the critical error message in health check results.
    /// </summary>
    public const string DataKeyCriticalErrorMessage = "criticalErrorMessage";

    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var lastPing = _state.LastHealthPingProcessedUtc;
        var timeSinceLastPing = lastPing.HasValue ? now - lastPing.Value : (TimeSpan?)null;

        // Build data dictionary with all relevant information
        var data = BuildDataDictionary(lastPing, timeSinceLastPing);

        if (_state.HasCriticalError)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"Critical error detected: {_state.CriticalErrorMessage}",
                data: data));
        }

        if (lastPing is null)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "No health ping has been processed yet. Endpoint may not have started.",
                data: data));
        }

        if (timeSinceLastPing > _options.UnhealthyAfter)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"No health ping for {timeSinceLastPing.Value.TotalSeconds:F0}s (threshold: {_options.UnhealthyAfter.TotalSeconds:F0}s). Message pump may be stuck.",
                data: data));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            $"Last health ping: {timeSinceLastPing!.Value.TotalSeconds:F0}s ago",
            data: data));
    }

    private IReadOnlyDictionary<string, object> BuildDataDictionary(
        DateTime? lastPing,
        TimeSpan? timeSinceLastPing)
    {
        var data = new Dictionary<string, object>
        {
            [DataKeyHasCriticalError] = _state.HasCriticalError,
            [DataKeyPingInterval] = _options.PingInterval.ToString(),
            [DataKeyUnhealthyAfter] = _options.UnhealthyAfter.ToString()
        };

        if (_state.TransportKey is not null)
        {
            data[DataKeyTransportKey] = _state.TransportKey;
        }

        if (lastPing.HasValue)
        {
            data[DataKeyLastHealthPingProcessedUtc] = lastPing.Value.ToString("O");
        }

        if (timeSinceLastPing.HasValue)
        {
            data[DataKeyTimeSinceLastPing] = timeSinceLastPing.Value.ToString();
        }

        if (_state.CriticalErrorMessage is not null)
        {
            data[DataKeyCriticalErrorMessage] = _state.CriticalErrorMessage;
        }

        return data;
    }
}
