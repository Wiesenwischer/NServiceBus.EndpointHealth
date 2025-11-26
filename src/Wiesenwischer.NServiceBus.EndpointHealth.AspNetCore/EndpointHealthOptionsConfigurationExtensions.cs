using Microsoft.Extensions.Configuration;

namespace Wiesenwischer.NServiceBus.EndpointHealth;

/// <summary>
/// Extension methods for configuring <see cref="EndpointHealthOptions"/> from <see cref="IConfiguration"/>.
/// </summary>
public static class EndpointHealthOptionsConfigurationExtensions
{
    /// <summary>
    /// Configures the endpoint health options from the specified configuration section.
    /// </summary>
    /// <param name="options">The options instance to configure.</param>
    /// <param name="configuration">The configuration to read from.</param>
    /// <param name="sectionName">The configuration section name. Defaults to "EndpointHealth".</param>
    /// <returns>The configured options instance for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method binds configuration values to the options. Values from configuration
    /// will override existing values in the options instance.
    /// </para>
    /// <para>
    /// Example configuration in appsettings.json:
    /// <code>
    /// {
    ///   "EndpointHealth": {
    ///     "TransportKey": "primary-sql",
    ///     "UnhealthyAfter": "00:02:00"
    ///   }
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// Environment variables can also be used:
    /// <code>
    /// ENDPOINTHEALTH__TRANSPORTKEY=primary-sql
    /// ENDPOINTHEALTH__UNHEALTHYAFTER=00:02:00
    /// </code>
    /// </para>
    /// </remarks>
    public static EndpointHealthOptions FromConfiguration(
        this EndpointHealthOptions options,
        IConfiguration configuration,
        string sectionName = "EndpointHealth")
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        if (configuration is null)
            throw new ArgumentNullException(nameof(configuration));

        var section = configuration.GetSection(sectionName);

        // Read individual values to avoid overwriting with defaults
        var transportKey = section[nameof(EndpointHealthOptions.TransportKey)];
        if (!string.IsNullOrWhiteSpace(transportKey))
            options.TransportKey = transportKey;

        var unhealthyAfterStr = section[nameof(EndpointHealthOptions.UnhealthyAfter)];
        if (!string.IsNullOrWhiteSpace(unhealthyAfterStr) && TimeSpan.TryParse(unhealthyAfterStr, out var unhealthyAfter))
            options.UnhealthyAfter = unhealthyAfter;

        var pingIntervalStr = section[nameof(EndpointHealthOptions.PingInterval)];
        if (!string.IsNullOrWhiteSpace(pingIntervalStr) && TimeSpan.TryParse(pingIntervalStr, out var pingInterval))
            options.PingInterval = pingInterval;

        return options;
    }
}
