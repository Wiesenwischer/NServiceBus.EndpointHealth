using NServiceBus;

namespace Wiesenwischer.NServiceBus.EndpointHealth;

/// <summary>
/// A synthetic health ping message used to verify that the message pump is working correctly.
/// This message is sent locally to the endpoint and processed by <see cref="HealthPingHandler"/>.
/// </summary>
public class HealthPing : IMessage
{
}
