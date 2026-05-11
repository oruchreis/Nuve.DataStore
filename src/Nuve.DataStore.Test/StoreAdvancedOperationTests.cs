using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nuve.DataStore.Serializer.JsonNet;

namespace Nuve.DataStore.Test;

[TestClass]
public class StoreAdvancedOperationTests
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
    public void KeyValueStore_ShouldCover_Advanced_Sync_Operations()
    {
        var rootNamespace = Bootstrap.NewRootNamespace("kv-advanced-sync");
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(rootNamespace: rootNamespace);
        serviceProvider.InitializeDataStore();

        var store = new KeyValueStore(defaultExpire: TimeSpan.FromMinutes(5));

        var batch = new Dictionary<string, string?>
        {
            ["k1"] = "value-1",
            ["k2"] = "value-2"
        };

        Assert.IsTrue(store.Set(batch));
        var typedBatch = store.Get<string>("k1", "k2");
        CollectionAssert.AreEquivalent(new[] { "value-1", "value-2" }, typedBatch.Values.Cast<string>().ToArray());

        Assert.ThrowsException<KeyNotFoundException>(() => store.Get(new Dictionary<string, Type>
        {
            ["k1"] = typeof(string),
            ["k2"] = typeof(string)
        }));

        Assert.AreEqual("value-1", store.Exchange("k1", "value-1b"));
        Assert.AreEqual("value-1b", store.Get<string>("k1"));

        Assert.IsTrue(store.SetExpire("k1", TimeSpan.FromMinutes(3)));
        Assert.IsTrue(store.GetExpire("k1") > TimeSpan.Zero);
        Assert.IsTrue(store.Ping("k1"));

        Assert.IsTrue(store.Rename("k1", "k1-renamed"));
        Assert.IsFalse(store.Contains("k1"));
        Assert.IsTrue(store.Contains("k1-renamed"));

        Assert.AreEqual(5, store.Increment("counter", 5));
        Assert.AreEqual(3, store.Decrement("counter", 2));

        Assert.AreEqual(5, store.AppendString("text", "hello"));
        Assert.AreEqual("ell", store.SubString("text", 1, 3));
        Assert.AreEqual(5, store.OverwriteString("text", 0, "y"));
        Assert.ThrowsException<Newtonsoft.Json.JsonReaderException>(() => store.Get<string>("text"));
        Assert.IsTrue(store.SizeInBytes("text") > 0);

        var executed = false;
        store.Lock("sync-lock", TimeSpan.FromSeconds(5), () => executed = true, skipWhenTimeout: false, throwWhenTimeout: true);
        Assert.IsTrue(executed);

        using var acquiredLock = store.AcquireLock("manual-lock", new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token, throwWhenTimeout: true);
        Assert.IsNotNull(acquiredLock);
        Assert.IsTrue(acquiredLock.Release());

        Assert.IsTrue(store.Count($"{rootNamespace}:*") >= 3);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task KeyValueStore_ShouldCover_Advanced_Async_Operations()
    {
        var rootNamespace = Bootstrap.NewRootNamespace("kv-advanced-async");
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(rootNamespace: rootNamespace);
        await serviceProvider.InitializeDataStoreAsync();

        var store = new KeyValueStore(defaultExpire: TimeSpan.FromMinutes(5));

        var batch = new Dictionary<string, string?>
        {
            ["k1"] = "value-1",
            ["k2"] = "value-2"
        };

        Assert.IsTrue(await store.SetAsync(batch));
        var typedBatch = await store.GetAsync<string>("k1", "k2");
        CollectionAssert.AreEquivalent(new[] { "value-1", "value-2" }, typedBatch.Values.Cast<string>().ToArray());

        await Assert.ThrowsExceptionAsync<KeyNotFoundException>(async () => await store.GetAsync(new Dictionary<string, Type>
        {
            ["k1"] = typeof(string),
            ["k2"] = typeof(string)
        }));

        Assert.AreEqual("value-1", await store.ExchangeAsync("k1", "value-1b"));
        Assert.AreEqual("value-1b", await store.GetAsync<string>("k1"));

        Assert.IsTrue(await store.SetExpireAsync("k1", TimeSpan.FromMinutes(3)));
        Assert.IsTrue(await store.GetExpireAsync("k1") > TimeSpan.Zero);
        Assert.IsTrue(await store.PingAsync("k1"));

        Assert.IsTrue(await store.RenameAsync("k1", "k1-renamed"));
        Assert.IsFalse(await store.ContainsAsync("k1"));
        Assert.IsTrue(await store.ContainsAsync("k1-renamed"));

        Assert.AreEqual(5, await store.IncrementAsync("counter", 5));
        Assert.AreEqual(3, await store.DecrementAsync("counter", 2));

        Assert.AreEqual(5, await store.AppendStringAsync("text", "hello"));
        Assert.AreEqual("ell", await store.SubStringAsync("text", 1, 3));
        Assert.AreEqual(5, await store.OverwriteStringAsync("text", 0, "y"));
        await Assert.ThrowsExceptionAsync<Newtonsoft.Json.JsonReaderException>(async () => await store.GetAsync<string>("text"));
        Assert.IsTrue(await store.SizeInBytesAsync("text") > 0);

        var executed = false;
        await store.LockAsync("sync-lock", TimeSpan.FromSeconds(5), () =>
        {
            executed = true;
            return Task.CompletedTask;
        }, skipWhenTimeout: false, throwWhenTimeout: true);
        Assert.IsTrue(executed);

        var acquiredLock = await store.AcquireLockAsync("manual-lock", new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token, throwWhenTimeout: true);
        Assert.IsNotNull(acquiredLock);
        Assert.IsTrue(await acquiredLock.ReleaseAsync());

        Assert.IsTrue(await store.CountAsync($"{rootNamespace}:*") >= 3);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void DictionaryStore_ShouldCover_Advanced_Sync_Operations()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("dictionary-advanced-sync"));
        serviceProvider.InitializeDataStore();

        var store = new DictionaryStore<string>("users", defaultExpire: TimeSpan.FromMinutes(5));

        store.Set(new Dictionary<string, string?>
        {
            ["u1"] = "john",
            ["u2"] = "mary"
        });

        Assert.IsTrue(store.IsExists());
        Assert.IsTrue(store.Contains("u1", "john"));
        CollectionAssert.AreEquivalent(new[] { "u1", "u2" }, store.Keys().ToArray());
        CollectionAssert.AreEquivalent(new[] { "john", "mary" }, store.Values().Cast<string>().ToArray());
        CollectionAssert.AreEquivalent(new Dictionary<string, string?>(store.ToDictionary()).Values.Cast<string>().ToArray(), new[] { "john", "mary" });
        CollectionAssert.AreEquivalent(new Dictionary<string, string?>(store.Get("u1", "u2")).Values.Cast<string>().ToArray(), new[] { "john", "mary" });

        var copy = new KeyValuePair<string, string?>[2];
        store.CopyTo(copy, 0);
        Assert.AreEqual(2, copy.Length);

        Assert.AreEqual(2, store.Increment("counter", 2));
        Assert.IsTrue(store.SizeInBytes("u1") > 0);
        Assert.IsTrue(store.Ping());
        Assert.IsTrue(store.GetExpire() > TimeSpan.Zero);

        var executed = false;
        store.Lock("dictionary-lock", TimeSpan.FromSeconds(5), () => executed = true, skipWhenTimeout: false, throwWhenTimeout: true);
        Assert.IsTrue(executed);

        using var acquiredLock = store.AcquireLock("dictionary-manual-lock", new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token, throwWhenTimeout: true);
        Assert.IsNotNull(acquiredLock);
        acquiredLock.Dispose();
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task DictionaryStore_ShouldCover_Advanced_Async_Operations()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("dictionary-advanced-async"));
        await serviceProvider.InitializeDataStoreAsync();

        var store = new DictionaryStore<string>("users", defaultExpire: TimeSpan.FromMinutes(5));

        await store.SetAsync(new Dictionary<string, string?>
        {
            ["u1"] = "john",
            ["u2"] = "mary"
        });

        Assert.IsTrue(await store.IsExistsAsync());
        Assert.IsTrue(await store.ContainsAsync("u1", "john"));
        CollectionAssert.AreEquivalent(new[] { "u1", "u2" }, (await store.KeysAsync()).ToArray());
        CollectionAssert.AreEquivalent(new[] { "john", "mary" }, (await store.ValuesAsync()).Cast<string>().ToArray());
        CollectionAssert.AreEquivalent(new Dictionary<string, string?>(await store.ToDictionaryAsync()).Values.Cast<string>().ToArray(), new[] { "john", "mary" });
        CollectionAssert.AreEquivalent(new Dictionary<string, string?>(await store.GetAsync("u1", "u2")).Values.Cast<string>().ToArray(), new[] { "john", "mary" });

        var copy = new KeyValuePair<string, string?>[2];
        await store.CopyToAsync(copy, 0);
        Assert.AreEqual(2, copy.Length);

        Assert.AreEqual(2, await store.IncrementAsync("counter", 2));
        Assert.IsTrue(await store.SizeInBytesAsync("u1") > 0);
        Assert.IsTrue(await store.PingAsync());
        Assert.IsTrue(await store.GetExpireAsync() > TimeSpan.Zero);

        var executed = false;
        await store.LockAsync("dictionary-lock", TimeSpan.FromSeconds(5), () =>
        {
            executed = true;
            return Task.CompletedTask;
        }, skipWhenTimeout: false, throwWhenTimeout: true);
        Assert.IsTrue(executed);

        var acquiredLock = await store.AcquireLockAsync("dictionary-manual-lock", new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token, throwWhenTimeout: true);
        Assert.IsNotNull(acquiredLock);
