# Troubleshooting

Common issues and solutions when using NServiceBus EndpointHealth.

## Health Check Always Returns Unhealthy

### Symptom
The `/health` endpoint always returns `503 Service Unavailable`.

### Possible Causes

#### 1. Endpoint Not Started
The health state is only updated after the endpoint starts and processes its first health ping.

**Solution**: Ensure the endpoint starts before health checks are queried.

```csharp
// Correct order
var endpoint = await Endpoint.Start(endpointConfig);
// Now health checks will work
```

#### 2. Transport Issues
If the transport (SQL Server, RabbitMQ, etc.) is unavailable, health pings cannot be processed.

**Solution**: Check transport connectivity and logs.

#### 3. UnhealthyAfter Too Short
If `UnhealthyAfter` is shorter than `PingInterval`, the endpoint will appear unhealthy between pings.

**Solution**: Ensure `UnhealthyAfter >= 2 × PingInterval`.

```csharp
endpointConfig.EnableEndpointHealth(options =>
{
    options.PingInterval = TimeSpan.FromSeconds(30);
    options.UnhealthyAfter = TimeSpan.FromMinutes(2); // At least 4x PingInterval
});
```

#### 4. Services Not Registered
The `IEndpointHealthState` and `EndpointHealthOptions` must be available in the DI container.

**Solution**: Ensure `EnableEndpointHealth()` is called on the endpoint configuration.

---

## Critical Error Not Detected

### Symptom
NServiceBus encounters a critical error, but `HasCriticalError` remains `false`.

### Possible Causes

#### 1. Custom Critical Error Handler Overriding
If you define your own critical error action, it may override the health tracking.

**Solution**: Call the health state registration in your custom handler:

```csharp
// If you need custom critical error handling
endpointConfig.DefineCriticalErrorAction(async (context, token) =>
{
    // Get the health state and register the error
    var healthState = serviceProvider.GetService<IEndpointHealthState>();
    healthState?.RegisterCriticalError(context.Error, context.Exception);

    // Your custom logic here
    await customHandling();
});
```

**Better Solution**: Call `EnableEndpointHealth()` after any custom critical error configuration, or don't override the critical error action.

---

## Health Pings Not Being Processed

### Symptom
`LastHealthPingProcessedUtc` never updates after initial startup.

### Possible Causes

#### 1. Delayed Delivery Not Supported
Some transport configurations may not support delayed message delivery.

**Solution**: Verify your transport supports delayed delivery. Most do by default.

#### 2. Message Handler Not Registered
The `HealthPingHandler` must be in an assembly scanned by NServiceBus.

**Solution**: Ensure the assembly containing the handler is scanned:

```csharp
// If using explicit assembly scanning
endpointConfig.AssemblyScanner().ExcludeAssemblies(...);
// Don't exclude Wiesenwischer.NServiceBus.EndpointHealth
```

#### 3. Queue Blocked
If the endpoint's queue is full or blocked, health pings won't be processed.

**Solution**: Check the queue status and message processing.

---

## Integration Tests Fail

### Symptom
Integration tests using Testcontainers fail with timeout or connection errors.

### Possible Causes

#### 1. Container Not Ready
SQL Server containers take time to initialize.

**Solution**: Use proper health checks in your test fixtures:

```csharp
var container = new MsSqlBuilder()
    .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
    .Build();

await container.StartAsync();
// Container is ready when StartAsync completes
```

#### 2. Docker Not Running
Testcontainers require Docker Desktop or Docker Engine.

**Solution**: Ensure Docker is running and accessible.

---

## Performance Concerns

### Symptom
Concerns about health ping messages impacting performance.

### Analysis

Health ping messages are:
- Very small (empty message body)
- Low frequency (default: every 60 seconds)
- Processed like any other message

**Impact**: Negligible for most workloads.

### If Concerned

Increase the ping interval:

```csharp
endpointConfig.EnableEndpointHealth(options =>
{
    options.PingInterval = TimeSpan.FromMinutes(5);
    options.UnhealthyAfter = TimeSpan.FromMinutes(15);
});
```

---

## ASP.NET Core Health Check Not Found

### Symptom
`AddNServiceBusEndpointHealth()` causes a compilation error.

### Solution

Ensure you have installed the ASP.NET Core package:

```bash
dotnet add package Wiesenwischer.NServiceBus.EndpointHealth.AspNetCore
```

And add the using directive:

```csharp
using Wiesenwischer.NServiceBus.EndpointHealth.AspNetCore;
```

---

## Getting Help

If you're still experiencing issues:

1. **Check the logs**: NServiceBus provides detailed logging
2. **Enable debug logging**: `builder.Logging.AddConsole().SetMinimumLevel(LogLevel.Debug);`
3. **Open an issue**: https://github.com/Wiesenwischer/NServiceBus.EndpointHealth/issues
