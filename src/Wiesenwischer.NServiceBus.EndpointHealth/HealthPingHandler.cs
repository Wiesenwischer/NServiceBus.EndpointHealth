using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
    private readonly ILogger<HealthPingHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthPingHandler"/> class.
    /// </summary>
    /// <param name="state">The endpoint health state to update.</param>
    /// <param name="options">The health options containing the ping interval.</param>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    public HealthPingHandler(
        IEndpointHealthState state,
        EndpointHealthOptions options,
        ILogger<HealthPingHandler>? logger = null)
    {
        _state = state;
        _options = options;
        _logger = logger ?? NullLogger<HealthPingHandler>.Instance;
    }

    /// <summary>
    /// Handles the health ping message by registering it was processed and scheduling the next ping.
    /// Stale pings from previous container instances are silently dropped without rescheduling.
    /// </summary>
    public async Task Handle(HealthPing message, IMessageHandlerContext context)
    {
        var messageId = context.MessageId;
        _logger.LogDebug("HealthPing received. MessageId={MessageId}, TransportKey={TransportKey}, InstanceId={InstanceId}",
            messageId, _state.TransportKey, message.InstanceId);

        if (message.InstanceId != _state.InstanceId)
        {
            _logger.LogDebug(
                "Dropping stale HealthPing from instance {StaleId}, current instance is {CurrentId}. MessageId={MessageId}",
                message.InstanceId, _state.InstanceId, messageId);
            return;
        }

        try
        {
            _state.RegisterHealthPingProcessed();
            _logger.LogDebug("HealthPing processed, state updated. LastPing={LastPing}",
                _state.LastHealthPingProcessedUtc);

            var sendOptions = new SendOptions();
            sendOptions.DelayDeliveryWith(_options.PingInterval);
            sendOptions.RouteToThisEndpoint();

            _logger.LogDebug("Sending next HealthPing with delay {Delay}. MessageId={MessageId}",
                _options.PingInterval, messageId);

            await context.Send(new HealthPing { InstanceId = _state.InstanceId }, sendOptions);

            _logger.LogDebug("Next HealthPing sent successfully. MessageId={MessageId}", messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process HealthPing or send next ping. MessageId={MessageId}, Error={Error}",
                messageId, ex.Message);
            throw;
        }
    }
}
