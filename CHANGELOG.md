# Changelog

All notable changes to this project will be documented in this file.

## [v2.1.0] - 2026-08-04

### Added
- **Server capability detection.** `RedisStoreProvider` asks the connected server for its version, topology and feature set while initializing and caches the answer in `RedisServerCapabilities`, exposed through the `Capabilities` property. A server that cannot be reached during startup no longer fails initialization; the capabilities stay unresolved and are asked for again on first use. `ServerType` and `IsClustered` report how the endpoint presents itself, which separates a client side cluster from a single endpoint.
- Conditional expiration now uses the native `PEXPIRE key ttl NX` on servers that support it (Redis 7.0 and later) and falls back to the equivalent script elsewhere, which keeps a deployment working across mixed environments such as Redis 7.x in production and KeyDB 6 in development. Until the capabilities are known the portable path is used, so an unreachable server degrades rather than breaks.
- **Multi-key requests adapt to the topology.** Whether a single command may span several slots is measured during initialization by attempting exactly such a write, because the endpoint alone does not reveal it: a proxy in front of a sharded database looks standalone, and among those some accept a cross-slot write while others reject it. Where it is accepted, `KeyValueStore.Set(IDictionary)` and `Get(params string[])` keep issuing one `MSET`/`MGET` and stay atomic. Where it is not, the keys are grouped by slot and one request is sent per slot, so the call succeeds on Redis Cluster, on the OSS clustering policy and on proxies instead of failing with `CROSSSLOT`. Grouping keeps each slot's write atomic but not the call as a whole.

### Changed
- `IDataStoreProvider.SetExpire`/`SetExpireAsync` and the write methods of `IKeyValueStoreProvider`, `IDictionaryStoreProvider`, `IHashSetStoreProvider`, and `ILinkedListStoreProvider` take an optional `bool onlyWhenMissing` parameter that applies the expiration only to keys without a TTL. Servers before Redis 7.0 have no `EXPIRE ... NX`, so this is implemented with Lua scripts that also work on Codis and KeyDB.
- **Set combinations over keys on different slots now explain themselves.** `HashSetStore` operations that combine the store with caller supplied keys (`DifferenceToHashSet`, `DifferenceToNewSet`, `IntersectionToHashSet`, `IntersectionToNewSet`, `UnionToHashSet`, `UnionToNewSet`, `MoveValue`) cannot be split per slot, because Redis has no way to combine sets across slots. On a server that refuses commands spanning slots they now fail with an `InvalidOperationException` that names each key with the slot it landed on and shows how to co-locate them with a shared hash tag, instead of surfacing a bare `CROSSSLOT` error.

### Fixed
- **`DefaultExpire` now always applies the initial TTL.** v2.0.8 tied the write-time expiration to `AutoPing`, so a store configured with `defaultExpire` but without `autoPing` produced keys with no TTL at all. `DefaultExpire` is once again applied on every write regardless of `AutoPing`.
- **`AutoPing` is now purely a sliding-expiration switch.** While it is disabled no operation extends an existing TTL — reads leave it alone and writes apply the expiration only to keys that do not have one yet, so a key keeps expiring from its first write. While it is enabled every read and write slides the TTL back to `DefaultExpire`, as before. An explicit `Ping()` call always refreshes.
- `KeyValueStore` writes no longer discard the remaining TTL. A plain Redis `SET` clears the expiration, which silently turned a write into an expiration reset; the remaining TTL is now preserved when `AutoPing` is disabled.
- Mutation paths that unconditionally refreshed the TTL (`LinkedListStore` `AddAfter`/`AddBefore`/`Insert`/`RemoveAt`/`Remove`/`Trim`, `HashSetStore` `Remove`/`MoveValue`, multi-key `KeyValueStore.Set`) now honour `AutoPing` instead of always sliding the expiration.
- **`HashSetStore` set operations no longer fail on a sharded deployment.** The value based `DifferenceWith`, `SymmetricDifferenceWith`, `IntersectionWith`, `UnionWith` and their variants stage the incoming values in a temporary key and then combine it with the store's master key. That temporary key hashed to a different slot, so `SDIFFSTORE` and `SUNIONSTORE` were rejected with `CROSSSLOT` on Redis Cluster, on the OSS clustering policy, and on proxied shard sets where those commands are not cross-slot capable. The temporary keys now carry the master key as a hash tag and therefore share its slot.

## [v2.0.10] - 2026-07-20

### Changed
- **Unknown connection name falls back to the default connection:** Resolving a store whose `connectionName` is not registered no longer throws immediately. When a default connection exists, a warning is logged and the default connection is used instead. An `InvalidOperationException` is still thrown only when no default connection is available to fall back to.

## [v2.0.9] - 2026-07-20

