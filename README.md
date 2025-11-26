# NServiceBus EndpointHealth

Robust health monitoring for NServiceBus endpoints.

[![CI](https://github.com/Wiesenwischer/NServiceBus.EndpointHealth/actions/workflows/ci.yml/badge.svg)](https://github.com/Wiesenwischer/NServiceBus.EndpointHealth/actions/workflows/ci.yml)
[![Unit Tests](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/Wiesenwischer/406ad9783017f2c6d0c89222f274562f/raw/unit-tests-badge.json)](https://github.com/Wiesenwischer/NServiceBus.EndpointHealth/actions/workflows/ci.yml)
[![Integration Tests](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/Wiesenwischer/406ad9783017f2c6d0c89222f274562f/raw/integration-tests-badge.json)](https://github.com/Wiesenwischer/NServiceBus.EndpointHealth/actions/workflows/ci.yml)
[![codecov](https://codecov.io/github/Wiesenwischer/NServiceBus.EndpointHealth/graph/badge.svg?token=W9QL9P7K53)](https://codecov.io/github/Wiesenwischer/NServiceBus.EndpointHealth)
[![NuGet](https://img.shields.io/nuget/v/Wiesenwischer.NServiceBus.EndpointHealth.svg)](https://www.nuget.org/packages/Wiesenwischer.NServiceBus.EndpointHealth)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Wiesenwischer.NServiceBus.EndpointHealth.svg)](https://www.nuget.org/packages/Wiesenwischer.NServiceBus.EndpointHealth)
[![License](https://img.shields.io/github/license/Wiesenwischer/NServiceBus.EndpointHealth.svg)](LICENSE)

## Packages

| Package | Description | NuGet |
|---------|-------------|-------|
| `Wiesenwischer.NServiceBus.EndpointHealth` | Core health monitoring for NServiceBus | [![NuGet](https://img.shields.io/nuget/v/Wiesenwischer.NServiceBus.EndpointHealth.svg)](https://www.nuget.org/packages/Wiesenwischer.NServiceBus.EndpointHealth) |
| `Wiesenwischer.NServiceBus.EndpointHealth.AspNetCore` | ASP.NET Core HealthCheck integration | [![NuGet](https://img.shields.io/nuget/v/Wiesenwischer.NServiceBus.EndpointHealth.AspNetCore.svg)](https://www.nuget.org/packages/Wiesenwischer.NServiceBus.EndpointHealth.AspNetCore) |

## Features

- **Synthetic Health Pings**: Self-sent messages that verify the message processing pipeline
- **Critical Error Tracking**: Automatic detection and reporting of NServiceBus critical errors
- **Container Health Checks**: Integration with Kubernetes liveness/readiness probes

## Quick Start

```bash
dotnet add package Wiesenwischer.NServiceBus.EndpointHealth
dotnet add package Wiesenwischer.NServiceBus.EndpointHealth.AspNetCore
```

```csharp
// Enable health monitoring
var endpointConfig = new EndpointConfiguration("my-endpoint");
endpointConfig.EnableEndpointHealth();

// Add ASP.NET Core health check
builder.Services.AddHealthChecks()
    .AddNServiceBusEndpointHealth();

app.MapHealthChecks("/health");
```

## Documentation

For detailed documentation, see the [docs](docs/Home.md) or the [Wiki](../../wiki):

- [How It Works](docs/How-It-Works.md)
- [Getting Started](docs/Getting-Started.md)
- [Configuration Options](docs/Configuration-Options.md)
- [ASP.NET Core Integration](docs/AspNetCore-Integration.md)
- [Architecture](docs/Architecture.md)
- [API Reference](docs/API-Reference.md)
- [Troubleshooting](docs/Troubleshooting.md)

## Building

```powershell
# Build with versioning
.\build.ps1

# Build + run tests
.\build.ps1 -Test

# Build + create NuGet packages
.\build.ps1 -Pack

# Publish to local Azure DevOps feed
.\publish-local.ps1
```

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for a list of changes.

## Contributing

Pull requests are welcome.

- Fork the repository and create a feature branch
- Write/update tests (`dotnet test`)
- Use [Conventional Commits](https://www.conventionalcommits.org/) for commit messages
- Open a PR against `main`

> **Note:** The `docs/` folder is automatically synced to the GitHub Wiki on push to `main`.

## Releasing

This project uses [release-please](https://github.com/googleapis/release-please) for automated releases.

**Automated flow:**
1. Commits with `feat:` or `fix:` prefixes trigger release-please to create/update a Release PR
2. The PR accumulates changes and auto-generates the changelog
3. Merge the PR when ready → a git tag is created → GitHub Release + NuGet publish

**Manual release:**
```bash
git tag v0.3.0
git push origin v0.3.0
```
This triggers the release workflow directly, bypassing release-please.

## License

MIT License
