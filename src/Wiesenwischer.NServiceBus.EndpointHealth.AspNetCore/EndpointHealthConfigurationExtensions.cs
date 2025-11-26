using Microsoft.Extensions.Configuration;
using NServiceBus;

namespace Wiesenwischer.NServiceBus.EndpointHealth.AspNetCore;

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
    /// Example usage:
    /// <code>
    /// // Configuration only
    /// endpointConfiguration.ConfigureEndpointHealth(configuration);
    ///
    /// // Configuration with programmatic override
    /// endpointConfiguration.ConfigureEndpointHealth(configuration, options =>
    /// {
    ///     if (string.IsNullOrWhiteSpace(options.TransportKey))
    ///         options.TransportKey = "default-transport";
    /// });
    /// </code>
    /// </para>
    /// </remarks>
#if NET9_0_OR_GREATER
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
#else
    public static EndpointConfiguration ConfigureEndpointHealth(
        this EndpointConfiguration endpointConfiguration,
        IEndpointHealthState healthState,
        IConfiguration configuration,
        Action<EndpointHealthOptions>? configure = null,
        string sectionName = "EndpointHealth")
    {
        return endpointConfiguration.EnableEndpointHealth(healthState, options =>
        {
            options.FromConfiguration(configuration, sectionName);
            configure?.Invoke(options);
        });
    }
#endif
}
