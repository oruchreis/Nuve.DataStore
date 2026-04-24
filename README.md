# Nuve.DataStore

A provider-based data store abstraction with explicit startup initialization, connection-scoped provider instances, named serializers, and Redis support.

|     |     |
| --- | --- |
| **Build** | ![Build status](https://github.com/oruchreis/Nuve.DataStore/workflows/Build,%20Test,%20Package/badge.svg) |
| **DataStore** | [![nuget](https://img.shields.io/nuget/v/Nuve.DataStore.svg)](https://www.nuget.org/packages/Nuve.DataStore/) |
| **DataStore.Redis** | [![nuget](https://img.shields.io/nuget/v/Nuve.DataStore.Redis.svg)](https://www.nuget.org/packages/Nuve.DataStore/) |
| **DataStore.Serializer.JsonNet** | [![nuget](https://img.shields.io/nuget/v/Nuve.DataStore.Serializer.JsonNet.svg)](https://www.nuget.org/packages/Nuve.DataStore/) |
| **DataStore.Serializer.Ceras** | [![nuget](https://img.shields.io/nuget/v/Nuve.DataStore.Serializer.Ceras.svg)](https://www.nuget.org/packages/Nuve.DataStore/) |

## Installation

```bash
dotnet add package Nuve.DataStore --version 2.0.5
dotnet add package Nuve.DataStore.Redis --version 2.0.5
dotnet add package Nuve.DataStore.Serializer.JsonNet --version 2.0.5
```

Add `Nuve.DataStore.Serializer.Ceras` only if you need the Ceras serializer.

## Core Concepts

- Providers are registered from code only.
- Connections can come from configuration, code, or both.
- Each connection owns its own `ConnectionOptions` and initializes its own provider instance.
- Stores are still created with `new`; they are not resolved from DI in `v2.0.x`.
- `InitializeDataStore()` or `InitializeDataStoreAsync()` is mandatory before any store is created or used.

## Quick Start

```csharp
using Nuve.DataStore;
using Nuve.DataStore.Redis;
using Nuve.DataStore.Serializer.JsonNet;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddDataStore()
    .AddDataStoreSerializer<JsonNetDataStoreSerializer>()
    .AddRedisDataStoreProvider()
    .AddDefaultConnection(
        provider: "redis",
        options: new ConnectionOptions
        {
            ConnectionString = "localhost:6379,abortConnect=false",
            ConnectionMode = ConnectionMode.Shared
        },
        rootNamespace: "app");

var app = builder.Build();

await app.Services.InitializeDataStoreAsync();

var store = new KeyValueStore();
await store.SetAsync("key", "value");
var value = await store.GetAsync<string>("key");
```

## Registration Flow

`AddDataStore()` can be used in two ways:

```csharp
builder.Services.AddDataStore();
```

This uses the application's registered `IConfiguration` when `DataStoreManager` is created.

```csharp
builder.Services.AddDataStore(builder.Configuration);
```

This uses the explicitly supplied configuration instead.

Configuration precedence is:

1. Configuration sources are merged first.
2. Code registrations are applied after configuration.
3. `ConnectionOptions` overload replaces options completely.
4. `Action<ConnectionOptions>` overload mutates the existing configuration-based options.

On `net48`, legacy `web.config` / `app.config` fallback is also supported when `IConfiguration` is not used.

## Configuration

Connections are configured as a keyed object, not an array. This allows later configuration providers to override only selected fields.

```json
{
  "DataStore": {
    "Connections": {
      "Default": {
        "IsDefault": true,
        "Provider": "redis",
        "Serializer": "json",
        "ConnectionString": "localhost:6379,abortConnect=false",
        "ConnectionMode": "Shared",
        "RetryCount": 5,
        "MaxPoolSize": 8,
        "PoolWaitTimeout": "00:00:02",
        "BackgroundProbeMinInterval": "00:00:05",
        "HealthCheckTimeout": "00:00:02",
        "SwapDisposeDelay": "00:00:05",
        "RootNamespace": "app"
      },
      "cache": {
        "Provider": "redis",
        "Serializer": "ceras",
        "ConnectionString": "localhost:6379,abortConnect=false",
        "ConnectionMode": "Pooled",
        "MaxPoolSize": 16,
        "RootNamespace": "cache"
      }
    }
  }
}
```

Partial override example:

```json
{
  "DataStore": {
    "Connections": {
      "Default": {
        "RootNamespace": "wcf"
      }
    }
  }
}
```

Required connection fields:

- `Provider`
- `ConnectionString`

Optional connection fields:

- `Serializer`
- `ConnectionMode`
- `RetryCount`
- `MaxPoolSize`
- `PoolWaitTimeout`
- `BackgroundProbeMinInterval`
- `HealthCheckTimeout`
- `SwapDisposeDelay`
- `RootNamespace`
- `CompressBiggerThan`
- `IsDefault`

## Providers

Providers are added from code and resolved by name with `StringComparer.OrdinalIgnoreCase`.

```csharp
builder.Services
    .AddDataStore()
    .AddRedisDataStoreProvider();          // default provider name: "redis"

builder.Services
    .AddDataStore()
    .AddRedisDataStoreProvider("cache");   // custom provider name
```

If the same provider name is registered more than once, the first registration is kept and a warning is logged.

## Connections

### Default connection

```csharp
builder.Services
    .AddDataStore()
    .AddRedisDataStoreProvider()
    .AddDefaultConnection(
        provider: "redis",
        options: new ConnectionOptions
        {
            ConnectionString = "localhost:6379,abortConnect=false",
            ConnectionMode = ConnectionMode.Shared
        },
        rootNamespace: "app");
```

### Named connection

```csharp
builder.Services
    .AddDataStore()
    .AddRedisDataStoreProvider()
    .AddConnection(
        name: "cache",
        provider: "redis",
        options: new ConnectionOptions
        {
            ConnectionString = "localhost:6380,abortConnect=false",
            ConnectionMode = ConnectionMode.Pooled,
            MaxPoolSize = 16
        },
        rootNamespace: "cache");
```

Use the named connection from a store:

```csharp
var defaultStore = new KeyValueStore();
var cacheStore = new KeyValueStore("cache");
```

## Code Overrides

Use the `ConnectionOptions` overload when code should replace the connection options completely:

```csharp
builder.Services
    .AddDataStore(builder.Configuration)
    .AddRedisDataStoreProvider()
    .AddConnection(
        name: "cache",
        provider: "redis",
        options: new ConnectionOptions
        {
            ConnectionString = "localhost:6379,abortConnect=false",
            ConnectionMode = ConnectionMode.Pooled,
            MaxPoolSize = 32
        },
        serializer: "ceras",
        rootNamespace: "cache");
```

Use the `Action<ConnectionOptions>` overload when configuration should be the base and code should only modify selected fields:

```csharp
builder.Services
    .AddDataStore(builder.Configuration)
    .AddRedisDataStoreProvider()
    .AddConnection(
        name: "cache",
        provider: "redis",
        configure: options =>
        {
            options.RetryCount = 10;
            options.MaxPoolSize = 32;
        },
        serializer: "ceras");
```

## Serializers

`AddDataStoreSerializer(...)` without a name sets the default serializer.

```csharp
builder.Services
    .AddDataStore()
    .AddDataStoreSerializer<JsonNetDataStoreSerializer>();
```

Named serializers can be registered and selected per connection:

```csharp
builder.Services
    .AddDataStore()
    .AddDataStoreSerializer<JsonNetDataStoreSerializer>("json")
    .AddDataStoreSerializer<CerasDataStoreSerializer>("ceras")
    .AddRedisDataStoreProvider()
    .AddDefaultConnection(
        provider: "redis",
        serializer: "json",
        options: new ConnectionOptions
        {
            ConnectionString = "localhost:6379,abortConnect=false"
        })
    .AddConnection(
        name: "binary-cache",
        provider: "redis",
        serializer: "ceras",
        options: new ConnectionOptions
        {
            ConnectionString = "localhost:6379,abortConnect=false"
        });
```

If a connection does not specify `Serializer`, the default serializer is used.

## Initialization

Provider initialization is explicit and eager. It is not lazy.

```csharp
await serviceProvider.InitializeDataStoreAsync();
// or
serviceProvider.InitializeDataStore();
```

What happens during initialization:

- All finalized connections are resolved.
- Each connection creates its own provider instance.
- Each provider is initialized with that connection's own `ConnectionOptions`.
- After initialization, store creation and provider lookup stay on the runtime fast path.

If a connection references an unknown provider or serializer, initialization fails immediately.

## Store Usage

### KeyValueStore

```csharp
var store = new KeyValueStore();

await store.SetAsync("user:1", "John");
var value = await store.GetAsync<string>("user:1");
```

### DictionaryStore

```csharp
var store = new DictionaryStore();

await store.SetAsync("user:1", "name", "John");
await store.SetAsync("user:1", "age", 30);

var name = await store.GetAsync<string>("user:1", "name");
```

### HashStore

```csharp
var store = new HashStore();

await store.SetAsync("user:1", "name", "John");
await store.SetAsync("user:1", "age", 30);

var all = await store.GetAllAsync("user:1");
```

### HashSetStore

```csharp
var store = new HashSetStore();

await store.AddAsync("tags", "redis");
await store.AddAsync("tags", "cache");

var exists = await store.ContainsAsync("tags", "redis");
```

### LinkedListStore

```csharp
var store = new LinkedListStore<string>();

await store.AddLastAsync("queue", "job1");
await store.AddLastAsync("queue", "job2");
```

## Distributed Lock

```csharp
var store = new KeyValueStore();
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

using var lockItem = store.AcquireLock(
    "resource-lock",
    waitCancelToken: cts.Token,
    slidingExpire: TimeSpan.FromSeconds(10),
    throwWhenTimeout: true);

if (lockItem is not null)
{
    // critical section
}
```

```csharp
await store.LockAsync(
    "resource-lock",
    waitTimeout: TimeSpan.FromSeconds(5),
    actionAsync: async () =>
    {
        await Task.CompletedTask;
    },
    slidingExpire: TimeSpan.FromSeconds(10),
    throwWhenTimeout: true);
```

## Important Notes

- Store instances are created with `new`.
- Do not create or use stores before `InitializeDataStore()` or `InitializeDataStoreAsync()`.
- Provider names are case-insensitive.
- Serializer names are case-insensitive.
- Duplicate connection names are resolved by the last registration.
- With `Action<ConnectionOptions>`, existing configuration values are preserved unless code changes them.
- With `ConnectionOptions`, the connection options are replaced completely.

## Testing

Tests use the `REDIS_TEST_CONNECTION` environment variable. The repository includes `test.runsettings`.

```xml
<RunSettings>
  <RunConfiguration>
    <EnvironmentVariables>
      <REDIS_TEST_CONNECTION>localhost:6379,abortConnect=false</REDIS_TEST_CONNECTION>
    </EnvironmentVariables>
  </RunConfiguration>
</RunSettings>
```

```bash
dotnet test --settings test.runsettings
```

## Supported Frameworks

- .NET Standard 2.1
- .NET Framework 4.8
- .NET 6
- .NET 7
- .NET 8
- .NET 9
- .NET 10
