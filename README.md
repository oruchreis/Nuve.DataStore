# Nuve.DataStore

A lightweight, provider-based data store abstraction with explicit startup initialization, DI-backed provider registration, and Redis support.

|     |     |
| --- | --- |
| **Build** | ![Build status](https://github.com/oruchreis/Nuve.DataStore/workflows/Build,%20Test,%20Package/badge.svg) |
| **DataStore** | [![nuget](https://img.shields.io/nuget/v/Nuve.DataStore.svg)](https://www.nuget.org/packages/Nuve.DataStore/) |
| **DataStore.Redis** | [![nuget](https://img.shields.io/nuget/v/Nuve.DataStore.Redis.svg)](https://www.nuget.org/packages/Nuve.DataStore.Redis/) |
| **DataStore.Serializer.JsonNet** | [![nuget](https://img.shields.io/nuget/v/Nuve.DataStore.Serializer.JsonNet.svg)](https://www.nuget.org/packages/Nuve.DataStore.Serializer.JsonNet/) |
| **DataStore.Serializer.Ceras** | [![nuget](https://img.shields.io/nuget/v/Nuve.DataStore.Serializer.Ceras.svg)](https://www.nuget.org/packages/Nuve.DataStore.Serializer.Ceras/) |

## Installation

```bash
dotnet add package Nuve.DataStore --version 2.0.4
dotnet add package Nuve.DataStore.Redis --version 2.0.4
dotnet add package Nuve.DataStore.Serializer.JsonNet --version 2.0.4
```

## Quick Start

### 1. Register DataStore

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddDataStore()
    .AddDataStoreSerializer<JsonNetDataStoreSerializer>()
    .AddDataStoreSerializer<CerasDataStoreSerializer>("ceras")
    .AddRedisDataStoreProvider("Redis")
    .AddDefaultConnection(
        provider: "Redis",
        options: new ConnectionOptions
        {
            ConnectionString = "localhost:6379,abortConnect=false",
            ConnectionMode = ConnectionMode.Shared
        },
        rootNamespace: "app");
```

### 2. Initialize DataStore

This step is required and must happen before any store instance is created or used.

```csharp
var app = builder.Build();

await app.Services.InitializeDataStoreAsync();
// or
// app.Services.InitializeDataStore();
```

### 3. Use stores

```csharp
var store = new KeyValueStore();

await store.SetAsync("key", "value");
var value = await store.GetAsync<string>("key");
```

## Configuration

Providers are registered from code only. Connection settings can come from configuration and then be overridden in code.

```csharp
builder.Services
    .AddDataStore(builder.Configuration)
    .AddRedisDataStoreProvider("Redis")
    .AddConnection(
        name: "cache",
        provider: "Redis",
        configure: options =>
        {
            options.RetryCount = 10;
        },
        rootNamespace: "cache");
```

```json
{
  "DataStore": {
    "Connections": {
      "Default": {
        "IsDefault": true,
        "Provider": "Redis",
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
        "Provider": "Redis",
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

Later configuration providers can override only the fields they need:

```json
{
  "DataStore": {
    "Connections": {
      "Default": {
        "RootNamespace": "Wcf"
      }
    }
  }
}
```

## Code Overrides

Use the `ConnectionOptions` overload when you want to replace the connection options completely.

```csharp
builder.Services
    .AddDataStore()
    .AddRedisDataStoreProvider("Redis")
    .AddConnection(
        name: "cache",
        provider: "Redis",
        options: new ConnectionOptions
        {
            ConnectionString = "localhost:6379,abortConnect=false",
            ConnectionMode = ConnectionMode.Pooled,
            MaxPoolSize = 16
        },
        serializer: "ceras",
        rootNamespace: "cache");
```

Use the `Action<ConnectionOptions>` overload when configuration should be the base and code should mutate only selected settings.

```csharp
builder.Services
    .AddDataStore(builder.Configuration)
    .AddRedisDataStoreProvider("Redis")
    .AddConnection(
        name: "cache",
        provider: "Redis",
        configure: options =>
        {
            options.RetryCount = 10;
            options.MaxPoolSize = 32;
        },
        serializer: "ceras");
```

## Serializers

`AddDataStoreSerializer(...)` without a name sets the default serializer. Named registrations can be selected per connection.

```csharp
builder.Services
    .AddDataStore()
    .AddDataStoreSerializer<JsonNetDataStoreSerializer>()
    .AddDataStoreSerializer<CerasDataStoreSerializer>("ceras")
    .AddRedisDataStoreProvider("Redis")
    .AddDefaultConnection(
        provider: "Redis",
        options: new ConnectionOptions
        {
            ConnectionString = "localhost:6379,abortConnect=false"
        })
    .AddConnection(
        name: "binary-cache",
        provider: "Redis",
        serializer: "ceras",
        options: new ConnectionOptions
        {
            ConnectionString = "localhost:6379,abortConnect=false"
        });
```

If a connection does not specify a serializer name, the default serializer is used.

## Multiple Connections

Each connection owns its own connection options and initializes its own provider instance at startup.

```csharp
builder.Services
    .AddDataStore()
    .AddRedisDataStoreProvider("Redis")
    .AddDefaultConnection(
        provider: "Redis",
        options: new ConnectionOptions
        {
            ConnectionString = "localhost:6379,abortConnect=false"
        },
        rootNamespace: "app")
    .AddConnection(
        name: "cache",
        provider: "Redis",
        options: new ConnectionOptions
        {
            ConnectionString = "localhost:6380,abortConnect=false",
            ConnectionMode = ConnectionMode.Pooled
        },
        rootNamespace: "cache");
```

```csharp
var defaultStore = new KeyValueStore();
var cacheStore = new KeyValueStore("cache");
```

## Store Usage Examples

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
var store = new LinkedListStore();

await store.AddLastAsync("queue", "job1");
await store.AddLastAsync("queue", "job2");

var first = await store.GetFirstAsync<string>("queue");
```

## Store Construction Notes

Store objects are still created with `new` in v2.0.x.

```csharp
var defaultStore = new KeyValueStore();
var cacheStore = new KeyValueStore("cache");
```

Stores are not resolved from DI yet, but their runtime dependencies come from the initialized `DataStoreManager`.

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
        // critical section
        await Task.CompletedTask;
    },
    slidingExpire: TimeSpan.FromSeconds(10),
    throwWhenTimeout: true);
```

## Custom Serializer

```csharp
builder.Services
    .AddDataStore()
    .AddDataStoreSerializer<JsonNetDataStoreSerializer>();
```

## Important Notes

- `InitializeDataStore()` or `InitializeDataStoreAsync()` must be called before creating or using any store.
- Store instances can be created with `new`; DI is required for providers, not for stores.
- Provider names are resolved using `StringComparer.OrdinalIgnoreCase`.
- Serializer names are resolved using `StringComparer.OrdinalIgnoreCase`.
- Each connection owns its own provider initialization and its own connection settings.
- Each connection can optionally select a named serializer; otherwise the default serializer is used.
- Configuration is applied first; keyed connection objects can be partially overridden by later configuration providers, and code registration can still replace or mutate them during startup.
- Duplicate provider names keep the first registration and log a warning.
- Duplicate connection names are overridden by the last registration.
- If a connection references an unknown provider, initialization fails immediately.
- Provider initialization is eager at startup initialization time; it is not lazy.

## Testing

Tests use the `REDIS_TEST_CONNECTION` environment variable. The repository includes `test.runsettings` for that.

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
