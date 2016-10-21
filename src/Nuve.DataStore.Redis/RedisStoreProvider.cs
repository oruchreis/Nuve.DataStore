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
        protected RedisProfiler Profiler;
        public void Initialize(string connectionString, IDataStoreProfiler profiler)
        {
            Redis = _connections.GetOrAdd(connectionString,
                cs =>
                {
                    var cm = ConnectionMultiplexer.Connect(ParseConnectionString(cs));
                    cm.PreserveAsyncOrder = false; //http://stackoverflow.com/questions/30797716/deadlock-when-accessing-stackexchange-redis
                    Thread.Sleep(1000); //https://github.com/StackExchange/StackExchange.Redis/issues/248#issuecomment-182504080   
                    Profiler = new RedisProfiler(cm, profiler);
                    //cm.RegisterProfiler(Profiler);
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
            var redisType = Profiler.Profile(() => Db.KeyType(key), key);
            return redisType == RedisType.List ? StoreKeyType.LinkedList :
               redisType == RedisType.Hash ? StoreKeyType.Dictionary :
               redisType == RedisType.SortedSet ? StoreKeyType.SortedSet :
               StoreKeyType.KeyValue;
        }

        async Task<StoreKeyType> IDataStoreProvider.GetKeyTypeAsync(string key)
        {
            var redisType = await Profiler.Profile(() => Db.KeyTypeAsync(key), key);
            return redisType == RedisType.List ? StoreKeyType.LinkedList :
               redisType == RedisType.Hash ? StoreKeyType.Dictionary :
               redisType == RedisType.SortedSet ? StoreKeyType.SortedSet :
               StoreKeyType.KeyValue;
        }

        TimeSpan? IDataStoreProvider.GetExpire(string key)
        {
            return Profiler.Profile(() => Db.KeyTimeToLive(key), key);
        }

        async Task<TimeSpan?> IDataStoreProvider.GetExpireAsync(string key)
        {
            return await Profiler.Profile(() => Db.KeyTimeToLiveAsync(key), key);
        }

        bool IDataStoreProvider.SetExpire(string key, TimeSpan expire)
        {
            return Profiler.Profile(() => Db.KeyExpire(key, expire), key);
        }

        async Task<bool> IDataStoreProvider.SetExpireAsync(string key, TimeSpan expire)
        {
            return await Profiler.Profile(() => Db.KeyExpireAsync(key, expire), key);
        }  

        bool IDataStoreProvider.Remove(string key)
        {
            return Profiler.Profile(() => Db.KeyDelete(key), key);
        }

        async Task<bool> IDataStoreProvider.RemoveAsync(string key)
        {
            return await Profiler.Profile(() => Db.KeyDeleteAsync(key), key);
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
    }
}
