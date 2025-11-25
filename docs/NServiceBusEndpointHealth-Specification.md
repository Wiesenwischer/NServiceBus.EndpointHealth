# NServiceBus EndpointHealth – Vollständige Spezifikation

## 1. Überblick
Diese Spezifikation beschreibt ein zweistufiges Open-Source-System zur technischen Überwachung von NServiceBus-Endpunkten. Es besteht aus zwei NuGet-Paketen:
- **Wiesenwischer.NServiceBus.EndpointHealth** (Core)
- **Wiesenwischer.NServiceBus.EndpointHealth.AspNetCore** (ASP.NET Integration)

Ziel:
- Sicherstellen, dass ein NServiceBus-Endpoint erkennt, wenn sein Message-Pump hängt, NServiceBus in einem CriticalError steckt oder die Infrastruktur (z. B. SQL-Transport) nicht mehr stabil arbeitet.
- Bereitstellen eines standardisierten HealthCheck-Mechanismus für ASP.NET Core und Container-Orchestrierung.

Diese Spezifikation enthält:
- Architektur
- Projektstruktur
- Quellcode
- API-Dokumentation
- NuGet-Metadaten
- Empfehlungen für Erweiterungen

## 2. Architekturziele
- Entkopplung von NServiceBus und Web-Stack
- Einheitliche Monitoring-Oberfläche
- Unterstützung seltener Business-Nachrichten über synthetische Health-Pings
- Kein Abhängigkeitszyklus zwischen Endpoint und WatchDog
- Erweiterbarkeit für OpenTelemetry / WatchDog / RSGO

## 3. Komponentenübersicht
### Paket 1: Wiesenwischer.NServiceBus.EndpointHealth
Enthält:
- `IEndpointHealthState`
- `EndpointHealthState`
- `EndpointHealthOptions`
- `HealthPing` (IMessage)
- `HealthPingHandler`
- `EndpointHealthFeature`
- Extension: `EnableEndpointHealth`

### Paket 2: Wiesenwischer.NServiceBus.EndpointHealth.AspNetCore
Enthält:
- `NServiceBusEndpointHealthCheck`
- Extension:  
  - `.AddNServiceBusEndpointHealth()`
  - optional `.MapNServiceBusEndpointHealthEndpoint()`

---

# Paket 1 – CORE

## 4. IEndpointHealthState
```csharp
public interface IEndpointHealthState
{
    DateTime? LastHealthPingProcessedUtc { get; }
    bool HasCriticalError { get; }
    string? CriticalErrorMessage { get; }

    void RegisterHealthPingProcessed();
    void RegisterCriticalError(string message, Exception? ex = null);
}
```

## 5. EndpointHealthState
```csharp
public class EndpointHealthState : IEndpointHealthState
{
    private readonly object _lock = new();
    private DateTime? _lastPing;
    private bool _critical;
    private string? _criticalMessage;

    public DateTime? LastHealthPingProcessedUtc { get { lock(_lock) return _lastPing; } }
    public bool HasCriticalError { get { lock(_lock) return _critical; } }
    public string? CriticalErrorMessage { get { lock(_lock) return _criticalMessage; } }

    public void RegisterHealthPingProcessed()
    {
        lock(_lock)
        {
            _lastPing = DateTime.UtcNow;
            _critical = false;
            _criticalMessage = null;
        }
    }

    public void RegisterCriticalError(string message, Exception? ex = null)
    {
        lock(_lock)
        {
            _critical = true;
            _criticalMessage = ex is null ? message : $"{message}: {ex.Message}";
        }
    }
}
```

## 6. EndpointHealthOptions
```csharp
public class EndpointHealthOptions
{
    public TimeSpan PingInterval { get; set; } = TimeSpan.FromSeconds(60);
    public TimeSpan UnhealthyAfter { get; set; } = TimeSpan.FromMinutes(2);
}
```

## 7. HealthPing (Message)
```csharp
public class HealthPing : IMessage {}
```

## 8. HealthPingHandler
```csharp
public class HealthPingHandler : IHandleMessages<HealthPing>
{
    private readonly IEndpointHealthState _state;

    public HealthPingHandler(IEndpointHealthState state)
    {
        _state = state;
    }

    public async Task Handle(HealthPing message, IMessageHandlerContext context)
    {
        _state.RegisterHealthPingProcessed();

        await context.SendLocal(new HealthPing())
                     .DelayDeliveryWith(TimeSpan.FromSeconds(60));
    }
}
```

