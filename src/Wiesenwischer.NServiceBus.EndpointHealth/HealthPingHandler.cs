using NServiceBus;

namespace Wiesenwischer.NServiceBus.EndpointHealth;

/// <summary>
/// Handles <see cref="HealthPing"/> messages by registering the ping with the health state
/// and scheduling the next health ping.
/// </summary>
public class HealthPingHandler : IHandleMessages<HealthPing>
{
    private readonly IEndpointHealthState _state;
    private readonly EndpointHealthOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthPingHandler"/> class.
    /// </summary>
    /// <param name="state">The endpoint health state to update.</param>
    /// <param name="options">The health options containing the ping interval.</param>
    public HealthPingHandler(IEndpointHealthState state, EndpointHealthOptions options)
    {
        _state = state;
        _options = options;
    }

    /// <summary>
    /// Handles the health ping message by registering it was processed and scheduling the next ping.
    /// </summary>
    public async Task Handle(HealthPing message, IMessageHandlerContext context)
    {
        _state.RegisterHealthPingProcessed();

        var sendOptions = new SendOptions();
        sendOptions.DelayDeliveryWith(_options.PingInterval);
        sendOptions.RouteToThisEndpoint();

        await context.Send(new HealthPing(), sendOptions);
    }
}
