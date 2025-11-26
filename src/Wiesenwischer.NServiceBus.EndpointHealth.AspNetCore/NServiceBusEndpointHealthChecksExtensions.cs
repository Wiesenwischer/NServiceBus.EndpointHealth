using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Wiesenwischer.NServiceBus.EndpointHealth;
using Wiesenwischer.NServiceBus.EndpointHealth.AspNetCore;

namespace Microsoft.Extensions.DependencyInjection;

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
    /// <para>
    /// This health check requires that <see cref="IEndpointHealthState"/> is registered in the service collection.
    /// </para>
    /// <para>
    /// For NServiceBus 8.x (.NET 9+), both <see cref="IEndpointHealthState"/> and <see cref="EndpointHealthOptions"/>
    /// are automatically registered when using <c>EnableEndpointHealth()</c>.
    /// </para>
    /// <para>
    /// For NServiceBus 7.x (.NET Core 3.1), you must manually register <see cref="IEndpointHealthState"/>
    /// and pass it to <c>EnableEndpointHealth(healthState, ...)</c>. The <see cref="EndpointHealthOptions"/>
    /// will be registered with default values if not already present.
    /// </para>
    /// </remarks>
    public static IHealthChecksBuilder AddNServiceBusEndpointHealth(
        this IHealthChecksBuilder builder,
        string name = "nservicebus-endpoint",
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null)
    {
        // Register default options if not already registered
        // This is needed for NServiceBus 7.x where DI containers are separate
        builder.Services.TryAddSingleton<EndpointHealthOptions>();

        return builder.AddCheck<NServiceBusEndpointHealthCheck>(
            name,
            failureStatus ?? HealthStatus.Unhealthy,
            tags ?? ["nservicebus", "endpoint", "messaging"]);
    }

    /// <summary>
    /// Adds the NServiceBus endpoint health check to the health check builder with custom options.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="configureOptions">Action to configure the health options.</param>
    /// <param name="name">The name of the health check. Defaults to "nservicebus-endpoint".</param>
    /// <param name="failureStatus">The failure status to report. Defaults to <see cref="HealthStatus.Unhealthy"/>.</param>
    /// <param name="tags">Optional tags to associate with the health check.</param>
    /// <returns>The health checks builder for chaining.</returns>
    public static IHealthChecksBuilder AddNServiceBusEndpointHealth(
        this IHealthChecksBuilder builder,
        Action<EndpointHealthOptions> configureOptions,
        string name = "nservicebus-endpoint",
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null)
    {
        var options = new EndpointHealthOptions();
        configureOptions(options);
        builder.Services.AddSingleton(options);

        return builder.AddCheck<NServiceBusEndpointHealthCheck>(
            name,
            failureStatus ?? HealthStatus.Unhealthy,
            tags ?? ["nservicebus", "endpoint", "messaging"]);
    }
}
