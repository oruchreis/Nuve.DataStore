using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nuve.DataStore.Redis;
using Nuve.DataStore.Serializer.JsonNet;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nuve.DataStore.Test;

[TestClass]
public class RedisStoreProviderTests
{
    [TestInitialize]
    public void Setup()
    {
        DataStoreManager.DefaultSerializer = new JsonNetDataStoreSerializer();
        DataStoreManager.RegisterProvider("Redis", typeof(RedisStoreProvider));

        DataStoreManager.CreateConnection(
            connectionName: "redis",
            providerName: "Redis",
            connectionString: "localhost:6379",
            rootNamespace: "test",
            isDefault: true);
    }

    [TestMethod]
    public void LockSlidingExpiration()
    {
        DataStoreManager.GetProvider("redis", out var provider, out var rootNameSpace, out int? defaultCompressBiggerThan);
        var slidingExpire = TimeSpan.FromSeconds(6);
        ((RedisStoreProvider)provider).Lock("test-lock", TimeSpan.FromSeconds(1), (lockItem) =>
        {            
            var lockObj = (RedisExtension.Lock)lockItem;
            Assert.IsNotNull(lockObj.LockAchieved);
            Console.WriteLine("{0:hh:mm:ss.fff}\tlock achieved: {1:hh:mm:ss.fff}", DateTimeOffset.UtcNow, lockObj.LockAchieved);
            var ttl = provider.GetExpire("test-lock");
            Console.WriteLine("lock-ttl at start: {0}", ttl!.Value.TotalMilliseconds);
            Assert.IsTrue(ttl.Value.TotalMilliseconds > (slidingExpire.TotalMilliseconds /2)); // ttl must close to slidingExpire
            for (int i = 0; i < 3; i++)
            {
                Console.WriteLine("Test iteration: {0}", i);
                Thread.Sleep(Math.Max((int)ttl.Value.TotalMilliseconds - ((int)slidingExpire.TotalMilliseconds / 2) + RedisExtension.Lock.CheckSlidingExpirationMs + 1000, 1));
                Console.WriteLine("{0:hh:mm:ss.fff}\tlock achieved after halflife: {1:hh:mm:ss.fff}", DateTimeOffset.UtcNow, lockObj.LockAchieved);
                ttl = provider.GetExpire("test-lock"); //after halflife sliding expiraration must be executed and lock must be extended to slidingExpire
                Console.WriteLine("lock-ttl after after halflife: {0}", ttl!.Value.TotalMilliseconds);
                Assert.IsTrue(ttl.Value.TotalMilliseconds > (slidingExpire.TotalMilliseconds /2)); // ttl must close to slidingExpire
            }
        }, throwWhenTimeout: true, slidingExpire: slidingExpire);

        Assert.IsFalse(((IKeyValueStoreProvider)provider).Contains("test-lock"));
    }
}
