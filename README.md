
# NServiceBus EndpointHealth

Dieses Repository enthält zwei NuGet-Pakete zur robusten technischen Überwachung von NServiceBus-Endpunkten:

- **Wiesenwischer.NServiceBus.EndpointHealth**  
  Core-Feature für NServiceBus, inkl. synthetischer Health-Pings, CriticalError-Tracking und State-API.

- **Wiesenwischer.NServiceBus.EndpointHealth.AspNetCore**  
  ASP.NET Core HealthCheck-Integration, basierend auf dem Core-Feature.

Ziel ist es, eine zuverlässige Möglichkeit zu schaffen, um zu erkennen:
- ob der NServiceBus Message Pump korrekt arbeitet  
- ob ein CriticalError ausgelöst wurde  
- ob der Endpoint intern hängt, auch wenn wenig Business-Nachrichten ankommen  
- ob der Endpoint im Container als *healthy* oder *unhealthy* markiert werden muss

---

## 📦 1. NuGet Pakete

| Paket | Beschreibung |
|------|--------------|
| **Wiesenwischer.NServiceBus.EndpointHealth** | Core-Feature für NSB Health Monitoring |
| **Wiesenwischer.NServiceBus.EndpointHealth.AspNetCore** | ASP.NET Integration (HealthCheck) |

---

## 🚀 2. Installation

### Core-Paket (NServiceBus)

```bash
dotnet add package Wiesenwischer.NServiceBus.EndpointHealth
```

### ASP.NET Core Integration

```bash
dotnet add package Wiesenwischer.NServiceBus.EndpointHealth.AspNetCore
```

---

## ⚙️ 3. Verwendung – NServiceBus Endpoint

```csharp
var endpointConfig = new EndpointConfiguration("my-endpoint");

endpointConfig.EnableEndpointHealth(options =>
{
    options.PingInterval = TimeSpan.FromSeconds(30);
    options.UnhealthyAfter = TimeSpan.FromMinutes(3);
});
```

**Das aktiviert:**

- HealthPing-Feature  
- HealthPingHandler  
- CriticalError-State  
- Hintergrund-Self-Pings  
- State für ASP.NET/Core  

---

## 🌐 4. Verwendung – ASP.NET Core Health Checks

In `Program.cs`:

```csharp
builder.Services
    .AddHealthChecks()
    .AddNServiceBusEndpointHealth();

var app = builder.Build();
app.MapHealthChecks("/health");
app.Run();
```

**HealthCheck liefert z. B.:**
- 200 OK → Message Pump arbeitet  
- 503 Service Unavailable → Ping zu alt / CriticalError aktiv  

---

## 🧱 5. Projektstruktur

```text
repo/
├─ src/
│  ├─ Wiesenwischer.NServiceBus.EndpointHealth/
│  └─ Wiesenwischer.NServiceBus.EndpointHealth.AspNetCore/
├─ test/
│  ├─ Wiesenwischer.NServiceBus.EndpointHealth.Tests/
│  └─ Wiesenwischer.NServiceBus.EndpointHealth.AspNetCore.Tests/
├─ NServiceBus.EndpointHealth.sln
└─ README.md
```

---

## 🧪 6. Unit Tests

Geplant:
- HealthPingHandler Tests
- CriticalError Tests
- ASP.NET HealthCheck Tests
- Options Tests

---

## 🔧 7. CI/CD (optional)

Empfohlene GitHub Action:

- Build
- Run Tests
- Pack NuGets
- Upload to NuGet on Tag Push (`v*.*.*`)

Kann auf Wunsch automatisch erstellt werden.

---

## 📜 8. Lizenz

MIT License (empfohlen).

---

## 🙌 9. Beitrag leisten

Pull Requests sind willkommen.

---

## 📄 10. Vollständige Spezifikation

Die ausführliche Spezifikation ist verfügbar unter:

**NServiceBusEndpointHealth-Specification.md**

---

Viel Erfolg beim Einsatz – Feedback & Erweiterungen sind jederzeit willkommen!
