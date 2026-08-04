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
        var lockKey = $"{namespacedKey}:__lock__";          // Sorun 4 fix: lock is on __lock__ suffix
        var fencingKey = $"{namespacedKey}:__fencing__:KeyValueStore";

        // lock key should exist with a TTL; data key should be untouched
        Assert.IsTrue(WaitUntil(() => _provider.GetExpire(lockKey) > TimeSpan.Zero, TimeSpan.FromSeconds(1)));
        Assert.IsNull(_provider.GetExpire(namespacedKey));  // data key not set by locking alone
        Assert.IsNull(_provider.GetExpire("sync-lock-path"));
        Assert.IsTrue(lockItem.FencingToken > 0);
        Assert.IsTrue(WaitUntil(() => _provider.GetExpire(fencingKey) > TimeSpan.Zero, TimeSpan.FromSeconds(5)));

        Assert.IsTrue(_provider.GetExpire(fencingKey) > TimeSpan.Zero);

        // release the lock so the lock key also expires
        lockItem.Dispose();
        Assert.IsTrue(WaitUntil(() => _provider.GetExpire(fencingKey) == null, TimeSpan.FromSeconds(14)));
    }

    [TestMethod]
    public void RedisHashSlot_ShouldMatchClusterSpecification()
    {
        // The slot of "foo" is a published value of the cluster specification, so it pins the
        // CRC16 implementation to the one the server uses.
        Assert.AreEqual(12182, RedisHashSlot.Of("foo"));

        // A hash tag makes only the tag decide the slot, which is what co-locates related keys.
        Assert.AreEqual(RedisHashSlot.Of("user1000"), RedisHashSlot.Of("{user1000}.following"));
        Assert.AreEqual(RedisHashSlot.Of("{user1000}.following"), RedisHashSlot.Of("{user1000}.followers"));

        // An empty tag does not count; the whole key decides instead.
        Assert.AreEqual(RedisHashSlot.Of("{}foo"), RedisHashSlot.Of("{}foo"));
        Assert.AreNotEqual(RedisHashSlot.Of("{}foo"), RedisHashSlot.Of("foo"));
    }

    [TestMethod]
    public void CrossSlotProbeKeys_ShouldBeOnDifferentSlots()
    {
        var first = RedisHashSlot.Of(RedisStoreProvider.CrossSlotProbeFirstKey!);
        var second = RedisHashSlot.Of(RedisStoreProvider.CrossSlotProbeSecondKey!);

        // If both probe keys shared a slot the probe would succeed on a cluster too, and the
        // provider would then send cross-slot writes that the server rejects.
        Assert.AreNotEqual(first, second,
            $"the probe has to span two slots; both keys resolved to {first}");
    }

    [TestMethod]
    public void Provider_ShouldDetectCrossSlotMultiKeySupport_DuringInitialize()
    {
        var capabilities = ((RedisStoreProvider)_provider).Capabilities;

        Assert.IsTrue(capabilities.IsResolved, "capabilities should be resolved while the provider initializes");
        Assert.IsFalse(capabilities.IsClustered, $"the test server is expected to be a single node, was {capabilities.ServerType}");

        // A single node places every key on one instance, so one write may span them all.
        Assert.IsTrue(capabilities.SupportsCrossSlotMultiKey);
    }

    [TestMethod]
    public void KeyValueStore_SetAllAndGetAll_ShouldKeepEveryKeyPairedWithItsValue()
    {
        var rootNamespace = DataStoreRuntime.Manager.GetConnection("redis").RootNamespace;
        var store = new KeyValueStore();
        var keys = Enumerable.Range(0, 25).Select(index => $"multi:{index}").ToArray();
        var values = keys.ToDictionary(key => key, key => $"value-of-{key}");

        Assert.IsTrue(store.Set(values));

        // Multi-key requests may be split per slot, so every key has to come back with its own
        // value rather than with a neighbour's.
        var fetched = store.Get<string>(keys);
        Assert.AreEqual(keys.Length, fetched.Count);
        foreach (var key in keys)
        {
            Assert.AreEqual(values[key], fetched[$"{rootNamespace}:{key}"]);
            Assert.AreEqual(values[key], store.Get<string>(key));
        }
    }

    [TestMethod]
    public void HashSetStore_SetCombination_AcrossSlots_ShouldExplainHowToCoLocateKeys()
    {
        var provider = (RedisStoreProvider)DataStoreAccess.GetDefaultProvider();
        // A single node accepts anything, so the topology that refuses it has to be simulated.
        provider.OverrideCapabilitiesForTests(crossSlotMultiKey: false);

        var store = new HashSetStore<string>("context");
        store.Add("a", "b");

        var foreignKey = "a-set-on-another-slot";
        Assert.AreNotEqual(RedisHashSlot.Of(store.MasterKey), RedisHashSlot.Of(foreignKey));

        var exception = Assert.ThrowsException<InvalidOperationException>(
            () => store.DifferenceToHashSet(foreignKey));

        StringAssert.Contains(exception.Message, "Difference requires every key to be on a single cluster slot");
        StringAssert.Contains(exception.Message, foreignKey);
        StringAssert.Contains(exception.Message, "hash tag");
    }

    [TestMethod]
    public void HashSetStore_SetCombination_WithSharedHashTag_ShouldBeAllowedAcrossSlotRestrictions()
    {
        var provider = (RedisStoreProvider)DataStoreAccess.GetDefaultProvider();
        provider.OverrideCapabilitiesForTests(crossSlotMultiKey: false);

        // Both keys carry the same hash tag, so the restriction does not apply to them.
        var store = new HashSetStore<string>("{shared-slot}:left");
        var right = new HashSetStore<string>("{shared-slot}:right");
        store.Add("a", "b");
        right.Add("b");

        Assert.AreEqual(RedisHashSlot.Of(store.MasterKey), RedisHashSlot.Of(right.MasterKey));

        var difference = store.DifferenceToHashSet(right.MasterKey);

        Assert.AreEqual(1, difference.Count);
        Assert.IsTrue(difference.Contains("a"));
    }

    [TestMethod]
    public void Provider_ShouldResolveServerCapabilities_DuringInitialize()
    {
        var provider = (RedisStoreProvider)_provider;
        var capabilities = provider.Capabilities;

        Assert.IsTrue(capabilities.IsResolved, "capabilities should be resolved while the provider initializes");
        Assert.IsTrue(capabilities.ServerVersion > new Version(2, 6), $"unexpected server version {capabilities.ServerVersion}");
        Assert.IsTrue(capabilities.SupportsScripting);

        // The conditional expire options arrived in Redis 7.0, so the flag has to follow the
        // connected server rather than being assumed — production and development differ here.
        Assert.AreEqual(
            capabilities.ServerVersion >= new Version(7, 0),
            capabilities.SupportsExpireOptions,
            $"SupportsExpireOptions does not match server version {capabilities.ServerVersion}");
    }

    [TestMethod]
    public void KeyValueStore_ParameterlessLock_ShouldBoundFencingCounterTtl_AndNotLeak()
    {
        var rootNamespace = DataStoreRuntime.Manager.GetConnection("redis").RootNamespace;

        // Parameter-less store => DefaultExpire = 0, used purely as a mutex over an ephemeral,
        // high-cardinality key that never holds data. Before the fix the fencing counter was
        // created with no expiration (one permanent key per distinct resource => unbounded growth).
        var store = new KeyValueStore();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        // Long enough that the assertions below cannot race the counter's own expiration on a
        // loaded server, while still proving the counter is bounded rather than permanent.
        var slidingExpire = TimeSpan.FromSeconds(10);
        var fencingKey = $"{rootNamespace}:ChangeBookState:123:__fencing__:KeyValueStore";

        using (var lockItem = store.AcquireLock(
            "ChangeBookState:123",
            throwWhenTimeout: true,
            slidingExpire: slidingExpire,
            waitCancelToken: cts.Token)!)
        {
            Assert.IsTrue(lockItem.FencingToken > 0);

            // The counter must be bounded even though DefaultExpire = 0 and the resource key has no TTL.
            Assert.IsTrue(WaitUntil(() => _provider.GetExpire(fencingKey) > TimeSpan.Zero, TimeSpan.FromSeconds(2)));
            var fencingTtl = _provider.GetExpire(fencingKey);
            Assert.IsNotNull(fencingTtl);
            // The point is that the counter expires at all rather than living forever, so a little
            // slack is allowed for the server rounding its own clock when reporting the remaining time.
            Assert.IsTrue(fencingTtl <= slidingExpire + TimeSpan.FromSeconds(1),
                $"fencing counter TTL should be bounded by slidingExpire ({slidingExpire}); was {fencingTtl}");
        }

        // Once abandoned, the counter self-expires — no unbounded leak across distinct keys.
        Assert.IsTrue(WaitUntil(() => _provider.GetExpire(fencingKey) == null, TimeSpan.FromSeconds(20)));
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
        var fencingKey1 = $"{store1.MasterKey}:__fencing__:HashStore";
        var fencingKey2 = $"{store2.MasterKey}:__fencing__:HashStore";

        Assert.IsTrue(firstToken > 0);
        Assert.AreEqual(firstToken, secondToken);   // independent MasterKeys → both start at 1
        Assert.IsTrue(WaitUntil(() => localProvider.GetExpire(fencingKey1) > TimeSpan.Zero, TimeSpan.FromSeconds(5)));
        Assert.IsTrue(WaitUntil(() => localProvider.GetExpire(fencingKey2) > TimeSpan.Zero, TimeSpan.FromSeconds(5)));

        Assert.IsTrue(localProvider.GetExpire(fencingKey1) > TimeSpan.Zero);
        Assert.IsTrue(localProvider.GetExpire(fencingKey2) > TimeSpan.Zero);
        // Old root-namespace-scoped format no longer created
        Assert.AreEqual(0, FindKeys(mux, $"{rootNamespace}:__fencing__:HashStore:*").Length);

        Assert.IsTrue(WaitUntil(() => localProvider.GetExpire(fencingKey1) == null, TimeSpan.FromSeconds(14)));
        Assert.IsTrue(WaitUntil(() => localProvider.GetExpire(fencingKey2) == null, TimeSpan.FromSeconds(14)));
    }

    [TestMethod]
    public void HashStore_Ping_ShouldExtendTrackedFencingKeyLifetime()
    {
        // The expiration has to outlast every step below, otherwise the assertions race the
        // counter's own expiration instead of measuring whether Ping extended it.
        var store = new HashStore("context-ping", defaultExpire: TimeSpan.FromSeconds(30));
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        using var mux = ConnectionMultiplexer.Connect(RedisTestHelpers.GetRedisConnectionString());

        store.Set("seed", "value");

        using (var lockItem = store.AcquireLock("Hotel:ClientSearchRequest_locker", cts.Token, throwWhenTimeout: true)!)
        {
            Assert.IsTrue(lockItem.FencingToken > 0);
        }

        var fencingKey = $"{store.MasterKey}:__fencing__:HashStore";
        Assert.IsTrue(WaitUntil(() => _provider.GetExpire(fencingKey) > TimeSpan.Zero, TimeSpan.FromSeconds(5)));
        var ttlBeforeWait = _provider.GetExpire(fencingKey);
        Assert.IsTrue(ttlBeforeWait > TimeSpan.Zero);

        Thread.Sleep(TimeSpan.FromMilliseconds(1200));

        var ttlAfterWait = _provider.GetExpire(fencingKey);
        Assert.IsTrue(ttlAfterWait > TimeSpan.Zero, $"fencing TTL before Ping was {ttlAfterWait}");
        Assert.IsTrue(store.Ping());

        var ttlAfterPing = _provider.GetExpire(fencingKey);
        Assert.IsTrue(ttlAfterPing >= ttlAfterWait,
            $"Ping should extend the fencing counter; before={ttlAfterWait}, after={ttlAfterPing}, defaultExpire={store.DefaultExpire}");
        Assert.IsTrue(ttlAfterPing > TimeSpan.FromSeconds(1), $"fencing TTL after Ping was {ttlAfterPing}");
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

        var dictionaryFencingKey = $"{dictionaryStore.MasterKey}:__fencing__:DictionaryStore<string>";
        var hashSetFencingKey = $"{hashSetStore.MasterKey}:__fencing__:HashSetStore<string>";
        var linkedListFencingKey = $"{linkedListStore.MasterKey}:__fencing__:LinkedListStore<string>";

        Assert.IsTrue(WaitUntil(() => _provider.GetExpire(dictionaryFencingKey) > TimeSpan.Zero, TimeSpan.FromSeconds(5)));
        Assert.IsTrue(WaitUntil(() => _provider.GetExpire(hashSetFencingKey) > TimeSpan.Zero, TimeSpan.FromSeconds(5)));
        Assert.IsTrue(WaitUntil(() => _provider.GetExpire(linkedListFencingKey) > TimeSpan.Zero, TimeSpan.FromSeconds(5)));

        Assert.IsTrue(_provider.GetExpire(dictionaryFencingKey) > TimeSpan.Zero);
        Assert.IsTrue(_provider.GetExpire(hashSetFencingKey) > TimeSpan.Zero);
        Assert.IsTrue(_provider.GetExpire(linkedListFencingKey) > TimeSpan.Zero);

        Assert.IsTrue(WaitUntil(() => _provider.GetExpire(dictionaryFencingKey) == null, TimeSpan.FromSeconds(14)));
        Assert.IsTrue(WaitUntil(() => _provider.GetExpire(hashSetFencingKey) == null, TimeSpan.FromSeconds(14)));
        Assert.IsTrue(WaitUntil(() => _provider.GetExpire(linkedListFencingKey) == null, TimeSpan.FromSeconds(14)));
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
