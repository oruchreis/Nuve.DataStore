using System;
using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Nuve.DataStore.Redis
{
    public partial class RedisStoreProvider : IDataStoreProvider
    {
        private static readonly ConcurrentDictionary<string, ConnectionMultiplexer> _connections = new ConcurrentDictionary<string, ConnectionMultiplexer>();
        protected ConnectionMultiplexer Redis;
        protected IDatabase Db { get { return Redis.GetDatabase(); } }
        public void Initialize(string connectionString, IDataStoreProfiler profiler)
        {
            Redis = _connections.GetOrAdd(connectionString,
                cs =>
                {
                    var cm = ConnectionMultiplexer.Connect(ParseConnectionString(cs));
                    cm.PreserveAsyncOrder = false; //http://stackoverflow.com/questions/30797716/deadlock-when-accessing-stackexchange-redis
                    Thread.Sleep(1000); //https://github.com/StackExchange/StackExchange.Redis/issues/248#issuecomment-182504080   
                    return cm;
                });      
        }

        protected ConfigurationOptions ParseConnectionString(string connString)
        {
            var config = ConfigurationOptions.Parse(connString, true);
            if (!connString.Contains("abortConnect="))
                config.AbortOnConnectFail = false;
            if (!connString.Contains("syncTimeout="))
                config.SyncTimeout = int.MaxValue;
            if (!connString.Contains("keepAlive="))
                config.KeepAlive = 180;

            return config;
        }

        StoreKeyType IDataStoreProvider.GetKeyType(string key)
        {
            var redisType = Db.KeyType(key);
            return redisType == RedisType.List ? StoreKeyType.LinkedList :
               redisType == RedisType.Hash ? StoreKeyType.Dictionary :
               redisType == RedisType.SortedSet ? StoreKeyType.SortedSet :
               StoreKeyType.KeyValue;
        }

        async Task<StoreKeyType> IDataStoreProvider.GetKeyTypeAsync(string key)
        {
            var redisType = await Db.KeyTypeAsync(key);
            return redisType == RedisType.List ? StoreKeyType.LinkedList :
               redisType == RedisType.Hash ? StoreKeyType.Dictionary :
               redisType == RedisType.SortedSet ? StoreKeyType.SortedSet :
               StoreKeyType.KeyValue;
        }

        TimeSpan? IDataStoreProvider.GetExpire(string key)
        {
            return Db.KeyTimeToLive(key);
        }

        async Task<TimeSpan?> IDataStoreProvider.GetExpireAsync(string key)
        {
            return await Db.KeyTimeToLiveAsync(key);
        }

        bool IDataStoreProvider.SetExpire(string key, TimeSpan expire)
        {
            return Db.KeyExpire(key, expire);
        }

        async Task<bool> IDataStoreProvider.SetExpireAsync(string key, TimeSpan expire)
        {
            return await Db.KeyExpireAsync(key, expire);
        }  

        bool IDataStoreProvider.Remove(string key)
        {
            return Db.KeyDelete(key);
        }

        async Task<bool> IDataStoreProvider.RemoveAsync(string key)
        {
            return await Db.KeyDeleteAsync(key);
        }

        void IDataStoreProvider.Lock(string lockKey, TimeSpan waitTimeout, TimeSpan lockerExpire, Action action, bool skipWhenTimeout, bool throwWhenTimeout)
        {
            try
            {
                using (Db.AcquireLock(lockKey, waitTimeout, lockerExpire))
                {
                    action();
                }                
            }
            catch (TimeoutException e)
            {
                if (!skipWhenTimeout)
                    action();
                if (throwWhenTimeout)
                    ExceptionDispatchInfo.Capture(e).Throw();
            }
        }

        async Task IDataStoreProvider.LockAsync(string lockKey, TimeSpan waitTimeout, TimeSpan lockerExpire, Func<Task> action, bool skipWhenTimeout, bool throwWhenTimeout)
        {
            try
            {
                using (await Db.AcquireLockAsync(lockKey, waitTimeout, lockerExpire))
                {
                    await action();
                }                
            }
            catch (TimeoutException e)
            {
                if (!skipWhenTimeout)
                    await action();
                if (throwWhenTimeout)
                    ExceptionDispatchInfo.Capture(e).Throw();
            }
        }   
    }
}
