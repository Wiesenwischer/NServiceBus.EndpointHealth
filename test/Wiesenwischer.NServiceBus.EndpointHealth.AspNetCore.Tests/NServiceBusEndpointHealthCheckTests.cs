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

    [Fact]
    public async Task CheckHealthAsync_DataContainsHasCriticalError()
    {
        // Arrange
        _stateMock.Setup(s => s.HasCriticalError).Returns(false);
        _stateMock.Setup(s => s.LastHealthPingProcessedUtc).Returns(DateTime.UtcNow);

        // Act
        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Data.Should().ContainKey(NServiceBusEndpointHealthCheck.DataKeyHasCriticalError);
        result.Data[NServiceBusEndpointHealthCheck.DataKeyHasCriticalError].Should().Be(false);
    }

    [Fact]
    public async Task CheckHealthAsync_DataContainsUnhealthyAfter()
    {
        // Arrange
        _stateMock.Setup(s => s.HasCriticalError).Returns(false);
        _stateMock.Setup(s => s.LastHealthPingProcessedUtc).Returns(DateTime.UtcNow);

        // Act
        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Data.Should().ContainKey(NServiceBusEndpointHealthCheck.DataKeyUnhealthyAfter);
        result.Data[NServiceBusEndpointHealthCheck.DataKeyUnhealthyAfter].Should().Be(_options.UnhealthyAfter.ToString());
    }

    [Fact]
    public async Task CheckHealthAsync_DataContainsTransportKey_WhenSet()
    {
        // Arrange
        _stateMock.Setup(s => s.HasCriticalError).Returns(false);
        _stateMock.Setup(s => s.LastHealthPingProcessedUtc).Returns(DateTime.UtcNow);
        _stateMock.Setup(s => s.TransportKey).Returns("primary-sql");

        // Act
        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Data.Should().ContainKey(NServiceBusEndpointHealthCheck.DataKeyTransportKey);
        result.Data[NServiceBusEndpointHealthCheck.DataKeyTransportKey].Should().Be("primary-sql");
    }

    [Fact]
    public async Task CheckHealthAsync_DataOmitsTransportKey_WhenNull()
    {
        // Arrange
        _stateMock.Setup(s => s.HasCriticalError).Returns(false);
        _stateMock.Setup(s => s.LastHealthPingProcessedUtc).Returns(DateTime.UtcNow);
        _stateMock.Setup(s => s.TransportKey).Returns((string?)null);

        // Act
        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Data.Should().NotContainKey(NServiceBusEndpointHealthCheck.DataKeyTransportKey);
    }

    [Fact]
    public async Task CheckHealthAsync_DataContainsLastHealthPingProcessedUtc_WhenSet()
    {
        // Arrange
        var pingTime = DateTime.UtcNow.AddSeconds(-30);
        _stateMock.Setup(s => s.HasCriticalError).Returns(false);
        _stateMock.Setup(s => s.LastHealthPingProcessedUtc).Returns(pingTime);

        // Act
        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Data.Should().ContainKey(NServiceBusEndpointHealthCheck.DataKeyLastHealthPingProcessedUtc);
        result.Data[NServiceBusEndpointHealthCheck.DataKeyLastHealthPingProcessedUtc].Should().Be(pingTime.ToString("O"));
    }

    [Fact]
    public async Task CheckHealthAsync_DataOmitsLastHealthPingProcessedUtc_WhenNull()
    {
        // Arrange
        _stateMock.Setup(s => s.HasCriticalError).Returns(false);
        _stateMock.Setup(s => s.LastHealthPingProcessedUtc).Returns((DateTime?)null);

        // Act
        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Data.Should().NotContainKey(NServiceBusEndpointHealthCheck.DataKeyLastHealthPingProcessedUtc);
    }

    [Fact]
    public async Task CheckHealthAsync_DataContainsTimeSinceLastPing_WhenPingExists()
    {
        // Arrange
        _stateMock.Setup(s => s.HasCriticalError).Returns(false);
        _stateMock.Setup(s => s.LastHealthPingProcessedUtc).Returns(DateTime.UtcNow.AddSeconds(-30));

        // Act
        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Data.Should().ContainKey(NServiceBusEndpointHealthCheck.DataKeyTimeSinceLastPing);
    }

    [Fact]
    public async Task CheckHealthAsync_DataContainsCriticalErrorMessage_WhenError()
    {
        // Arrange
        _stateMock.Setup(s => s.HasCriticalError).Returns(true);
        _stateMock.Setup(s => s.CriticalErrorMessage).Returns("Transport failed");

        // Act
        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Data.Should().ContainKey(NServiceBusEndpointHealthCheck.DataKeyCriticalErrorMessage);
        result.Data[NServiceBusEndpointHealthCheck.DataKeyCriticalErrorMessage].Should().Be("Transport failed");
    }

    [Fact]
    public async Task CheckHealthAsync_DataOmitsCriticalErrorMessage_WhenNoError()
    {
        // Arrange
        _stateMock.Setup(s => s.HasCriticalError).Returns(false);
        _stateMock.Setup(s => s.LastHealthPingProcessedUtc).Returns(DateTime.UtcNow);
        _stateMock.Setup(s => s.CriticalErrorMessage).Returns((string?)null);

        // Act
        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Data.Should().NotContainKey(NServiceBusEndpointHealthCheck.DataKeyCriticalErrorMessage);
    }
}
