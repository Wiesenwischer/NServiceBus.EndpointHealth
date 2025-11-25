using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Wiesenwischer.NServiceBus.EndpointHealth.AspNetCore;

/// <summary>
/// Extension methods for adding NServiceBus endpoint health checks to ASP.NET Core.
/// </summary>
public static class NServiceBusEndpointHealthChecksExtensions
{
    /// <summary>
    /// Adds the NServiceBus endpoint health check to the health check builder.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">The name of the health check. Defaults to "nservicebus-endpoint".</param>
    /// <param name="failureStatus">The failure status to report. Defaults to <see cref="HealthStatus.Unhealthy"/>.</param>
    /// <param name="tags">Optional tags to associate with the health check.</param>
    /// <returns>The health checks builder for chaining.</returns>
    /// <remarks>
    /// This health check requires that <see cref="IEndpointHealthState"/> and <see cref="EndpointHealthOptions"/>
    /// are registered in the service collection. These are automatically registered when using
    /// <c>EnableEndpointHealth()</c> on your NServiceBus endpoint configuration.
    /// </remarks>
    public static IHealthChecksBuilder AddNServiceBusEndpointHealth(
        this IHealthChecksBuilder builder,
        string name = "nservicebus-endpoint",
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null)
    {
        return builder.AddCheck<NServiceBusEndpointHealthCheck>(
            name,
            failureStatus ?? HealthStatus.Unhealthy,
            tags ?? ["nservicebus", "endpoint", "messaging"]);
    }
}
