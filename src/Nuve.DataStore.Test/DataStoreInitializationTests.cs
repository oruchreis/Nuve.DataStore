using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nuve.DataStore.Redis;
using Nuve.DataStore.Serializer.JsonNet;

namespace Nuve.DataStore.Test;

[TestClass]
public class DataStoreInitializationTests
{
    [TestInitialize]
    public void TestInitialize()
    {
        DataStoreRuntime.ResetForTests();
        CountingSerializer.Reset();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        DataStoreRuntime.ResetForTests();
        CountingSerializer.Reset();
    }

    [TestMethod]
    public void KeyValueStore_Throws_When_DataStore_Runtime_Not_Initialized()
    {
        var ex = Assert.ThrowsException<InvalidOperationException>(() =>
        {
            _ = new KeyValueStore();
        });

        StringAssert.Contains(ex.Message, "InitializeDataStore");
    }

    [TestMethod]
    public async Task KeyValueStore_Works_After_InitializeDataStore()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("kv-sync"));

        serviceProvider.InitializeDataStore();

        var store = new KeyValueStore();

        var key = "init-sync:key";
        var value = "hello-sync";

        await store.SetAsync(key, value);
        var result = await store.GetAsync<string>(key);

        Assert.AreEqual(value, result);
    }

    [TestMethod]
    public async Task KeyValueStore_Works_After_InitializeDataStoreAsync()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("kv-async"));

        await serviceProvider.InitializeDataStoreAsync();

        var store = new KeyValueStore();

        var key = "init-async:key";
        var value = "hello-async";

        await store.SetAsync(key, value);
        var result = await store.GetAsync<string>(key);

        Assert.AreEqual(value, result);
    }

    [TestMethod]
    public async Task Named_Connections_Use_Different_Root_Namespaces()
    {
        var rootNamespace1 = Bootstrap.NewRootNamespace("ns-app");
        var rootNamespace2 = Bootstrap.NewRootNamespace("ns-cache");
        var connectionString = RedisTestHelpers.GetRedisConnectionString();

        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(builder =>
        {
            builder.AddDefaultConnection(
                provider: "Redis",
                options: new ConnectionOptions
                {
                    ConnectionString = connectionString,
                    ConnectionMode = ConnectionMode.Shared
                },
                rootNamespace: rootNamespace1);

            builder.AddConnection(
                name: "cache",
                provider: "Redis",
                options: new ConnectionOptions
                {
                    ConnectionString = connectionString,
                    ConnectionMode = ConnectionMode.Shared
                },
                rootNamespace: rootNamespace2);
        });

        serviceProvider.InitializeDataStore();

        var defaultStore = new KeyValueStore();
        var namedStore = new KeyValueStore(connectionName: "cache");

        const string key = "same-key";
        const string defaultValue = "default-value";
        const string namedValue = "named-value";

        await defaultStore.SetAsync(key, defaultValue);
        await namedStore.SetAsync(key, namedValue);

        var resultFromDefault = await defaultStore.GetAsync<string>(key);
        var resultFromNamed = await namedStore.GetAsync<string>(key);

        Assert.AreEqual(defaultValue, resultFromDefault);
        Assert.AreEqual(namedValue, resultFromNamed);
    }

    [TestMethod]
    public async Task Custom_Serializer_Is_Used_When_Registered()
    {
        using var serviceProvider = new ServiceCollection()
            .AddDataStore()
            .AddDataStoreSerializer(new CountingSerializer())
            .AddRedisDataStoreProvider("Redis")
            .AddDefaultConnection(
                provider: "Redis",
                options: new ConnectionOptions
                {
                    ConnectionString = RedisTestHelpers.GetRedisConnectionString(),
                    ConnectionMode = ConnectionMode.Shared
                },
                rootNamespace: Bootstrap.NewRootNamespace("serializer"))
            .Services
            .BuildServiceProvider();

        serviceProvider.InitializeDataStore();

        var store = new KeyValueStore();

        const string key = "serializer:value";
        const string value = "probe";

        await store.SetAsync(key, value);
        var result = await store.GetAsync<string>(key);

        Assert.AreEqual(value, result);
        Assert.AreEqual(1, CountingSerializer.SerializeCount);
        Assert.AreEqual(1, CountingSerializer.DeserializeCount);
    }

    [TestMethod]
    public async Task Connection_Uses_Named_Serializer_When_Configured()
    {
        CountingSerializer.Reset();

        using var serviceProvider = new ServiceCollection()
            .AddDataStore()
            .AddDataStoreSerializer<JsonNetDataStoreSerializer>()
            .AddDataStoreSerializer("counting", new CountingSerializer())
            .AddRedisDataStoreProvider("Redis")
            .AddDefaultConnection(
                provider: "Redis",
                options: new ConnectionOptions
                {
                    ConnectionString = RedisTestHelpers.GetRedisConnectionString(),
                    ConnectionMode = ConnectionMode.Shared
                },
                rootNamespace: Bootstrap.NewRootNamespace("default-serializer"))
            .AddConnection(
                name: "counting",
                provider: "Redis",
                options: new ConnectionOptions
                {
                    ConnectionString = RedisTestHelpers.GetRedisConnectionString(),
                    ConnectionMode = ConnectionMode.Shared
                },
                serializer: "counting",
                rootNamespace: Bootstrap.NewRootNamespace("named-serializer"))
            .Services
            .BuildServiceProvider();

        serviceProvider.InitializeDataStore();

        var store = new KeyValueStore(connectionName: "counting");

        await store.SetAsync("key", "value");
        var result = await store.GetAsync<string>("key");

        Assert.AreEqual("value", result);
        Assert.AreEqual(1, CountingSerializer.SerializeCount);
        Assert.AreEqual(1, CountingSerializer.DeserializeCount);
    }

    [TestMethod]
    public void AddConnection_Action_Uses_Configuration_As_Base()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DataStore:Connections:cache:Provider"] = "Redis",
                ["DataStore:Connections:cache:ConnectionString"] = "config-connection",
                ["DataStore:Connections:cache:RetryCount"] = "7",
                ["DataStore:Connections:cache:RootNamespace"] = "from-config"
            })
            .Build();

        var services = new ServiceCollection();
        var builder = services
            .AddDataStore(configuration)
            .AddRedisDataStoreProvider("Redis");

        builder.AddConnection(
            name: "cache",
            provider: "Redis",
            configure: options =>
            {
                options.RetryCount = 11;
            },
            rootNamespace: "from-code");

        var registrationStore = DataStoreServiceCollectionExtensions.GetOrAddRegistrationStore(builder.Services);
        var registration = registrationStore.Connections.Single(x => x.Name == "cache");

        Assert.AreEqual("config-connection", registration.Options.ConnectionString);
        Assert.AreEqual(11, registration.Options.RetryCount);
        Assert.AreEqual("from-code", registration.RootNamespace);
    }

    private sealed class SerializerProbeModel
    {
        public int Id { get; set; }

        public string? Name { get; set; }
    }

    private sealed class CountingSerializer : IDataStoreSerializer
    {
        public static int SerializeCount;
        public static int DeserializeCount;

        public static void Reset()
        {
            SerializeCount = 0;
            DeserializeCount = 0;
        }

        public byte[] Serialize<T>(T? objectToSerialize)
        {
            Interlocked.Increment(ref SerializeCount);
            return System.Text.Encoding.UTF8.GetBytes(objectToSerialize?.ToString() ?? string.Empty);
        }

        public byte[] Serialize(object? objectToSerialize, Type type)
        {
            Interlocked.Increment(ref SerializeCount);
            return System.Text.Encoding.UTF8.GetBytes(objectToSerialize?.ToString() ?? string.Empty);
        }

        public T? Deserialize<T>(byte[]? serializedObject)
        {
            Interlocked.Increment(ref DeserializeCount);

            if (typeof(T) == typeof(string))
                return (T)(object)(System.Text.Encoding.UTF8.GetString(serializedObject ?? []));

            return default;
        }

        public object? Deserialize(byte[]? serializedObject, Type type)
        {
            Interlocked.Increment(ref DeserializeCount);

            if (type == typeof(string))
                return System.Text.Encoding.UTF8.GetString(serializedObject ?? []);

            return null;
        }
    }
}
