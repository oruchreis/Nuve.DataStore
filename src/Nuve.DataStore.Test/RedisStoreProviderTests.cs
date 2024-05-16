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
internal class RedisStoreProviderTests
{
    [TestInitialize]
    public void Setup()
    {
        DataStoreManager.DefaultSerializer = new JsonNetDataStoreSerializer();
        DataStoreManager.RegisterProvider("Redis", typeof(RedisStoreProvider));

        DataStoreManager.CreateConnection(
            connectionName: "redis",
            providerName: "Redis",
            connectionString: "redis:6379",
            rootNamespace: "test",
            isDefault: true);
    }

    [TestMethod]
    public void LockSlidingExpiration()
    {
        var keyvalueStore = new KeyValueStore();
        keyvalueStore.Lock("test-lock", TimeSpan.FromSeconds(1), () =>
        {
            var ttl = keyvalueStore.GetExpire("test-lock");
            Console.WriteLine("lock-ttl at start: {0}", ttl.Value.TotalMilliseconds);
            Assert.IsTrue(ttl.Value.TotalMilliseconds > 0);
            Thread.Sleep(1000);
            ttl = keyvalueStore.GetExpire("test-lock");
            Console.WriteLine("lock-ttl after 1sn: {0}", ttl.Value.TotalMilliseconds);
            Assert.IsTrue(ttl.Value.TotalMilliseconds > 1000);
            Thread.Sleep(1000);
            ttl = keyvalueStore.GetExpire("test-lock");
            Console.WriteLine("lock-ttl after 2sn: {0}", ttl.Value.TotalMilliseconds);
            Assert.IsTrue(ttl.Value.TotalMilliseconds > 1000);
            Thread.Sleep(5000);
            ttl = keyvalueStore.GetExpire("test-lock");
            Console.WriteLine("lock-ttl after 7sn: {0}", ttl.Value.TotalMilliseconds);
            Assert.IsTrue(ttl.Value.TotalMilliseconds > 1000);
        }, throwWhenTimeout: true, skipWhenTimeout: false, slidingExpire: TimeSpan.FromSeconds(2));

        Assert.IsFalse(keyvalueStore.Contains("test-lock"));
    }
}
