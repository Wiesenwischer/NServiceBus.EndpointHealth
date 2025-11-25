using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Wiesenwischer.NServiceBus.EndpointHealth;
using Wiesenwischer.NServiceBus.EndpointHealth.AspNetCore;

namespace Wiesenwischer.NServiceBus.EndpointHealth.AspNetCore.Tests;

public class NServiceBusEndpointHealthChecksExtensionsTests
{
    private ServiceCollection CreateServicesWithRequiredDependencies()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IEndpointHealthState>(new EndpointHealthState());
        services.AddSingleton(new EndpointHealthOptions());
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        return services;
    }

    [Fact]
    public void AddNServiceBusEndpointHealth_RegistersHealthCheck()
    {
        // Arrange
        var services = CreateServicesWithRequiredDependencies();

        services.AddHealthChecks()
            .AddNServiceBusEndpointHealth();

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var healthCheckService = serviceProvider.GetService<HealthCheckService>();

        // Assert
        healthCheckService.Should().NotBeNull();
    }

    [Fact]
    public void AddNServiceBusEndpointHealth_UsesDefaultName()
    {
        // Arrange
        var services = CreateServicesWithRequiredDependencies();

        // Act
        services.AddHealthChecks()
            .AddNServiceBusEndpointHealth();

        var serviceProvider = services.BuildServiceProvider();
        var healthCheckOptions = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>();

        // Assert
        healthCheckOptions.Value.Registrations
            .Should().ContainSingle(r => r.Name == "nservicebus-endpoint");
    }

    [Fact]
    public void AddNServiceBusEndpointHealth_AllowsCustomName()
    {
        // Arrange
        var services = CreateServicesWithRequiredDependencies();

        // Act
        services.AddHealthChecks()
            .AddNServiceBusEndpointHealth(name: "my-custom-endpoint-check");

        var serviceProvider = services.BuildServiceProvider();
        var healthCheckOptions = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>();

        // Assert
        healthCheckOptions.Value.Registrations
            .Should().ContainSingle(r => r.Name == "my-custom-endpoint-check");
    }

    [Fact]
    public void AddNServiceBusEndpointHealth_HasDefaultTags()
    {
        // Arrange
        var services = CreateServicesWithRequiredDependencies();

        // Act
        services.AddHealthChecks()
            .AddNServiceBusEndpointHealth();

        var serviceProvider = services.BuildServiceProvider();
        var healthCheckOptions = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>();

        // Assert
        var registration = healthCheckOptions.Value.Registrations.Single();
        registration.Tags.Should().Contain("nservicebus");
        registration.Tags.Should().Contain("endpoint");
        registration.Tags.Should().Contain("messaging");
    }

    [Fact]
    public void AddNServiceBusEndpointHealth_AllowsCustomTags()
    {
        // Arrange
        var services = CreateServicesWithRequiredDependencies();

        // Act
        services.AddHealthChecks()
            .AddNServiceBusEndpointHealth(tags: ["custom", "tags"]);

        var serviceProvider = services.BuildServiceProvider();
        var healthCheckOptions = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>();

        // Assert
        var registration = healthCheckOptions.Value.Registrations.Single();
        registration.Tags.Should().Contain("custom");
        registration.Tags.Should().Contain("tags");
    }

    [Fact]
    public void AddNServiceBusEndpointHealth_DefaultFailureStatusIsUnhealthy()
    {
        // Arrange
        var services = CreateServicesWithRequiredDependencies();

        // Act
        services.AddHealthChecks()
            .AddNServiceBusEndpointHealth();

        var serviceProvider = services.BuildServiceProvider();
        var healthCheckOptions = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>();

        // Assert
        var registration = healthCheckOptions.Value.Registrations.Single();
        registration.FailureStatus.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public void AddNServiceBusEndpointHealth_AllowsCustomFailureStatus()
    {
        // Arrange
        var services = CreateServicesWithRequiredDependencies();

        // Act
        services.AddHealthChecks()
            .AddNServiceBusEndpointHealth(failureStatus: HealthStatus.Degraded);

        var serviceProvider = services.BuildServiceProvider();
        var healthCheckOptions = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>();

        // Assert
        var registration = healthCheckOptions.Value.Registrations.Single();
        registration.FailureStatus.Should().Be(HealthStatus.Degraded);
    }
}