#if !NET48
        await acquiredLock.DisposeAsync();
#else
        acquiredLock.Dispose();
#endif
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void HashStore_ShouldCover_Advanced_Sync_Operations()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("hash-advanced-sync"));
        serviceProvider.InitializeDataStore();

        var store = new HashStore("users", defaultExpire: TimeSpan.FromMinutes(5));
        store.Set(new Dictionary<string, object?>
        {
            ["name"] = "john",
            ["age"] = 30
        });

        Assert.IsTrue(store.Contains("name", "john"));
        CollectionAssert.AreEquivalent(new[] { "name", "age" }, store.Keys().ToArray());
        CollectionAssert.AreEquivalent(new[] { "john", "30" }, store.Values<object>().Select(x => x?.ToString()).ToArray());
        Assert.AreEqual(2, store.Count());

        var typedBatch = store.Get(new Dictionary<string, Type>
        {
            ["name"] = typeof(string),
            ["age"] = typeof(int)
        });
        Assert.AreEqual("john", typedBatch["name"]);
        Assert.AreEqual(30, typedBatch["age"]);

        Assert.AreEqual(2, store.Increment("counter", 2));
        Assert.IsTrue(store.SizeInBytes("name") > 0);

        var executed = false;
        store.Lock("hash-lock", TimeSpan.FromSeconds(5), () => executed = true, skipWhenTimeout: false, throwWhenTimeout: true);
        Assert.IsTrue(executed);

        using var acquiredLock = store.AcquireLock("hash-manual-lock", new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token, throwWhenTimeout: true);
        Assert.IsNotNull(acquiredLock);
        acquiredLock.Dispose();
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task HashStore_ShouldCover_Advanced_Async_Operations()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("hash-advanced-async"));
        await serviceProvider.InitializeDataStoreAsync();

        var store = new HashStore("users", defaultExpire: TimeSpan.FromMinutes(5));
        await store.SetAsync(new Dictionary<string, object?>
        {
            ["name"] = "john",
            ["age"] = 30
        });

        Assert.IsTrue(await store.ContainsAsync("name", "john"));
        CollectionAssert.AreEquivalent(new[] { "name", "age" }, (await store.KeysAsync()).ToArray());
        CollectionAssert.AreEquivalent(new[] { "john", "30" }, (await store.ValuesAsync<object>()).Select(x => x?.ToString()).ToArray());
        Assert.AreEqual(2, await store.CountAsync());

        var typedBatch = await store.GetAsync(new Dictionary<string, Type>
        {
            ["name"] = typeof(string),
            ["age"] = typeof(int)
        });
        Assert.AreEqual("john", typedBatch["name"]);
        Assert.AreEqual(30, typedBatch["age"]);

        Assert.AreEqual(2, await store.IncrementAsync("counter", 2));
        Assert.IsTrue(await store.SizeInBytesAsync("name") > 0);

        var executed = false;
        await store.LockAsync("hash-lock", TimeSpan.FromSeconds(5), () =>
        {
            executed = true;
            return Task.CompletedTask;
        }, skipWhenTimeout: false, throwWhenTimeout: true);
        Assert.IsTrue(executed);

        var acquiredLock = await store.AcquireLockAsync("hash-manual-lock", new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token, throwWhenTimeout: true);
        Assert.IsNotNull(acquiredLock);
