using Microsoft.Extensions.DependencyInjection;
using NServiceBus;
using Wiesenwischer.NServiceBus.EndpointHealth;

namespace Wiesenwischer.NServiceBus.EndpointHealth.IntegrationTests;

[Collection("SqlServer")]
public class EndpointHealthIntegrationTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;
    private IEndpointInstance? _endpoint;
    private EndpointHealthState? _healthState;

    public EndpointHealthIntegrationTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        // Create NServiceBus tables
        await CreateNServiceBusTablesAsync();
    }

    public async Task DisposeAsync()
    {
        if (_endpoint != null)
        {
            await _endpoint.Stop();
        }
    }

    private async Task CreateNServiceBusTablesAsync()
    {
        await using var connection = await _fixture.CreateConnectionAsync();

        // NServiceBus SQL Transport will create tables automatically
        // We just need to ensure the database is accessible
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1";
        await command.ExecuteScalarAsync();
    }

    private async Task<IEndpointInstance> StartEndpointAsync(
        EndpointHealthState healthState,
        Action<EndpointHealthOptions>? configureOptions = null)
    {
        var endpointConfig = new EndpointConfiguration($"IntegrationTest.Endpoint.{Guid.NewGuid():N}");

        // Configure SQL Transport
        var transport = endpointConfig.UseTransport<SqlServerTransport>();
        transport.ConnectionString(_fixture.ConnectionString);
        transport.DefaultSchema("dbo");

        // Configure persistence (in-memory for tests)
        endpointConfig.UsePersistence<LearningPersistence>();

        // Enable installers to create queues
        endpointConfig.EnableInstallers();

        // Register our shared health state
        endpointConfig.RegisterComponents(services =>
        {
            services.AddSingleton<IEndpointHealthState>(healthState);
            services.AddSingleton(healthState);
        });

        // Enable endpoint health with options
        endpointConfig.EnableEndpointHealth(options =>
        {
            options.PingInterval = TimeSpan.FromSeconds(2);
            options.UnhealthyAfter = TimeSpan.FromSeconds(10);
            configureOptions?.Invoke(options);
        });

        // Disable retry for faster tests
        endpointConfig.Recoverability()
            .Immediate(i => i.NumberOfRetries(0))
            .Delayed(d => d.NumberOfRetries(0));

        // Start endpoint
        return await Endpoint.Start(endpointConfig);
    }

    [Fact]
    public async Task Endpoint_WhenStarted_RegistersInitialHealthPing()
    {
        // Arrange
        _healthState = new EndpointHealthState();

        // Act
        _endpoint = await StartEndpointAsync(_healthState);

        // Allow startup task to complete
        await Task.Delay(TimeSpan.FromMilliseconds(500));

        // Assert - Initial ping should be registered on startup
        _healthState.LastHealthPingProcessedUtc.Should().NotBeNull();
        _healthState.HasCriticalError.Should().BeFalse();
    }

    [Fact]
    public async Task Endpoint_ProcessesHealthPingMessages_UpdatesTimestamp()
    {
        // Arrange
        _healthState = new EndpointHealthState();
        _endpoint = await StartEndpointAsync(_healthState, options =>
        {
            options.PingInterval = TimeSpan.FromSeconds(1);
        });

        // Wait for initial ping
        await Task.Delay(TimeSpan.FromMilliseconds(500));
        var initialPing = _healthState.LastHealthPingProcessedUtc;
        initialPing.Should().NotBeNull("Initial ping should be registered");

        // Act - Wait for the next health ping to be processed
        await Task.Delay(TimeSpan.FromSeconds(2));

        // Assert - Timestamp should be updated
        _healthState.LastHealthPingProcessedUtc.Should().NotBeNull();
        _healthState.LastHealthPingProcessedUtc.Should().BeAfter(initialPing!.Value);
    }

    [Fact]
    public async Task Endpoint_ContinuesProcessingHealthPings_StaysHealthy()
    {
        // Arrange
        _healthState = new EndpointHealthState();
        _endpoint = await StartEndpointAsync(_healthState, options =>
        {
            options.PingInterval = TimeSpan.FromSeconds(1);
            options.UnhealthyAfter = TimeSpan.FromSeconds(5);
        });

        // Wait for initial ping
        await Task.Delay(TimeSpan.FromMilliseconds(500));

        // Act - Wait and check multiple times
        for (int i = 0; i < 3; i++)
        {
            await Task.Delay(TimeSpan.FromSeconds(1.5));

            // Assert - Should stay healthy (timestamp within threshold)
            _healthState.LastHealthPingProcessedUtc.Should().NotBeNull();
            var timeSinceLastPing = DateTime.UtcNow - _healthState.LastHealthPingProcessedUtc!.Value;
            timeSinceLastPing.Should().BeLessThan(TimeSpan.FromSeconds(5),
                $"Health ping should be processed within threshold (iteration {i + 1})");
        }
    }
}
