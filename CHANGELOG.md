# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Data properties in `HealthCheckResult` for monitoring systems and HealthChecks UI
  - `transportKey` - Logical transport cluster identifier
  - `lastHealthPingProcessedUtc` - ISO 8601 timestamp of last ping
  - `timeSinceLastPing` - Time elapsed since last ping
  - `unhealthyAfter` - Configured threshold
  - `hasCriticalError` - Critical error flag
  - `criticalErrorMessage` - Error details

### Fixed

- Integration tests now properly register routing services

## [0.2.0] - 2025-01-15

### Added

- `TransportKey` property for logical transport cluster grouping
- Multi-target support for NServiceBus 7.x (.NET Core 3.1) and 8.x (.NET 9)
- `FromConfiguration()` extension for loading options from `IConfiguration`
- `ConfigureEndpointHealth()` extension combining configuration binding with endpoint setup
- Comprehensive integration tests with Testcontainers

### Changed

- Extension methods moved to target namespaces for better discoverability
  - `EnableEndpointHealth` → `NServiceBus` namespace
  - `AddNServiceBusEndpointHealth` → `Microsoft.Extensions.DependencyInjection` namespace
- Unified API for NServiceBus 7.x and 8.x health state configuration
- Default `UnhealthyAfter` changed to 3 minutes (was 2 minutes)

## [0.1.0] - 2025-01-10

### Added

- Initial release
- Synthetic health ping mechanism for endpoint monitoring
- `HealthPing` message and `HealthPingHandler` for pipeline verification
- `IEndpointHealthState` interface for health state access
- `EndpointHealthState` thread-safe implementation
- Critical error tracking via `RegisterCriticalError()`
- `NServiceBusEndpointHealthCheck` for ASP.NET Core health checks
- `EndpointHealthOptions` for configuring ping interval and unhealthy threshold
- Support for Kubernetes liveness/readiness probes
- GitHub Wiki documentation sync

[Unreleased]: https://github.com/Wiesenwischer/NServiceBus.EndpointHealth/compare/v0.2.0...HEAD
[0.2.0]: https://github.com/Wiesenwischer/NServiceBus.EndpointHealth/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/Wiesenwischer/NServiceBus.EndpointHealth/releases/tag/v0.1.0
