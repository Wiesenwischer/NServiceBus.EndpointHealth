# Architecture

This document describes the internal architecture of NServiceBus EndpointHealth.

## Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                    NServiceBus Endpoint                          │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │                  EndpointHealthFeature                     │  │
│  │  ┌─────────────────┐  ┌─────────────────────────────────┐ │  │
│  │  │HealthPingHandler│  │  EndpointHealthState (Singleton) │ │  │
│  │  │                 │  │  - LastHealthPingProcessedUtc    │ │  │
│  │  │  1. Update state│──│  - HasCriticalError              │ │  │
│  │  │  2. Schedule    │  │  - CriticalErrorMessage          │ │  │
│  │  │     next ping   │  └─────────────────────────────────┘ │  │
│  │  └────────┬────────┘             ▲                        │  │
│  │           │                      │                        │  │
│  │           ▼                      │ Query State            │  │
│  │  ┌─────────────────┐             │                        │  │
│  │  │   HealthPing    │             │                        │  │
│  │  │   (IMessage)    │             │                        │  │
│  │  └────────┬────────┘             │                        │  │
│  │           │                      │                        │  │
│  │           ▼                      │                        │  │
│  │  ┌─────────────────┐             │                        │  │
│  │  │   Transport     │             │                        │  │
│  │  │ (SQL/RabbitMQ)  │             │                        │  │
│  │  └─────────────────┘             │                        │  │
│  └──────────────────────────────────┼────────────────────────┘  │
└─────────────────────────────────────┼────────────────────────────┘
                                      │
                                      │
┌─────────────────────────────────────┼────────────────────────────┐
│               ASP.NET Core Application                           │
│  ┌──────────────────────────────────┼────────────────────────┐  │
│  │         NServiceBusEndpointHealthCheck                     │  │
│  │                                  │                         │  │
│  │   CheckHealthAsync() ────────────┘                         │  │
│  │     - Check HasCriticalError                               │  │
│  │     - Check LastPing vs UnhealthyAfter                     │  │
│  │     - Return Healthy/Unhealthy                             │  │
│  └────────────────────────────────────────────────────────────┘  │
│                                                                   │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │                   /health endpoint                          │  │
│  │                                                             │  │
│  │   HTTP 200 OK          → Healthy                           │  │
│  │   HTTP 503 Unavailable → Unhealthy                         │  │
│  └────────────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────────────┘
```

## Components

### Core Package

#### IEndpointHealthState

The interface for accessing health state. Thread-safe and injectable.

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

#### EndpointHealthState

Thread-safe implementation using lock-based synchronization. Registered as a singleton in NServiceBus's DI container.

#### HealthPing / HealthPingHandler

- `HealthPing`: Empty marker message (`IMessage`)
- `HealthPingHandler`: Processes pings and schedules the next one

#### EndpointHealthFeature

NServiceBus Feature that:
1. Registers services in DI
2. Starts the `HealthPingStartupTask`

#### HealthPingStartupTask

Runs when the endpoint starts:
1. Registers initial health ping timestamp
2. Sends the first `HealthPing` message

### ASP.NET Core Package

#### NServiceBusEndpointHealthCheck

Implements `IHealthCheck`:
1. Checks `HasCriticalError`
2. Compares `LastHealthPingProcessedUtc` against `UnhealthyAfter`
3. Returns appropriate `HealthCheckResult`

## Message Flow

```
1. Endpoint Starts
   └── HealthPingStartupTask.OnStart()
       ├── RegisterHealthPingProcessed()    [State: LastPing = now]
       └── Send HealthPing (no delay)

2. HealthPing Received
   └── HealthPingHandler.Handle()
       ├── RegisterHealthPingProcessed()    [State: LastPing = now]
       └── Send HealthPing (delay: PingInterval)

3. Health Check Query
   └── NServiceBusEndpointHealthCheck.CheckHealthAsync()
       ├── HasCriticalError? → Unhealthy
       ├── LastPing is null? → Unhealthy
       ├── (now - LastPing) > UnhealthyAfter? → Unhealthy
       └── Otherwise → Healthy
```

## Thread Safety

`EndpointHealthState` uses a simple lock-based approach:
- All reads and writes are protected by the same lock
- This ensures consistent state across concurrent access
- Lock contention is minimal due to short critical sections

## Critical Error Handling

The `EnableEndpointHealth` extension registers a critical error handler:

```csharp
cfg.DefineCriticalErrorAction(async (context, cancellationToken) =>
{
    healthState?.RegisterCriticalError(context.Error, context.Exception);
    await Task.CompletedTask;
});
```

This ensures critical errors are captured even when:
- The message pump is stuck
- The transport is unavailable
- Recovery attempts have been exhausted

## Extension Points

### Custom Health State

You can register your own `IEndpointHealthState` implementation:

```csharp
endpointConfig.RegisterComponents(services =>
{
    services.AddSingleton<IEndpointHealthState, MyCustomHealthState>();
});

// Then enable health (it will use your registered implementation)
endpointConfig.EnableEndpointHealth();
```

### Additional Health Checks

Combine with other health checks:

```csharp
builder.Services.AddHealthChecks()
    .AddNServiceBusEndpointHealth()
    .AddSqlServer(connectionString)
    .AddRabbitMQ(rabbitConnectionString);
```
