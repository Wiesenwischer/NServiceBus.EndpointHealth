using Microsoft.Extensions.Configuration;
using Wiesenwischer.NServiceBus.EndpointHealth;

namespace NServiceBus;

/// <summary>
/// Extension methods for configuring endpoint health monitoring with configuration support.
/// </summary>
public static class EndpointHealthConfigurationExtensions
{
    /// <summary>
    /// Configures the endpoint health monitoring feature using values from configuration.
    /// </summary>
    /// <param name="endpointConfiguration">The endpoint configuration.</param>
    /// <param name="configuration">The configuration to read from.</param>
    /// <param name="configure">Optional action to further configure or override options.</param>
    /// <param name="sectionName">The configuration section name. Defaults to "EndpointHealth".</param>
    /// <returns>The endpoint configuration for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method first applies configuration values, then invokes the optional configure callback.
    /// This allows configuration values to be overridden programmatically when needed.
    /// </para>
    /// <para>
    /// For NServiceBus 7.x, you must set <see cref="EndpointHealthOptions.HealthState"/>
    /// in the configure callback to share the health state with ASP.NET Core DI.
    /// </para>
    /// <para>
    /// Example usage:
    /// <code>
    /// // NServiceBus 8.x (NET9+): Configuration only
    /// endpointConfiguration.ConfigureEndpointHealth(configuration);
    ///
    /// // NServiceBus 7.x: Must provide health state
    /// var healthState = new EndpointHealthState();
    /// services.AddSingleton&lt;IEndpointHealthState&gt;(healthState);
    ///
    /// endpointConfiguration.ConfigureEndpointHealth(configuration, options =>
    /// {
    ///     options.HealthState = healthState;
    /// });
    /// </code>
    /// </para>
    /// </remarks>
    public static EndpointConfiguration ConfigureEndpointHealth(
        this EndpointConfiguration endpointConfiguration,
        IConfiguration configuration,
        Action<EndpointHealthOptions>? configure = null,
        string sectionName = "EndpointHealth")
    {
        return endpointConfiguration.EnableEndpointHealth(options =>
        {
            options.FromConfiguration(configuration, sectionName);
            configure?.Invoke(options);
        });
    }
}
