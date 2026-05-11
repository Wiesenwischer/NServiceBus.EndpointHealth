#if NET9_0_OR_GREATER
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NServiceBus;

namespace Wiesenwischer.NServiceBus.EndpointHealth;

/// <summary>
/// Background service that periodically sends HealthPing messages to verify the message pump is working.
/// Uses a simple timer instead of NServiceBus delayed delivery to avoid dependency on the
/// delayed message forwarding infrastructure.
/// </summary>
internal class HealthPingBackgroundService : BackgroundService
{
    private readonly IMessageSession _messageSession;
    private readonly IEndpointHealthState _state;
    private readonly EndpointHealthOptions _options;
    private readonly ILogger<HealthPingBackgroundService> _logger;

    public HealthPingBackgroundService(
        IMessageSession messageSession,
        IEndpointHealthState state,
        EndpointHealthOptions options,
        ILogger<HealthPingBackgroundService> logger)
    {
        _messageSession = messageSession;
        _state = state;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_options.StartupDelay > TimeSpan.Zero)
        {
            await Task.Delay(_options.StartupDelay, stoppingToken);
        }

        _logger.LogInformation("HealthPing background service started. Interval={Interval}, InstanceId={InstanceId}",
            _options.PingInterval, _state.InstanceId);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var sendOptions = new SendOptions();
                sendOptions.RouteToThisEndpoint();

                await _messageSession.Send(new HealthPing { InstanceId = _state.InstanceId }, sendOptions, stoppingToken);
                _logger.LogInformation("HealthPing sent by background service. InstanceId={InstanceId}", _state.InstanceId);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send HealthPing from background service. Will retry in {Interval}", _options.PingInterval);
            }

            try
            {
                await Task.Delay(_options.PingInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("HealthPing background service stopped.");
    }
}
#endif
