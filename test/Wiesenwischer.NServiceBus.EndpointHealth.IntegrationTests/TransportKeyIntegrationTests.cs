using Microsoft.Extensions.DependencyInjection;
using NServiceBus;
using Wiesenwischer.NServiceBus.EndpointHealth;

namespace Wiesenwischer.NServiceBus.EndpointHealth.IntegrationTests;

/// <summary>
/// Integration tests for TransportKey functionality.
/// </summary>
[Collection("SqlServer")]
public class TransportKeyIntegrationTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;
    private IEndpointInstance? _endpoint;
    private EndpointHealthState? _healthState;

    public TransportKeyIntegrationTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        if (_endpoint != null)
        {
            await _endpoint.Stop();
        }
    }

    private async Task<IEndpointInstance> StartEndpointAsync(
        EndpointHealthState healthState,
        string? transportKey = null)
    {
        var endpointConfig = new EndpointConfiguration($"TransportKey.Test.{Guid.NewGuid():N}");

        var transport = endpointConfig.UseTransport<SqlServerTransport>();
        transport.ConnectionString(_fixture.ConnectionString);
        transport.DefaultSchema("dbo");

        endpointConfig.UsePersistence<LearningPersistence>();
        endpointConfig.EnableInstallers();

        endpointConfig.RegisterComponents(services =>
        {
            services.AddSingleton<IEndpointHealthState>(healthState);
        });

        endpointConfig.EnableEndpointHealth(options =>
        {
            options.TransportKey = transportKey;
            options.PingInterval = TimeSpan.FromSeconds(1);
            options.UnhealthyAfter = TimeSpan.FromSeconds(5);
        });

        endpointConfig.Recoverability()
            .Immediate(i => i.NumberOfRetries(0))
            .Delayed(d => d.NumberOfRetries(0));

        return await Endpoint.Start(endpointConfig);
    }

    [Fact]
    public async Task EndpointWithTransportKey_HealthStateContainsTransportKey()
    {
        // Arrange
        const string transportKey = "primary-sql-cluster";
        _healthState = new EndpointHealthState(transportKey);

        // Act
        _endpoint = await StartEndpointAsync(_healthState, transportKey);
        await Task.Delay(TimeSpan.FromMilliseconds(500));

        // Assert
        _healthState.TransportKey.Should().Be(transportKey);
        _healthState.LastHealthPingProcessedUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task EndpointWithoutTransportKey_HealthStateHasNullTransportKey()
    {
        // Arrange
        _healthState = new EndpointHealthState();

        // Act
        _endpoint = await StartEndpointAsync(_healthState, transportKey: null);
        await Task.Delay(TimeSpan.FromMilliseconds(500));

        // Assert
        _healthState.TransportKey.Should().BeNull();
        _healthState.LastHealthPingProcessedUtc.Should().NotBeNull();
    }

    [Fact]
    public void MultipleHealthStatesWithDifferentTransportKeys_AreIndependent()
    {
        // Arrange
        var primaryState = new EndpointHealthState("primary-sql");
        var secondaryState = new EndpointHealthState("secondary-sql");
        var legacyState = new EndpointHealthState("legacy-rabbitmq");

        // Act
        primaryState.RegisterHealthPingProcessed();
        secondaryState.RegisterCriticalError("Connection lost", null);
        // legacyState left without any updates

        // Assert
        primaryState.TransportKey.Should().Be("primary-sql");
        primaryState.LastHealthPingProcessedUtc.Should().NotBeNull();
        primaryState.HasCriticalError.Should().BeFalse();

        secondaryState.TransportKey.Should().Be("secondary-sql");
        secondaryState.HasCriticalError.Should().BeTrue();

        legacyState.TransportKey.Should().Be("legacy-rabbitmq");
        legacyState.LastHealthPingProcessedUtc.Should().BeNull();
        legacyState.HasCriticalError.Should().BeFalse();
    }

    [Fact]
    public void TransportKey_IsImmutable_CannotBeChangedAfterCreation()
    {
        // Arrange
        const string initialKey = "initial-transport";
        _healthState = new EndpointHealthState(initialKey);

        // Act & Assert - TransportKey has no setter, so this is verified by compilation
        _healthState.TransportKey.Should().Be(initialKey);

        // Verify multiple reads return same value
        var key1 = _healthState.TransportKey;
        var key2 = _healthState.TransportKey;
        key1.Should().Be(key2);
    }

    [Fact]
    public void TransportKey_WithEmptyString_IsPreserved()
    {
        // Arrange & Act
        _healthState = new EndpointHealthState("");

        // Assert
        _healthState.TransportKey.Should().Be("");
    }

    [Fact]
    public void TransportKey_WithWhitespace_IsPreserved()
    {
        // Arrange & Act
        _healthState = new EndpointHealthState("  ");

        // Assert
        _healthState.TransportKey.Should().Be("  ");
    }
}
