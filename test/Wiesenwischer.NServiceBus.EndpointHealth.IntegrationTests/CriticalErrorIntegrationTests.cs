using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus;
using Wiesenwischer.NServiceBus.EndpointHealth;

namespace Wiesenwischer.NServiceBus.EndpointHealth.IntegrationTests;

/// <summary>
/// Integration tests for critical error handling scenarios.
/// </summary>
[Collection("SqlServer")]
public class CriticalErrorIntegrationTests : IAsyncLifetime
{
    private TestServer? _server;
    private HttpClient? _client;
    private EndpointHealthState? _healthState;

    public CriticalErrorIntegrationTests(SqlServerFixture fixture)
    {
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        _client?.Dispose();
        _server?.Dispose();
        return Task.CompletedTask;
    }

    private TestServer CreateTestServer(EndpointHealthState healthState)
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<IEndpointHealthState>(healthState);
                services.AddHealthChecks()
                    .AddNServiceBusEndpointHealth();
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
    public async Task CriticalError_AfterHealthyState_MarksEndpointUnhealthy()
    {
        // Arrange
        _healthState = new EndpointHealthState();
        _server = CreateTestServer(_healthState);
        _client = _server.CreateClient();

        // Simulate healthy state
        _healthState.RegisterHealthPingProcessed();

        // Verify healthy first
        var healthyResponse = await _client.GetAsync("/health");
        healthyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - Register critical error
        _healthState.RegisterCriticalError("Database connection lost", new InvalidOperationException("Connection timeout"));

        // Assert
        var unhealthyResponse = await _client.GetAsync("/health");
        unhealthyResponse.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task CriticalError_RecoveryAfterHealthPing_RestoresHealthy()
    {
        // Arrange
        _healthState = new EndpointHealthState();
        _server = CreateTestServer(_healthState);
        _client = _server.CreateClient();

        // Start in healthy state
        _healthState.RegisterHealthPingProcessed();

        // Register critical error
        _healthState.RegisterCriticalError("Temporary error", null);

        // Verify unhealthy
        var unhealthyResponse = await _client.GetAsync("/health");
        unhealthyResponse.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

        // Act - Health ping clears critical error
        _healthState.RegisterHealthPingProcessed();

        // Assert
        var healthyResponse = await _client.GetAsync("/health");
        healthyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        _healthState.HasCriticalError.Should().BeFalse();
    }

    [Fact]
    public void CriticalError_WithException_IncludesExceptionDetails()
    {
        // Arrange
        _healthState = new EndpointHealthState();
        var exception = new InvalidOperationException("Inner exception message");

        // Act
        _healthState.RegisterCriticalError("Outer error message", exception);

        // Assert
        _healthState.HasCriticalError.Should().BeTrue();
        _healthState.CriticalErrorMessage.Should().Contain("Outer error message");
        _healthState.CriticalErrorMessage.Should().Contain("Inner exception message");
    }

    [Fact]
    public void CriticalError_WithoutException_ContainsOnlyMessage()
    {
        // Arrange
        _healthState = new EndpointHealthState();

        // Act
        _healthState.RegisterCriticalError("Simple error message", null);

        // Assert
        _healthState.HasCriticalError.Should().BeTrue();
        _healthState.CriticalErrorMessage.Should().Be("Simple error message");
    }

    [Fact]
    public async Task CriticalError_TakesPrecedenceOverRecentHealthPing()
    {
        // Arrange
        _healthState = new EndpointHealthState();
        _server = CreateTestServer(_healthState);
        _client = _server.CreateClient();

        // Recent health ping
        _healthState.RegisterHealthPingProcessed();

        // Act - Both conditions exist: recent ping AND critical error
        // Critical error should take precedence
        _healthState.RegisterCriticalError("Critical failure", null);

        // Assert - Still healthy from last ping, but critical error overrides
        _healthState.LastHealthPingProcessedUtc.Should().NotBeNull();
        _healthState.HasCriticalError.Should().BeTrue();

        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public void MultipleCriticalErrors_LastOneWins()
    {
        // Arrange
        _healthState = new EndpointHealthState();

        // Act
        _healthState.RegisterCriticalError("First error", null);
        _healthState.RegisterCriticalError("Second error", null);
        _healthState.RegisterCriticalError("Third error", null);

        // Assert
        _healthState.CriticalErrorMessage.Should().Be("Third error");
    }
}
