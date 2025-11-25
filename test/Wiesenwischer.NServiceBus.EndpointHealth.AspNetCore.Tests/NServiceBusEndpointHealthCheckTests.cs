using Microsoft.Extensions.Diagnostics.HealthChecks;
using Wiesenwischer.NServiceBus.EndpointHealth;
using Wiesenwischer.NServiceBus.EndpointHealth.AspNetCore;

namespace Wiesenwischer.NServiceBus.EndpointHealth.AspNetCore.Tests;

public class NServiceBusEndpointHealthCheckTests
{
    private readonly Mock<IEndpointHealthState> _stateMock;
    private readonly EndpointHealthOptions _options;
    private readonly NServiceBusEndpointHealthCheck _healthCheck;

    public NServiceBusEndpointHealthCheckTests()
    {
        _stateMock = new Mock<IEndpointHealthState>();
        _options = new EndpointHealthOptions
        {
            UnhealthyAfter = TimeSpan.FromMinutes(2)
        };
        _healthCheck = new NServiceBusEndpointHealthCheck(_stateMock.Object, _options);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenCriticalError_ReturnsUnhealthy()
    {
        // Arrange
        _stateMock.Setup(s => s.HasCriticalError).Returns(true);
        _stateMock.Setup(s => s.CriticalErrorMessage).Returns("Database connection lost");

        // Act
        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("Critical error detected");
        result.Description.Should().Contain("Database connection lost");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenNoHealthPingProcessed_ReturnsUnhealthy()
    {
        // Arrange
        _stateMock.Setup(s => s.HasCriticalError).Returns(false);
        _stateMock.Setup(s => s.LastHealthPingProcessedUtc).Returns((DateTime?)null);

        // Act
        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("No health ping has been processed yet");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenLastPingTooOld_ReturnsUnhealthy()
    {
        // Arrange
        _stateMock.Setup(s => s.HasCriticalError).Returns(false);
        _stateMock.Setup(s => s.LastHealthPingProcessedUtc)
            .Returns(DateTime.UtcNow.AddMinutes(-5)); // 5 minutes ago, threshold is 2 minutes

        // Act
        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("No health ping for");
        result.Description.Should().Contain("Message pump may be stuck");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenHealthy_ReturnsHealthy()
    {
        // Arrange
        _stateMock.Setup(s => s.HasCriticalError).Returns(false);
        _stateMock.Setup(s => s.LastHealthPingProcessedUtc)
            .Returns(DateTime.UtcNow.AddSeconds(-30)); // 30 seconds ago, threshold is 2 minutes

        // Act
        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("Last health ping");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenJustOnThreshold_ReturnsHealthy()
    {
        // Arrange
        _stateMock.Setup(s => s.HasCriticalError).Returns(false);
        _stateMock.Setup(s => s.LastHealthPingProcessedUtc)
            .Returns(DateTime.UtcNow.AddMinutes(-2).AddSeconds(1)); // Just under 2 minutes

        // Act
        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_CriticalErrorTakesPrecedenceOverStaleTimestamp()
    {
        // Arrange - Both critical error and stale timestamp
        _stateMock.Setup(s => s.HasCriticalError).Returns(true);
        _stateMock.Setup(s => s.CriticalErrorMessage).Returns("Critical failure");
        _stateMock.Setup(s => s.LastHealthPingProcessedUtc)
            .Returns(DateTime.UtcNow.AddMinutes(-5));

        // Act
        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert - Critical error message should be returned
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("Critical error detected");
    }

    [Fact]
    public async Task CheckHealthAsync_RespectsCustomUnhealthyAfterOption()
    {
        // Arrange
        var customOptions = new EndpointHealthOptions
        {
            UnhealthyAfter = TimeSpan.FromMinutes(10)
        };
        var healthCheck = new NServiceBusEndpointHealthCheck(_stateMock.Object, customOptions);

        _stateMock.Setup(s => s.HasCriticalError).Returns(false);
        _stateMock.Setup(s => s.LastHealthPingProcessedUtc)
            .Returns(DateTime.UtcNow.AddMinutes(-5)); // 5 minutes ago, but threshold is 10 minutes

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
    }
}
