using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nuve.DataStore.Redis;

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
}