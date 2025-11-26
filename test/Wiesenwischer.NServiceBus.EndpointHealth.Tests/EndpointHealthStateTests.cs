using Wiesenwischer.NServiceBus.EndpointHealth;

namespace Wiesenwischer.NServiceBus.EndpointHealth.Tests;

public class EndpointHealthStateTests
{
    [Fact]
    public void InitialState_HasNoLastPing()
    {
        // Arrange
        var state = new EndpointHealthState();

        // Assert
        state.LastHealthPingProcessedUtc.Should().BeNull();
    }

    [Fact]
    public void InitialState_HasNoCriticalError()
    {
        // Arrange
        var state = new EndpointHealthState();

        // Assert
        state.HasCriticalError.Should().BeFalse();
        state.CriticalErrorMessage.Should().BeNull();
    }

    [Fact]
    public void RegisterHealthPingProcessed_SetsLastPingTime()
    {
        // Arrange
        var state = new EndpointHealthState();
        var beforeTime = DateTime.UtcNow;

        // Act
        state.RegisterHealthPingProcessed();

        // Assert
        var afterTime = DateTime.UtcNow;
        state.LastHealthPingProcessedUtc.Should().NotBeNull();
        state.LastHealthPingProcessedUtc.Should().BeOnOrAfter(beforeTime);
        state.LastHealthPingProcessedUtc.Should().BeOnOrBefore(afterTime);
    }

    [Fact]
    public void RegisterHealthPingProcessed_ClearsCriticalError()
    {
        // Arrange
        var state = new EndpointHealthState();
        state.RegisterCriticalError("Test error");

        // Act
        state.RegisterHealthPingProcessed();

        // Assert
        state.HasCriticalError.Should().BeFalse();
        state.CriticalErrorMessage.Should().BeNull();
    }

    [Fact]
    public void RegisterCriticalError_SetsCriticalErrorState()
    {
        // Arrange
        var state = new EndpointHealthState();

        // Act
        state.RegisterCriticalError("Something went wrong");

        // Assert
        state.HasCriticalError.Should().BeTrue();
        state.CriticalErrorMessage.Should().Be("Something went wrong");
    }

    [Fact]
    public void RegisterCriticalError_WithException_IncludesExceptionMessage()
    {
        // Arrange
        var state = new EndpointHealthState();
        var exception = new InvalidOperationException("Database connection failed");

        // Act
        state.RegisterCriticalError("Transport error", exception);

        // Assert
        state.HasCriticalError.Should().BeTrue();
        state.CriticalErrorMessage.Should().Be("Transport error: Database connection failed");
    }

    [Fact]
    public void RegisterCriticalError_DoesNotAffectLastPingTime()
    {
        // Arrange
        var state = new EndpointHealthState();
        state.RegisterHealthPingProcessed();
        var lastPing = state.LastHealthPingProcessedUtc;

        // Act
        state.RegisterCriticalError("Error");

        // Assert
        state.LastHealthPingProcessedUtc.Should().Be(lastPing);
    }

    [Fact]
    public async Task State_IsThreadSafe()
    {
        // Arrange
        var state = new EndpointHealthState();
        var iterations = 1000;
        var tasks = new List<Task>();

        // Act - run multiple operations concurrently
        for (int i = 0; i < iterations; i++)
        {
            tasks.Add(Task.Run(() => state.RegisterHealthPingProcessed()));
            tasks.Add(Task.Run(() => state.RegisterCriticalError("Error")));
            tasks.Add(Task.Run(() => _ = state.LastHealthPingProcessedUtc));
            tasks.Add(Task.Run(() => _ = state.HasCriticalError));
            tasks.Add(Task.Run(() => _ = state.CriticalErrorMessage));
        }

        // Assert - no exceptions should be thrown
        var allTasksCompleted = await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(10)).ContinueWith(t => !t.IsFaulted);
        allTasksCompleted.Should().BeTrue();
    }

    [Fact]
    public void DefaultConstructor_HasNullTransportKey()
    {
        // Arrange & Act
        var state = new EndpointHealthState();

        // Assert
        state.TransportKey.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithTransportKey_SetsTransportKey()
    {
        // Arrange & Act
        var state = new EndpointHealthState("primary-sql");

        // Assert
        state.TransportKey.Should().Be("primary-sql");
    }

    [Fact]
    public void Constructor_WithNullTransportKey_SetsNullTransportKey()
    {
        // Arrange & Act
        var state = new EndpointHealthState(null);

        // Assert
        state.TransportKey.Should().BeNull();
    }

    [Fact]
    public void TransportKey_IsImmutable()
    {
        // Arrange
        var state = new EndpointHealthState("initial-key");

        // Assert - TransportKey should not be settable after construction
        state.TransportKey.Should().Be("initial-key");

        // Operations should not affect TransportKey
        state.RegisterHealthPingProcessed();
        state.TransportKey.Should().Be("initial-key");

        state.RegisterCriticalError("Error");
        state.TransportKey.Should().Be("initial-key");
    }
}
