using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nuve.DataStore.Redis
{
    public partial class RedisStoreProvider : IDictionaryStoreProvider
    {
        bool IDictionaryStoreProvider.IsExists(string dictKey)
        {
            return Profiler.Profile(() => Db.KeyExists(dictKey), dictKey);
        }

        async Task<bool> IDictionaryStoreProvider.IsExistsAsync(string dictKey)
        {
            return await Profiler.Profile(() => Db.KeyExistsAsync(dictKey), dictKey);
        }

        string IDictionaryStoreProvider.Get(string dictKey, string itemKey)
        {
            return Profiler.Profile(() => Db.HashGet(dictKey, itemKey), () => dictKey + "=>" + itemKey);
        }

        async Task<string> IDictionaryStoreProvider.GetAsync(string dictKey, string itemKey)
        {
            return await Profiler.Profile(() => Db.HashGetAsync(dictKey, itemKey), () => dictKey + "=>" + itemKey);
        }

        IDictionary<string, string> IDictionaryStoreProvider.Get(string dictKey, params string[] itemKeys)
        {
            return Profiler.Profile(() =>
                             {
                                 var values = Db.HashGet(dictKey, itemKeys.Select(item => (RedisValue) item).ToArray());
                                 return itemKeys.Zip(values, (k, v) => new {k, v}).ToDictionary(kv => (string) kv.k, kv => (string) kv.v);
                             }, () => dictKey + "=>" + string.Join(",", itemKeys));
        }

        async Task<IDictionary<string, string>> IDictionaryStoreProvider.GetAsync(string dictKey, params string[] itemKeys)
        {
            return await Profiler.Profile(async () =>
                                    {
                                        var values = await Db.HashGetAsync(dictKey, itemKeys.Select(item => (RedisValue) item).ToArray());
                                        return itemKeys.Zip(values, (k, v) => new {k, v}).ToDictionary(kv => (string) kv.k, kv => (string) kv.v);
                                    }, () => dictKey + "=>" + string.Join(",", itemKeys));
        }

        bool IDictionaryStoreProvider.Set(string dictKey, string itemKey, string itemValue, bool overwrite)
        {
            return Profiler.Profile(() => Db.HashSet(dictKey, itemKey, itemValue, overwrite ? When.Always : When.NotExists), 
                () => dictKey + "=>" + itemKey);
        }

        async Task<bool> IDictionaryStoreProvider.SetAsync(string dictKey, string itemKey, string itemValue, bool overwrite)
        {
            return await Profiler.Profile(() => Db.HashSetAsync(dictKey, itemKey, itemValue, overwrite ? When.Always : When.NotExists), 
                () => dictKey + "=>" + itemKey);
        }

        void IDictionaryStoreProvider.Set(string dictKey, IDictionary<string, string> keyValues)
        {
            Profiler.Profile(() => Db.HashSet(dictKey, keyValues.Select(kv => new HashEntry(kv.Key, kv.Value)).ToArray()), 
                () => dictKey + "=>" + string.Join(",", keyValues.Keys));
        }

        async Task IDictionaryStoreProvider.SetAsync(string dictKey, IDictionary<string, string> keyValues)
        {
            await Profiler.Profile(() => Db.HashSetAsync(dictKey, keyValues.Select(kv => new HashEntry(kv.Key, kv.Value)).ToArray()),
                () => dictKey + "=>" + string.Join(",", keyValues.Keys));
        }

        long IDictionaryStoreProvider.Remove(string dictKey, params string[] itemKeys)
        {
            return Profiler.Profile(() => Db.HashDelete(dictKey, itemKeys.Select(item => (RedisValue)item).ToArray()),
                () => dictKey + "=>" + string.Join(",", itemKeys));
        }

        async Task<long> IDictionaryStoreProvider.RemoveAsync(string dictKey, params string[] itemKeys)
        {
            return await Profiler.Profile(() => Db.HashDeleteAsync(dictKey, itemKeys.Select(item => (RedisValue)item).ToArray()),
                () => dictKey + "=>" + string.Join(",", itemKeys));
        }

        bool IDictionaryStoreProvider.Contains(string dictKey, string itemKey)
        {
            return Profiler.Profile(() => Db.HashExists(dictKey, itemKey), 
                () => dictKey + "=>" + itemKey);
        }

        async Task<bool> IDictionaryStoreProvider.ContainsAsync(string dictKey, string itemKey)
        {
            return await Profiler.Profile(() => Db.HashExistsAsync(dictKey, itemKey),() => dictKey + "=>" + itemKey);
        }

        long IDictionaryStoreProvider.Count(string dictKey)
        {
            return Profiler.Profile(() => Db.HashLength(dictKey), dictKey);
        }

        async Task<long> IDictionaryStoreProvider.CountAsync(string dictKey)
        {
            return await Profiler.Profile(() => Db.HashLengthAsync(dictKey), dictKey);
        }

        IDictionary<string, string> IDictionaryStoreProvider.GetDictionary(string dictKey)
        {
            return Profiler.Profile(() => Db.HashGetAll(dictKey).ToStringDictionary(), dictKey);
        }

        async Task<IDictionary<string, string>> IDictionaryStoreProvider.GetDictionaryAsync(string dictKey)
        {
            return await Profiler.Profile(async () => (await Db.HashGetAllAsync(dictKey)).ToStringDictionary(), dictKey);
        }

        IList<string> IDictionaryStoreProvider.Keys(string dictKey)
        {
            return Profiler.Profile(() => Db.HashKeys(dictKey).ToStringArray(), dictKey);
        }

        async Task<IList<string>> IDictionaryStoreProvider.KeysAsync(string dictKey)
        {
            return await Profiler.Profile(async () => (await Db.HashKeysAsync(dictKey)).ToStringArray(), dictKey);
        }

        IList<string> IDictionaryStoreProvider.Values(string dictKey)
        {
            return Profiler.Profile(() => Db.HashValues(dictKey).ToStringArray(), dictKey);
        }

        async Task<IList<string>> IDictionaryStoreProvider.ValuesAsync(string dictKey)
        {
            return await Profiler.Profile(async () => (await Db.HashValuesAsync(dictKey)).ToStringArray(), dictKey);
        }

        long IDictionaryStoreProvider.Increment(string dictKey, string itemKey, long value)
        {
            return Profiler.Profile(() => Db.HashIncrement(dictKey, itemKey, value), () => dictKey + "=>" + itemKey);
        }

        async Task<long> IDictionaryStoreProvider.IncrementAsync(string dictKey, string itemKey, long value)
        {
            return await Profiler.Profile(() => Db.HashIncrementAsync(dictKey, itemKey, value), () => dictKey + "=>" + itemKey);
        }

        long IDictionaryStoreProvider.SizeInBytes(string dictKey, string itemKey)
        {
            //todo: Redis 3.2'de bu özellik var, StackExchange'de yok.
            return Profiler.Profile(() => Encoding.ASCII.GetByteCount(Db.HashGet(dictKey, itemKey)), () => dictKey + "=>" + itemKey);
        }

        async Task<long> IDictionaryStoreProvider.SizeInBytesAsync(string dictKey, string itemKey)
        {
            //todo: Redis 3.2'de bu özellik var, StackExchange'de yok.
            return await Profiler.Profile(async () => Encoding.ASCII.GetByteCount(await Db.HashGetAsync(dictKey, itemKey)), () => dictKey + "=>" + itemKey);
        }
    }
}