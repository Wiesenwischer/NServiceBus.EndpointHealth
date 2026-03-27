using NServiceBus;
using NServiceBus.Testing;
using Wiesenwischer.NServiceBus.EndpointHealth;

namespace Wiesenwischer.NServiceBus.EndpointHealth.Tests;

public class HealthPingHandlerTests
{
    [Fact]
    public async Task Handle_RegistersHealthPingProcessed()
    {
        // Arrange
        var state = new Mock<IEndpointHealthState>();
        var options = new EndpointHealthOptions { PingInterval = TimeSpan.FromSeconds(30) };
        var handler = new HealthPingHandler(state.Object, options);
        var context = new TestableMessageHandlerContext();

        // Act
        await handler.Handle(new HealthPing(), context);

        // Assert
        state.Verify(s => s.RegisterHealthPingProcessed(), Times.Once);
    }

    [Fact]
    public async Task Handle_DoesNotSendNextPing()
    {
        // Arrange - handler no longer schedules the next ping (background service does that)
        var state = new Mock<IEndpointHealthState>();
        var options = new EndpointHealthOptions { PingInterval = TimeSpan.FromSeconds(30) };
        var handler = new HealthPingHandler(state.Object, options);
        var context = new TestableMessageHandlerContext();

        // Act
        await handler.Handle(new HealthPing(), context);

        // Assert - no messages sent from handler
        context.SentMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_DropsStaleInstancePing()
    {
        // Arrange — message carries a different InstanceId than the current endpoint instance
        var currentInstanceId = Guid.NewGuid();
        var staleInstanceId = Guid.NewGuid();

        var state = new Mock<IEndpointHealthState>();
        state.Setup(s => s.InstanceId).Returns(currentInstanceId);

        var options = new EndpointHealthOptions { PingInterval = TimeSpan.FromSeconds(30) };
        var handler = new HealthPingHandler(state.Object, options);
        var context = new TestableMessageHandlerContext();

        // Act
        await handler.Handle(new HealthPing { InstanceId = staleInstanceId }, context);

        // Assert — stale ping is silently dropped: no state update
        context.SentMessages.Should().BeEmpty();
        state.Verify(s => s.RegisterHealthPingProcessed(), Times.Never);
    }
}
