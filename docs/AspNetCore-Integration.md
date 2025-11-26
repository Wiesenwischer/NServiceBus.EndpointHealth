# ASP.NET Core Integration

This guide covers integration with ASP.NET Core, including configuration binding and health checks.

## Basic Setup

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add health checks
builder.Services.AddHealthChecks()
    .AddNServiceBusEndpointHealth();

var app = builder.Build();

// Map default health endpoint
app.MapHealthChecks("/health");

app.Run();
```

## Configuration from appsettings.json

The ASP.NET Core package provides extensions for loading `EndpointHealthOptions` from configuration.

### appsettings.json

```json
{
  "EndpointHealth": {
    "TransportKey": "primary-sql",
    "PingInterval": "00:00:30",
    "UnhealthyAfter": "00:03:00"
  }
}
```

### Using ConfigureEndpointHealth

The simplest approach is to use `ConfigureEndpointHealth`, which combines configuration binding with the core `EnableEndpointHealth`:

```csharp
var builder = WebApplication.CreateBuilder(args);

var endpointConfig = new EndpointConfiguration("my-endpoint");

// Load all options from configuration
endpointConfig.ConfigureEndpointHealth(builder.Configuration);

// Or with additional code-based overrides
endpointConfig.ConfigureEndpointHealth(builder.Configuration, options =>
{
    // Provide a fallback if TransportKey not configured
    if (string.IsNullOrWhiteSpace(options.TransportKey))
        options.TransportKey = "default-transport";
});
```

### Using FromConfiguration

For more control, use `FromConfiguration` directly within `EnableEndpointHealth`:

```csharp
endpointConfig.EnableEndpointHealth(options =>
{
    // First apply configuration
    options.FromConfiguration(builder.Configuration);

    // Then override specific values
    options.PingInterval = TimeSpan.FromSeconds(15);
});
```

### Custom Configuration Section

Both methods support custom section names:

```json
{
  "MyApp": {
    "Health": {
      "TransportKey": "my-transport"
    }
  }
}
```

```csharp
endpointConfig.ConfigureEndpointHealth(
    configuration,
    sectionName: "MyApp:Health");
```

### Environment Variables

Configuration also works with environment variables:

```text
ENDPOINTHEALTH__TRANSPORTKEY=primary-sql
ENDPOINTHEALTH__PINGINTERVAL=00:00:30
ENDPOINTHEALTH__UNHEALTHYAFTER=00:03:00
```

## Separate Liveness and Readiness Probes

For Kubernetes, you typically want separate endpoints for liveness and readiness:

```csharp
// Configure health check options
builder.Services.AddHealthChecks()
    .AddNServiceBusEndpointHealth(tags: ["ready", "live"])
    .AddSqlServer(connectionString, tags: ["ready"]);

var app = builder.Build();

// Liveness - only NServiceBus check
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

// Readiness - all checks including database
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
```

## Custom Response Format

Return detailed JSON responses:

```csharp
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        };

        await context.Response.WriteAsJsonAsync(response);
    }
});
```

Example response:

```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "nservicebus-endpoint",
      "status": "Healthy",
      "description": "Last health ping: 5s ago",
      "duration": 0.5
    }
  ],
  "totalDuration": 1.2
}
```

## UI Dashboard Integration

### Using AspNetCore.HealthChecks.UI

```bash
dotnet add package AspNetCore.HealthChecks.UI
dotnet add package AspNetCore.HealthChecks.UI.InMemory.Storage
```

```csharp
builder.Services
    .AddHealthChecks()
    .AddNServiceBusEndpointHealth();

builder.Services
    .AddHealthChecksUI()
    .AddInMemoryStorage();

var app = builder.Build();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecksUI(options =>
{
    options.UIPath = "/health-ui";
});
```

Access the dashboard at `/health-ui`.

## Combining Multiple NServiceBus Endpoints

If your application hosts multiple NServiceBus endpoints:

```csharp
// Register multiple health states with named options
builder.Services.AddSingleton<IEndpointHealthState, EndpointHealthState>(
    sp => new EndpointHealthState()); // For endpoint 1
builder.Services.AddKeyedSingleton<IEndpointHealthState>("orders",
    sp => new EndpointHealthState()); // For endpoint 2

// Add health checks for each
builder.Services.AddHealthChecks()
    .AddNServiceBusEndpointHealth(name: "sales-endpoint")
    .AddCheck<OrdersEndpointHealthCheck>("orders-endpoint");
```

## Authorization

Protect health endpoints from unauthorized access:

```csharp
app.MapHealthChecks("/health", new HealthCheckOptions())
    .RequireAuthorization("HealthCheckPolicy");

// Or use a specific policy
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("HealthCheckPolicy", policy =>
        policy.RequireRole("HealthMonitor"));
});
```

For internal Kubernetes probes, you might want no auth:

```csharp
// Public endpoint for Kubernetes
app.MapHealthChecks("/health/live");

// Protected endpoint for detailed status
app.MapHealthChecks("/health/detailed", new HealthCheckOptions
{
    ResponseWriter = DetailedHealthWriter
}).RequireAuthorization();
```

## Caching Health Results

For high-traffic scenarios, cache health check results:

```csharp
builder.Services.Configure<HealthCheckPublisherOptions>(options =>
{
    options.Delay = TimeSpan.FromSeconds(5);
    options.Period = TimeSpan.FromSeconds(10);
});
```

## Metrics and Telemetry

### With Application Insights

```csharp
builder.Services.AddApplicationInsightsTelemetry();

// Health checks automatically report to Application Insights
builder.Services.AddHealthChecks()
    .AddNServiceBusEndpointHealth();
```

### With Prometheus

```bash
dotnet add package prometheus-net.AspNetCore
dotnet add package prometheus-net.AspNetCore.HealthChecks
```

```csharp
builder.Services.AddHealthChecks()
    .AddNServiceBusEndpointHealth()
    .ForwardToPrometheus();

var app = builder.Build();

app.UseMetricServer();
app.UseHttpMetrics();
```

## Graceful Shutdown

Handle graceful shutdown properly:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks()
    .AddNServiceBusEndpointHealth();

var app = builder.Build();

var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

lifetime.ApplicationStopping.Register(() =>
{
    // Stop accepting new requests
    // Kubernetes will stop routing traffic when /health returns unhealthy
});

app.MapHealthChecks("/health");
app.Run();
```
