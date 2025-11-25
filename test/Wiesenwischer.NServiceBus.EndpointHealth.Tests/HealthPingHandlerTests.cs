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
    public async Task Handle_SchedulesNextHealthPing()
    {
        // Arrange
        var state = new Mock<IEndpointHealthState>();
        var options = new EndpointHealthOptions { PingInterval = TimeSpan.FromSeconds(30) };
        var handler = new HealthPingHandler(state.Object, options);
        var context = new TestableMessageHandlerContext();

        // Act
        await handler.Handle(new HealthPing(), context);

        // Assert
        context.SentMessages.Should().HaveCount(1);
        var sentMessage = context.SentMessages[0];
        sentMessage.Message.Should().BeOfType<HealthPing>();
    }

    [Fact]
    public async Task Handle_SchedulesNextPingWithConfiguredInterval()
    {
        // Arrange
        var state = new Mock<IEndpointHealthState>();
        var pingInterval = TimeSpan.FromSeconds(45);
        var options = new EndpointHealthOptions { PingInterval = pingInterval };
        var handler = new HealthPingHandler(state.Object, options);
        var context = new TestableMessageHandlerContext();

        // Act
        await handler.Handle(new HealthPing(), context);

        // Assert
        var sentMessage = context.SentMessages[0];
        sentMessage.Options.GetDeliveryDelay().Should().Be(pingInterval);
    }

    [Fact]
    public async Task Handle_RoutesToThisEndpoint()
    {
        // Arrange
        var state = new Mock<IEndpointHealthState>();
        var options = new EndpointHealthOptions();
        var handler = new HealthPingHandler(state.Object, options);
        var context = new TestableMessageHandlerContext();

        // Act
        await handler.Handle(new HealthPing(), context);

        // Assert
        var sentMessage = context.SentMessages[0];
        sentMessage.Options.IsRoutingToThisEndpoint().Should().BeTrue();
    }
}
