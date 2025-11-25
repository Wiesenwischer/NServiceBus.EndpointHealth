# NServiceBus EndpointHealth - Wiki

Welcome to the NServiceBus EndpointHealth wiki! This documentation provides comprehensive information about using and extending the health monitoring packages for NServiceBus.

## 📦 Packages

| Package | Description | NuGet |
|---------|-------------|-------|
| `Wiesenwischer.NServiceBus.EndpointHealth` | Core health monitoring feature for NServiceBus | [![NuGet](https://img.shields.io/nuget/v/Wiesenwischer.NServiceBus.EndpointHealth.svg)](https://www.nuget.org/packages/Wiesenwischer.NServiceBus.EndpointHealth) |
| `Wiesenwischer.NServiceBus.EndpointHealth.AspNetCore` | ASP.NET Core HealthCheck integration | [![NuGet](https://img.shields.io/nuget/v/Wiesenwischer.NServiceBus.EndpointHealth.AspNetCore.svg)](https://www.nuget.org/packages/Wiesenwischer.NServiceBus.EndpointHealth.AspNetCore) |

## 📖 Documentation

- [Getting Started](Getting-Started)
- [Configuration Options](Configuration-Options)
- [ASP.NET Core Integration](AspNetCore-Integration)
- [Architecture](Architecture)
- [API Reference](API-Reference)
- [Troubleshooting](Troubleshooting)

## 🎯 What Problem Does This Solve?

NServiceBus endpoints can become unhealthy in ways that are difficult to detect:

1. **Message Pump Stuck**: The endpoint process is running, but the message pump is no longer processing messages
2. **Critical Errors**: NServiceBus has encountered a critical error (e.g., transport failures)
3. **Infrastructure Issues**: The underlying transport (SQL Server, RabbitMQ, etc.) is unavailable

This library provides:

- **Synthetic Health Pings**: Self-sent messages that verify the entire message processing pipeline is working
- **Critical Error Tracking**: Automatic detection and reporting of NServiceBus critical errors
- **Container Health Checks**: Integration with Kubernetes liveness/readiness probes via ASP.NET Core Health Checks

## 🚀 Quick Start

### 1. Install the packages

```bash
dotnet add package Wiesenwischer.NServiceBus.EndpointHealth
dotnet add package Wiesenwischer.NServiceBus.EndpointHealth.AspNetCore
```

### 2. Enable on your NServiceBus endpoint

```csharp
var endpointConfig = new EndpointConfiguration("my-endpoint");

endpointConfig.EnableEndpointHealth(options =>
{
    options.PingInterval = TimeSpan.FromSeconds(30);
    options.UnhealthyAfter = TimeSpan.FromMinutes(2);
});
```

### 3. Add ASP.NET Core health check

```csharp
builder.Services.AddHealthChecks()
    .AddNServiceBusEndpointHealth();

app.MapHealthChecks("/health");
```

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/Wiesenwischer/NServiceBus.EndpointHealth/blob/main/LICENSE) file for details.

