using Microsoft.Extensions.Configuration;
using Wiesenwischer.NServiceBus.EndpointHealth;
using Wiesenwischer.NServiceBus.EndpointHealth.AspNetCore;

namespace Wiesenwischer.NServiceBus.EndpointHealth.AspNetCore.Tests;

public class EndpointHealthOptionsConfigurationExtensionsTests
{
    [Fact]
    public void FromConfiguration_WithTransportKey_SetsTransportKey()
    {
        // Arrange
        var options = new EndpointHealthOptions();
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["EndpointHealth:TransportKey"] = "primary-sql"
        });

        // Act
        options.FromConfiguration(configuration);

        // Assert
        options.TransportKey.Should().Be("primary-sql");
    }

    [Fact]
    public void FromConfiguration_WithUnhealthyAfter_SetsUnhealthyAfter()
    {
        // Arrange
        var options = new EndpointHealthOptions();
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["EndpointHealth:UnhealthyAfter"] = "00:05:00"
        });

        // Act
        options.FromConfiguration(configuration);

        // Assert
        options.UnhealthyAfter.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void FromConfiguration_WithPingInterval_SetsPingInterval()
    {
        // Arrange
        var options = new EndpointHealthOptions();
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["EndpointHealth:PingInterval"] = "00:00:30"
        });

        // Act
        options.FromConfiguration(configuration);

        // Assert
        options.PingInterval.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void FromConfiguration_WithAllValues_SetsAllValues()
    {
        // Arrange
        var options = new EndpointHealthOptions();
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["EndpointHealth:TransportKey"] = "project-sql",
            ["EndpointHealth:UnhealthyAfter"] = "00:10:00",
            ["EndpointHealth:PingInterval"] = "00:02:00"
        });

        // Act
        options.FromConfiguration(configuration);

        // Assert
        options.TransportKey.Should().Be("project-sql");
        options.UnhealthyAfter.Should().Be(TimeSpan.FromMinutes(10));
        options.PingInterval.Should().Be(TimeSpan.FromMinutes(2));
    }

    [Fact]
    public void FromConfiguration_WithCustomSectionName_ReadsFromCustomSection()
    {
        // Arrange
        var options = new EndpointHealthOptions();
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["CustomSection:TransportKey"] = "custom-transport"
        });

        // Act
        options.FromConfiguration(configuration, "CustomSection");

        // Assert
        options.TransportKey.Should().Be("custom-transport");
    }

    [Fact]
    public void FromConfiguration_WithEmptySection_KeepsDefaults()
    {
        // Arrange
        var options = new EndpointHealthOptions();
        var configuration = CreateConfiguration(new Dictionary<string, string?>());

        // Act
        options.FromConfiguration(configuration);

        // Assert
        options.TransportKey.Should().BeNull();
        options.UnhealthyAfter.Should().Be(TimeSpan.FromMinutes(3)); // default
        options.PingInterval.Should().Be(TimeSpan.FromSeconds(60)); // default
    }

    [Fact]
    public void FromConfiguration_WithEmptyTransportKey_KeepsExistingValue()
    {
        // Arrange
        var options = new EndpointHealthOptions { TransportKey = "existing-key" };
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["EndpointHealth:TransportKey"] = ""
        });

        // Act
        options.FromConfiguration(configuration);

        // Assert
        options.TransportKey.Should().Be("existing-key");
    }

    [Fact]
    public void FromConfiguration_WithWhitespaceTransportKey_KeepsExistingValue()
    {
        // Arrange
        var options = new EndpointHealthOptions { TransportKey = "existing-key" };
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["EndpointHealth:TransportKey"] = "   "
        });

        // Act
        options.FromConfiguration(configuration);

        // Assert
        options.TransportKey.Should().Be("existing-key");
    }

    [Fact]
    public void FromConfiguration_ReturnsOptionsForChaining()
    {
        // Arrange
        var options = new EndpointHealthOptions();
        var configuration = CreateConfiguration(new Dictionary<string, string?>());

        // Act
        var result = options.FromConfiguration(configuration);

        // Assert
        result.Should().BeSameAs(options);
    }

    [Fact]
    public void FromConfiguration_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        EndpointHealthOptions options = null!;
        var configuration = CreateConfiguration(new Dictionary<string, string?>());

        // Act & Assert
        var action = () => options.FromConfiguration(configuration);
        action.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void FromConfiguration_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new EndpointHealthOptions();
        IConfiguration configuration = null!;

        // Act & Assert
        var action = () => options.FromConfiguration(configuration);
        action.Should().Throw<ArgumentNullException>().WithParameterName("configuration");
    }

    [Fact]
    public void FromConfiguration_PartialOverwrite_OnlyOverwritesProvidedValues()
    {
        // Arrange
        var options = new EndpointHealthOptions
        {
            TransportKey = "original-key",
            UnhealthyAfter = TimeSpan.FromMinutes(5),
            PingInterval = TimeSpan.FromSeconds(45)
        };
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["EndpointHealth:TransportKey"] = "new-key"
            // Note: UnhealthyAfter and PingInterval not provided
        });

        // Act
        options.FromConfiguration(configuration);

        // Assert
        options.TransportKey.Should().Be("new-key");
        options.UnhealthyAfter.Should().Be(TimeSpan.FromMinutes(5)); // unchanged
        options.PingInterval.Should().Be(TimeSpan.FromSeconds(45)); // unchanged
    }

    private static IConfiguration CreateConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
