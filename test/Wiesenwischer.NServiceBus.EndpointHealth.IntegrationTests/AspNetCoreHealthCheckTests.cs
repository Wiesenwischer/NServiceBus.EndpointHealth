using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NServiceBus;
using Wiesenwischer.NServiceBus.EndpointHealth;

namespace Wiesenwischer.NServiceBus.EndpointHealth.IntegrationTests;

/// <summary>
/// E2E tests for ASP.NET Core health check integration.
/// </summary>
[Collection("SqlServer")]
public class AspNetCoreHealthCheckTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;
    private IEndpointInstance? _endpoint;
    private TestServer? _server;
    private HttpClient? _client;
    private EndpointHealthState? _healthState;

    public AspNetCoreHealthCheckTests(SqlServerFixture fixture)
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

    private TestServer CreateTestServer(EndpointHealthState healthState, Action<EndpointHealthOptions>? configureOptions = null)
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<IEndpointHealthState>(healthState);

                if (configureOptions != null)
                {
                    services.AddHealthChecks()
                        .AddNServiceBusEndpointHealth(configureOptions);
                }
                else
                {
                    services.AddHealthChecks()
                        .AddNServiceBusEndpointHealth();
                }
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

    private async Task<IEndpointInstance> StartEndpointAsync(EndpointHealthState healthState)
    {
        var endpointConfig = new EndpointConfiguration($"E2E.Test.{Guid.NewGuid():N}");

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
            options.PingInterval = TimeSpan.FromSeconds(1);
            options.UnhealthyAfter = TimeSpan.FromSeconds(5);
        });

        endpointConfig.Recoverability()
            .Immediate(i => i.NumberOfRetries(0))
            .Delayed(d => d.NumberOfRetries(0));

        return await Endpoint.Start(endpointConfig);
    }

    [Fact]
    public async Task HealthEndpoint_WhenEndpointHealthy_ReturnsHealthy()
    {
        // Arrange
        _healthState = new EndpointHealthState();
        _server = CreateTestServer(_healthState);
        _client = _server.CreateClient();

        // Start endpoint and wait for health ping
        _endpoint = await StartEndpointAsync(_healthState);
        await Task.Delay(TimeSpan.FromMilliseconds(500));

        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy");
    }

    [Fact]
    public async Task HealthEndpoint_WhenNoHealthPingReceived_ReturnsUnhealthy()
    {
        // Arrange
        _healthState = new EndpointHealthState();
        _server = CreateTestServer(_healthState);
        _client = _server.CreateClient();

        // Do NOT start endpoint - no health pings will be registered

        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Unhealthy");
    }

    [Fact]
    public async Task HealthEndpoint_WhenCriticalError_ReturnsUnhealthy()
    {
        // Arrange
        _healthState = new EndpointHealthState();
        _server = CreateTestServer(_healthState);
        _client = _server.CreateClient();

        // Simulate endpoint started but then critical error occurred
        _healthState.RegisterHealthPingProcessed();
        _healthState.RegisterCriticalError("Simulated critical error", new InvalidOperationException("Test exception"));

        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task HealthEndpoint_WithCustomFailureStatus_ReturnsDegraded()
    {
        // Arrange
        _healthState = new EndpointHealthState();
        _server = CreateTestServer(_healthState, options =>
        {
            options.UnhealthyAfter = TimeSpan.FromSeconds(1);
        });

        // Override the health check with Degraded status
        _server.Dispose();
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<IEndpointHealthState>(_healthState);
                services.AddHealthChecks()
                    .AddNServiceBusEndpointHealth(failureStatus: HealthStatus.Degraded);
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapHealthChecks("/health");
                });
            });
        _server = new TestServer(builder);
        _client = _server.CreateClient();

        // No health ping registered

        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK); // Degraded returns 200
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Degraded");
    }

    [Fact]
    public async Task HealthEndpoint_WithCustomTags_CanBeFiltered()
    {
        // Arrange
        _healthState = new EndpointHealthState();
        _healthState.RegisterHealthPingProcessed();

        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<IEndpointHealthState>(_healthState);
                services.AddHealthChecks()
                    .AddNServiceBusEndpointHealth(tags: ["critical", "messaging"]);
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapHealthChecks("/health/critical", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
                    {
                        Predicate = check => check.Tags.Contains("critical")
                    });
                    endpoints.MapHealthChecks("/health/other", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
                    {
                        Predicate = check => check.Tags.Contains("non-existent-tag")
                    });
                });
            });

        _server = new TestServer(builder);
        _client = _server.CreateClient();

        // Act
        var criticalResponse = await _client.GetAsync("/health/critical");
        var otherResponse = await _client.GetAsync("/health/other");

        // Assert
        criticalResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var criticalContent = await criticalResponse.Content.ReadAsStringAsync();
        criticalContent.Should().Contain("Healthy");

        otherResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var otherContent = await otherResponse.Content.ReadAsStringAsync();
        otherContent.Should().Contain("Healthy"); // No checks matched, so healthy by default
    }
}
