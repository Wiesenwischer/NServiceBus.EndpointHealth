namespace Wiesenwischer.NServiceBus.EndpointHealth;

/// <summary>
/// Configuration options for the endpoint health monitoring feature.
/// </summary>
public class EndpointHealthOptions
{
    /// <summary>
    /// Gets or sets an optional logical key for the transport cluster.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The transport key allows grouping endpoints by their shared transport infrastructure
    /// (e.g., "primary-sql", "project-sql", "nsb-main"). This is useful for monitoring
    /// scenarios where multiple endpoints share a common transport and a transport failure
    /// would affect all of them.
    /// </para>
    /// <para>
    /// This is a logical identifier, not derived from connection strings or infrastructure.
    /// It should be set via configuration or environment variables for consistent grouping.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// options.TransportKey = "primary-sql";
    /// </code>
    /// </example>
    public string? TransportKey { get; set; }

    /// <summary>
    /// Gets or sets the interval at which health ping messages are sent.
    /// Default is 60 seconds.
    /// </summary>
    public TimeSpan PingInterval { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Gets or sets the time after which the endpoint is considered unhealthy
    /// if no health ping has been processed. Default is 3 minutes.
    /// </summary>
    /// <remarks>
    /// This value should be greater than <see cref="PingInterval"/> to allow
    /// for some tolerance in message processing delays. The default of 3 minutes
    /// allows for 2-3 missed pings before the endpoint is considered unhealthy.
    /// </remarks>
    public TimeSpan UnhealthyAfter { get; set; } = TimeSpan.FromMinutes(3);

    /// <summary>
    /// Gets or sets an external health state instance to use.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For NServiceBus 7.x, you must provide an external health state instance because
    /// NServiceBus uses an internal container that is separate from ASP.NET Core DI.
    /// Register the same instance in both ASP.NET Core DI and here.
    /// </para>
    /// <para>
    /// For NServiceBus 8.x (NET9_0+), this is optional. If not set, a new instance
    /// will be created and registered in DI automatically.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // NServiceBus 7.x usage:
    /// var healthState = new EndpointHealthState("my-transport");
    /// services.AddSingleton&lt;IEndpointHealthState&gt;(healthState);
    ///
    /// endpointConfig.EnableEndpointHealth(options =>
    /// {
    ///     options.HealthState = healthState;
    /// });
    /// </code>
    /// </example>
    public IEndpointHealthState? HealthState { get; set; }
}
