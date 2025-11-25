using NServiceBus;
using NServiceBus.Settings;

namespace Wiesenwischer.NServiceBus.EndpointHealth;

/// <summary>
/// Extension methods for configuring endpoint health monitoring on <see cref="EndpointConfiguration"/>.
/// </summary>
public static class EndpointHealthConfigurationExtensions
{
    internal const string ExternalHealthStateKey = "EndpointHealth.ExternalState";

#if NET9_0_OR_GREATER
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
#else
    /// <summary>
    /// Enables the endpoint health monitoring feature with an externally provided health state.
    /// </summary>
    /// <param name="configuration">The endpoint configuration.</param>
    /// <param name="healthState">The health state instance that will be shared with ASP.NET Core health checks.</param>
    /// <param name="configure">Optional action to configure health options.</param>
    /// <returns>The endpoint configuration for chaining.</returns>
    /// <remarks>
    /// <para>
    /// For NServiceBus 7.x, you must provide an external health state instance because
    /// NServiceBus uses an internal container that is separate from ASP.NET Core DI.
    /// </para>
    /// <para>
    /// Register the same instance in ASP.NET Core DI:
    /// <code>
    /// var healthState = new EndpointHealthState();
    /// services.AddSingleton&lt;IEndpointHealthState&gt;(healthState);
    /// // Then pass healthState to EnableEndpointHealth
    /// </code>
    /// </para>
    /// </remarks>
    public static EndpointConfiguration EnableEndpointHealth(
        this EndpointConfiguration configuration,
        IEndpointHealthState healthState,
        Action<EndpointHealthOptions>? configure = null)
    {
        if (healthState == null)
            throw new ArgumentNullException(nameof(healthState));

        var options = new EndpointHealthOptions();
        configure?.Invoke(options);

        var settings = configuration.GetSettings();
        settings.Set(options);
        settings.Set(ExternalHealthStateKey, healthState);
        configuration.EnableFeature<EndpointHealthFeature>();

        return configuration;
    }
#endif

    internal static SettingsHolder GetSettings(this EndpointConfiguration configuration)
    {
        // Use reflection to access the internal Settings property
        var settingsProperty = typeof(EndpointConfiguration)
            .GetProperty("Settings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        return (SettingsHolder)settingsProperty!.GetValue(configuration)!;
    }
}
