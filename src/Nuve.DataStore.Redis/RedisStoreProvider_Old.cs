using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Nuve.Data.DataStore.Redis
{
    /// <summary>
    /// AcquireMultipleLocks tarafından kullanılan, dispose olabilen bir collection
    /// </summary>
    public class DisposableCollection : Collection<IDisposable>, IDisposable
    {
        public void Dispose()
        {
            foreach (var item in Items)
            {
                item.Dispose();
            }
        }
    }


    internal class RetryExceededException : Exception
    {
        
    }

    /// <summary>
    /// Belli bir tipe göre redis üzerinde işlem yapmayı sağlar.
    /// </summary>
    public class RedisStoreProvider_Old : IDataStoreProvider
    {
        private readonly ConnectionMultiplexer _multiplexer;
        private readonly int _defaultDatabase = 0;

        public RedisStoreProvider_Old(string parameterStr)
        {
            if (string.IsNullOrEmpty(parameterStr))
                throw new ArgumentNullException("parameterStr", "Redis için en az bir adet connection string belirtiniz.");

            var parameterParts = parameterStr.Split(';');
            var connectionString = parameterParts.First();
            if (parameterParts.Length > 1)
            {
                foreach (var parameterPart in parameterParts.Skip(1).ToList())
                {
                    var properties = parameterPart.Split('=');
                    if (properties.Length < 2)
                        continue;
                    if (properties[0].Equals("Database", StringComparison.InvariantCultureIgnoreCase))
                        int.TryParse(properties[1], out _defaultDatabase);
                }
            }

            var connOption = new ConfigurationOptions
                             {
                                 AbortOnConnectFail = false,
                                 //ConnectTimeout = 8000,
                                 SyncTimeout = 1500, 
                                 KeepAlive = 180
                             };

            connOption.EndPoints.Add(connectionString);
            _multiplexer = ConnectionMultiplexer.Connect(connOption);

            DefaultLockWaitTimeout = TimeSpan.FromSeconds(5);
            DefaultLockingExpire = TimeSpan.FromSeconds(10);
        }

        protected IDatabase Redis
        {
            get
            {
                return _multiplexer.GetDatabase(_defaultDatabase);
            }
        }

        private static long _timeoutExceptionCount = 0;

        protected T CheckTimeout<T>(Func<T> action, int retryCount = 0)
        {
            if (retryCount > 3)
                throw new RetryExceededException();

            try
            {
                return action();
            }
            catch (TimeoutException)
            {
                Interlocked.Increment(ref _timeoutExceptionCount);
                Thread.Sleep(10);
                CheckTimeout(action, retryCount + 1);
                throw;
            }
            catch (RetryExceededException)
            {
                try
                {
                    return Activator.CreateInstance<T>();
                }
                catch
                {
                    return default(T);
                }
            }
        }

        protected void CheckTimeout(Action action, int retryCount=0)
        {
            if (retryCount > 3)
                throw new RetryExceededException();

            try
            {
                action();
            }
            catch (TimeoutException)
            {
                Interlocked.Increment(ref _timeoutExceptionCount);
                Thread.Sleep(10);
                CheckTimeout(action, retryCount + 1);
                throw;
            }
            catch (RetryExceededException)
            {
            }
        }

        public virtual string Get(string key)
        {
            return CheckTimeout(() => Redis.StringGet(key));
        }

        public virtual IDictionary<string, string> GetAll(params string[] keys)
        {
            return CheckTimeout(() =>
                         {
                             var values = Redis.StringGet(keys.Select(k => (RedisKey) k).ToArray());
                             var result = new Dictionary<string, string>();
                             for (var i = 0; i < values.Length; i++)
                             {
                                 result.Add(keys[i], values[i]);
                             }
                             return result;
                         });
        }

        public virtual bool Set(string key, string entity)
        {
            return CheckTimeout(() => Redis.StringSet(key, entity));
        }

        public virtual void SetAll(IDictionary<string, string> keyValues)
        {
            CheckTimeout(() => Redis.StringSet(keyValues.ToDictionary(kv => (RedisKey)kv.Key, kv => (RedisValue)kv.Value).ToArray()));
        }

        public virtual bool SetExpire(string key, TimeSpan expire)
        {
            return CheckTimeout(() => Redis.KeyExpire(key, expire));
        }

        public virtual TimeSpan GetTimeToLive(string key)
        {
            return CheckTimeout(() => Redis.StringGetWithExpiry(key).Expiry ?? default(TimeSpan));
        }

        public virtual void SavePermanently()
        {
            //
        }

        /// <summary>
        /// Failover dahil tüm bağlı sunucularda silme işlemi gerçekleştirir. Çünkü birinde silip diğerinde silmese ve client switch olsa silinememiş gibi duracak.
        /// </summary>
        /// <param name="key"></param>
        public virtual bool Remove(string key)
        {
            return CheckTimeout(() => Redis.KeyDelete(key));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual bool Contains(string key)
        {
            return CheckTimeout(() => Redis.KeyExists(key));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oldKey"></param>
        /// <param name="newKey"></param>
        public virtual void Rename(string oldKey, string newKey)
        {
            CheckTimeout(() => Redis.KeyRename(oldKey, newKey));
        }

        /// <summary>
        /// Bir key'deki sayısal değeri artırır.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public long Increment(string key)
        {
            return CheckTimeout(() => Redis.StringIncrement(key));
        }

        /// <summary>
        /// Bir key'deki sayısal değeri azaltır.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public long Decrement(string key)
        {
            return CheckTimeout(() => Redis.StringDecrement(key));
        }

        /// <summary>
        /// Belli bir key'i kilitler. Bu key'e ancak bir client erişebilir. Disposible olduğu için Dispose olduğunda kilit de kalkar.
        /// </summary>
        /// <param name="lockKey">lock edilecek key.</param>
        /// <param name="waitTimeout">Lock'ı elde etmek için beklenilecek max süre. süre aşımında timeout exception alırsınız.</param>
        /// <param name="lockingExpire">lock oluşturulduktan sonra en fazla ne kadar süre tutulabilir?</param>
        /// <returns></returns>
        public IDisposable Lock(string lockKey, TimeSpan waitTimeout, TimeSpan lockingExpire)
        {
            //burada checktimeout çağrılmaz!! çünkü lock timeoutexception fırlatır.
            return Redis.AcquireLock(lockKey, waitTimeout, lockingExpire);
        }

        public void AddItemToList(string listKey, string item)
        {
            CheckTimeout(() => Redis.ListRightPush(listKey, item));
        }

        public void InsertItemToList(string listKey, int index, string item)
        {
            CheckTimeout(() => Redis.ListSetByIndex(listKey, index, item));
        }

        public void RemoveItemFromList(string listKey, string item)
        {
            CheckTimeout(() => Redis.ListRemove(listKey, item));
        }

        public List<string> GetList(string listKey)
        {
            return CheckTimeout(() => Redis.ListRange(listKey).Select(v => (string)v).ToList());
        }

        public long GetListCount(string listKey)
        {
            return CheckTimeout(() => Redis.ListLength(listKey));
        }

        public int GetIndexInList(string listKey, string item)
        {
            return CheckTimeout(() => (int)Redis.ScriptEvaluate(LuaCommands.IndexOf, new RedisKey[] { listKey }, new RedisValue[] { item }));
        }

        public List<string> GetItemsByIndexFromList(string listKey, int startIndex, int endIndex)
        {
            return CheckTimeout(() => Redis.ListRange(listKey, startIndex, endIndex).Select(v => (string)v).ToList());
        }

        public void SetItemToDictionary(string dictKey, string itemKey, string itemValue)
        {
            CheckTimeout(() => Redis.HashSet(dictKey, itemKey, itemValue));
        }

        public void SetItemsToDictionary(string dictKey, IEnumerable<KeyValuePair<string, string>> keyValues)
        {
            CheckTimeout(() => Redis.HashSet(dictKey, keyValues.Select(kv => new HashEntry(kv.Key, kv.Value)).ToArray()));
        }

        public void RemoveItemFromDictionary(string dictKey, string itemKey)
        {
            CheckTimeout(() => Redis.HashDelete(dictKey, itemKey));
        }

        public List<string> GetKeysOfDictionary(string dictKey)
        {
            return CheckTimeout(() => Redis.HashKeys(dictKey).Select(v => (string)v).ToList());
        }

        public List<string> GetAllValuesOfDictionary(string dictKey)
        {
            return CheckTimeout(() => Redis.HashValues(dictKey).Select(v => (string)v).ToList());
        }

        public Dictionary<string, string> GetDictionary(string dictKey)
        {
            return CheckTimeout(() => Redis.HashGetAll(dictKey).ToDictionary(h => (string)h.Name, h => (string)h.Value));
        }

        public long GetDictionaryCount(string dictKey)
        {
            return CheckTimeout(() => Redis.HashLength(dictKey));
        }

        public bool ContainsKeyInDictionary(string dictKey, string itemKey)
        {
            return CheckTimeout(() => Redis.HashExists(dictKey, itemKey));
        }

        public string GetValueFromDictionary(string dictKey, string itemKey)
        {
            return CheckTimeout(() => Redis.HashGet(dictKey, itemKey));
        }
        public List<string> GetValuesFromDictionary(string dictKey, params string[] keys)
        {
            return CheckTimeout(() => Redis.HashGet(dictKey, keys.Select(k => (RedisValue)k).ToArray()).Select(v => (string)v).ToList());
        }

        public void AddItemToSortedSet(string setKey, string item, double score)
        {
            CheckTimeout(() => Redis.SortedSetAdd(setKey, item, score));
        }

        public void AddItemsToSortedSet(string setKey, IEnumerable<KeyValuePair<string, double>> items)
        {
            CheckTimeout(() => Redis.SortedSetAdd(setKey, items.Select(i => new SortedSetEntry(i.Key, i.Value)).ToArray()));
        }

        public void RemoveItemFromSortedSet(string setKey, string item)
        {
            CheckTimeout(() => Redis.SortedSetRemove(setKey, item));
        }

        public void RemoveStartsWithFromSortedSet(string setKey, string startsWith)
        {
            CheckTimeout(() =>
                         {
                             if (startsWith.Length == 0)
                                 return;
                             var nextChar = (char) (startsWith[startsWith.Length - 1] + 1);
                             Redis.SortedSetRemoveRangeByValue(setKey, startsWith, startsWith.Substring(0, startsWith.Length - 1) + nextChar, Exclude.Stop);
                         });
        }

        public List<string> GetStartsWithFromSortedSet(string setKey, string startsWith)
        {
            return CheckTimeout(() =>
                                {
                                    if (startsWith.Length == 0)
                                        return new List<string>();
                                    var nextChar = (char) (startsWith[startsWith.Length - 1] + 1);
                                    return Redis.SortedSetRangeByValue(setKey, startsWith, startsWith.Substring(0, startsWith.Length - 1) + nextChar, Exclude.Stop).Select(v => (string) v).ToList();
                                });
        }

        public long GetSortedSetCount(string setKey)
        {
            return CheckTimeout(() => Redis.SortedSetLength(setKey));
        }

        public string GetItemFromSortedSet(string setKey, int index)
        {
            return CheckTimeout(() => Redis.SortedSetRangeByRank(setKey, index, index).FirstOrDefault());
        }

        public List<KeyValuePair<string, double>> GetSortedSet(string setKey)
        {
            return CheckTimeout(() => Redis.SortedSetRangeByRankWithScores(setKey).Select(s => new KeyValuePair<string, double>(s.Element, s.Score)).ToList());
        }

        public List<string> SearchKey(string keyPattern)
        {
            return CheckTimeout(() => _multiplexer.GetServer(_multiplexer.GetEndPoints().First()).Keys(pattern: keyPattern).Select(v => (string)v).ToList());
        }

        /// <summary>
        /// Bir key'e erişme esnasında lock varsa burada en fazla ne kadar bekleneceği. Süre aşımında Timeout exception fırlatacaktır.
        /// </summary>
        public TimeSpan DefaultLockWaitTimeout { get; set; }

        /// <summary>
        /// Bir key için lock oluşturulduğunda sonsuza kadar bu key durmaz. Bu lock key'ine de expire verilir. Bu süre sonunda lock key'i expire olacaktır.
        /// </summary>
        public TimeSpan DefaultLockingExpire { get; set; }

        public Dictionary<string, string> DebugInformation
        {
            get
            {
                return new Dictionary<string, string>
                       {
                           {"TimeoutException Count", _timeoutExceptionCount.ToString()}
                       };
            }
        }

        public EntityType GetKeyType(string key)
        {
            var redisType = Redis.KeyType(key);
            return
                redisType == RedisType.List ? EntityType.List : 
                redisType == RedisType.Hash ? EntityType.Dictionary : 
                redisType == RedisType.SortedSet ? EntityType.SortedSet :
                EntityType.Key;
        }
    }
}