### Fixed
- **Fencing counter leak for ephemeral lock keys:** When a store has `DefaultExpire=0` and the locked resource key has no live TTL (e.g. a parameter-less `KeyValueStore` used purely as a mutex over high-cardinality keys), `GetFencingExpire` returned `null`, which made the provider create the fencing counter with no expiration. Each distinct resource key left a permanent `__fencing__` counter, growing unbounded. `GetFencingExpire` now falls back to the lock's sliding expiration when the resource key has no positive TTL, so abandoned fencing counters self-expire while actively contended keys keep their counter alive (its TTL is refreshed on every acquire). This reverses the unconditional permanent-counter behavior introduced in v2.0.8 for keys without a TTL.

## [v2.0.8] - 2026-05-20

### Fixed
- **Fencing monotonicity:** Permanent resource keys (no TTL) with `DefaultExpire=0` no longer fall back to `slidingExpire` as fencing counter TTL. When `GetFencingExpire` returns `null` the fencing counter is created without expiration, preserving monotonic counter growth across lock cycles.
- **Cross-instance Ping:** Removed per-store-instance `_trackedFencingKeysByResource` tracking. Fencing key is now deterministically computed via `BuildFencingKey(resourceKey)` so `Ping`/`autoPing` can extend the fencing counter TTL without requiring the exact store instance that created the lock.
- **KeyValueStore lock key collision:** `KeyValueStore` Lock methods now acquire the physical Redis lock on `{resourceKey}:__lock__` instead of `resourceKey` itself, preventing `SET NX` from failing silently on keys that already hold data.
- **autoPing semantics:** When `autoPing=false`, write operations (`Set`, `Increment`, `Decrement`, `Exchange`, etc.) no longer atomically reset the key TTL. TTL management becomes fully explicit — only a manual `Ping()` call applies `DefaultExpire`. When `autoPing=true`, writes continue to atomically apply `DefaultExpire` (previous behavior preserved). A new `AutoExpire` protected property centralises this decision.
- **Ping fencing key extend:** `Ping`/`PingAsync` now always attempts to extend the fencing counter TTL regardless of whether the data key's `SetExpire` succeeded. This ensures the fencing counter stays alive for `KeyValueStore` instances used purely for distributed locking (no data key stored).
- Simplified `BuildFencingKey` to a single deterministic form `{resourceKey}:__fencing__:{TypeName}` — the previous per-lock `logicalLockKey` suffix caused counter proliferation across different callers for the same resource.

## [v2.0.7] - 2026-05-11

### Fixed
- Store-level distributed locks now scope fencing counters to the underlying store resource instead of sharing a single counter across different `HashStore`, `DictionaryStore`, `HashSetStore`, or `LinkedListStore` master keys.
- Fencing counters now receive a retention TTL based on the store's `DefaultExpire`, the current resource TTL, or the lock sliding expiration as a fallback, preventing unbounded fencing-key leaks.
- `Ping` and `autoPing` for tracked store resources now extend the TTL of associated fencing counters alongside the store key.
- `KeyValueStore` lock fencing now uses the full resource key and no longer leaves provider-generated fencing keys without expiration when a fallback TTL is available.

## [v2.0.6] - 2026-05-11

### Fixed
- `HashStore` write operations now apply `DefaultExpire` deterministically instead of relying on best-effort `autoPing`.
- `DictionaryStore` write operations now apply `DefaultExpire` deterministically instead of relying on best-effort `autoPing`.
- `HashSetStore` mutation operations now apply `DefaultExpire` deterministically instead of relying on best-effort `autoPing`.
- `LinkedListStore` mutation operations now apply `DefaultExpire` deterministically instead of relying on best-effort `autoPing`.
- `KeyValueStore.Set` and selected `DictionaryStore`, `HashStore`, `HashSetStore`, and `LinkedListStore` write paths now attach `DefaultExpire` through provider-side atomic Redis operations where supported.
- Fencing token counters are now separated from transient physical lock keys when locks are acquired through store APIs, preventing unbounded `__fencing__` key growth for context-based lock keys.

### Added
- Integration tests covering default TTL application for `HashStore`, `DictionaryStore`, `HashSetStore`, and `LinkedListStore`.

## [v2.0.5] - 2026-04-24

### Added
- `AddDataStore()` now automatically uses a registered `IConfiguration` when one is available from the built `IServiceProvider`.

### Changed
- Configuration finalization moved from `AddDataStore()` registration time to `DataStoreManager` creation time.
- Explicit `IConfiguration` parameter is still supported and takes precedence over `IServiceProvider` resolution.

## [v2.0.4] - 2026-04-24

### Added
- `AddDataStore()` now automatically uses a registered `IConfiguration` when one is already present in `IServiceCollection`.

### Changed
- Explicit `IConfiguration` parameter is still supported, but no longer required in common ASP.NET Core registration flows.

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
