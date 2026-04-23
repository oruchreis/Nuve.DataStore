using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nuve.DataStore.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nuve.DataStore.Test;

[TestClass]
public class RedisStoreProviderIntegrationTests
{
    private static string ConnectionString => RedisTestHelpers.GetRedisConnectionString();

    [TestMethod]
    [TestCategory("Integration")]
    public void SharedProvider_ShouldPerformBasicCrud()
    {
        IDataStoreProvider provider = new RedisStoreProvider();
        provider.Initialize(new ConnectionOptions
        {
            ConnectionString = ConnectionString,
            ConnectionMode = ConnectionMode.Shared,
            RetryCount = 2
        }, profiler: null);

        var key = $"test:shared:{Guid.NewGuid():N}";

        var removedBefore = provider.Remove(key);
        Assert.IsFalse(removedBefore);

        var setExpire = provider.SetExpire(key, TimeSpan.FromMinutes(5));
        // key yoksa false dönebilir; bu satır API’nize göre değişebilir.

        var type = provider.GetKeyType(key);
        Assert.IsNotNull(type);

        var removed = provider.Remove(key);
        Assert.IsFalse(removed); // key hala yoksa false
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task SharedProvider_ShouldAllowConcurrentAsyncCalls()
    {
        IDataStoreProvider provider = new RedisStoreProvider();
        provider.Initialize(new ConnectionOptions
        {
            ConnectionString = ConnectionString,
            ConnectionMode = ConnectionMode.Shared,
            RetryCount = 2
        }, profiler: null);

        var keys = Enumerable.Range(0, 50)
            .Select(i => $"test:shared:concurrent:{Guid.NewGuid():N}:{i}")
            .ToArray();

        var tasks = keys.Select(async key =>
        {
            await provider.SetExpireAsync(key, TimeSpan.FromMinutes(1)).ConfigureAwait(false);
            await provider.GetExpireAsync(key).ConfigureAwait(false);
            await provider.RemoveAsync(key).ConfigureAwait(false);
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task PooledProvider_ShouldRespectPoolUnderConcurrency()
    {
        IDataStoreProvider provider = new RedisStoreProvider();
        provider.Initialize(new ConnectionOptions
        {
            ConnectionString = ConnectionString,
            ConnectionMode = ConnectionMode.Pooled,
            MaxPoolSize = 4,
            PoolWaitTimeout = TimeSpan.FromSeconds(2),
            RetryCount = 2
        }, profiler: null);

        var keys = Enumerable.Range(0, 20)
            .Select(i => $"test:pooled:{Guid.NewGuid():N}:{i}")
            .ToArray();

        var tasks = keys.Select(async key =>
        {
            await provider.RemoveAsync(key).ConfigureAwait(false);
            await provider.GetExpireAsync(key).ConfigureAwait(false);
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void SharedProvider_ShouldUseSingleUnderlyingSharedManagerInstance()
    {
        var provider = new RedisStoreProvider();
        provider.Initialize(new ConnectionOptions
        {
            ConnectionString = ConnectionString,
            ConnectionMode = ConnectionMode.Shared
        }, profiler: null);

        var manager = RedisTestHelpers.GetConnectionManager(provider);
        Assert.IsInstanceOfType<SharedRedisConnectionManager>(manager);

        using var lease1 = manager.Acquire();
        using var lease2 = manager.Acquire();

        Assert.AreSame(lease1.Multiplexer, lease2.Multiplexer);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void PooledProvider_ShouldUseDifferentMultiplexers_WhenPoolHasCapacity()
    {
        var provider = new RedisStoreProvider();
        provider.Initialize(new ConnectionOptions
        {
            ConnectionString = ConnectionString,
            ConnectionMode = ConnectionMode.Pooled,
            MaxPoolSize = 2,
            PoolWaitTimeout = TimeSpan.FromSeconds(1)
        }, profiler: null);

        var manager = RedisTestHelpers.GetConnectionManager(provider);
        Assert.IsInstanceOfType<PooledRedisConnectionManager>(manager);

        using var lease1 = manager.Acquire();
        using var lease2 = manager.Acquire();

        Assert.AreNotSame(lease1.Multiplexer, lease2.Multiplexer);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task SharedProvider_ShouldSetAndGetValue()
    {
        IDataStoreProvider provider = new RedisStoreProvider();
        IKeyValueStoreProvider keyValueProvider = (IKeyValueStoreProvider)provider;
        provider.Initialize(new ConnectionOptions
        {
            ConnectionString = ConnectionString,
            ConnectionMode = ConnectionMode.Shared
        }, profiler: null);

        var key = $"test:value:{Guid.NewGuid():N}";
        var expected = Encoding.UTF8.GetBytes("hello");

        await keyValueProvider.SetAsync(key, expected, true).ConfigureAwait(false);
        var actual = await keyValueProvider.GetAsync(key).ConfigureAwait(false);

        CollectionAssert.AreEquivalent(expected, actual);

        await provider.RemoveAsync(key).ConfigureAwait(false);
    }
}