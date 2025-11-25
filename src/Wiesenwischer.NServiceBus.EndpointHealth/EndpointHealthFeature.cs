using Microsoft.Extensions.DependencyInjection;
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

        context.Services.AddSingleton(options);
        context.Services.AddSingleton<IEndpointHealthState, EndpointHealthState>();

        context.RegisterStartupTask(provider =>
            new HealthPingStartupTask(provider.GetRequiredService<IEndpointHealthState>()));
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
}
