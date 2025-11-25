using NServiceBus;
using NServiceBus.Settings;

namespace Wiesenwischer.NServiceBus.EndpointHealth;

/// <summary>
/// Extension methods for configuring endpoint health monitoring on <see cref="EndpointConfiguration"/>.
/// </summary>
public static class EndpointHealthConfigurationExtensions
{
    /// <summary>
    /// Enables the endpoint health monitoring feature.
    /// </summary>
    /// <param name="configuration">The endpoint configuration.</param>
    /// <param name="configure">Optional action to configure health options.</param>
    /// <returns>The endpoint configuration for chaining.</returns>
    /// <remarks>
    /// This enables:
    /// <list type="bullet">
    /// <item>Synthetic health ping messages to verify message pump is working</item>
    /// <item>Critical error tracking</item>
    /// <item>Health state accessible via <see cref="IEndpointHealthState"/></item>
    /// </list>
    /// </remarks>
    public static EndpointConfiguration EnableEndpointHealth(
        this EndpointConfiguration configuration,
        Action<EndpointHealthOptions>? configure = null)
    {
        var options = new EndpointHealthOptions();
        configure?.Invoke(options);

        var settings = configuration.GetSettings();
        settings.Set(options);
        configuration.EnableFeature<EndpointHealthFeature>();

        return configuration;
    }

    internal static SettingsHolder GetSettings(this EndpointConfiguration configuration)
    {
        // Use reflection to access the internal Settings property
        var settingsProperty = typeof(EndpointConfiguration)
            .GetProperty("Settings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        return (SettingsHolder)settingsProperty!.GetValue(configuration)!;
    }
}
