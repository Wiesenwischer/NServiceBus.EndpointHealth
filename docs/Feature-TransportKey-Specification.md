# TransportKey Feature – Technical Specification

## Packages

- Wiesenwischer.NServiceBus.EndpointHealth (Core)
- Wiesenwischer.NServiceBus.EndpointHealth.AspNetCore (Integration / Hosting)

## Goal

An optional, logical TransportKey that allows grouping NServiceBus endpoints logically by their transport cluster (e.g., by shared SQL transport, RabbitMQ cluster, ASB namespace), without being coupled to ConnectionStrings, databases, or specific transport types.

This specification describes the implementation for version 0.2.0 of the packages.

---

## 1. Motivation

In the current health architecture (WatchDog + EndpointHealth), the following problem exists:

- Multiple endpoints share the same transport (e.g., a SQL transport database).
- If this shared transport fails, all affected endpoints are marked as "unhealthy".
- Without additional information, it's difficult to determine in WatchDog:
  - Is there an infrastructure/transport problem?
  - Or is it an error with individual endpoints?

A previous idea was a fingerprint based on the ConnectionString:

- Server=10.0.0.5;Database=NServiceBus
- Server=sql-prod;Database=NServiceBus
- Server=sql-prod.domain.local;Database=NServiceBus

These three variants can point to the same physical database but produce different results:

- IP vs. Hostname vs. FQDN
- Different parameter ordering
- Differences in optional parameters
- No access possible when DB is unreachable

Therefore, a technical fingerprint is unsuitable.

**Solution**: A logical TransportKey, assigned by the host (Configuration, ENV, ReadyStackGo).

Examples:

- primary-sql
- project-sql
- nsb-main
- transport-eu-central

Endpoints with the same TransportKey belong to a common transport cluster from a monitoring perspective.

---

## 2. Architecture Decisions

| Decision | Rationale |
|----------|-----------|
| TransportKey is optional | No breaking change, gradual adoption possible |
| No automatic derivation | No dependency on DB/Transport |
| No fingerprint from ConnectionString | Too many error sources |
| Set via Config or Code | Central management |
| Core has no IConfiguration | Clean separation |

The TransportKey is a logical cluster ID, not a technical hash.

---

## 3. Core Package: Wiesenwischer.NServiceBus.EndpointHealth

### 3.1 EndpointHealthOptions

```csharp
public sealed class EndpointHealthOptions
{
    /// <summary>
    /// Optional logical key for the transport cluster,
    /// e.g., "primary-sql", "project-sql".
    /// </summary>
    public string? TransportKey { get; set; }

    /// <summary>
    /// Time span after which an endpoint without a successfully
    /// processed health ping is considered unhealthy.
    /// </summary>
    public TimeSpan UnhealthyAfter { get; set; } = TimeSpan.FromMinutes(3);

    /// <summary>
    /// Interval at which health ping messages are sent.
    /// </summary>
    public TimeSpan PingInterval { get; set; } = TimeSpan.FromMinutes(1);
}
```

---

### 3.2 IEndpointHealthState

```csharp
public interface IEndpointHealthState
{
    string? TransportKey { get; }

    DateTime? LastHealthPingProcessedUtc { get; }

    bool HasCriticalError { get; }

    string? CriticalErrorMessage { get; }
}
```

The TransportKey is taken from EndpointHealthOptions and stored in the state, so it can be queried by WatchDog or a Health API.

---

### 3.3 EnableEndpointHealth

```csharp
public static class EndpointHealthConfigurationExtensions
{
    public static void EnableEndpointHealth(
        this EndpointConfiguration endpointConfiguration,
        Action<EndpointHealthOptions>? configure = null)
    {
        var options = new EndpointHealthOptions();

        configure?.Invoke(options);

        endpointConfiguration.Settings.Set("EndpointHealth.Options", options);
        endpointConfiguration.EnableFeature<EndpointHealthFeature>();
    }
}
```

Important:
- No IConfiguration
- No ASP.NET dependency
- Pure NServiceBus core feature

---

## 4. Integration / Hosting Layer

### 4.1 FromConfiguration

