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

## Store Usage Examples

### KeyValueStore
Simple key-value storage.

```csharp
var store = new KeyValueStore();

await store.SetAsync("user:1", "John");
var value = await store.GetAsync<string>("user:1");
```

**Description:**  
Stores a single value per key. Suitable for caching, simple data storage, and fast lookups.

---

### DictionaryStore
Key-based structured storage (similar to a dictionary/map per root key).

```csharp
var store = new DictionaryStore();

await store.SetAsync("user:1", "name", "John");
await store.SetAsync("user:1", "age", 30);

var name = await store.GetAsync<string>("user:1", "name");
```

**Description:**  
Stores multiple fields under a single key. Useful for grouped data like objects or records.

---

### HashStore
Optimized structured storage for field-value pairs.

```csharp
var store = new HashStore();

await store.SetAsync("user:1", "name", "John");
await store.SetAsync("user:1", "age", 30);

var all = await store.GetAllAsync("user:1");
```

**Description:**  
Similar to DictionaryStore but optimized for bulk operations and retrieving all fields at once.

---

### HashSetStore
Set-based storage (unique values only).

```csharp
var store = new HashSetStore();

await store.AddAsync("tags", "redis");
await store.AddAsync("tags", "cache");
await store.AddAsync("tags", "redis"); // duplicate ignored

var exists = await store.ContainsAsync("tags", "redis");
```

**Description:**  
Stores unique values per key. Ideal for tags, categories, or membership checks.

---

### LinkedListStore
Ordered list storage.

```csharp
var store = new LinkedListStore();

await store.AddLastAsync("queue", "job1");
await store.AddLastAsync("queue", "job2");

var first = await store.GetFirstAsync<string>("queue");
```

**Description:**  
Maintains insertion order. Useful for queues, logs, or ordered processing scenarios.

---

### Using Named Connections

```csharp
var cacheStore = new KeyValueStore("cache");

await cacheStore.SetAsync("key", "value");
```

**Description:**  
Allows using different logical namespaces or configurations over the same provider.

---

### Distributed Lock

```csharp
var store = new KeyValueStore();

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

using var lockItem = store.AcquireLock( //or async AcquireLockAsync
    "resource-lock",
    throwWhenTimeout: true,
    slidingExpire: TimeSpan.FromSeconds(10),
    waitCancelToken: cts.Token);

if (lockItem is not null)
{
    // critical section
}

//OR

await store.LockAsync(
    "resource-lock",
    async () =>
    {
        // critical section
    },
    throwWhenTimeout: true,
    slidingExpire: TimeSpan.FromSeconds(10),
    waitCancelToken: cts.Token);
);

```

**Description:**  
Provides a distributed locking mechanism to coordinate access across multiple processes or instances.  
Supports timeout, sliding expiration, and automatic renewal while the lock is held.

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