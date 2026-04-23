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
    }

    [TestCleanup]
    public void TestCleanup()
    {
        DataStoreRuntime.ResetForTests();
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

        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(builder =>
        {
            builder.AddDefaultConnection(
                provider: "Redis",
                rootNamespace: rootNamespace1);

            builder.AddConnection(
                name: "cache",
                provider: "Redis",
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
            .AddDataStoreSerializer<JsonNetDataStoreSerializer>()
            .AddRedisDataStoreProvider(
                providerName: "Redis",
                options: new ConnectionOptions
                {
                    ConnectionString = RedisTestHelpers.GetRedisConnectionString(),
                    ConnectionMode = ConnectionMode.Shared
                })
            .AddDefaultConnection(
                provider: "Redis",
                rootNamespace: Bootstrap.NewRootNamespace("serializer"))
            .Services
            .BuildServiceProvider();

        serviceProvider.InitializeDataStore();

        var store = new KeyValueStore();

        var model = new SerializerProbeModel
        {
            Id = 42,
            Name = "probe"
        };

        const string key = "serializer:model";

        await store.SetAsync(key, model);
        var result = await store.GetAsync<SerializerProbeModel>(key);

        Assert.IsNotNull(result);
        Assert.AreEqual(model.Id, result!.Id);
        Assert.AreEqual(model.Name, result.Name);
    }

    private sealed class SerializerProbeModel
    {
        public int Id { get; set; }

        public string? Name { get; set; }
    }
}