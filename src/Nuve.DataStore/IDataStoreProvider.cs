using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nuve.DataStore
{
    public interface IDataStoreProvider
    {
        void Initialize(string connectionString, IDataStoreProfiler profiler);
        Task InitializeAsync(string connectionString, IDataStoreProfiler profiler);
        StoreKeyType GetKeyType(string key);
        Task<StoreKeyType> GetKeyTypeAsync(string key);
        TimeSpan? GetExpire(string key);
        Task<TimeSpan?> GetExpireAsync(string key);
        bool SetExpire(string key, TimeSpan expire);
        Task<bool> SetExpireAsync(string key, TimeSpan expire);
        bool Remove(string key);        
        Task<bool> RemoveAsync(string key);        
        void Lock(string lockKey, TimeSpan waitTimeout, Action action, TimeSpan slidingExpire, bool skipWhenTimeout = true, bool throwWhenTimeout = false);
        Task LockAsync(string lockKey, TimeSpan waitTimeout, Func<Task> action, TimeSpan slidingExpire, bool skipWhenTimeout = true, bool throwWhenTimeout = false);
    }
}
