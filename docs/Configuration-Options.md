# Configuration Options

This page describes all available configuration options for NServiceBus EndpointHealth.

## EndpointHealthOptions

The `EndpointHealthOptions` class provides configuration for the health monitoring feature.

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `PingInterval` | `TimeSpan` | 60 seconds | How often health ping messages are sent |
| `UnhealthyAfter` | `TimeSpan` | 2 minutes | Time after last ping before marking as unhealthy |

### Example Configuration

```csharp
endpointConfig.EnableEndpointHealth(options =>
{
    // Send health pings every 30 seconds
    options.PingInterval = TimeSpan.FromSeconds(30);

    // Mark as unhealthy if no ping for 3 minutes
    options.UnhealthyAfter = TimeSpan.FromMinutes(3);
});
```

## Recommended Settings

### High Availability / Production

For production environments where quick detection is critical:

```csharp
endpointConfig.EnableEndpointHealth(options =>
{
    options.PingInterval = TimeSpan.FromSeconds(15);
    options.UnhealthyAfter = TimeSpan.FromSeconds(45);
});
```

### Low Traffic Environments

For endpoints with infrequent business messages:

```csharp
endpointConfig.EnableEndpointHealth(options =>
{
    options.PingInterval = TimeSpan.FromSeconds(60);
    options.UnhealthyAfter = TimeSpan.FromMinutes(5);
});
```

### Development / Testing

For development environments where you want less noise:

```csharp
endpointConfig.EnableEndpointHealth(options =>
{
    options.PingInterval = TimeSpan.FromMinutes(5);
    options.UnhealthyAfter = TimeSpan.FromMinutes(15);
});
```

## Important Considerations

### Relationship Between Options

The `UnhealthyAfter` value should always be greater than `PingInterval` to allow for:
- Message processing latency
- Transport delays
- Brief network issues

**Recommended**: `UnhealthyAfter >= 2 × PingInterval`

### Impact on Transport

Health ping messages use the same transport as business messages:
- They consume minimal queue capacity
- They provide end-to-end verification of the message pipeline
- Short ping intervals increase transport load slightly

### Delayed Delivery Support

The health ping feature relies on delayed message delivery. Ensure your transport supports this:
- ✅ SQL Server Transport
- ✅ RabbitMQ Transport
- ✅ Azure Service Bus Transport
- ✅ Learning Transport (for testing)

## ASP.NET Core Health Check Options

When adding the health check, you can customize its registration:

```csharp
builder.Services.AddHealthChecks()
    .AddNServiceBusEndpointHealth(
        name: "my-endpoint-health",           // Custom name
        failureStatus: HealthStatus.Degraded, // Use Degraded instead of Unhealthy
        tags: ["critical", "messaging"]       // Custom tags for filtering
    );
```

### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `name` | `string` | `"nservicebus-endpoint"` | Name of the health check |
| `failureStatus` | `HealthStatus?` | `Unhealthy` | Status to report on failure |
| `tags` | `IEnumerable<string>?` | `["nservicebus", "endpoint", "messaging"]` | Tags for filtering |

### Filtering Health Checks

You can use tags to filter which health checks are exposed on different endpoints:

```csharp
// Liveness probe - only critical checks
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("critical")
});

// Readiness probe - all checks
app.MapHealthChecks("/health/ready");
```
