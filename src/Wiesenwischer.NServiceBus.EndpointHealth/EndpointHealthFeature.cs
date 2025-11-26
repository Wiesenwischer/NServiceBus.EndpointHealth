#if NET9_0_OR_GREATER
using Microsoft.Extensions.DependencyInjection;
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

        context.RegisterStartupTask(provider =>
            new HealthPingStartupTask(provider.GetRequiredService<IEndpointHealthState>()));
#else
        // NServiceBus 7.x uses internal container separate from ASP.NET Core DI
        // External state is required via options.HealthState
        var state = options.HealthState
            ?? throw new InvalidOperationException(
                "For NServiceBus 7.x, you must provide an external IEndpointHealthState instance via EndpointHealthOptions.HealthState. " +
                "Create an EndpointHealthState instance, register it in ASP.NET Core DI, and set it on options.HealthState.");
        context.Container.RegisterSingleton(options);
        context.Container.RegisterSingleton<IEndpointHealthState>(state);

        context.RegisterStartupTask(new HealthPingStartupTask(state));
#endif
    }
}

/// <summary>
/// Startup task that sends the initial health ping when the endpoint starts.
/// </summary>
internal class HealthPingStartupTask : FeatureStartupTask
{
    private readonly IEndpointHealthState _state;

    public HealthPingStartupTask(IEndpointHealthState state)
    {
        _state = state;
    }

#if NET9_0_OR_GREATER
    protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
    {
        // Register initial state so we're healthy from the start
        _state.RegisterHealthPingProcessed();

        var sendOptions = new SendOptions();
        sendOptions.RouteToThisEndpoint();

        return session.Send(new HealthPing(), sendOptions, cancellationToken);
    }

    protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
#else
    protected override Task OnStart(IMessageSession session)
    {
        // Register initial state so we're healthy from the start
        _state.RegisterHealthPingProcessed();

        var sendOptions = new SendOptions();
        sendOptions.RouteToThisEndpoint();

        return session.Send(new HealthPing(), sendOptions);
    }

    protected override Task OnStop(IMessageSession session)
    {
        return Task.CompletedTask;
    }
#endif
}
