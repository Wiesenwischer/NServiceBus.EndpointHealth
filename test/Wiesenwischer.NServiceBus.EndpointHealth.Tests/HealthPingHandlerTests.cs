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

        // Assert — stale ping is silently dropped: no reschedule, no state update
        context.SentMessages.Should().BeEmpty();
        state.Verify(s => s.RegisterHealthPingProcessed(), Times.Never);
    }

    [Fact]
    public async Task Handle_PropagatesInstanceIdToNextPing()
    {
        // Arrange — message carries the same InstanceId as the current endpoint instance
        var instanceId = Guid.NewGuid();

        var state = new Mock<IEndpointHealthState>();
        state.Setup(s => s.InstanceId).Returns(instanceId);

        var options = new EndpointHealthOptions { PingInterval = TimeSpan.FromSeconds(30) };
        var handler = new HealthPingHandler(state.Object, options);
        var context = new TestableMessageHandlerContext();

        // Act
        await handler.Handle(new HealthPing { InstanceId = instanceId }, context);

        // Assert — the next scheduled ping carries the same InstanceId
        context.SentMessages.Should().HaveCount(1);
        var nextPing = context.SentMessages[0].Message.Should().BeOfType<HealthPing>().Subject;
        nextPing.InstanceId.Should().Be(instanceId);
    }
}
