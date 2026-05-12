#if NET9_0_OR_GREATER
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
#endif
using NServiceBus;
using NServiceBus.Features;

namespace Wiesenwischer.NServiceBus.EndpointHealth;

/// <summary>
/// NServiceBus feature that enables endpoint health monitoring through synthetic health pings.
/// </summary>
public class EndpointHealthFeature : Feature
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EndpointHealthFeature"/> class.
    /// </summary>
    public EndpointHealthFeature()
    {
        Defaults(s =>
        {
            if (!s.HasSetting<EndpointHealthOptions>())
            {
                s.Set(new EndpointHealthOptions());
            }
        });
    }

    /// <summary>
    /// Sets up the feature by registering services and the startup task.
    /// </summary>
    protected override void Setup(FeatureConfigurationContext context)
    {
        var options = context.Settings.Get<EndpointHealthOptions>();

#if NET9_0_OR_GREATER
        // NServiceBus 8.x uses Microsoft.Extensions.DependencyInjection
        // Use external state if provided, otherwise create new instance
        var state = options.HealthState ?? new EndpointHealthState(options.TransportKey);
        context.Services.AddSingleton(options);
        context.Services.AddSingleton<IEndpointHealthState>(state);

        // Treat any processed message as a health signal so a backlogged input queue
        // (synthetic HealthPing stuck behind business messages) cannot trigger a false stuck-pump alert.
        context.Pipeline.Register(new HealthSignalBehavior(state), "Updates endpoint health state on every incoming message.");

        context.RegisterStartupTask(provider =>
        {
            var logger = provider.GetService<ILogger<HealthPingStartupTask>>()
                ?? NullLogger<HealthPingStartupTask>.Instance;
            // Resolve from DI so endpoints that register a shared IEndpointHealthState via
            // RegisterComponents (after Feature.Setup) get the same instance the rest of
            // the app sees, not the one created locally above.
            var resolvedState = provider.GetRequiredService<IEndpointHealthState>();
            return new HealthPingStartupTask(resolvedState, options, logger);
        });
#else
        // NServiceBus 7.x uses internal container separate from ASP.NET Core DI
        // External state is required via options.HealthState
        var state = options.HealthState
            ?? throw new InvalidOperationException(
                "For NServiceBus 7.x, you must provide an external IEndpointHealthState instance via EndpointHealthOptions.HealthState. " +
                "Create an EndpointHealthState instance, register it in ASP.NET Core DI, and set it on options.HealthState.");
        context.Container.RegisterSingleton(options);
        context.Container.RegisterSingleton<IEndpointHealthState>(state);

        // Treat any processed message as a health signal so a backlogged input queue
        // (synthetic HealthPing stuck behind business messages) cannot trigger a false stuck-pump alert.
        context.Pipeline.Register(new HealthSignalBehavior(state), "Updates endpoint health state on every incoming message.");

        context.RegisterStartupTask(new HealthPingStartupTask(state, options));
#endif
    }
}

/// <summary>
/// Startup task that registers the initial health ping and runs the periodic ping loop
/// while the endpoint is active.
/// </summary>
internal class HealthPingStartupTask : FeatureStartupTask
{
    private readonly IEndpointHealthState _state;
    private readonly EndpointHealthOptions _options;
    private CancellationTokenSource? _cts;
    private Task? _pingLoop;
#if NET9_0_OR_GREATER
    private readonly ILogger<HealthPingStartupTask> _logger;

    public HealthPingStartupTask(IEndpointHealthState state, EndpointHealthOptions options, ILogger<HealthPingStartupTask> logger)
    {
        _state = state;
        _options = options;
        _logger = logger;
    }
#else
    public HealthPingStartupTask(IEndpointHealthState state, EndpointHealthOptions options)
    {
        _state = state;
        _options = options;
    }
#endif

#if NET9_0_OR_GREATER
    protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("EndpointHealth starting. TransportKey={TransportKey}", _state.TransportKey);

        // Register initial state so we're healthy from the start
        _state.RegisterHealthPingProcessed();
        _logger.LogInformation("EndpointHealth initialized. Interval={Interval}.", _options.PingInterval);

        _cts = new CancellationTokenSource();
        _pingLoop = Task.Run(() => SendPingsAsync(session, _cts.Token));
        return Task.CompletedTask;
    }

    private async Task SendPingsAsync(IMessageSession session, CancellationToken ct)
    {
        if (_options.StartupDelay > TimeSpan.Zero)
        {
            try { await Task.Delay(_options.StartupDelay, ct); }
            catch (OperationCanceledException) { return; }
        }

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var sendOptions = new SendOptions();
                sendOptions.RouteToThisEndpoint();
                await session.Send(new HealthPing(), sendOptions, ct);
                _logger.LogInformation("HealthPing sent.");
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HealthPing send failed; retrying in {Interval}", _options.PingInterval);
            }

            try { await Task.Delay(_options.PingInterval, ct); }
            catch (OperationCanceledException) { break; }
        }
    }

    protected override async Task OnStop(IMessageSession session, CancellationToken cancellationToken = default)
    {
        _cts?.Cancel();
        if (_pingLoop != null)
        {
            try { await _pingLoop; } catch { /* shutdown */ }
        }
        _cts?.Dispose();
        _logger.LogInformation("EndpointHealth stopping. TransportKey={TransportKey}, LastPing={LastPing}",
            _state.TransportKey, _state.LastHealthPingProcessedUtc);
    }
#else
    protected override Task OnStart(IMessageSession session)
    {
        _state.RegisterHealthPingProcessed();

        _cts = new CancellationTokenSource();
        _pingLoop = Task.Run(() => SendPingsAsync(session, _cts.Token));
        return Task.CompletedTask;
    }

    private async Task SendPingsAsync(IMessageSession session, CancellationToken ct)
    {
        if (_options.StartupDelay > TimeSpan.Zero)
        {
            try { await Task.Delay(_options.StartupDelay, ct); }
            catch (OperationCanceledException) { return; }
        }

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var sendOptions = new SendOptions();
                sendOptions.RouteToThisEndpoint();
                await session.Send(new HealthPing(), sendOptions);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch
            {
                // Swallow and retry next interval.
            }

            try { await Task.Delay(_options.PingInterval, ct); }
            catch (OperationCanceledException) { break; }
        }
    }

    protected override async Task OnStop(IMessageSession session)
    {
        _cts?.Cancel();
        if (_pingLoop != null)
        {
            try { await _pingLoop; } catch { /* shutdown */ }
        }
        _cts?.Dispose();
    }
#endif
}