```csharp
using Microsoft.Extensions.Configuration;

public static class EndpointHealthOptionsConfigurationExtensions
{
    public static EndpointHealthOptions FromConfiguration(
        this EndpointHealthOptions options,
        IConfiguration configuration,
        string sectionName = "EndpointHealth")
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        if (configuration is null)
            throw new ArgumentNullException(nameof(configuration));

        var section = configuration.GetSection(sectionName);

        var transportKey = section[nameof(EndpointHealthOptions.TransportKey)];
        if (!string.IsNullOrWhiteSpace(transportKey))
            options.TransportKey = transportKey;

        var unhealthyAfterStr = section[nameof(EndpointHealthOptions.UnhealthyAfter)];
        if (!string.IsNullOrWhiteSpace(unhealthyAfterStr) && TimeSpan.TryParse(unhealthyAfterStr, out var unhealthyAfter))
            options.UnhealthyAfter = unhealthyAfter;

        var pingIntervalStr = section[nameof(EndpointHealthOptions.PingInterval)];
        if (!string.IsNullOrWhiteSpace(pingIntervalStr) && TimeSpan.TryParse(pingIntervalStr, out var pingInterval))
            options.PingInterval = pingInterval;

        return options;
    }
}
```

---

### 4.2 ConfigureEndpointHealth

```csharp
using Microsoft.Extensions.Configuration;

public static class EndpointHealthConfigurationExtensions
{
    public static void ConfigureEndpointHealth(
        this EndpointConfiguration endpointConfiguration,
        IConfiguration configuration,
        Action<EndpointHealthOptions>? configure = null,
        string sectionName = "EndpointHealth")
    {
        endpointConfiguration.EnableEndpointHealth(options =>
        {
            options.FromConfiguration(configuration, sectionName);

            configure?.Invoke(options);
        });
    }
}
```

Order:
1. Config is applied
2. Callback can override or extend

---

## 5. Configuration

### appsettings.json

```json
{
  "EndpointHealth": {
    "TransportKey": "primary-sql",
    "UnhealthyAfter": "00:02:00",
    "PingInterval": "00:00:30"
  }
}
```

### Environment Variables

```text
ENDPOINTHEALTH__TRANSPORTKEY=primary-sql
ENDPOINTHEALTH__UNHEALTHYAFTER=00:02:00
ENDPOINTHEALTH__PINGINTERVAL=00:00:30
```

---

## 6. Usage in Endpoint

### Config Only

```csharp
endpointConfiguration.ConfigureEndpointHealth(configuration);
```

### Config + Override

```csharp
endpointConfiguration.ConfigureEndpointHealth(configuration, options =>
{
    if (string.IsNullOrWhiteSpace(options.TransportKey))
        options.TransportKey = "default-transport";
});
```

### Code Only

```csharp
endpointConfiguration.EnableEndpointHealth(options =>
{
    options.TransportKey = "project-sql";
    options.UnhealthyAfter = TimeSpan.FromMinutes(3);
});
```

---

## 7. Usage in WatchDog

### Grouping Logic

- TransportKey != null → Group by TransportKey
- TransportKey == null → Separate, ungrouped endpoint

Example:

```text
Transport: primary-sql (UNHEALTHY)
 - endpoint-a
 - endpoint-b
 - endpoint-c

Transport: project-sql (HEALTHY)
 - endpoint-d
 - endpoint-e

Ungrouped:
 - legacy-endpoint-x
```

This makes it immediately visible when an entire transport cluster is affected.

---

## 8. Scope for Version 0.2.0

Must include:

- TransportKey in EndpointHealthOptions
- TransportKey in IEndpointHealthState + implementation
- FromConfiguration Extension
- ConfigureEndpointHealth Extension
- Documentation

Explicitly NOT included:

- No DB-based fingerprint
- No automatic transport discovery
- No ReadyStackGo integration
- No breaking changes for existing users

---

## 9. Benefits of the Design

- Completely transport-agnostic
- No dependency on DB, DNS, or infrastructure
- Stable against IP/Hostname/FQDN differences
- Easy to configure centrally (ENV / appsettings / RSGO)
- Clean separation of Core / Hosting
- Multi-node and multi-transport capable
- Easy to test and extend

---

This specification serves as the direct working basis for version 0.2.0.
