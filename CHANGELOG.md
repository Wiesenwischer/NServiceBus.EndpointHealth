# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.4.0](https://github.com/Wiesenwischer/NServiceBus.EndpointHealth/compare/v0.3.0...v0.4.0) (2026-05-12)


### ⚠ BREAKING CHANGES

* removes IEndpointHealthState.InstanceId and HealthPing.InstanceId.

### Code Refactoring

* drop obsolete InstanceId stale-ping check ([9e15fa6](https://github.com/Wiesenwischer/NServiceBus.EndpointHealth/commit/9e15fa62fb8d8ab4240e7ea0cb09978beb8d9691))

## [0.3.0](https://github.com/Wiesenwischer/NServiceBus.EndpointHealth/compare/v0.2.1...v0.3.0) (2026-05-11)


### Features

* replace delayed delivery with background service for HealthPing ([24c73a9](https://github.com/Wiesenwischer/NServiceBus.EndpointHealth/commit/24c73a923c05cc79f9f0ac1263c20c532fc644ab))
* treat any processed message as endpoint health signal ([5b37241](https://github.com/Wiesenwischer/NServiceBus.EndpointHealth/commit/5b37241877c8e4266f7e47102d5b7773d50f0280))


### Bug Fixes

* **healthping:** drop stale pings from previous container instances via InstanceId ([34c871d](https://github.com/Wiesenwischer/NServiceBus.EndpointHealth/commit/34c871df816241bc27d097197b96c4b90f229388))
* log HealthPing handler at Information level for debugging ([2da0a31](https://github.com/Wiesenwischer/NServiceBus.EndpointHealth/commit/2da0a311d114e75f8d9bc6714f29304dc8f6e85b))
* log HealthPing sends at Information level ([e9d93fb](https://github.com/Wiesenwischer/NServiceBus.EndpointHealth/commit/e9d93fbaac20f61dd402cd76242bc7ee5189fbec))
* make HealthPing startup delay configurable ([f7cadd0](https://github.com/Wiesenwischer/NServiceBus.EndpointHealth/commit/f7cadd0b0d02c12a8a7e51bbce3d054dcbd65a23))
* register HealthPing via IMessageConvention to bypass assembly scanning issues ([3049308](https://github.com/Wiesenwischer/NServiceBus.EndpointHealth/commit/30493081b3307a7d7be435f948a99d07125daf97))
* remove async modifier from HealthPing handler without awaits ([b06949e](https://github.com/Wiesenwischer/NServiceBus.EndpointHealth/commit/b06949ef74cfe58f926156003e654f8193c066b4))
* resolve health state from DI in StartupTask factory ([d2b475c](https://github.com/Wiesenwischer/NServiceBus.EndpointHealth/commit/d2b475c549aa187e79a9d21fa76abab29abbe120))
* run periodic HealthPing loop from FeatureStartupTask, drop HostedService ([be4ce24](https://github.com/Wiesenwischer/NServiceBus.EndpointHealth/commit/be4ce24391e3db7f66d86e347cc7629ecf01615c))
* **tests:** host EndpointHealth integration tests so BackgroundService runs ([f898464](https://github.com/Wiesenwischer/NServiceBus.EndpointHealth/commit/f89846415e7cc885e363fd3a20a21c4664d3a69b))

## [0.2.1](https://github.com/Wiesenwischer/NServiceBus.EndpointHealth/compare/v0.2.0...v0.2.1) (2025-12-16)


### Features

* **logging:** add diagnostic logging to HealthPingHandler and startup ([8b47718](https://github.com/Wiesenwischer/NServiceBus.EndpointHealth/commit/8b47718299f05575b981a9ff505b8eba949e1fcf))

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
