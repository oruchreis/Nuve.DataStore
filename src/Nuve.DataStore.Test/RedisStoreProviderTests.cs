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
            Console.WriteLine("lock achieved: {0}",lockObj.LockAchieved.Value.ToString("mm:ss.fff"));
            var ttl = provider.GetExpire("test-lock");
            Console.WriteLine("lock-ttl at start: {0}", ttl.Value.TotalMilliseconds);
            Assert.IsTrue(ttl.Value.TotalMilliseconds > 5000); // ttl must close to 6sn
            Thread.Sleep(3500);
            Console.WriteLine("lock achieved: {0}", lockObj.LockAchieved.Value.ToString("mm:ss.fff"));
            ttl = provider.GetExpire("test-lock"); //after 3sn sliding expiraration must be executed and lock must be extended to 6sn
            Console.WriteLine("lock-ttl after 3sn: {0}", ttl.Value.TotalMilliseconds);
            Assert.IsTrue(ttl.Value.TotalMilliseconds > 5000); // ttl must close to 6sn
            Thread.Sleep(3500);
            Console.WriteLine("lock achieved: {0}", lockObj.LockAchieved.Value.ToString("mm:ss.fff"));
            ttl = provider.GetExpire("test-lock");
            Console.WriteLine("lock-ttl after 6sn: {0}", ttl.Value.TotalMilliseconds);
            Assert.IsTrue(ttl.Value.TotalMilliseconds > 5000);
            Thread.Sleep(8000); //one last check
            Console.WriteLine("lock achieved: {0}", lockObj.LockAchieved.Value.ToString("mm:ss.fff"));
            ttl = provider.GetExpire("test-lock");
            Console.WriteLine("lock-ttl after 14sn: {0}", ttl.Value.TotalMilliseconds);
            Assert.IsTrue(ttl.Value.TotalMilliseconds > 0);
        }, throwWhenTimeout: true, slidingExpire: slidingExpire);

        Assert.IsFalse(((IKeyValueStoreProvider)provider).Contains("test-lock"));
    }
}
