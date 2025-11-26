using Wiesenwischer.NServiceBus.EndpointHealth;

namespace Wiesenwischer.NServiceBus.EndpointHealth.Tests;

public class EndpointHealthOptionsTests
{
    [Fact]
    public void DefaultPingInterval_Is60Seconds()
    {
        // Arrange & Act
        var options = new EndpointHealthOptions();

        // Assert
        options.PingInterval.Should().Be(TimeSpan.FromSeconds(60));
    }

    [Fact]
    public void DefaultUnhealthyAfter_Is3Minutes()
    {
        // Arrange & Act
        var options = new EndpointHealthOptions();

        // Assert
        options.UnhealthyAfter.Should().Be(TimeSpan.FromMinutes(3));
    }

    [Fact]
    public void PingInterval_CanBeCustomized()
    {
        // Arrange & Act
        var options = new EndpointHealthOptions
        {
            PingInterval = TimeSpan.FromSeconds(30)
        };

        // Assert
        options.PingInterval.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void UnhealthyAfter_CanBeCustomized()
    {
        // Arrange & Act
        var options = new EndpointHealthOptions
        {
            UnhealthyAfter = TimeSpan.FromMinutes(5)
        };

        // Assert
        options.UnhealthyAfter.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void DefaultTransportKey_IsNull()
    {
        // Arrange & Act
        var options = new EndpointHealthOptions();

        // Assert
        options.TransportKey.Should().BeNull();
    }

    [Fact]
    public void TransportKey_CanBeSet()
    {
        // Arrange & Act
        var options = new EndpointHealthOptions
        {
            TransportKey = "primary-sql"
        };

        // Assert
        options.TransportKey.Should().Be("primary-sql");
    }
}
