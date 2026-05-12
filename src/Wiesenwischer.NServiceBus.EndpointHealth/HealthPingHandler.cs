using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NServiceBus;

namespace Wiesenwischer.NServiceBus.EndpointHealth;

/// <summary>
/// Handles <see cref="HealthPing"/> messages by registering the ping with the health state.
/// </summary>
/// <remarks>
/// The <see cref="HealthSignalBehavior"/> already updates the timestamp for every incoming message
/// before this handler runs, so this handler is mainly here to log ping receipts and to mark
/// <see cref="HealthPing"/> as a known handled message type.
/// </remarks>
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
    /// Handles the health ping message.
    /// </summary>
    public Task Handle(HealthPing message, IMessageHandlerContext context)
    {
        _logger.LogInformation("HealthPing received. MessageId={MessageId}, TransportKey={TransportKey}",
            context.MessageId, _state.TransportKey);

        _state.RegisterHealthPingProcessed();
        return Task.CompletedTask;
    }
}
