# Changelog

All notable changes to this project will be documented in this file.

## [v2.0.3] - 2026-04-24

### Changed
- `DataStore:Connections` configuration now uses a name-keyed object instead of an array.
- Connection configuration can now be partially overridden by later configuration providers without repeating the whole connection object.
- Named serializer selection remains connection-based and now participates in keyed configuration overrides.

### Added
- Keyed configuration support for connection overrides in `Microsoft.Extensions.Configuration` based applications.

### Breaking Changes
- `DataStore:Connections` JSON shape changed from array items with `Name` to an object keyed by connection name.

## [v2.0.2] - 2026-04-24

### Changed
- Connection configuration is now owned by each connection instead of provider-level settings.
- Provider registration is code-only and no longer reads provider options from configuration.
- `AddRedisDataStoreProvider` now registers only the provider name and type; connection options are supplied by `AddConnection` or configuration.
- Each configured connection now initializes its own provider instance during `InitializeDataStore` or `InitializeDataStoreAsync`.
- `AddDataStoreSerializer(...)` now sets the default serializer registration.
- Duplicate provider registrations with the same name now keep the first registration and emit a warning instead of throwing.

### Added
- `AddConnection` and `AddDefaultConnection` overloads for both `ConnectionOptions` and `Action<ConnectionOptions>`.
- Connection-level configuration loading for `ConnectionString`, `ConnectionMode`, retry, pooling, health-check, and namespace settings.
- Named serializer registration and optional per-connection serializer selection.
- Startup override support where configuration is loaded first and code-based connection configuration can replace or mutate it.

### Breaking Changes
- The `DataStore:Providers` and `DataStore:DefaultConnection` configuration sections were removed.
- The `DataStore:Connections` section now contains both connection metadata and connection options.
- Provider-specific options can no longer be configured from configuration; providers must be registered from code.

## [v2.0.1] - 2026-04-23

### Added
- Configuration file based provider settings.

## [v2.0.0] - 2026-04-23

### Added
- Dependency Injection (`IServiceCollection`) integration
- Provider-based configuration via code
- Pooled and Shared connection modes
- Support for .NET 9 and .NET 10

### Changed
- DataStore initialization moved to explicit startup step

### Breaking Changes
- DataStore must be initialized using `InitializeDataStore` or `InitializeDataStoreAsync`
- Static `DataStoreManager` usage removed
- `DataStoreManager` and `DataStoreBase` are now DI-integrated

## [v1.2.13] - 2026-04-13

### Added
- Fencing token support to distributed locks

## [v1.2.11] - 2026-04-10

### Added
- Lock object, `AcquireLock`, extend lock, and release lock methods

## [v1.2.10] - 2024-08-09

### Added
- `Count` method

## [v1.2.9] - 2024-05-16

### Added
- Locking with sliding expiration
- This changelog file

### Changed
- English XML doc comments

### Removed
- `lockExpire` parameter of `Lock` methods
