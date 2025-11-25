namespace Wiesenwischer.NServiceBus.EndpointHealth;

/// <summary>
/// Configuration options for the endpoint health monitoring feature.
/// </summary>
public class EndpointHealthOptions
{
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
}
