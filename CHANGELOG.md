# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.0](https://github.com/Wiesenwischer/NServiceBus.EndpointHealth/compare/v0.2.0...v0.2.0) (2025-11-26)


### Features

* add CHANGELOG.md and release-please automation ([31350c9](https://github.com/Wiesenwischer/NServiceBus.EndpointHealth/commit/31350c9ba6ebffe121e05960d493a502a6dbeecf))
* use release-please manifest for versioning and add pingInterval to health data ([3817cda](https://github.com/Wiesenwischer/NServiceBus.EndpointHealth/commit/3817cda1a647c2f9a2a7c801af94b209ed7bfdf3))


### Bug Fixes

* correct jq syntax for reading manifest version ([a045b35](https://github.com/Wiesenwischer/NServiceBus.EndpointHealth/commit/a045b35842bebc54182dd6a859b0bde395eb0e69))
* integrate release-please with existing CI/GitVersion workflow ([71e8eee](https://github.com/Wiesenwischer/NServiceBus.EndpointHealth/commit/71e8eeeb8058f8aecdefdd26ec445b612f317ee3))
* properly suppress git errors in build scripts ([ed65df3](https://github.com/Wiesenwischer/NServiceBus.EndpointHealth/commit/ed65df3d16a0da450715db406c94121b7e8381ed))
* suppress git stderr in build scripts ([224a6fa](https://github.com/Wiesenwischer/NServiceBus.EndpointHealth/commit/224a6faea81e462f5217c0721be09c0304db2912))
* update build scripts to work without GitVersion ([60e37f0](https://github.com/Wiesenwischer/NServiceBus.EndpointHealth/commit/60e37f05f468314d783f8e8984b7177396ae3041))
* use bracket syntax for jq to access dot key ([efcc1d2](https://github.com/Wiesenwischer/NServiceBus.EndpointHealth/commit/efcc1d255d91f7446b1d206e84c4f798f0acfc97))


### Miscellaneous Chores

* release v0.2.0 ([4ec0a3d](https://github.com/Wiesenwischer/NServiceBus.EndpointHealth/commit/4ec0a3d37bca3ee3f7d22f643437c4dd7c9d7eff))

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
