# Changelog

All notable changes to this project will be documented in this file.

## [v2.0.0] - 2026-04-23

### Added
- Dependency Injection (IServiceCollection) integration
- Provider-based configuration via code
- Pooled and Shared connection modes
- Support for .NET 9 and .NET 10

### Changed
- DataStore initialization moved to explicit startup step

### Breaking Changes
- DataStore must be initialized using InitializeDataStore or InitializeDataStoreAsync
- Static DataStoreManager usage removed
- DataStoreManager and DataStoreBase are now DI-integrated


## [v1.2.13] - 2026-04-13

### Added
- Fencing Token support to distributed locks

## [v1.2.11] - 2026-04-10

### Added
- Lock object, AcquireLock, Extend Lock, Release Lock methods

## [v1.2.10] - 2024-08-09

### Added
- Count method

## [v1.2.9] - 2024-05-16

### Added
- [Feature] Locking with sliding expiration
- This changelog file

### Changed
- [Feature] English xml doc comments

### Removed
- [Feature] lockExpire parameter of Lock methods
