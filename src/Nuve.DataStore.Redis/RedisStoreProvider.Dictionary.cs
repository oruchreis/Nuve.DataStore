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
            return Db.KeyExists(dictKey);
        }

        async Task<bool> IDictionaryStoreProvider.IsExistsAsync(string dictKey)
        {
            return await Db.KeyExistsAsync(dictKey);
        }

        byte[] IDictionaryStoreProvider.Get(string dictKey, string itemKey)
        {
            return Db.HashGet(dictKey, itemKey);
        }

        async Task<byte[]> IDictionaryStoreProvider.GetAsync(string dictKey, string itemKey)
        {
            return await Db.HashGetAsync(dictKey, itemKey);
        }

        IDictionary<string, byte[]> IDictionaryStoreProvider.Get(string dictKey, params string[] itemKeys)
        {
            var values = Db.HashGet(dictKey, itemKeys.Select(item => (RedisValue)item).ToArray());
            return itemKeys.Zip(values, (k, v) => new { k, v }).ToDictionary(kv => kv.k, kv => (byte[])kv.v);
        }

        async Task<IDictionary<string, byte[]>> IDictionaryStoreProvider.GetAsync(string dictKey, params string[] itemKeys)
        {
            var values = await Db.HashGetAsync(dictKey, itemKeys.Select(item => (RedisValue)item).ToArray());
            return itemKeys.Zip(values, (k, v) => new { k, v }).ToDictionary(kv => (string)kv.k, kv => (byte[])kv.v);
        }

        bool IDictionaryStoreProvider.Set(string dictKey, string itemKey, byte[] itemValue, bool overwrite)
        {
            return Db.HashSet(dictKey, itemKey, itemValue, overwrite ? When.Always : When.NotExists);
        }

        async Task<bool> IDictionaryStoreProvider.SetAsync(string dictKey, string itemKey, byte[] itemValue, bool overwrite)
        {
            return await Db.HashSetAsync(dictKey, itemKey, itemValue, overwrite ? When.Always : When.NotExists);
        }

        void IDictionaryStoreProvider.Set(string dictKey, IDictionary<string, byte[]> keyValues)
        {
            Db.HashSet(dictKey, keyValues.Select(kv => new HashEntry(kv.Key, kv.Value)).ToArray());
        }

        async Task IDictionaryStoreProvider.SetAsync(string dictKey, IDictionary<string, byte[]> keyValues)
        {
            await Db.HashSetAsync(dictKey, keyValues.Select(kv => new HashEntry(kv.Key, kv.Value)).ToArray());
        }

        long IDictionaryStoreProvider.Remove(string dictKey, params string[] itemKeys)
        {
            return Db.HashDelete(dictKey, itemKeys.Select(item => (RedisValue)item).ToArray());
        }

        async Task<long> IDictionaryStoreProvider.RemoveAsync(string dictKey, params string[] itemKeys)
        {
            return await Db.HashDeleteAsync(dictKey, itemKeys.Select(item => (RedisValue)item).ToArray());
        }

        bool IDictionaryStoreProvider.Contains(string dictKey, string itemKey)
        {
            return Db.HashExists(dictKey, itemKey);
        }

        async Task<bool> IDictionaryStoreProvider.ContainsAsync(string dictKey, string itemKey)
        {
            return await Db.HashExistsAsync(dictKey, itemKey);
        }

        long IDictionaryStoreProvider.Count(string dictKey)
        {
            return Db.HashLength(dictKey);
        }

        async Task<long> IDictionaryStoreProvider.CountAsync(string dictKey)
        {
            return await Db.HashLengthAsync(dictKey);
        }

        IDictionary<string, byte[]> IDictionaryStoreProvider.GetDictionary(string dictKey)
        {
            return Db.HashGetAll(dictKey).ToDictionary(rv => (string)rv.Name, rv => (byte[])rv.Value);
        }

        async Task<IDictionary<string, byte[]>> IDictionaryStoreProvider.GetDictionaryAsync(string dictKey)
        {
            return (await Db.HashGetAllAsync(dictKey)).ToDictionary(rv => (string)rv.Name, rv => (byte[])rv.Value);
        }

        IList<string> IDictionaryStoreProvider.Keys(string dictKey)
        {
            return Db.HashKeys(dictKey).ToStringArray();
        }

        async Task<IList<string>> IDictionaryStoreProvider.KeysAsync(string dictKey)
        {
            return (await Db.HashKeysAsync(dictKey)).ToStringArray();
        }

        IList<byte[]> IDictionaryStoreProvider.Values(string dictKey)
        {
            return Db.HashValues(dictKey).Select(rv => (byte[])rv).ToList();
        }

        async Task<IList<byte[]>> IDictionaryStoreProvider.ValuesAsync(string dictKey)
        {
            return (await Db.HashValuesAsync(dictKey)).Select(rv => (byte[])rv).ToList();
        }

        long IDictionaryStoreProvider.Increment(string dictKey, string itemKey, long value)
        {
            return Db.HashIncrement(dictKey, itemKey, value);
        }

        async Task<long> IDictionaryStoreProvider.IncrementAsync(string dictKey, string itemKey, long value)
        {
            return await Db.HashIncrementAsync(dictKey, itemKey, value);
        }

        long IDictionaryStoreProvider.SizeInBytes(string dictKey, string itemKey)
        {
            //todo: Redis 3.2'de bu özellik var, StackExchange'de yok.
            return Encoding.ASCII.GetByteCount(Db.HashGet(dictKey, itemKey));
        }

        async Task<long> IDictionaryStoreProvider.SizeInBytesAsync(string dictKey, string itemKey)
        {
            //todo: Redis 3.2'de bu özellik var, StackExchange'de yok.
            return Encoding.ASCII.GetByteCount(await Db.HashGetAsync(dictKey, itemKey));
        }

        async Task<long> IDictionaryStoreProvider.RenameKeyAsync(string dictKey, string oldKey, string newKey)
        {
            var value = await Db.HashGetAsync(dictKey, oldKey);
            return await Db.HashSetAsync(dictKey, newKey, value) && await Db.HashDeleteAsync(dictKey, oldKey) ? 1 : 0;
        }

        long IDictionaryStoreProvider.RenameKey(string dictKey, string oldKey, string newKey)
        {
            var value = Db.HashGet(dictKey, oldKey);
            return Db.HashSet(dictKey, newKey, value) && Db.HashDelete(dictKey, oldKey) ? 1 : 0;
        }
    }
}