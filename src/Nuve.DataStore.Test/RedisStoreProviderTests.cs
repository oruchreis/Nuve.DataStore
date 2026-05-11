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
    public void KeyValueStore_AcquireLock_ShouldUseRootNamespace_AndKeepFencingKeyPersistent()
    {
        var rootNamespace = DataStoreRuntime.Manager.GetConnection("redis").RootNamespace;
        var store = new KeyValueStore();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        using var lockItem = store.AcquireLock(
            "sync-lock-path",
            throwWhenTimeout: true,
            slidingExpire: TimeSpan.FromSeconds(4),
            waitCancelToken: cts.Token)!;

        var namespacedKey = $"{rootNamespace}:sync-lock-path";
        var fencingKey = $"{rootNamespace}:__fencing__:KeyValueStore:sync-lock-path";

        Assert.IsTrue(_provider.GetExpire(namespacedKey) > TimeSpan.Zero);
        Assert.IsNull(_provider.GetExpire("sync-lock-path"));
        Assert.IsTrue(lockItem.FencingToken > 0);
        Assert.IsNull(_provider.GetExpire(fencingKey));
    }

    [TestMethod]
    public void HashStore_AcquireLock_ShouldShareFencingCounter_AcrossDifferentMasterKeys()
    {
        var rootNamespace = DataStoreRuntime.Manager.GetConnection("redis").RootNamespace;
        var localProvider = DataStoreRuntime.Manager.GetConnection("redis").Provider;

        var store1 = new HashStore("context-1");
        var store2 = new HashStore("context-2");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        long firstToken;
        using (var firstLock = store1.AcquireLock("Hotel:ClientSearchRequest_locker", cts.Token, throwWhenTimeout: true)!)
        {
            firstToken = firstLock.FencingToken;
        }

        using var secondLock = store2.AcquireLock("Hotel:ClientSearchRequest_locker", cts.Token, throwWhenTimeout: true)!;
        var secondToken = secondLock.FencingToken;

        Assert.IsTrue(firstToken > 0);
        Assert.AreEqual(firstToken + 1, secondToken);
        Assert.IsNull(localProvider.GetExpire($"{rootNamespace}:__fencing__:HashStore:Hotel:ClientSearchRequest_locker"));
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
