using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nuve.DataStore.Serializer.JsonNet;

namespace Nuve.DataStore.Test;

[TestClass]
public class StoreExpirationTests
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
    [TestCategory("Integration")]
    public void HashStore_Set_ShouldApplyDefaultExpire_Immediately_WithAutoPingAndCustomSerializer()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("hash-expire-sync"));
        serviceProvider.InitializeDataStore();

        var serializer = new JsonNetDataStoreSerializer();
        var store = new HashStore(
            "context",
            connectionName: "redis",
            defaultExpire: TimeSpan.FromMinutes(5),
            autoPing: true,
            serializer: serializer);

        Assert.IsTrue(store.Set("field", "value"));
        Assert.IsTrue(store.GetExpire() > TimeSpan.Zero);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task HashStore_SetAsync_ShouldApplyDefaultExpire_Immediately_WithAutoPingAndCustomSerializer()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("hash-expire-async"));
        await serviceProvider.InitializeDataStoreAsync();

        var serializer = new JsonNetDataStoreSerializer();
        var store = new HashStore(
            "context",
            connectionName: "redis",
            defaultExpire: TimeSpan.FromMinutes(5),
            autoPing: true,
            serializer: serializer);

        Assert.IsTrue(await store.SetAsync("field", "value"));
        Assert.IsTrue(await store.GetExpireAsync() > TimeSpan.Zero);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void HashStore_Set_ShouldApplyDefaultExpire_EvenWhenAutoPingIsFalse()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("hash-expire-noautoping"));
        serviceProvider.InitializeDataStore();

        var store = new HashStore("context", defaultExpire: TimeSpan.FromMinutes(5), autoPing: false);

        Assert.IsTrue(store.Set("field", "value"));
        Assert.IsTrue(store.GetExpire() > TimeSpan.Zero);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void DictionaryStore_Set_ShouldApplyDefaultExpire_EvenWhenAutoPingIsFalse()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("dictionary-expire-noautoping"));
        serviceProvider.InitializeDataStore();

        var store = new DictionaryStore<string>("context", defaultExpire: TimeSpan.FromMinutes(5), autoPing: false);

        Assert.IsTrue(store.Set("field", "value"));
        Assert.IsTrue(store.GetExpire() > TimeSpan.Zero);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void HashSetStore_Add_ShouldApplyDefaultExpire()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("hashset-expire"));
        serviceProvider.InitializeDataStore();

        var store = new HashSetStore<string>("context", defaultExpire: TimeSpan.FromMinutes(5), autoPing: false);
        var provider = DataStoreAccess.GetDefaultProvider();

        Assert.AreEqual(1, store.Add("value"));
        Assert.IsTrue(provider.GetExpire(store.MasterKey) > TimeSpan.Zero);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void LinkedListStore_AddLast_ShouldApplyDefaultExpire()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("linkedlist-expire"));
        serviceProvider.InitializeDataStore();

        var store = new LinkedListStore<string>("context", defaultExpire: TimeSpan.FromMinutes(5), autoPing: false);
        var provider = DataStoreAccess.GetDefaultProvider();

        Assert.AreEqual(1, store.AddLast("value"));
        Assert.IsTrue(provider.GetExpire(store.MasterKey) > TimeSpan.Zero);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void KeyValueStore_Increment_ShouldApplyDefaultExpire()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("keyvalue-expire"));
        serviceProvider.InitializeDataStore();

        var store = new KeyValueStore(defaultExpire: TimeSpan.FromMinutes(5), autoPing: false);

        Assert.AreEqual(1, store.Increment("counter"));
        Assert.IsTrue(store.GetExpire("counter") > TimeSpan.Zero);
    }
}