## 9. EndpointHealthFeature
```csharp
public class EndpointHealthFeature : Feature
{
    public EndpointHealthFeature()
    {
        EnableByDefault();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        context.Services.AddSingleton<IEndpointHealthState, EndpointHealthState>();
        context.RegisterStartupTask(new HealthPingStartupTask());
    }
}

public class HealthPingStartupTask : FeatureStartupTask
{
    protected override Task OnStart(IMessageSession session)
    {
        return session.SendLocal(new HealthPing());
    }

    protected override Task OnStop(IMessageSession session)
    {
        return Task.CompletedTask;
    }
}
```

## 10. Extension – EnableEndpointHealth
```csharp
public static class EndpointHealthConfigurationExtensions
{
    public static void EnableEndpointHealth(
        this EndpointConfiguration cfg,
        Action<EndpointHealthOptions>? configure = null)
    {
        cfg.EnableFeature<EndpointHealthFeature>();

        var options = new EndpointHealthOptions();
        configure?.Invoke(options);
        cfg.GetSettings().Set(options);

        cfg.DefineCriticalErrorAction(async ctx =>
        {
            var state = (IEndpointHealthState?)ctx.Builder.Build(typeof(IEndpointHealthState));
            state?.RegisterCriticalError(ctx.Error, ctx.Exception);
            await Task.CompletedTask;
        });
    }
}
```

---

# Paket 2 – ASP.NET CORE

## 11. NServiceBusEndpointHealthCheck
```csharp
public class NServiceBusEndpointHealthCheck : IHealthCheck
{
    private readonly IEndpointHealthState _state;
    private readonly EndpointHealthOptions _options;

    public NServiceBusEndpointHealthCheck(
        IEndpointHealthState state,
        IOptions<EndpointHealthOptions> options)
    {
        _state = state;
        _options = options.Value;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var lastPing = _state.LastHealthPingProcessedUtc;

        if (_state.HasCriticalError)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Critical error detected"));
        }

        if (lastPing is null || now - lastPing > _options.UnhealthyAfter)
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy($"No HealthPing for > {_options.UnhealthyAfter}"));
        }

        return Task.FromResult(HealthCheckResult.Healthy());
    }
}
```

## 12. Extension – AddNServiceBusEndpointHealth
```csharp
public static class NServiceBusEndpointHealthChecksExtensions
{
    public static IHealthChecksBuilder AddNServiceBusEndpointHealth(
        this IHealthChecksBuilder builder)
    {
        return builder.AddCheck<NServiceBusEndpointHealthCheck>(
            "nservicebus-endpoint-health", failureStatus: null, tags: new[] { "nservicebus" });
    }
}
```

## 13. Usage Example (ASP.NET Core)
```csharp
builder.Services.AddHealthChecks()
    .AddNServiceBusEndpointHealth();

app.MapHealthChecks("/health");
```

---

# 14. Projektstruktur (für GitHub)
```text
NServiceBus.EndpointHealth/
├─ src/
│  ├─ Wiesenwischer.NServiceBus.EndpointHealth/
│  └─ Wiesenwischer.NServiceBus.EndpointHealth.AspNetCore/
├─ test/
│  ├─ Wiesenwischer.NServiceBus.EndpointHealth.Tests/
│  └─ Wiesenwischer.NServiceBus.EndpointHealth.AspNetCore.Tests/
├─ README.md
├─ LICENSE
└─ NServiceBus.EndpointHealth.sln
```

---

# 15. NuGet-Paket Metadaten – Core
```xml
<PackageId>Wiesenwischer.NServiceBus.EndpointHealth</PackageId>
<Authors>Marcus Dammann</Authors>
<Company>Wiesenwischer</Company>
<Description>NServiceBus health monitoring feature.</Description>
<PackageLicenseExpression>MIT</PackageLicenseExpression>
<RepositoryUrl>https://github.com/Wiesenwischer/NServiceBus.EndpointHealth</RepositoryUrl>
```

---

# 16. NuGet-Paket Metadaten – ASP.NET Core
```xml
<PackageId>Wiesenwischer.NServiceBus.EndpointHealth.AspNetCore</PackageId>
<Description>ASP.NET Core integration for NServiceBus EndpointHealth.</Description>
```

---

# 17. Erweiterungen (optional)
- OpenTelemetry Integration
- WatchDog Aggregation
- RSGO-Integration
- Disk/DB/Transport Checks
- Prometheus Exporter

---

# 18. Fertig – Diese Spezifikation kann von Claude direkt umgesetzt werden

