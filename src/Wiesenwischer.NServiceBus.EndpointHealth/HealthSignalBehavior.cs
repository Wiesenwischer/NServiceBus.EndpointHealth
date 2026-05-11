using NServiceBus;
using NServiceBus.Pipeline;

namespace Wiesenwischer.NServiceBus.EndpointHealth;

/// <summary>
/// Pipeline behavior that treats every incoming message as evidence that the message pump is alive.
/// </summary>
/// <remarks>
/// Synthetic <see cref="HealthPing"/> messages share the endpoint's input queue with regular work.
/// When the queue is backlogged with thousands of messages, the ping waits behind them (FIFO) and the
/// health check would falsely report the pump as stuck even though it is processing messages normally.
/// This behavior updates <see cref="IEndpointHealthState.LastHealthPingProcessedUtc"/> as soon as the
/// pipeline starts handling any message, so the endpoint stays healthy whenever the pump is making progress.
/// </remarks>
internal class HealthSignalBehavior : Behavior<IIncomingPhysicalMessageContext>
{
    private readonly IEndpointHealthState _state;

    public HealthSignalBehavior(IEndpointHealthState state)
    {
        _state = state;
    }

    public override Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
    {
        _state.RegisterHealthPingProcessed();
        return next();
    }
}
