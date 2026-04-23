# Nuve.DataStore

A lightweight, provider-based data store abstraction with DI integration and Redis support.

|     |     |
| --- | --- |
| **Build** | ![Build status](https://github.com/oruchreis/Nuve.DataStore/workflows/Build,%20Test,%20Package/badge.svg) |
| **DataStore** | [![nuget](https://img.shields.io/nuget/v/Nuve.DataStore.svg)](https://www.nuget.org/packages/Nuve.DataStore/) |
| **DataStore.Redis** | [![nuget](https://img.shields.io/nuget/v/Nuve.DataStore.Redis.svg)](https://www.nuget.org/packages/Nuve.DataStore.Redis/) |
| **DataStore.Serializer.JsonNet** | [![nuget](https://img.shields.io/nuget/v/Nuve.DataStore.Serializer.JsonNet.svg)](https://www.nuget.org/packages/Nuve.DataStore.Serializer.JsonNet/) |
| **DataStore.Serializer.Ceras** | [![nuget](https://img.shields.io/nuget/v/Nuve.DataStore.Serializer.Ceras.svg)](https://www.nuget.org/packages/Nuve.DataStore.Serializer.Ceras/) |


## Installation

```bash
dotnet add package Nuve.DataStore
dotnet add package Nuve.DataStore.Redis
dotnet add package Nuve.DataStore.Serializer.JsonNet
```

---

## Quick Start

### 1. Register services

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddDataStore()
    .AddRedisDataStoreProvider("Redis", new ConnectionOptions
    {
        ConnectionString = "localhost:6379"
    })
    .AddDefaultConnection(
        provider: "Redis",
        rootNamespace: "app");
```

---

### 2. Initialize DataStore

**This step is required.**

```csharp
var app = builder.Build();

await app.Services.InitializeDataStoreAsync();
// or
// app.Services.InitializeDataStore();
```

---

### 3. Use stores

```csharp
private static readonly KeyValueStore _store = new();

await _store.SetAsync("key", "value");
var value = await _store.GetAsync<string>("key");
```

---

## Multiple Connections

```csharp
builder.Services
    .AddDataStore()
    .AddRedisDataStoreProvider("Redis", new ConnectionOptions
    {
        ConnectionString = "localhost:6379"
    })
    .AddDefaultConnection("Redis", rootNamespace: "app")
    .AddConnection("cache", "Redis", rootNamespace: "cache");
```

```csharp
var defaultStore = new KeyValueStore();
var cacheStore = new KeyValueStore("cache");
```

---

## Custom Serializer

```csharp
builder.Services
    .AddDataStore()
    .AddDataStoreSerializer<JsonNetDataStoreSerializer>();
```

---

## Important Notes

- `InitializeDataStore()` or `InitializeDataStoreAsync()` **must be called before creating any store instances**
- Store instances can be created using `new` (DI is not required for stores)
- Providers are registered via DI and initialized at startup
- Provider names are **case-insensitive**
- Each connection represents a logical namespace over the same provider
- Duplicate provider or connection names will throw exceptions
- If a connection references an unregistered provider, initialization will fail

---

## Testing

Use `.runsettings` to configure environment variables for tests:

```xml
<RunSettings>
  <RunConfiguration>
    <EnvironmentVariables>
      <DATASTORE_REDIS_CONNECTION>localhost:6379</DATASTORE_REDIS_CONNECTION>
    </EnvironmentVariables>
  </RunConfiguration>
</RunSettings>
```

Run tests:

```bash
dotnet test --settings test.runsettings
```

---

## Supported Frameworks

- .NET Framework 4.8
- .NET Standard 2.1
- .NET 6+
- .NET 7
- .NET 8
- .NET 9
- .NET 10