using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus;
using Wiesenwischer.NServiceBus.EndpointHealth;

namespace Wiesenwischer.NServiceBus.EndpointHealth.IntegrationTests;

/// <summary>
/// Integration tests for configuration loading from IConfiguration (appsettings.json).
/// </summary>
[Collection("SqlServer")]
public class ConfigurationIntegrationTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;
    private IEndpointInstance? _endpoint;

    public ConfigurationIntegrationTests(SqlServerFixture fixture)
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

    [Fact]
    public void FromConfiguration_LoadsTransportKey()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EndpointHealth:TransportKey"] = "production-sql"
            })
            .Build();

        var options = new EndpointHealthOptions();

        // Act
        options.FromConfiguration(config);

        // Assert
        options.TransportKey.Should().Be("production-sql");
    }

    [Fact]
    public void FromConfiguration_LoadsPingInterval()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EndpointHealth:PingInterval"] = "00:00:30"
            })
            .Build();

        var options = new EndpointHealthOptions();

        // Act
        options.FromConfiguration(config);

        // Assert
        options.PingInterval.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void FromConfiguration_LoadsUnhealthyAfter()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EndpointHealth:UnhealthyAfter"] = "00:02:00"
            })
            .Build();

        var options = new EndpointHealthOptions();

        // Act
        options.FromConfiguration(config);

        // Assert
        options.UnhealthyAfter.Should().Be(TimeSpan.FromMinutes(2));
    }

    [Fact]
    public void FromConfiguration_LoadsAllValues()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EndpointHealth:TransportKey"] = "multi-config-test",
                ["EndpointHealth:PingInterval"] = "00:00:45",
                ["EndpointHealth:UnhealthyAfter"] = "00:05:00"
            })
            .Build();

        var options = new EndpointHealthOptions();

        // Act
        options.FromConfiguration(config);

        // Assert
        options.TransportKey.Should().Be("multi-config-test");
        options.PingInterval.Should().Be(TimeSpan.FromSeconds(45));
        options.UnhealthyAfter.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void FromConfiguration_WithCustomSectionName_LoadsFromCustomSection()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MyCustomSection:TransportKey"] = "custom-section-transport",
                ["MyCustomSection:PingInterval"] = "00:01:30"
            })
            .Build();

        var options = new EndpointHealthOptions();

        // Act
        options.FromConfiguration(config, "MyCustomSection");

        // Assert
        options.TransportKey.Should().Be("custom-section-transport");
        options.PingInterval.Should().Be(TimeSpan.FromSeconds(90));
    }

    [Fact]
    public void FromConfiguration_WithMissingSection_KeepsDefaults()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var options = new EndpointHealthOptions();
        var defaultPingInterval = options.PingInterval;
        var defaultUnhealthyAfter = options.UnhealthyAfter;

        // Act
        options.FromConfiguration(config);

        // Assert
        options.TransportKey.Should().BeNull();
        options.PingInterval.Should().Be(defaultPingInterval);
        options.UnhealthyAfter.Should().Be(defaultUnhealthyAfter);
    }

    [Fact]
    public void FromConfiguration_WithPartialValues_OnlyOverridesProvided()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EndpointHealth:TransportKey"] = "partial-config"
                // PingInterval and UnhealthyAfter not provided
            })
            .Build();

        var options = new EndpointHealthOptions();
        var defaultPingInterval = options.PingInterval;
        var defaultUnhealthyAfter = options.UnhealthyAfter;

        // Act
        options.FromConfiguration(config);

        // Assert
        options.TransportKey.Should().Be("partial-config");
        options.PingInterval.Should().Be(defaultPingInterval);
        options.UnhealthyAfter.Should().Be(defaultUnhealthyAfter);
    }

    [Fact]
    public void FromConfiguration_WithInvalidTimeSpan_KeepsDefault()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EndpointHealth:PingInterval"] = "invalid-timespan",
                ["EndpointHealth:UnhealthyAfter"] = "also-invalid"
            })
            .Build();

        var options = new EndpointHealthOptions();
        var defaultPingInterval = options.PingInterval;
        var defaultUnhealthyAfter = options.UnhealthyAfter;

        // Act
        options.FromConfiguration(config);

        // Assert
        options.PingInterval.Should().Be(defaultPingInterval);
        options.UnhealthyAfter.Should().Be(defaultUnhealthyAfter);
    }

    [Fact]
    public async Task ConfigureEndpointHealth_WithConfiguration_AppliesSettings()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EndpointHealth:TransportKey"] = "e2e-config-test",
                ["EndpointHealth:PingInterval"] = "00:00:02",
                ["EndpointHealth:UnhealthyAfter"] = "00:00:10"
            })
            .Build();

        var healthState = new EndpointHealthState("e2e-config-test");

        var endpointConfig = new EndpointConfiguration($"Config.E2E.{Guid.NewGuid():N}");

        var transport = endpointConfig.UseTransport<SqlServerTransport>();
        transport.ConnectionString(_fixture.ConnectionString);
        transport.DefaultSchema("dbo");

        endpointConfig.UsePersistence<LearningPersistence>();
        endpointConfig.EnableInstallers();

        endpointConfig.RegisterComponents(services =>
        {
            services.AddSingleton<IEndpointHealthState>(healthState);
        });

        // Act - Use ConfigureEndpointHealth with IConfiguration
        endpointConfig.ConfigureEndpointHealth(config, options =>
        {
            options.HealthState = healthState;
        });

        endpointConfig.Recoverability()
            .Immediate(i => i.NumberOfRetries(0))
            .Delayed(d => d.NumberOfRetries(0));

        _endpoint = await Endpoint.Start(endpointConfig);

        // Wait for health ping
        await Task.Delay(TimeSpan.FromMilliseconds(500));

        // Assert
        healthState.TransportKey.Should().Be("e2e-config-test");
        healthState.LastHealthPingProcessedUtc.Should().NotBeNull();
    }

    [Fact]
    public void FromConfiguration_SupportsEnvironmentVariableFormat()
    {
        // Arrange - Environment variables use double underscore for hierarchy separator
        // When using AddInMemoryCollection, we need to use colon (:) separator directly
        // The double underscore (__) is only translated by AddEnvironmentVariables()
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ENDPOINTHEALTH:TRANSPORTKEY"] = "env-var-transport",
                ["ENDPOINTHEALTH:PINGINTERVAL"] = "00:00:20",
                ["ENDPOINTHEALTH:UNHEALTHYAFTER"] = "00:01:00"
            })
            .Build();

        var options = new EndpointHealthOptions();

        // Act - Case-insensitive section lookup
        options.FromConfiguration(config, "ENDPOINTHEALTH");

        // Assert
        options.TransportKey.Should().Be("env-var-transport");
        options.PingInterval.Should().Be(TimeSpan.FromSeconds(20));
        options.UnhealthyAfter.Should().Be(TimeSpan.FromMinutes(1));
    }
}