#if !NET48
        await acquiredLock.DisposeAsync();
#else
        acquiredLock.Dispose();
#endif
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void HashSetStore_ShouldCover_Advanced_Sync_Operations()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("hashset-advanced-sync"));
        serviceProvider.InitializeDataStore();

        var left = new HashSetStore<string>("left");
        var right = new HashSetStore<string>("right");
        var moved = new HashSetStore<string>("left-moved");
        left.Add("a", "b", "c");
        right.Add("b", "c", "d");

        CollectionAssert.AreEquivalent(new[] { "a" }, left.DifferenceToHashSet(right.MasterKey).ToArray());
        CollectionAssert.AreEquivalent(new[] { "b", "c" }, left.IntersectToHashSet(right.MasterKey).ToArray());
        CollectionAssert.AreEquivalent(new[] { "a", "b", "c", "d" }, left.UnionToHashSet(right.MasterKey).ToArray());

        Assert.IsTrue(left.Overlaps(right.MasterKey));
        Assert.AreEqual(2, left.IntersectWith("b", "c"));
        Assert.AreEqual(2, left.Count());
        Assert.AreEqual(3, left.UnionToNewSet("left-union", right.MasterKey));
        left.MoveValue(moved.MasterKey, "a");
        Assert.AreEqual(1, left.DifferenceWith("b"));
        Assert.AreEqual(1, left.SymmetricDifferenceWith("c", "e"));

        var lockExecuted = false;
        left.Lock("hashset-lock", TimeSpan.FromSeconds(5), () => lockExecuted = true, skipWhenTimeout: false, throwWhenTimeout: true);
        Assert.IsTrue(lockExecuted);

        using var acquiredLock = left.AcquireLock("hashset-manual-lock", new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token, throwWhenTimeout: true);
        Assert.IsNotNull(acquiredLock);
        Assert.IsTrue(acquiredLock.Release());
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task HashSetStore_ShouldCover_Advanced_Async_Operations()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("hashset-advanced-async"));
        await serviceProvider.InitializeDataStoreAsync();

        var left = new HashSetStore<string>("left");
        var right = new HashSetStore<string>("right");
        var moved = new HashSetStore<string>("left-moved");
        await left.AddAsync("a", "b", "c");
        await right.AddAsync("b", "c", "d");

        CollectionAssert.AreEquivalent(new[] { "a" }, (await left.DifferenceToHashSetAsync(right.MasterKey)).ToArray());
        CollectionAssert.AreEquivalent(new[] { "b", "c" }, (await left.IntersectToHashSetAsync(right.MasterKey)).ToArray());
        CollectionAssert.AreEquivalent(new[] { "a", "b", "c", "d" }, (await left.UnionToHashSetAsync(right.MasterKey)).ToArray());

        Assert.IsTrue(await left.OverlapsAsync(right.MasterKey));
        Assert.AreEqual(2, await left.IntersectWithAsync("b", "c"));
        Assert.AreEqual(2, await left.CountAsync());
        Assert.AreEqual(3, await left.UnionToNewSetAsync("left-union", right.MasterKey));
        await left.MoveValueAsync(moved.MasterKey, "a");
        Assert.AreEqual(1, await left.DifferenceWithAsync("b"));
        Assert.AreEqual(1, await left.SymmetricDifferenceWithAsync("c", "e"));

        var lockExecuted = false;
        await left.LockAsync("hashset-lock", TimeSpan.FromSeconds(5), () =>
        {
            lockExecuted = true;
            return Task.CompletedTask;
        }, skipWhenTimeout: false, throwWhenTimeout: true);
        Assert.IsTrue(lockExecuted);

        var acquiredLock = await left.AcquireLockAsync("hashset-manual-lock", new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token, throwWhenTimeout: true);
        Assert.IsNotNull(acquiredLock);
        Assert.IsTrue(await acquiredLock.ReleaseAsync());
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void LinkedListStore_ShouldCover_Advanced_Sync_Operations()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("linkedlist-advanced-sync"));
        serviceProvider.InitializeDataStore();

        var store = new LinkedListStore<string>("queue");
        store.AddLast("b");
        store.AddFirst("a");
        store.AddAfter("a", "a2");
        store.AddBefore("b", "a3");

        CollectionAssert.AreEqual(new[] { "a", "a2", "a3", "b" }, store.GetRange(0, -1).ToArray());
        store.Set(1, "x");
        Assert.AreEqual("x", store.Get(1));
        Assert.AreEqual(4, store.Count());
        Assert.IsTrue(store.Insert(2, "y"));
        Assert.IsTrue(store.RemoveAt(2));
        Assert.AreEqual(1, store.Remove("a3"));
        store.Trim(0, 1);
        CollectionAssert.AreEqual(new[] { "a", "x" }, store.GetRange(0, -1).ToArray());
        Assert.IsTrue(store.Contains("x"));

        var copy = new string?[2];
        store.CopyTo(copy, 0);
        CollectionAssert.AreEqual(new[] { "a", "x" }, copy);

        var lockExecuted = false;
        store.Lock("linkedlist-lock", TimeSpan.FromSeconds(5), () => lockExecuted = true, skipWhenTimeout: false, throwWhenTimeout: true);
        Assert.IsTrue(lockExecuted);

        using var acquiredLock = store.AcquireLock("linkedlist-manual-lock", new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token, throwWhenTimeout: true);
        Assert.IsNotNull(acquiredLock);
        Assert.IsTrue(acquiredLock.Release());
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task LinkedListStore_ShouldCover_Advanced_Async_Operations()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("linkedlist-advanced-async"));
        await serviceProvider.InitializeDataStoreAsync();

        var store = new LinkedListStore<string>("queue");
        await store.AddLastAsync("b");
        await store.AddFirstAsync("a");
        await store.AddAfterAsync("a", "a2");
        await store.AddBeforeAsync("b", "a3");

        CollectionAssert.AreEqual(new[] { "a", "a2", "a3", "b" }, (await store.GetRangeAsync(0, -1)).ToArray());
        await store.SetAsync(1, "x");
        Assert.AreEqual("x", await store.GetAsync(1));
        Assert.AreEqual(4, await store.CountAsync());
        Assert.IsTrue(await store.InsertAsync(2, "y"));
        Assert.IsTrue(await store.RemoveAtAsync(2));
        Assert.AreEqual(1, await store.RemoveAsync("a3"));
        await store.TrimAsync(0, 1);
        CollectionAssert.AreEqual(new[] { "a", "x" }, (await store.GetRangeAsync(0, -1)).ToArray());
        Assert.IsTrue(store.Contains("x"));

        var lockExecuted = false;
        await store.LockAsync("linkedlist-lock", TimeSpan.FromSeconds(5), () =>
        {
            lockExecuted = true;
            return Task.CompletedTask;
        }, skipWhenTimeout: false, throwWhenTimeout: true);
        Assert.IsTrue(lockExecuted);

        var acquiredLock = await store.AcquireLockAsync("linkedlist-manual-lock", new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token, throwWhenTimeout: true);
        Assert.IsNotNull(acquiredLock);
        Assert.IsTrue(await acquiredLock.ReleaseAsync());
    }

    [TestMethod]
    public void UtilityTypes_ShouldCover_Core_Behavior()
    {
        var compressor = new DeflateCompressor();
        var original = Encoding.UTF8.GetBytes("compress-me");
        using var compressedStream = new MemoryStream();
        compressor.Compress(compressedStream, original);
        using var decompressedStream = new MemoryStream();
        compressor.Decompress(decompressedStream, compressedStream.ToArray());
        CollectionAssert.AreEqual(original, decompressedStream.ToArray());
        CollectionAssert.AreEqual(Encoding.ASCII.GetBytes("__Compressed_D__"), compressor.Signature);

        var serializer = new JsonNetDataStoreSerializer();
        var payload = new UtilityPayload { Id = 7, Name = "seven" };
        var serialized = serializer.Serialize(payload);
        var deserialized = serializer.Deserialize<UtilityPayload>(serialized);
        Assert.IsNotNull(deserialized);
        Assert.AreEqual(payload.Id, deserialized.Id);
        Assert.AreEqual(payload.Name, deserialized.Name);

        var defaultSerializer = new DefaultSerializer();
        Assert.ThrowsException<System.Reflection.TargetParameterCountException>(() => defaultSerializer.Serialize(payload, typeof(UtilityPayload)));

        var profilerA = new ProfilerContext("g", "l");
        var profilerB = new ProfilerContext("g", "l");
        Assert.AreEqual(profilerA, profilerB);
        Assert.IsTrue(profilerA == profilerB);
        Assert.IsFalse(profilerA != profilerB);
        Assert.AreEqual(profilerA.GetHashCode(), profilerB.GetHashCode());
    }

    private sealed class UtilityPayload
    {
        public int Id { get; set; }

        public string? Name { get; set; }
    }
}
