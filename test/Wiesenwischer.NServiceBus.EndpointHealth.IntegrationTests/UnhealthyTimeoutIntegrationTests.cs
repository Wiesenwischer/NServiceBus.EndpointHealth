using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus;
using Wiesenwischer.NServiceBus.EndpointHealth;

namespace Wiesenwischer.NServiceBus.EndpointHealth.IntegrationTests;

/// <summary>
/// Integration tests for unhealthy timeout scenarios.
/// Tests the behavior when health pings stop being received.
/// </summary>
[Collection("SqlServer")]
public class UnhealthyTimeoutIntegrationTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;
    private IEndpointInstance? _endpoint;
    private TestServer? _server;
    private HttpClient? _client;
    private EndpointHealthState? _healthState;

    public UnhealthyTimeoutIntegrationTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        _server?.Dispose();
        if (_endpoint != null)
        {
            await _endpoint.Stop();
        }
    }

    private TestServer CreateTestServer(EndpointHealthState healthState, TimeSpan unhealthyAfter)
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<IEndpointHealthState>(healthState);
                services.AddHealthChecks()
                    .AddNServiceBusEndpointHealth(options =>
                    {
                        options.UnhealthyAfter = unhealthyAfter;
                    });
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapHealthChecks("/health");
                });
            });

        return new TestServer(builder);
    }

    [Fact]
    public async Task HealthCheck_WhenPingWithinThreshold_ReturnsHealthy()
    {
        // Arrange
        _healthState = new EndpointHealthState();
        var unhealthyAfter = TimeSpan.FromSeconds(5);
        _server = CreateTestServer(_healthState, unhealthyAfter);
        _client = _server.CreateClient();

        // Register a health ping
        _healthState.RegisterHealthPingProcessed();

        // Act - Check immediately (well within threshold)
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthCheck_WhenPingExceedsThreshold_ReturnsUnhealthy()
    {
        // Arrange
        _healthState = new EndpointHealthState();
        var unhealthyAfter = TimeSpan.FromSeconds(1);
        _server = CreateTestServer(_healthState, unhealthyAfter);
        _client = _server.CreateClient();

        // Register a health ping
        _healthState.RegisterHealthPingProcessed();

        // Verify healthy first
        var healthyResponse = await _client.GetAsync("/health");
        healthyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - Wait for threshold to expire
        await Task.Delay(TimeSpan.FromSeconds(1.5));

        // Assert
        var unhealthyResponse = await _client.GetAsync("/health");
        unhealthyResponse.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task HealthCheck_WhenPingJustBeforeThreshold_ReturnsHealthy()
    {
        // Arrange
        _healthState = new EndpointHealthState();
        var unhealthyAfter = TimeSpan.FromSeconds(2);
        _server = CreateTestServer(_healthState, unhealthyAfter);
        _client = _server.CreateClient();

        // Register a health ping
        _healthState.RegisterHealthPingProcessed();

        // Wait just under the threshold
        await Task.Delay(TimeSpan.FromSeconds(1.5));

        // Act
        var response = await _client.GetAsync("/health");

        // Assert - Should still be healthy
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthCheck_WhenPingRefreshed_StaysHealthy()
    {
        // Arrange
        _healthState = new EndpointHealthState();
        var unhealthyAfter = TimeSpan.FromSeconds(2);
        _server = CreateTestServer(_healthState, unhealthyAfter);
        _client = _server.CreateClient();

        // Register initial ping
        _healthState.RegisterHealthPingProcessed();

        // Wait 1 second, then refresh
        await Task.Delay(TimeSpan.FromSeconds(1));
        _healthState.RegisterHealthPingProcessed();

        // Wait another 1.5 seconds (would exceed original threshold of 2s)
        await Task.Delay(TimeSpan.FromSeconds(1.5));

        // Act
        var response = await _client.GetAsync("/health");

        // Assert - Should still be healthy because ping was refreshed
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthCheck_WithVeryShortThreshold_DetectsUnhealthyQuickly()
    {
        // Arrange
        _healthState = new EndpointHealthState();
        var unhealthyAfter = TimeSpan.FromMilliseconds(200);
        _server = CreateTestServer(_healthState, unhealthyAfter);
        _client = _server.CreateClient();

        // Register a health ping
        _healthState.RegisterHealthPingProcessed();

        // Verify healthy
        var healthyResponse = await _client.GetAsync("/health");
        healthyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - Wait for very short threshold
        await Task.Delay(TimeSpan.FromMilliseconds(300));

        // Assert
        var unhealthyResponse = await _client.GetAsync("/health");
        unhealthyResponse.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task HealthCheck_WithLongThreshold_ToleratesDelays()
    {
        // Arrange
        _healthState = new EndpointHealthState();
        var unhealthyAfter = TimeSpan.FromSeconds(10);
        _server = CreateTestServer(_healthState, unhealthyAfter);
        _client = _server.CreateClient();

        // Register a health ping
        _healthState.RegisterHealthPingProcessed();

        // Wait a significant time but under threshold
        await Task.Delay(TimeSpan.FromSeconds(5));

        // Act
        var response = await _client.GetAsync("/health");

        // Assert - Should still be healthy with long threshold
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthCheck_RecoveryFromUnhealthy_WhenPingReceived()
    {
        // Arrange
        _healthState = new EndpointHealthState();
        var unhealthyAfter = TimeSpan.FromSeconds(1);
        _server = CreateTestServer(_healthState, unhealthyAfter);
        _client = _server.CreateClient();

        // Register a ping, then let it expire
        _healthState.RegisterHealthPingProcessed();
        await Task.Delay(TimeSpan.FromSeconds(1.5));

        // Verify unhealthy
        var unhealthyResponse = await _client.GetAsync("/health");
        unhealthyResponse.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

        // Act - New ping received
        _healthState.RegisterHealthPingProcessed();

        // Assert - Should be healthy again
        var recoveredResponse = await _client.GetAsync("/health");
        recoveredResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthCheck_NoPingEverReceived_ReturnsUnhealthy()
    {
        // Arrange
        _healthState = new EndpointHealthState();
        var unhealthyAfter = TimeSpan.FromSeconds(1);
        _server = CreateTestServer(_healthState, unhealthyAfter);
        _client = _server.CreateClient();

        // No ping registered at all

        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task RealEndpoint_StopsProcessingPings_BecomesUnhealthy()
    {
        // Arrange - Start endpoint with very short intervals
        _healthState = new EndpointHealthState();
        var unhealthyAfter = TimeSpan.FromSeconds(3);
        _server = CreateTestServer(_healthState, unhealthyAfter);
        _client = _server.CreateClient();

        var endpointConfig = new EndpointConfiguration($"Timeout.Test.{Guid.NewGuid():N}");

        var transport = endpointConfig.UseTransport<SqlServerTransport>();
        transport.ConnectionString(_fixture.ConnectionString);
        transport.DefaultSchema("dbo");

        endpointConfig.UsePersistence<LearningPersistence>();
        endpointConfig.EnableInstallers();

        endpointConfig.RegisterComponents(services =>
        {
            services.AddSingleton<IEndpointHealthState>(_healthState);
        });

        endpointConfig.EnableEndpointHealth(options =>
        {
            options.PingInterval = TimeSpan.FromSeconds(1);
            options.UnhealthyAfter = unhealthyAfter;
        });

        endpointConfig.Recoverability()
            .Immediate(i => i.NumberOfRetries(0))
            .Delayed(d => d.NumberOfRetries(0));

        _endpoint = await Endpoint.Start(endpointConfig);

        // Wait for initial ping
        await Task.Delay(TimeSpan.FromMilliseconds(500));

        // Verify healthy
        var healthyResponse = await _client.GetAsync("/health");
        healthyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - Stop endpoint (simulates crash/failure)
        await _endpoint.Stop();
        _endpoint = null;

        // Wait for threshold to expire
        await Task.Delay(TimeSpan.FromSeconds(4));

        // Assert - Should now be unhealthy
        var unhealthyResponse = await _client.GetAsync("/health");
        unhealthyResponse.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }
}
