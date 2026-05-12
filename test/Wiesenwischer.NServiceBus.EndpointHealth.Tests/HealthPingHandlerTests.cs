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

}
