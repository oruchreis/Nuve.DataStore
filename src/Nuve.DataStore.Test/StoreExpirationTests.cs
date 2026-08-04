using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Nuve.DataStore.Test;

/// <summary>
/// DefaultExpire always applies the initial TTL. AutoPing decides whether an operation may
/// extend an already expiring key: enabled means every read and write slides the TTL,
/// disabled means the key keeps expiring from its first write.
/// </summary>
[TestClass]
public class StoreExpirationTests
{
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan Elapse = TimeSpan.FromSeconds(2);

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
    // Initial TTL is applied regardless of autoPing
    // ──────────────────────────────────────────────────────────────

    [TestMethod]
    [TestCategory("Integration")]
    public void HashStore_Set_WithAutoPingFalse_ShouldApplyInitialTtl()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("hash-initial-ttl"));
        serviceProvider.InitializeDataStore();

        var store = new HashStore("context", defaultExpire: Ttl, autoPing: false);

        Assert.IsTrue(store.Set("field", "value"));
        Assert.IsTrue(store.GetExpire() > TimeSpan.Zero);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task HashStore_SetAsync_WithAutoPingFalse_ShouldApplyInitialTtl()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("hash-initial-ttl-async"));
        await serviceProvider.InitializeDataStoreAsync();

        var store = new HashStore("context", defaultExpire: Ttl, autoPing: false);

        Assert.IsTrue(await store.SetAsync("field", "value"));
        Assert.IsTrue(await store.GetExpireAsync() > TimeSpan.Zero);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void DictionaryStore_Set_WithAutoPingFalse_ShouldApplyInitialTtl()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("dictionary-initial-ttl"));
        serviceProvider.InitializeDataStore();

        var store = new DictionaryStore<string>("context", defaultExpire: Ttl, autoPing: false);

        Assert.IsTrue(store.Set("field", "value"));
        Assert.IsTrue(store.GetExpire() > TimeSpan.Zero);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void HashSetStore_Add_WithAutoPingFalse_ShouldApplyInitialTtl()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("hashset-initial-ttl"));
        serviceProvider.InitializeDataStore();

        var store = new HashSetStore<string>("context", defaultExpire: Ttl, autoPing: false);
        var provider = DataStoreAccess.GetDefaultProvider();

        Assert.AreEqual(1, store.Add("value"));
        Assert.IsTrue(provider.GetExpire(store.MasterKey) > TimeSpan.Zero);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void LinkedListStore_AddLast_WithAutoPingFalse_ShouldApplyInitialTtl()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("linkedlist-initial-ttl"));
        serviceProvider.InitializeDataStore();

        var store = new LinkedListStore<string>("context", defaultExpire: Ttl, autoPing: false);
        var provider = DataStoreAccess.GetDefaultProvider();

        Assert.AreEqual(1, store.AddLast("value"));
        Assert.IsTrue(provider.GetExpire(store.MasterKey) > TimeSpan.Zero);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void KeyValueStore_Set_WithAutoPingFalse_ShouldApplyInitialTtl()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("keyvalue-initial-ttl"));
        serviceProvider.InitializeDataStore();

        var store = new KeyValueStore(defaultExpire: Ttl, autoPing: false);

        Assert.IsTrue(store.Set("mykey", "value"));
        Assert.IsTrue(store.GetExpire("mykey") > TimeSpan.Zero);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void KeyValueStore_Increment_WithAutoPingFalse_ShouldApplyInitialTtl()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("keyvalue-incr-initial-ttl"));
        serviceProvider.InitializeDataStore();

        var store = new KeyValueStore(defaultExpire: Ttl, autoPing: false);

        Assert.AreEqual(1, store.Increment("counter"));
        Assert.IsTrue(store.GetExpire("counter") > TimeSpan.Zero);
    }

    // ──────────────────────────────────────────────────────────────
    // autoPing = false  →  nothing extends an existing TTL
    // ──────────────────────────────────────────────────────────────

    [TestMethod]
    [TestCategory("Integration")]
    public void HashStore_SecondSet_WithAutoPingFalse_ShouldNotExtendTtl()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("hash-no-extend"));
        serviceProvider.InitializeDataStore();

        var store = new HashStore("context", defaultExpire: Ttl, autoPing: false);

        store.Set("f1", "v1");
        var first = store.GetExpire();

        Thread.Sleep(Elapse);
        store.Set("f2", "v2");
        var second = store.GetExpire();

        Assert.IsTrue(second < first, $"TTL should keep decreasing; was {first} then {second}");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void LinkedListStore_SecondAddLast_WithAutoPingFalse_ShouldNotExtendTtl()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("linkedlist-no-extend"));
        serviceProvider.InitializeDataStore();

        var store = new LinkedListStore<string>("context", defaultExpire: Ttl, autoPing: false);
        var provider = DataStoreAccess.GetDefaultProvider();

        store.AddLast("v1");
        var first = provider.GetExpire(store.MasterKey);

        Thread.Sleep(Elapse);
        store.AddLast("v2");
        var second = provider.GetExpire(store.MasterKey);

        Assert.IsTrue(second < first, $"TTL should keep decreasing; was {first} then {second}");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void HashSetStore_Remove_WithAutoPingFalse_ShouldNotExtendTtl()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("hashset-remove-no-extend"));
        serviceProvider.InitializeDataStore();

        var store = new HashSetStore<string>("context", defaultExpire: Ttl, autoPing: false);
        var provider = DataStoreAccess.GetDefaultProvider();

        store.Add("v1", "v2");
        var first = provider.GetExpire(store.MasterKey);

        Thread.Sleep(Elapse);
        store.Remove("v1");
        var second = provider.GetExpire(store.MasterKey);

        Assert.IsTrue(second < first, $"TTL should keep decreasing; was {first} then {second}");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void KeyValueStore_SecondSet_WithAutoPingFalse_ShouldPreserveRemainingTtl()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("keyvalue-keep-ttl"));
        serviceProvider.InitializeDataStore();

        var store = new KeyValueStore(defaultExpire: Ttl, autoPing: false);

        store.Set("mykey", "first");
        var first = store.GetExpire("mykey");

        Thread.Sleep(Elapse);
        Assert.IsTrue(store.Set("mykey", "second"));
        var second = store.GetExpire("mykey");

        // A plain Redis SET would drop the TTL entirely; the remaining TTL must survive the write.
        Assert.IsNotNull(second);
        Assert.IsTrue(second < first, $"TTL should keep decreasing; was {first} then {second}");
        Assert.AreEqual("second", store.Get<string>("mykey"));
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void KeyValueStore_Set_WithoutOverwrite_WithAutoPingFalse_ShouldNotReplaceValue()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("keyvalue-no-overwrite"));
        serviceProvider.InitializeDataStore();

        var store = new KeyValueStore(defaultExpire: Ttl, autoPing: false);

        Assert.IsTrue(store.Set("mykey", "first"));
        Assert.IsFalse(store.Set("mykey", "second", overwrite: false));
        Assert.AreEqual("first", store.Get<string>("mykey"));
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void HashStore_Read_WithAutoPingFalse_ShouldNotExtendTtl()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("hash-read-no-extend"));
        serviceProvider.InitializeDataStore();

        var store = new HashStore("context", defaultExpire: Ttl, autoPing: false);

        store.Set("field", "value");
        var first = store.GetExpire();

        Thread.Sleep(Elapse);
        Assert.AreEqual("value", store.Get<string>("field"));
        var second = store.GetExpire();

        Assert.IsTrue(second < first, $"a read must not slide the TTL; was {first} then {second}");
    }

    // ──────────────────────────────────────────────────────────────
    // autoPing = true  →  writes slide the TTL
    // ──────────────────────────────────────────────────────────────

    [TestMethod]
    [TestCategory("Integration")]
    public void HashStore_SecondSet_WithAutoPingTrue_ShouldRefreshTtl()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("hash-refresh"));
        serviceProvider.InitializeDataStore();

        var store = new HashStore("context", defaultExpire: Ttl, autoPing: true);

        store.Set("f1", "v1");
        Thread.Sleep(Elapse);
        var beforeSecondWrite = store.GetExpire();

        store.Set("f2", "v2");
        var afterSecondWrite = store.GetExpire();

        Assert.IsTrue(afterSecondWrite > beforeSecondWrite,
            $"a write should slide the TTL back; was {beforeSecondWrite} then {afterSecondWrite}");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void KeyValueStore_SecondSet_WithAutoPingTrue_ShouldRefreshTtl()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("keyvalue-refresh"));
        serviceProvider.InitializeDataStore();

        var store = new KeyValueStore(defaultExpire: Ttl, autoPing: true);

        store.Set("mykey", "first");
        Thread.Sleep(Elapse);
        var beforeSecondWrite = store.GetExpire("mykey");

        store.Set("mykey", "second");
        var afterSecondWrite = store.GetExpire("mykey");

        Assert.IsTrue(afterSecondWrite > beforeSecondWrite,
            $"a write should slide the TTL back; was {beforeSecondWrite} then {afterSecondWrite}");
    }

    // ──────────────────────────────────────────────────────────────
    // Explicit Ping and the no-DefaultExpire case
    // ──────────────────────────────────────────────────────────────

    [TestMethod]
    [TestCategory("Integration")]
    public void HashStore_ExplicitPing_WithAutoPingFalse_ShouldRefreshTtl()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("hash-explicit-ping"));
        serviceProvider.InitializeDataStore();

        var store = new HashStore("context", defaultExpire: Ttl, autoPing: false);

        store.Set("field", "value");
        Thread.Sleep(Elapse);
        var beforePing = store.GetExpire();

        Assert.IsTrue(store.Ping());
        var afterPing = store.GetExpire();

        Assert.IsTrue(afterPing > beforePing,
            $"an explicit Ping is a deliberate refresh; was {beforePing} then {afterPing}");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void HashStore_Set_WithoutDefaultExpire_ShouldNotApplyTtl()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("hash-no-default-expire"));
        serviceProvider.InitializeDataStore();

        var store = new HashStore("context");

        Assert.IsTrue(store.Set("field", "value"));
        Assert.IsNull(store.GetExpire());
    }

    // ──────────────────────────────────────────────────────────────
    // Helper keys must stay on the same cluster slot as the key they accompany
    // ──────────────────────────────────────────────────────────────

    [TestMethod]
    [TestCategory("Integration")]
    public void HashSetStore_SetOperations_OverValues_ShouldStayOnOneSlot()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("hashset-slot"));
        serviceProvider.InitializeDataStore();

        var store = new HashSetStore<string>("context");

        // These operations stage the incoming values in a temporary key and then combine it with
        // MasterKey, which a sharded deployment only accepts when both keys share a hash tag.
        var tempKey = store.BuildCoLocatedKey(store.MasterKey, "__temp_probe");
        Assert.AreEqual($"{{{store.MasterKey}}}__temp_probe", tempKey);
        Assert.AreEqual(HashSlotOf(store.MasterKey), HashSlotOf(tempKey), "temp key must share the slot of MasterKey");

        // A key that already carries a tag keeps it instead of being wrapped again.
        var tagged = store.BuildCoLocatedKey("{orders}:items", "__temp_probe");
        Assert.AreEqual("{orders}:items__temp_probe", tagged);
        Assert.AreEqual(HashSlotOf("{orders}:items"), HashSlotOf(tagged));

        // Behaviour must be unchanged on a single node.
        store.Add("a", "b", "c");
        Assert.AreEqual(2, store.DifferenceWith("c"));
        Assert.IsTrue(store.Contains("a", "b"));
        Assert.IsFalse(store.Contains("c"));
    }

    /// <summary>
    /// The slot a key hashes to, following the Redis rule of using the content between the first
    /// '{' and the '}' that follows it when that content is not empty.
    /// </summary>
    private static int HashSlotOf(string key)
    {
        var start = key.IndexOf('{');
        if (start >= 0)
        {
            var end = key.IndexOf('}', start + 1);
            if (end > start + 1)
                key = key.Substring(start + 1, end - start - 1);
        }

        return Crc16(key) % 16384;
    }

    private static int Crc16(string value)
    {
        const ushort polynomial = 0x1021;
        ushort crc = 0;

        foreach (var b in System.Text.Encoding.ASCII.GetBytes(value))
        {
            crc ^= (ushort)(b << 8);
            for (var i = 0; i < 8; i++)
                crc = (ushort)((crc & 0x8000) != 0 ? (crc << 1) ^ polynomial : crc << 1);
        }

        return crc;
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void KeyValueStore_Set_WithoutDefaultExpire_ShouldNotApplyTtl()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("keyvalue-no-default-expire"));
        serviceProvider.InitializeDataStore();

        var store = new KeyValueStore();

        Assert.IsTrue(store.Set("mykey", "value"));
        Assert.IsNull(store.GetExpire("mykey"));
    }
}
