namespace Wiesenwischer.NServiceBus.EndpointHealth;

/// <summary>
/// Thread-safe implementation of <see cref="IEndpointHealthState"/> that tracks
/// the health state of an NServiceBus endpoint.
/// </summary>
public class EndpointHealthState : IEndpointHealthState
{
    private readonly object _lock = new();
    private DateTime? _lastPing;
    private bool _critical;
    private string? _criticalMessage;

    /// <inheritdoc />
    public DateTime? LastHealthPingProcessedUtc
    {
        get
        {
            lock (_lock)
            {
                return _lastPing;
            }
        }
    }

    /// <inheritdoc />
    public bool HasCriticalError
    {
        get
        {
            lock (_lock)
            {
                return _critical;
            }
        }
    }

    /// <inheritdoc />
    public string? CriticalErrorMessage
    {
        get
        {
            lock (_lock)
            {
                return _criticalMessage;
            }
        }
    }

    /// <inheritdoc />
    public void RegisterHealthPingProcessed()
    {
        lock (_lock)
        {
            _lastPing = DateTime.UtcNow;
            _critical = false;
            _criticalMessage = null;
        }
    }

    /// <inheritdoc />
    public void RegisterCriticalError(string message, Exception? ex = null)
    {
        lock (_lock)
        {
            _critical = true;
            _criticalMessage = ex is null ? message : $"{message}: {ex.Message}";
        }
    }
}
