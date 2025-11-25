# Getting Started

This guide will help you set up NServiceBus EndpointHealth in your application.

## Prerequisites

- .NET 9.0 or later
- NServiceBus 8.x
- (Optional) ASP.NET Core for health check endpoints

## Installation

### Core Package

Install the core package for NServiceBus health monitoring:

```bash
dotnet add package Wiesenwischer.NServiceBus.EndpointHealth
```

### ASP.NET Core Integration (Optional)

If you want to expose health checks via HTTP:

```bash
dotnet add package Wiesenwischer.NServiceBus.EndpointHealth.AspNetCore
```

## Basic Setup

### 1. Enable Health Monitoring on Your Endpoint

```csharp
var endpointConfig = new EndpointConfiguration("my-endpoint");

// Enable endpoint health with default options
endpointConfig.EnableEndpointHealth();

// Or with custom options
endpointConfig.EnableEndpointHealth(options =>
{
    options.PingInterval = TimeSpan.FromSeconds(30);    // How often to send health pings
    options.UnhealthyAfter = TimeSpan.FromMinutes(2);   // When to mark as unhealthy
});

// Configure your transport, persistence, etc.
endpointConfig.UseTransport<SqlServerTransport>();

var endpoint = await Endpoint.Start(endpointConfig);
```

### 2. Access Health State Programmatically (Optional)

If you need to check health state without ASP.NET Core:

```csharp
// Inject IEndpointHealthState into your handlers or services
public class MyHandler : IHandleMessages<SomeMessage>
{
    private readonly IEndpointHealthState _healthState;

    public MyHandler(IEndpointHealthState healthState)
    {
        _healthState = healthState;
    }

    public Task Handle(SomeMessage message, IMessageHandlerContext context)
    {
        if (_healthState.HasCriticalError)
        {
            // Handle critical error scenario
        }

        var lastPing = _healthState.LastHealthPingProcessedUtc;
        // Use for custom health logic

        return Task.CompletedTask;
    }
}
```

### 3. Add ASP.NET Core Health Checks (Recommended)

In your `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add health checks
builder.Services.AddHealthChecks()
    .AddNServiceBusEndpointHealth();

var app = builder.Build();

// Map health endpoint
app.MapHealthChecks("/health");

app.Run();
```

## Kubernetes Integration

Configure your Kubernetes deployment to use the health endpoint:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: my-nservicebus-endpoint
spec:
  template:
    spec:
      containers:
      - name: endpoint
        livenessProbe:
          httpGet:
            path: /health
            port: 80
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health
            port: 80
          initialDelaySeconds: 5
          periodSeconds: 5
```

## How It Works

1. **On Startup**: The `HealthPingStartupTask` sends the first `HealthPing` message and registers the initial timestamp
2. **Continuous Monitoring**: The `HealthPingHandler` processes each ping, updates the timestamp, and schedules the next ping
3. **Health Check**: When queried, the `NServiceBusEndpointHealthCheck` compares the last ping timestamp against the `UnhealthyAfter` threshold

## Next Steps

- [Configuration Options](Configuration-Options) - Fine-tune the health monitoring behavior
- [ASP.NET Core Integration](AspNetCore-Integration) - Advanced health check configuration
- [Architecture](Architecture) - Understand how the library works internally
