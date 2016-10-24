using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Nuve.DataStore.Redis
{
    public partial class RedisStoreProvider : IKeyValueStoreProvider
    {
        string IKeyValueStoreProvider.Get(string key)
        {
            return Profiler.Profile(()=> Db.StringGet(key), key);
        }

        async Task<string> IKeyValueStoreProvider.GetAsync(string key)
        {
            return await Profiler.Profile(() => Db.StringGetAsync(key), key);
        }

        IDictionary<string, string> IKeyValueStoreProvider.GetAll(params string[] keys)
        {
            return Profiler.Profile(() =>
                                    {
                                        var values = Db.StringGet(keys.Select(item => (RedisKey) item).ToArray());
                                        return keys.Zip(values, (k, v) => new {k, v}).ToDictionary(kv => kv.k, kv => (string) kv.v);
                                    }, string.Join(",", keys));
        }

        async Task<IDictionary<string, string>> IKeyValueStoreProvider.GetAllAsync(params string[] keys)
        {
            return await Profiler.Profile(async () =>
                                                {
                                                    var values = await Db.StringGetAsync(keys.Select(item => (RedisKey) item).ToArray());
                                                    return keys.Zip(values, (k, v) => new {k, v}).ToDictionary(kv => kv.k, kv => (string) kv.v);
                                                }, string.Join(",", keys));
        }

        bool IKeyValueStoreProvider.Set(string key, string entity, bool overwrite)
        {
            return Profiler.Profile(() => Db.StringSet(key, entity, when: overwrite ? When.Always : When.NotExists), key);
        }

        async Task<bool> IKeyValueStoreProvider.SetAsync(string key, string entity, bool overwrite)
        {
            return await Profiler.Profile(() => Db.StringSetAsync(key, entity, when: overwrite ? When.Always : When.NotExists), key);
        }

        bool IKeyValueStoreProvider.SetAll(IDictionary<string, string> keyValues, bool overwrite)
        {
            return Profiler.Profile(() => Db.StringSet(keyValues.Select(kv => new KeyValuePair<RedisKey, RedisValue>(kv.Key, kv.Value)).ToArray(),
                overwrite ? When.Always : When.NotExists), () => string.Join(",", keyValues.Keys));
        }

        async Task<bool> IKeyValueStoreProvider.SetAllAsync(IDictionary<string, string> keyValues, bool overwrite)
        {
            return await Profiler.Profile(()=> Db.StringSetAsync(keyValues.Select(kv => new KeyValuePair<RedisKey, RedisValue>(kv.Key, kv.Value)).ToArray(),
                overwrite ? When.Always : When.NotExists), () => string.Join(",", keyValues.Keys));
        }

        string IKeyValueStoreProvider.Exchange(string key, string value)
        {
            return Profiler.Profile(()=> Db.StringGetSet(key, value), key);
        }

        async Task<string> IKeyValueStoreProvider.ExchangeAsync(string key, string value)
        {
            return await Profiler.Profile(() => Db.StringGetSetAsync(key, value), key);
        }

        long IKeyValueStoreProvider.AppendString(string key, string value)
        {
            return Profiler.Profile(()=> Db.StringAppend(key, value), key);
        }

        async Task<long> IKeyValueStoreProvider.AppendStringAsync(string key, string value)
        {
            return await Profiler.Profile(() => Db.StringAppendAsync(key, value), key);
        }

        string IKeyValueStoreProvider.SubString(string key, long start, long end)
        {
            return Profiler.Profile(()=> Db.StringGetRange(key, start, end), key);
        }

        async Task<string> IKeyValueStoreProvider.SubStringAsync(string key, long start, long end)
        {
            return await Profiler.Profile(() => Db.StringGetRangeAsync(key, start, end), key);
        }

        long IKeyValueStoreProvider.OverwriteString(string key, long offset, string value)
        {
            return Profiler.Profile(() => (long) Db.StringSetRange(key, offset, value), key);
        }

        async Task<long> IKeyValueStoreProvider.OverwriteStringAsync(string key, long offset, string value)
        {
            return (long) await Profiler.Profile(() => Db.StringSetRangeAsync(key, offset, value), key);
        }

        long IKeyValueStoreProvider.SizeInBytes(string key)
        {
            return Profiler.Profile(()=> Db.StringLength(key), key);
        }

        async Task<long> IKeyValueStoreProvider.SizeInBytesAsync(string key)
        {
            return await Profiler.Profile(() => Db.StringLengthAsync(key), key);
        }

        bool IKeyValueStoreProvider.Contains(string key)
        {
            return Profiler.Profile(() => Db.KeyExists(key), key);
        }

        async Task<bool> IKeyValueStoreProvider.ContainsAsync(string key)
        {
            return await Profiler.Profile(() => Db.KeyExistsAsync(key), key);
        }

        bool IKeyValueStoreProvider.Rename(string oldKey, string newKey)
        {
            return Profiler.Profile(() => Db.KeyRename(oldKey, newKey), ()=> oldKey + "|" + newKey);
        }

        async Task<bool> IKeyValueStoreProvider.RenameAsync(string oldKey, string newKey)
        {
            return await Profiler.Profile(() => Db.KeyRenameAsync(oldKey, newKey), () => oldKey + "|" + newKey);
        }

        long IKeyValueStoreProvider.Increment(string key, long amount)
        {
            return Profiler.Profile(() => Db.StringIncrement(key, amount), key);
            ;
        }

        async Task<long> IKeyValueStoreProvider.IncrementAsync(string key, long amount)
        {
            return await Profiler.Profile(() => Db.StringIncrementAsync(key, amount), key);
        }

        long IKeyValueStoreProvider.Decrement(string key, long amount)
        {
            return Profiler.Profile(() => Db.StringDecrement(key, amount), key);
        }

        async Task<long> IKeyValueStoreProvider.DecrementAsync(string key, long amount)
        {
            return await Profiler.Profile(() => Db.StringDecrementAsync(key, amount), key);
        }
    }
}