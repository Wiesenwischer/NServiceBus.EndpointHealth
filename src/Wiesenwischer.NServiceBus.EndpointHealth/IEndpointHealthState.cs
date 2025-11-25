namespace Wiesenwischer.NServiceBus.EndpointHealth;

/// <summary>
/// Provides access to the health state of an NServiceBus endpoint.
/// </summary>
public interface IEndpointHealthState
{
    /// <summary>
    /// Gets the UTC timestamp of the last successfully processed health ping.
    /// Returns null if no health ping has been processed yet.
    /// </summary>
    DateTime? LastHealthPingProcessedUtc { get; }

    /// <summary>
    /// Gets a value indicating whether a critical error has occurred.
    /// </summary>
    bool HasCriticalError { get; }

    /// <summary>
    /// Gets the critical error message if a critical error has occurred.
    /// </summary>
    string? CriticalErrorMessage { get; }

    /// <summary>
    /// Registers that a health ping was successfully processed.
    /// This resets the critical error state.
    /// </summary>
    void RegisterHealthPingProcessed();

    /// <summary>
    /// Registers that a critical error has occurred.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="ex">The optional exception that caused the error.</param>
    void RegisterCriticalError(string message, Exception? ex = null);
}
