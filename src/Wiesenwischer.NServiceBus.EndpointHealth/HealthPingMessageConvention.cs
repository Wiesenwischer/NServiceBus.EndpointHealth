using NServiceBus;

namespace Wiesenwischer.NServiceBus.EndpointHealth;

/// <summary>
/// Message convention that ensures HealthPing is recognized as a valid NServiceBus message
/// regardless of custom conventions configured by the endpoint.
/// </summary>
internal class HealthPingMessageConvention : IMessageConvention
{
    public string Name => "EndpointHealth HealthPing Convention";

    public bool IsMessageType(Type type) => type == typeof(HealthPing);

    public bool IsCommandType(Type type) => false;

    public bool IsEventType(Type type) => false;
}
