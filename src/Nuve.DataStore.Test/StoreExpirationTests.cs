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

    // ──────────────────────────────────────────────────────────────
    // autoPing = true  →  write applies DefaultExpire atomically
    // ──────────────────────────────────────────────────────────────

    [TestMethod]
    [TestCategory("Integration")]
    public void HashStore_Set_WithAutoPingTrue_ShouldApplyDefaultExpire()
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
    public async Task HashStore_SetAsync_WithAutoPingTrue_ShouldApplyDefaultExpire()
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
    public void DictionaryStore_Set_WithAutoPingTrue_ShouldApplyDefaultExpire()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("dictionary-expire-autopingtrue"));
        serviceProvider.InitializeDataStore();

        var store = new DictionaryStore<string>("context", defaultExpire: TimeSpan.FromMinutes(5), autoPing: true);

        Assert.IsTrue(store.Set("field", "value"));
        Assert.IsTrue(store.GetExpire() > TimeSpan.Zero);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void HashSetStore_Add_WithAutoPingTrue_ShouldApplyDefaultExpire()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("hashset-expire-autopingtrue"));
        serviceProvider.InitializeDataStore();

        var store = new HashSetStore<string>("context", defaultExpire: TimeSpan.FromMinutes(5), autoPing: true);
        var provider = DataStoreAccess.GetDefaultProvider();

        Assert.AreEqual(1, store.Add("value"));
        Assert.IsTrue(provider.GetExpire(store.MasterKey) > TimeSpan.Zero);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void LinkedListStore_AddLast_WithAutoPingTrue_ShouldApplyDefaultExpire()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("linkedlist-expire-autopingtrue"));
        serviceProvider.InitializeDataStore();

        var store = new LinkedListStore<string>("context", defaultExpire: TimeSpan.FromMinutes(5), autoPing: true);
        var provider = DataStoreAccess.GetDefaultProvider();

        Assert.AreEqual(1, store.AddLast("value"));
        Assert.IsTrue(provider.GetExpire(store.MasterKey) > TimeSpan.Zero);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void KeyValueStore_Increment_WithAutoPingTrue_ShouldApplyDefaultExpire()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("keyvalue-expire-autopingtrue"));
        serviceProvider.InitializeDataStore();

        var store = new KeyValueStore(defaultExpire: TimeSpan.FromMinutes(5), autoPing: true);

        Assert.AreEqual(1, store.Increment("counter"));
        Assert.IsTrue(store.GetExpire("counter") > TimeSpan.Zero);
    }

    // ──────────────────────────────────────────────────────────────
    // autoPing = false  →  writes do NOT touch TTL; Ping() does
    // ──────────────────────────────────────────────────────────────

    [TestMethod]
    [TestCategory("Integration")]
    public void HashStore_Set_WithAutoPingFalse_ShouldNotApplyDefaultExpire()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("hash-expire-noautoping"));
        serviceProvider.InitializeDataStore();

        var store = new HashStore("context", defaultExpire: TimeSpan.FromMinutes(5), autoPing: false);

        Assert.IsTrue(store.Set("field", "value"));
        Assert.IsNull(store.GetExpire());  // no TTL set — user controls it explicitly
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void HashStore_Set_WithAutoPingFalse_ExplicitPing_ShouldApplyDefaultExpire()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("hash-expire-noautoping-ping"));
        serviceProvider.InitializeDataStore();

        var store = new HashStore("context", defaultExpire: TimeSpan.FromMinutes(5), autoPing: false);

        Assert.IsTrue(store.Set("field", "value"));
        Assert.IsNull(store.GetExpire());

        Assert.IsTrue(store.Ping());           // explicit Ping sets TTL
        Assert.IsTrue(store.GetExpire() > TimeSpan.Zero);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void DictionaryStore_Set_WithAutoPingFalse_ShouldNotApplyDefaultExpire()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("dictionary-expire-noautoping"));
        serviceProvider.InitializeDataStore();

        var store = new DictionaryStore<string>("context", defaultExpire: TimeSpan.FromMinutes(5), autoPing: false);

        Assert.IsTrue(store.Set("field", "value"));
        Assert.IsNull(store.GetExpire());
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void HashSetStore_Add_WithAutoPingFalse_ShouldNotApplyDefaultExpire()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("hashset-expire-noautoping"));
        serviceProvider.InitializeDataStore();

        var store = new HashSetStore<string>("context", defaultExpire: TimeSpan.FromMinutes(5), autoPing: false);
        var provider = DataStoreAccess.GetDefaultProvider();

        Assert.AreEqual(1, store.Add("value"));
        Assert.IsNull(provider.GetExpire(store.MasterKey));
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void LinkedListStore_AddLast_WithAutoPingFalse_ShouldNotApplyDefaultExpire()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("linkedlist-expire-noautoping"));
        serviceProvider.InitializeDataStore();

        var store = new LinkedListStore<string>("context", defaultExpire: TimeSpan.FromMinutes(5), autoPing: false);
        var provider = DataStoreAccess.GetDefaultProvider();

        Assert.AreEqual(1, store.AddLast("value"));
        Assert.IsNull(provider.GetExpire(store.MasterKey));
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void KeyValueStore_Set_WithAutoPingFalse_ShouldNotApplyDefaultExpire()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("keyvalue-set-noautoping"));
        serviceProvider.InitializeDataStore();

        var store = new KeyValueStore(defaultExpire: TimeSpan.FromMinutes(5), autoPing: false);

        Assert.IsTrue(store.Set("mykey", "value"));
        Assert.IsNull(store.GetExpire("mykey"));
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void KeyValueStore_Increment_WithAutoPingFalse_ShouldNotApplyDefaultExpire()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("keyvalue-incr-noautoping"));
        serviceProvider.InitializeDataStore();

        var store = new KeyValueStore(defaultExpire: TimeSpan.FromMinutes(5), autoPing: false);

        Assert.AreEqual(1, store.Increment("counter"));
        Assert.IsNull(store.GetExpire("counter"));
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void KeyValueStore_Increment_WithAutoPingFalse_ExplicitPing_ShouldApplyDefaultExpire()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("keyvalue-incr-noautoping-ping"));
        serviceProvider.InitializeDataStore();

        var store = new KeyValueStore(defaultExpire: TimeSpan.FromMinutes(5), autoPing: false);

        Assert.AreEqual(1, store.Increment("counter"));
        Assert.IsNull(store.GetExpire("counter"));

        Assert.IsTrue(store.SetExpire("counter", store.DefaultExpire));
        Assert.IsTrue(store.GetExpire("counter") > TimeSpan.Zero);
    }

    // ──────────────────────────────────────────────────────────────
    // autoPing=true repeatedly refreshes TTL on subsequent writes
    // ──────────────────────────────────────────────────────────────

    [TestMethod]
    [TestCategory("Integration")]
    public void HashStore_MultipleWrites_WithAutoPingTrue_ShouldKeepRefreshingTTL()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("hash-expire-multiwrite"));
        serviceProvider.InitializeDataStore();

        var store = new HashStore("context", defaultExpire: TimeSpan.FromSeconds(30), autoPing: true);

        store.Set("f1", "v1");
        var ttl1 = store.GetExpire();

        Thread.Sleep(500);
        store.Set("f2", "v2");
        var ttl2 = store.GetExpire();

        // Both writes should have applied the TTL; second write resets it so ttl2 >= ttl1
        Assert.IsTrue(ttl1 > TimeSpan.Zero);
        Assert.IsTrue(ttl2 > TimeSpan.Zero);
        Assert.IsTrue(ttl2 >= ttl1 - TimeSpan.FromMilliseconds(600));
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void HashStore_MultipleWrites_WithAutoPingFalse_ShouldNotRefreshTTL()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("hash-expire-multiwrite-noautoping"));
        serviceProvider.InitializeDataStore();

        var store = new HashStore("context", defaultExpire: TimeSpan.FromSeconds(30), autoPing: false);

        store.Set("f1", "v1");
        Assert.IsNull(store.GetExpire());  // first write: no TTL

        store.Set("f2", "v2");
        Assert.IsNull(store.GetExpire());  // subsequent write: still no TTL

        store.Ping();
        Assert.IsTrue(store.GetExpire() > TimeSpan.Zero);  // explicit Ping applies TTL
    }
}
