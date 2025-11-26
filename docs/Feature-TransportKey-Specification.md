# TransportKey Feature – Technische Spezifikation

## Pakete

- Wiesenwischer.NServiceBus.EndpointHealth (Core)
- Wiesenwischer.NServiceBus.EndpointHealth.AspNetCore (Integration / Hosting)

## Ziel

Ein optionaler, logischer TransportKey, der es ermöglicht, NServiceBus-Endpunkte logisch nach ihrem Transport-Cluster zu gruppieren (z. B. nach gemeinsamem SQL-Transport, RabbitMQ-Cluster, ASB-Namespace), ohne an ConnectionStrings, Datenbanken oder konkrete Transporttypen gekoppelt zu sein.

Diese Spezifikation beschreibt die Umsetzung für Version 0.2.0 der Pakete.

---

## 1. Motivation

In der aktuellen Health-Architektur (WatchDog + EndpointHealth) existiert folgendes Problem:

- Mehrere Endpunkte teilen sich denselben Transport (z. B. eine SQL-Transportdatenbank).
- Fällt dieser gemeinsame Transport aus, werden alle betroffenen Endpunkte als „unhealthy“ markiert.
- Ohne zusätzliche Information ist im WatchDog schwer zu erkennen:
  - Liegt ein Infrastruktur-/Transportproblem vor?
  - Oder ein Fehler einzelner Endpunkte?

Eine frühere Idee war ein Fingerprint auf Basis des ConnectionStrings:

- Server=10.0.0.5;Database=NServiceBus
- Server=sql-prod;Database=NServiceBus
- Server=sql-prod.domain.local;Database=NServiceBus

Diese drei Varianten können auf dieselbe physische Datenbank zeigen, erzeugen aber unterschiedliche Ergebnisse:

- IP vs. Hostname vs. FQDN
- Unterschiedliche Parameter-Reihenfolge
- Unterschiede bei optionalen Parametern
- Kein Zugriff möglich, wenn DB nicht erreichbar ist

Daher ist ein technischer Fingerprint ungeeignet.

Lösung: Ein logischer TransportKey, der vom Host (Configuration, ENV, ReadyStackGo) vergeben wird.

Beispiele:

- primary-sql
- project-sql
- nsb-main
- transport-eu-central

Endpunkte mit gleichem TransportKey gehören aus Sicht des Monitorings zu einem gemeinsamen Transport-Cluster.

---

## 2. Architektur-Entscheidungen

| Entscheidung | Begründung |
|------------|-----------|
| TransportKey ist optional | Kein Breaking Change, schrittweise 
| Einführung möglich | Keine automatische Ableitung | Keine Abhängigkeit 
| von DB/Transport | Kein Fingerprint aus ConnectionString | Zu viele 
| Fehlerquellen | Setzung per Config oder Code | Zentrale Steuerung | 
| Core kennt kein IConfiguration | Saubere Trennung |

Der TransportKey ist eine logische Cluster-ID, kein technischer Hash.

---

## 3. Core-Paket: Wiesenwischer.NServiceBus.EndpointHealth

### 3.1 EndpointHealthOptions

```csharp
public sealed class EndpointHealthOptions {
    /// <summary>
    /// Optionaler logischer Key für das Transport-Cluster,
    /// z.B. "primary-sql", "project-sql".
    /// </summary>
    public string? TransportKey { get; set; }

    /// <summary>
    /// Zeitspanne, nach der ein Endpoint ohne erfolgreich
    /// verarbeiteten Health-Ping als unhealthy gilt.
    /// </summary>
    public TimeSpan UnhealthyAfter { get; set; } = TimeSpan.FromMinutes(1); } ```

---

### 3.2 IEndpointHealthState

```csharp
public interface IEndpointHealthState
{
    string? TransportKey { get; }

    DateTime? LastHealthPingProcessedUtc { get; }

    bool HasCriticalError { get; }

    string? CriticalErrorMessage { get; } } ```

Der TransportKey wird aus den EndpointHealthOptions übernommen und im State gespeichert, damit er vom WatchDog oder einer Health-API abgefragt werden kann.

---

### 3.3 EnableEndpointHealth

```csharp
public static class EndpointHealthExtensions {
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

Wichtig:
- Kein IConfiguration
- Keine ASP.NET-Abhängigkeit
- Reines NServiceBus-Core-Feature

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

        var configOptions = configuration
            .GetSection(sectionName)
            .Get<EndpointHealthOptions>();

        if (configOptions is null)
            return options;

        if (!string.IsNullOrWhiteSpace(configOptions.TransportKey))
            options.TransportKey = configOptions.TransportKey;

        if (configOptions.UnhealthyAfter != default)
            options.UnhealthyAfter = configOptions.UnhealthyAfter;

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

Reihenfolge:
1. Config wird angewendet
2. Callback kann überschreiben oder ergänzen

---

## 5. Konfiguration

### appsettings.json

```json
{
  "EndpointHealth": {
    "TransportKey": "primary-sql",
    "UnhealthyAfter": "00:02:00"
  }
}
```

### Environment Variablen

```text
ENDPOINTHEALTH__TRANSPORTKEY=primary-sql
ENDPOINTHEALTH__UNHEALTHYAFTER=00:02:00
```

---

## 6. Verwendung im Endpoint

### Nur Config

```csharp
endpointConfiguration.ConfigureEndpointHealth(configuration);
```

### Config + Override

```csharp
endpointConfiguration.ConfigureEndpointHealth(configuration, options => {
    if (string.IsNullOrWhiteSpace(options.TransportKey))
        options.TransportKey = "default-transport"; }); ```

### Nur Code

```csharp
endpointConfiguration.EnableEndpointHealth(options => {
    options.TransportKey = "project-sql";
    options.UnhealthyAfter = TimeSpan.FromMinutes(3); }); ```

---

## 7. Verwendung im WatchDog

### Gruppierungslogik

- TransportKey != null → Gruppierung nach TransportKey
- TransportKey == null → separater, ungruppierter Endpoint

Beispiel:

```text
Transport: primary-sql (UNHEALTHY)
 - endpoint-a
 - endpoint-b
 - endpoint-c

Transport: project-sql (HEALTHY)
 - endpoint-d
 - endpoint-e

Ungruppiert:
 - legacy-endpoint-x
```

So wird sofort sichtbar, wenn ein kompletter Transport-Cluster betroffen ist.

---

## 8. Scope für Version 0.2.0

Muss enthalten:

- TransportKey in EndpointHealthOptions
- TransportKey in IEndpointHealthState + Implementierung
- FromConfiguration Extension
- ConfigureEndpointHealth Extension
- Dokumentation

Explizit NICHT enthalten:

- Kein DB-basierter Fingerprint
- Kein automatisches Transport-Discovery
- Keine ReadyStackGo-Integration
- Keine Breaking Changes für bestehende User

---

## 9. Vorteile des Designs

- Vollständig transport-agnostisch
- Keine Abhängigkeit von DB, DNS oder Infrastruktur
- Stabil gegen IP/Hostname/FQDN-Unterschiede
- Leicht zentral konfigurierbar (ENV / appsettings / RSGO)
- Saubere Trennung Core / Hosting
- Multinode- und Multitransport-fähig
- Einfach testbar und erweiterbar

---

Diese Spezifikation ist als direkte Arbeitsgrundlage für Version 0.2.0 gedacht