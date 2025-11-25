# API Reference

Complete API reference for NServiceBus EndpointHealth packages.

## Wiesenwischer.NServiceBus.EndpointHealth

### Namespace: `Wiesenwischer.NServiceBus.EndpointHealth`

---

### IEndpointHealthState

Interface for accessing endpoint health state.

```csharp
public interface IEndpointHealthState
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `LastHealthPingProcessedUtc` | `DateTime?` | UTC timestamp of last processed health ping. Null if never processed. |
| `HasCriticalError` | `bool` | True if a critical error has occurred. |
| `CriticalErrorMessage` | `string?` | Error message if critical error occurred. |

#### Methods

| Method | Description |
|--------|-------------|
| `RegisterHealthPingProcessed()` | Records that a health ping was processed. Clears critical error state. |
| `RegisterCriticalError(string message, Exception? ex)` | Records a critical error. |

---

### EndpointHealthState

Thread-safe implementation of `IEndpointHealthState`.

```csharp
public class EndpointHealthState : IEndpointHealthState
```

Registered as singleton in DI container.

---

### EndpointHealthOptions

Configuration options for health monitoring.

```csharp
public class EndpointHealthOptions
```

#### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `PingInterval` | `TimeSpan` | 60s | Interval between health pings |
| `UnhealthyAfter` | `TimeSpan` | 2min | Time without ping before unhealthy |

---

### HealthPing

Marker message for health pings.

```csharp
public class HealthPing : IMessage
```

---

### HealthPingHandler

Handler for `HealthPing` messages.

```csharp
public class HealthPingHandler : IHandleMessages<HealthPing>
```

#### Constructor

```csharp
public HealthPingHandler(IEndpointHealthState state, EndpointHealthOptions options)
```

---

### EndpointHealthFeature

NServiceBus feature for health monitoring.

```csharp
public class EndpointHealthFeature : Feature
```

---

### EndpointHealthConfigurationExtensions

Extension methods for `EndpointConfiguration`.

```csharp
public static class EndpointHealthConfigurationExtensions
```

#### Methods

| Method | Description |
|--------|-------------|
| `EnableEndpointHealth(Action<EndpointHealthOptions>? configure)` | Enables health monitoring on the endpoint. |

#### Example

```csharp
endpointConfig.EnableEndpointHealth(options =>
{
    options.PingInterval = TimeSpan.FromSeconds(30);
    options.UnhealthyAfter = TimeSpan.FromMinutes(2);
});
```

---

## Wiesenwischer.NServiceBus.EndpointHealth.AspNetCore

### Namespace: `Wiesenwischer.NServiceBus.EndpointHealth.AspNetCore`

---

### NServiceBusEndpointHealthCheck

ASP.NET Core health check implementation.

```csharp
public class NServiceBusEndpointHealthCheck : IHealthCheck
```

#### Constructor

```csharp
public NServiceBusEndpointHealthCheck(
    IEndpointHealthState state,
    EndpointHealthOptions options)
```

#### Methods

| Method | Description |
|--------|-------------|
| `CheckHealthAsync(HealthCheckContext, CancellationToken)` | Performs the health check. |

#### Health Check Logic

1. If `HasCriticalError` is true → `Unhealthy`
2. If `LastHealthPingProcessedUtc` is null → `Unhealthy`
3. If time since last ping > `UnhealthyAfter` → `Unhealthy`
4. Otherwise → `Healthy`

---

### NServiceBusEndpointHealthChecksExtensions

Extension methods for `IHealthChecksBuilder`.

```csharp
public static class NServiceBusEndpointHealthChecksExtensions
```

#### Methods

| Method | Description |
|--------|-------------|
| `AddNServiceBusEndpointHealth(...)` | Adds the NServiceBus endpoint health check. |

#### Signature

```csharp
public static IHealthChecksBuilder AddNServiceBusEndpointHealth(
    this IHealthChecksBuilder builder,
    string name = "nservicebus-endpoint",
    HealthStatus? failureStatus = null,
    IEnumerable<string>? tags = null)
```

#### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `name` | `string` | `"nservicebus-endpoint"` | Name of the health check |
| `failureStatus` | `HealthStatus?` | `Unhealthy` | Status on failure |
| `tags` | `IEnumerable<string>?` | `["nservicebus", "endpoint", "messaging"]` | Tags for filtering |

#### Example

```csharp
builder.Services.AddHealthChecks()
    .AddNServiceBusEndpointHealth(
        name: "my-endpoint",
        failureStatus: HealthStatus.Degraded,
        tags: ["critical"]);
```
