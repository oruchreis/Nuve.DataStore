using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nuve.DataStore.Redis;
using StackExchange.Redis;

namespace Nuve.DataStore.Test;

[TestClass]
public class RedisStoreProviderTests
{
    private ServiceProvider _serviceProvider = default!;
    private IDataStoreProvider _provider = default!;
    private static readonly TimeSpan AssertionPollInterval = TimeSpan.FromMilliseconds(100);

    [TestInitialize]
    public void TestInitialize()
    {
        DataStoreRuntime.ResetForTests();

        _serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("provider"));

        _serviceProvider.InitializeDataStore();

        var context = DataStoreRuntime.Manager.GetConnection("redis");
        _provider = context.Provider;
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _serviceProvider.Dispose();
        DataStoreRuntime.ResetForTests();
    }

    private static bool WaitUntil(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
                return true;

            Thread.Sleep(AssertionPollInterval);
        }

        return condition();
    }

    private static string[] FindKeys(IConnectionMultiplexer multiplexer, string pattern)
    {
        var endpoint = multiplexer.GetEndPoints().First();
        var server = multiplexer.GetServer(endpoint);
        return server.Keys(pattern: pattern).Select(item => (string)item).ToArray();
    }

    [TestMethod]
    public void LockSlidingExpiration()
    {
        var slidingExpire = TimeSpan.FromSeconds(6);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        var lockItem = _provider.AcquireLock(
            "test-lock",
            throwWhenTimeout: true,
            slidingExpire: slidingExpire,
            waitCancelToken: cts.Token)!;

        try
        {
            Assert.IsNotNull(lockItem.LockAchieved);
            Console.WriteLine("{0:hh:mm:ss.fff}\tlock achieved: {1:hh:mm:ss.fff}", DateTimeOffset.UtcNow, lockItem.LockAchieved);

            var ttl = _provider.GetExpire("test-lock");
            Console.WriteLine("lock-ttl at start: {0}", ttl!.Value.TotalMilliseconds);

            Assert.IsTrue(ttl.Value.TotalMilliseconds > (slidingExpire.TotalMilliseconds / 2));

            for (int i = 0; i < 3; i++)
            {
                Console.WriteLine("Test iteration: {0}", i);

                Thread.Sleep(
                    Math.Max(
                        (int)ttl.Value.TotalMilliseconds
                        - ((int)slidingExpire.TotalMilliseconds / 2)
                        + RedisDataStoreLock.CheckSlidingExpirationMs
                        + 1000,
                        1));

                Console.WriteLine(
                    "{0:hh:mm:ss.fff}\tlock achieved after halflife: {1:hh:mm:ss.fff}",
                    DateTimeOffset.UtcNow,
                    lockItem.LockAchieved);

                ttl = _provider.GetExpire("test-lock");
                Console.WriteLine("lock-ttl after after halflife: {0}", ttl!.Value.TotalMilliseconds);

                Assert.IsTrue(ttl.Value.TotalMilliseconds > (slidingExpire.TotalMilliseconds / 2));
            }
        }
        finally
        {
            lockItem.Dispose();
        }

        Assert.IsFalse(((IKeyValueStoreProvider)_provider).Contains("test-lock"));
    }

    [TestMethod]
    public void Count()
    {
        var keyValueProvider = (IKeyValueStoreProvider)_provider;

        keyValueProvider.Set("Test:1", [], true);
        keyValueProvider.Set("Test:2", [], true);
        keyValueProvider.Set("Test:3", [], true);
        keyValueProvider.Set("Test:4", [], true);

        Assert.AreEqual(4, keyValueProvider.Count("Test:*"));
    }

    [TestMethod]
    public async Task GetKeyType_ShouldDetect_RedisDataTypes()
    {
        var rootNamespace = DataStoreRuntime.Manager.GetConnection("redis").RootNamespace;
        var keyValueStore = new KeyValueStore();
        var dictionaryStore = new DictionaryStore<string>("typed-dictionary");
        var listStore = new LinkedListStore<string>("typed-list");
        var setStore = new HashSetStore<string>("typed-set");

        keyValueStore.Set("typed-kv", "value");
        dictionaryStore.Set("field", "value");
        listStore.AddLast("value");
        setStore.Add("value");

        using var mux = await ConnectionMultiplexer.ConnectAsync(RedisTestHelpers.GetRedisConnectionString());
        await mux.GetDatabase().SortedSetAddAsync($"{rootNamespace}:typed-sorted", "member", 1);

        Assert.AreEqual(StoreKeyType.KeyValue, _provider.GetKeyType($"{rootNamespace}:typed-kv"));
        Assert.AreEqual(StoreKeyType.Dictionary, _provider.GetKeyType(dictionaryStore.MasterKey));
        Assert.AreEqual(StoreKeyType.LinkedList, _provider.GetKeyType(listStore.MasterKey));
        Assert.AreEqual(StoreKeyType.HashSet, _provider.GetKeyType(setStore.MasterKey));
        Assert.AreEqual(StoreKeyType.SortedSet, _provider.GetKeyType($"{rootNamespace}:typed-sorted"));

        Assert.AreEqual(StoreKeyType.KeyValue, await _provider.GetKeyTypeAsync($"{rootNamespace}:typed-kv"));
        Assert.AreEqual(StoreKeyType.Dictionary, await _provider.GetKeyTypeAsync(dictionaryStore.MasterKey));
        Assert.AreEqual(StoreKeyType.LinkedList, await _provider.GetKeyTypeAsync(listStore.MasterKey));
        Assert.AreEqual(StoreKeyType.HashSet, await _provider.GetKeyTypeAsync(setStore.MasterKey));
        Assert.AreEqual(StoreKeyType.SortedSet, await _provider.GetKeyTypeAsync($"{rootNamespace}:typed-sorted"));
    }

    [TestMethod]
    public async Task AcquireLock_ShouldExpose_FencingToken_Ttl_Extend_And_Release()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        var lockItem = _provider.AcquireLock(
            "advanced-lock",
            throwWhenTimeout: true,
            slidingExpire: TimeSpan.FromSeconds(4),
            waitCancelToken: cts.Token)!;

        Assert.IsNotNull(lockItem);
        Assert.IsNotNull(lockItem.OwnerToken);
        Assert.IsTrue(lockItem.FencingToken > 0);
        Assert.IsNotNull(lockItem.LockAchieved);
        Assert.IsTrue((await lockItem.GetTtlAsync()) > TimeSpan.Zero);
        Assert.IsTrue(lockItem.Extend(TimeSpan.FromSeconds(5)));
        Assert.IsTrue(await lockItem.ExtendAsync(TimeSpan.FromSeconds(5)));
        Assert.IsTrue(await lockItem.ReleaseAsync());
        Assert.IsFalse(await lockItem.ReleaseAsync());
    }

    [TestMethod]
    public void KeyValueStore_AcquireLock_ShouldUseRootNamespace_AndExpireFencingKey()
    {
        var rootNamespace = DataStoreRuntime.Manager.GetConnection("redis").RootNamespace;
        var store = new KeyValueStore(defaultExpire: TimeSpan.FromSeconds(10));
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        using var mux = ConnectionMultiplexer.Connect(RedisTestHelpers.GetRedisConnectionString());

        using var lockItem = store.AcquireLock(
            "sync-lock-path",
            throwWhenTimeout: true,
            slidingExpire: TimeSpan.FromSeconds(4),
            waitCancelToken: cts.Token)!;

        var namespacedKey = $"{rootNamespace}:sync-lock-path";
        var fencingPattern = $"{namespacedKey}:*__fencing__:*sync-lock-path";

        Assert.IsTrue(WaitUntil(() => _provider.GetExpire(namespacedKey) > TimeSpan.Zero, TimeSpan.FromSeconds(1)));
        Assert.IsNull(_provider.GetExpire("sync-lock-path"));
        Assert.IsTrue(lockItem.FencingToken > 0);
        Assert.IsTrue(WaitUntil(() => FindKeys(mux, fencingPattern).Length == 1, TimeSpan.FromSeconds(5)));

        var fencingKey = FindKeys(mux, fencingPattern).Single();
        Assert.IsTrue(_provider.GetExpire(fencingKey) > TimeSpan.Zero);

        Assert.IsTrue(WaitUntil(() => FindKeys(mux, fencingPattern).Length == 0, TimeSpan.FromSeconds(14)));
    }

    [TestMethod]
    public void HashStore_AcquireLock_ShouldUseMasterKeyScopedFencingCounters_AndAvoidSharedLeak()
    {
        var rootNamespace = DataStoreRuntime.Manager.GetConnection("redis").RootNamespace;
        var localProvider = DataStoreRuntime.Manager.GetConnection("redis").Provider;

        var store1 = new HashStore("context-1", defaultExpire: TimeSpan.FromSeconds(10));
        var store2 = new HashStore("context-2", defaultExpire: TimeSpan.FromSeconds(10));
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        using var mux = ConnectionMultiplexer.Connect(RedisTestHelpers.GetRedisConnectionString());

        store1.Set("seed", "value-1");
        store2.Set("seed", "value-2");

        long firstToken;
        using (var firstLock = store1.AcquireLock("Hotel:ClientSearchRequest_locker", cts.Token, throwWhenTimeout: true)!)
        {
            firstToken = firstLock.FencingToken;
        }

        using var secondLock = store2.AcquireLock("Hotel:ClientSearchRequest_locker", cts.Token, throwWhenTimeout: true)!;
        var secondToken = secondLock.FencingToken;
        var fencingPattern1 = $"{store1.MasterKey}:*__fencing__:*Hotel:ClientSearchRequest_locker";
        var fencingPattern2 = $"{store2.MasterKey}:*__fencing__:*Hotel:ClientSearchRequest_locker";

        Assert.IsTrue(firstToken > 0);
        Assert.AreEqual(firstToken, secondToken);
        Assert.IsTrue(WaitUntil(() => FindKeys(mux, fencingPattern1).Length == 1, TimeSpan.FromSeconds(5)));
        Assert.IsTrue(WaitUntil(() => FindKeys(mux, fencingPattern2).Length == 1, TimeSpan.FromSeconds(5)));

        var fencingKey1 = FindKeys(mux, fencingPattern1).Single();
        var fencingKey2 = FindKeys(mux, fencingPattern2).Single();

        Assert.IsTrue(localProvider.GetExpire(fencingKey1) > TimeSpan.Zero);
        Assert.IsTrue(localProvider.GetExpire(fencingKey2) > TimeSpan.Zero);
        Assert.AreEqual(0, FindKeys(mux, $"{rootNamespace}:__fencing__:HashStore:Hotel:ClientSearchRequest_locker").Length);

        Assert.IsTrue(WaitUntil(() => FindKeys(mux, fencingPattern1).Length == 0, TimeSpan.FromSeconds(14)));
        Assert.IsTrue(WaitUntil(() => FindKeys(mux, fencingPattern2).Length == 0, TimeSpan.FromSeconds(14)));
    }

    [TestMethod]
    public void HashStore_Ping_ShouldExtendTrackedFencingKeyLifetime()
    {
        var store = new HashStore("context-ping", defaultExpire: TimeSpan.FromSeconds(10));
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        using var mux = ConnectionMultiplexer.Connect(RedisTestHelpers.GetRedisConnectionString());

        store.Set("seed", "value");

        using (var lockItem = store.AcquireLock("Hotel:ClientSearchRequest_locker", cts.Token, throwWhenTimeout: true)!)
        {
            Assert.IsTrue(lockItem.FencingToken > 0);
        }

        var fencingPattern = $"{store.MasterKey}:*__fencing__:*Hotel:ClientSearchRequest_locker";
        Assert.IsTrue(WaitUntil(() => FindKeys(mux, fencingPattern).Length == 1, TimeSpan.FromSeconds(5)));
        var fencingKey = FindKeys(mux, fencingPattern).Single();
        var ttlBeforeWait = _provider.GetExpire(fencingKey);
        Assert.IsTrue(ttlBeforeWait > TimeSpan.Zero);

        Thread.Sleep(TimeSpan.FromMilliseconds(1200));

        var ttlAfterWait = _provider.GetExpire(fencingKey);
        Assert.IsTrue(ttlAfterWait > TimeSpan.Zero);
        Assert.IsTrue(store.Ping());

        var ttlAfterPing = _provider.GetExpire(fencingKey);
        Assert.IsTrue(ttlAfterPing >= ttlAfterWait);
        Assert.IsTrue(ttlAfterPing > TimeSpan.FromSeconds(1));
    }

    [TestMethod]
    public void DictionaryHashSetAndLinkedListLocks_ShouldUseStoreScopedExpiringFencingKeys()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        using var mux = ConnectionMultiplexer.Connect(RedisTestHelpers.GetRedisConnectionString());

        var dictionaryStore = new DictionaryStore<string>("dictionary-context", defaultExpire: TimeSpan.FromSeconds(10));
        dictionaryStore.Set("seed", "value");
        using (var lockItem = dictionaryStore.AcquireLock("common-lock", cts.Token, throwWhenTimeout: true)!)
        {
            Assert.IsTrue(lockItem.FencingToken > 0);
        }

        var hashSetStore = new HashSetStore<string>("hashset-context", defaultExpire: TimeSpan.FromSeconds(10));
        hashSetStore.Add("seed");
        using (var lockItem = hashSetStore.AcquireLock("common-lock", cts.Token, throwWhenTimeout: true)!)
        {
            Assert.IsTrue(lockItem.FencingToken > 0);
        }

        var linkedListStore = new LinkedListStore<string>("linkedlist-context", defaultExpire: TimeSpan.FromSeconds(10));
        linkedListStore.AddLast("seed");
        using (var lockItem = linkedListStore.AcquireLock("common-lock", cts.Token, throwWhenTimeout: true)!)
        {
            Assert.IsTrue(lockItem.FencingToken > 0);
        }

        var dictionaryPattern = $"{dictionaryStore.MasterKey}:*__fencing__:*common-lock";
        var hashSetPattern = $"{hashSetStore.MasterKey}:*__fencing__:*common-lock";
        var linkedListPattern = $"{linkedListStore.MasterKey}:*__fencing__:*common-lock";

        Assert.IsTrue(WaitUntil(() => FindKeys(mux, dictionaryPattern).Length == 1, TimeSpan.FromSeconds(5)));
        Assert.IsTrue(WaitUntil(() => FindKeys(mux, hashSetPattern).Length == 1, TimeSpan.FromSeconds(5)));
        Assert.IsTrue(WaitUntil(() => FindKeys(mux, linkedListPattern).Length == 1, TimeSpan.FromSeconds(5)));

        Assert.IsTrue(_provider.GetExpire(FindKeys(mux, dictionaryPattern).Single()) > TimeSpan.Zero);
        Assert.IsTrue(_provider.GetExpire(FindKeys(mux, hashSetPattern).Single()) > TimeSpan.Zero);
        Assert.IsTrue(_provider.GetExpire(FindKeys(mux, linkedListPattern).Single()) > TimeSpan.Zero);

        Assert.IsTrue(WaitUntil(() => FindKeys(mux, dictionaryPattern).Length == 0, TimeSpan.FromSeconds(14)));
        Assert.IsTrue(WaitUntil(() => FindKeys(mux, hashSetPattern).Length == 0, TimeSpan.FromSeconds(14)));
        Assert.IsTrue(WaitUntil(() => FindKeys(mux, linkedListPattern).Length == 0, TimeSpan.FromSeconds(14)));
    }

    [TestMethod]
    public async Task Lock_WhenTimeout_ShouldRespectSkipBehavior()
    {
        using var ownerCts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        using var owner = _provider.AcquireLock(
            "skip-lock",
            throwWhenTimeout: true,
            slidingExpire: TimeSpan.FromSeconds(4),
            waitCancelToken: ownerCts.Token)!;

        var executedSync = false;
        _provider.Lock(
            "skip-lock",
            waitTimeout: TimeSpan.FromMilliseconds(200),
            action: () => executedSync = true,
            slidingExpire: TimeSpan.FromSeconds(4),
            skipWhenTimeout: true,
            throwWhenTimeout: false);
        Assert.IsFalse(executedSync);

        _provider.Lock(
            "skip-lock",
            waitTimeout: TimeSpan.FromMilliseconds(200),
            action: () => executedSync = true,
            slidingExpire: TimeSpan.FromSeconds(4),
            skipWhenTimeout: false,
            throwWhenTimeout: false);
        Assert.IsTrue(executedSync);

        var executedAsync = false;
        await _provider.LockAsync(
            "skip-lock",
            waitTimeout: TimeSpan.FromMilliseconds(200),
            action: () =>
            {
                executedAsync = true;
                return Task.CompletedTask;
            },
            slidingExpire: TimeSpan.FromSeconds(4),
            skipWhenTimeout: true,
            throwWhenTimeout: false);
        Assert.IsFalse(executedAsync);

        await _provider.LockAsync(
            "skip-lock",
            waitTimeout: TimeSpan.FromMilliseconds(200),
            action: () =>
            {
                executedAsync = true;
                return Task.CompletedTask;
            },
            slidingExpire: TimeSpan.FromSeconds(4),
            skipWhenTimeout: false,
            throwWhenTimeout: false);
        Assert.IsTrue(executedAsync);
    }
}
