using Hazelcast.Client;
using Hazelcast.Config;
using Hazelcast.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Nuve.DataStore.Hazelcast
{
    public partial class HazelcastStoreProvider : IDataStoreProvider
    {
        protected IHazelcastInstance Client;

        void IDataStoreProvider.Initialize(string connectionString, IDataStoreProfiler profiler)
        {
            var configParts = connectionString.Split('|');
            var configs = new Dictionary<string, string>();
            foreach (var part in configParts)
            {
                var keyValue = part.Split('=');
                if (keyValue.Length != 2)
                    continue;
                configs[keyValue[0].Trim()] = keyValue[1].Trim();
            }
            var clientConfig = new ClientConfig();
            clientConfig.GetNetworkConfig().AddAddress(configs["members"].Split(','));
            Client = HazelcastClient.NewHazelcastClient(clientConfig);
            DefaultMap = Client.GetMap<string, string>(_keyValueMapKey);
        }

        TimeSpan? IDataStoreProvider.GetExpire(string key)
        {
            return TimeSpan.FromMilliseconds(DefaultMap.GetEntryView(key).GetExpirationTime());
        }

        Task<TimeSpan?> IDataStoreProvider.GetExpireAsync(string key)
        {
            return Task.FromResult((TimeSpan?)TimeSpan.FromMilliseconds(DefaultMap.GetEntryView(key).GetExpirationTime()));
        }

        StoreKeyType IDataStoreProvider.GetKeyType(string key)
        {
            if (DefaultMap.ContainsKey(key))
                return StoreKeyType.KeyValue;
            if (!Client.GetMap<string, string>(key).IsEmpty())
                return StoreKeyType.Dictionary;
            if (!Client.GetList<string>(key).IsEmpty())
                return StoreKeyType.LinkedList;
            if (!Client.GetSet<string>(key).IsEmpty())
                return StoreKeyType.HashSet;
            return StoreKeyType.KeyValue;
        }

        Task<StoreKeyType> IDataStoreProvider.GetKeyTypeAsync(string key)
        {
            return Task.FromResult(((IDataStoreProvider)this).GetKeyType(key));
        }

        void IDataStoreProvider.Lock(string lockKey, TimeSpan waitTimeout, TimeSpan lockerExpire, 
            Action action, bool skipWhenTimeout, bool throwWhenTimeout)
        {
            var locker = Client.GetLock(lockKey);
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            while (!locker.TryLock((long)lockerExpire.TotalMilliseconds, TimeUnit.Milliseconds))
            {
                if (stopWatch.Elapsed >= waitTimeout)
                    throw new TimeoutException(string.Format("{0} anahtarı timeout süresince kilitli kaldı.", lockKey));
                Thread.Sleep(100);
            }
            
            try
            {
                action();
            }
            finally
            {
                locker.Unlock();
            }
        }

        async Task IDataStoreProvider.LockAsync(string lockKey, TimeSpan waitTimeout, TimeSpan lockerExpire, 
            Func<Task> action, bool skipWhenTimeout, bool throwWhenTimeout)
        {
            var locker = Client.GetLock(lockKey);
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            while (!locker.TryLock((long)lockerExpire.TotalMilliseconds, TimeUnit.Milliseconds))
            {
                if (stopWatch.Elapsed >= waitTimeout)
                    throw new TimeoutException(string.Format("{0} anahtarı timeout süresince kilitli kaldı.", lockKey));
                await Task.Delay(100);
            }

            try
            {
                await action();
            }
            finally
            {
                locker.Unlock();
            }
        }

        bool IDataStoreProvider.Remove(string key)
        {
            DefaultMap.Delete(key);
            return true;
        }

        Task<bool> IDataStoreProvider.RemoveAsync(string key)
        {
            DefaultMap.Delete(key);
            return Task.FromResult(true);
        }

        bool IDataStoreProvider.SetExpire(string key, TimeSpan expire)
        {
            
            if (DefaultMap.ContainsKey(key))
                DefaultMap.ent
        }

        Task<bool> IDataStoreProvider.SetExpireAsync(string key, TimeSpan expire)
        {
            throw new NotImplementedException();
        }
    }
}
