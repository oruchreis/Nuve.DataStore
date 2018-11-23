using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Nuve.DataStore.Redis
{
    public partial class RedisStoreProvider : IKeyValueStoreProvider
    {
        byte[] IKeyValueStoreProvider.Get(string key)
        {
            return Db.StringGet(key);
        }

        async Task<byte[]> IKeyValueStoreProvider.GetAsync(string key)
        {
            return await Db.StringGetAsync(key);
        }

        IDictionary<string, byte[]> IKeyValueStoreProvider.GetAll(params string[] keys)
        {
            var values = Db.StringGet(keys.Select(item => (RedisKey)item).ToArray());
            return keys.Zip(values, (k, v) => new { k, v }).ToDictionary(kv => kv.k, kv => (byte[])kv.v);
        }

        async Task<IDictionary<string, byte[]>> IKeyValueStoreProvider.GetAllAsync(params string[] keys)
        {
            var values = await Db.StringGetAsync(keys.Select(item => (RedisKey)item).ToArray());
            return keys.Zip(values, (k, v) => new { k, v }).ToDictionary(kv => kv.k, kv => (byte[])kv.v);
        }

        bool IKeyValueStoreProvider.Set(string key, byte[] entity, bool overwrite)
        {
            return Db.StringSet(key, entity, when: overwrite ? When.Always : When.NotExists);
        }

        async Task<bool> IKeyValueStoreProvider.SetAsync(string key, byte[] entity, bool overwrite)
        {
            return await Db.StringSetAsync(key, entity, when: overwrite ? When.Always : When.NotExists);
        }

        bool IKeyValueStoreProvider.SetAll(IDictionary<string, byte[]> keyValues, bool overwrite)
        {
            return Db.StringSet(keyValues.Select(kv => new KeyValuePair<RedisKey, RedisValue>(kv.Key, kv.Value)).ToArray(),
                overwrite ? When.Always : When.NotExists);
        }

        async Task<bool> IKeyValueStoreProvider.SetAllAsync(IDictionary<string, byte[]> keyValues, bool overwrite)
        {
            return await Db.StringSetAsync(keyValues.Select(kv => new KeyValuePair<RedisKey, RedisValue>(kv.Key, kv.Value)).ToArray(),
                overwrite ? When.Always : When.NotExists);
        }

        byte[] IKeyValueStoreProvider.Exchange(string key, byte[] value)
        {
            return Db.StringGetSet(key, value);
        }

        async Task<byte[]> IKeyValueStoreProvider.ExchangeAsync(string key, byte[] value)
        {
            return await Db.StringGetSetAsync(key, value);
        }

        long IKeyValueStoreProvider.AppendString(string key, string value)
        {
            return Db.StringAppend(key, value);
        }

        async Task<long> IKeyValueStoreProvider.AppendStringAsync(string key, string value)
        {
            return await Db.StringAppendAsync(key, value);
        }

        string IKeyValueStoreProvider.SubString(string key, long start, long end)
        {
            return Db.StringGetRange(key, start, end);
        }

        async Task<string> IKeyValueStoreProvider.SubStringAsync(string key, long start, long end)
        {
            return await Db.StringGetRangeAsync(key, start, end);
        }

        long IKeyValueStoreProvider.OverwriteString(string key, long offset, string value)
        {
            return (long)Db.StringSetRange(key, offset, value);
        }

        async Task<long> IKeyValueStoreProvider.OverwriteStringAsync(string key, long offset, string value)
        {
            return (long)await Db.StringSetRangeAsync(key, offset, value);
        }

        long IKeyValueStoreProvider.SizeInBytes(string key)
        {
            return Db.StringLength(key);
        }

        async Task<long> IKeyValueStoreProvider.SizeInBytesAsync(string key)
        {
            return await Db.StringLengthAsync(key);
        }

        bool IKeyValueStoreProvider.Contains(string key)
        {
            return Db.KeyExists(key);
        }

        async Task<bool> IKeyValueStoreProvider.ContainsAsync(string key)
        {
            return await Db.KeyExistsAsync(key);
        }

        bool IKeyValueStoreProvider.Rename(string oldKey, string newKey)
        {
            return Db.KeyRename(oldKey, newKey);
        }

        async Task<bool> IKeyValueStoreProvider.RenameAsync(string oldKey, string newKey)
        {
            return await Db.KeyRenameAsync(oldKey, newKey);
        }

        long IKeyValueStoreProvider.Increment(string key, long amount)
        {
            return Db.StringIncrement(key, amount);
            ;
        }

        async Task<long> IKeyValueStoreProvider.IncrementAsync(string key, long amount)
        {
            return await Db.StringIncrementAsync(key, amount);
        }

        long IKeyValueStoreProvider.Decrement(string key, long amount)
        {
            return Db.StringDecrement(key, amount);
        }

        async Task<long> IKeyValueStoreProvider.DecrementAsync(string key, long amount)
        {
            return await Db.StringDecrementAsync(key, amount);
        }
    }
}