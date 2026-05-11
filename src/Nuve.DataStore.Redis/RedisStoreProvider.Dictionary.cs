using StackExchange.Redis;

namespace Nuve.DataStore.Redis;

public partial class RedisStoreProvider : IDictionaryStoreProvider
{
    bool IDictionaryStoreProvider.IsExists(string dictKey)
    {
        return RedisCall(Db =>
        {
            return Db.KeyExists(dictKey);
        });
    }

    async Task<bool> IDictionaryStoreProvider.IsExistsAsync(string dictKey)
    {
        return await RedisCallAsync(async Db =>
        {
            return await Db.KeyExistsAsync(dictKey);
        });
    }

    byte[] IDictionaryStoreProvider.Get(string dictKey, string itemKey)
    {
        return RedisCall(Db =>
        {
            return Db.HashGet(dictKey, itemKey);
        })!;
    }

    async Task<byte[]> IDictionaryStoreProvider.GetAsync(string dictKey, string itemKey)
    {
        return (await RedisCallAsync(async Db =>
        {
            return await Db.HashGetAsync(dictKey, itemKey);
        }))!;
    }

    IDictionary<string, byte[]> IDictionaryStoreProvider.Get(string dictKey, params string[] itemKeys)
    {
        return RedisCall(Db =>
        {
            var values = Db.HashGet(dictKey, itemKeys.Select(item => (RedisValue)item).ToArray());
            return itemKeys.Zip(values, (k, v) => new { k, v }).ToDictionary(kv => kv.k, kv => (byte[])kv.v!);
        })!;
    }

    async Task<IDictionary<string, byte[]>> IDictionaryStoreProvider.GetAsync(string dictKey, params string[] itemKeys)
    {
        return (await RedisCallAsync(async Db =>
        {
            var values = await Db.HashGetAsync(dictKey, itemKeys.Select(item => (RedisValue)item).ToArray());
            return itemKeys.Zip(values, (k, v) => new { k, v }).ToDictionary(kv => kv.k, kv => (byte[])kv.v!);
        }))!;
    }

    bool IDictionaryStoreProvider.Set(string dictKey, string itemKey, byte[] itemValue, bool overwrite, TimeSpan? expire)
    {
        return RedisCall(Db =>
        {
            if (expire != null)
                return ExecuteTransaction(Db, tran =>
                    tran.HashSetAsync(dictKey, itemKey, itemValue, overwrite ? When.Always : When.NotExists), dictKey, expire);

            return Db.HashSet(dictKey, itemKey, itemValue, overwrite ? When.Always : When.NotExists);
        });
    }

    async Task<bool> IDictionaryStoreProvider.SetAsync(string dictKey, string itemKey, byte[] itemValue, bool overwrite, TimeSpan? expire)
    {
        return (await RedisCallAsync(async Db =>
        {
            if (expire != null)
                return await ExecuteTransactionAsync(Db, tran =>
                    tran.HashSetAsync(dictKey, itemKey, itemValue, overwrite ? When.Always : When.NotExists), dictKey, expire).ConfigureAwait(false);

            return await Db.HashSetAsync(dictKey, itemKey, itemValue, overwrite ? When.Always : When.NotExists).ConfigureAwait(false);
        }))!;
    }

    void IDictionaryStoreProvider.Set(string dictKey, IDictionary<string, byte[]> keyValues, TimeSpan? expire)
    {
        RedisCall(Db =>
        {
            var entries = keyValues.Select(kv => new HashEntry(kv.Key, kv.Value)).ToArray();
            if (expire != null)
            {
                ExecuteTransaction(Db, tran => tran.HashSetAsync(dictKey, entries), dictKey, expire);
                return;
            }

            Db.HashSet(dictKey, entries);
        });
    }

    async Task IDictionaryStoreProvider.SetAsync(string dictKey, IDictionary<string, byte[]> keyValues, TimeSpan? expire)
    {
        await RedisCallAsync(async Db =>
        {
            var entries = keyValues.Select(kv => new HashEntry(kv.Key, kv.Value)).ToArray();
            if (expire != null)
            {
                await ExecuteTransactionAsync(Db, tran => tran.HashSetAsync(dictKey, entries), dictKey, expire).ConfigureAwait(false);
                return;
            }

            await Db.HashSetAsync(dictKey, entries).ConfigureAwait(false);
        });
    }

    long IDictionaryStoreProvider.Remove(string dictKey, params string[] itemKeys)
    {
        return RedisCall(Db =>
        {
            return Db.HashDelete(dictKey, itemKeys.Select(item => (RedisValue)item).ToArray());
        });
    }

    async Task<long> IDictionaryStoreProvider.RemoveAsync(string dictKey, params string[] itemKeys)
    {
        return await RedisCallAsync(async Db =>
        {
            return await Db.HashDeleteAsync(dictKey, itemKeys.Select(item => (RedisValue)item).ToArray());
        });
    }

    bool IDictionaryStoreProvider.Contains(string dictKey, string itemKey)
    {
        return RedisCall(Db =>
        {
            return Db.HashExists(dictKey, itemKey);
        });
    }

    async Task<bool> IDictionaryStoreProvider.ContainsAsync(string dictKey, string itemKey)
    {
        return (await RedisCallAsync(async Db => { return await Db.HashExistsAsync(dictKey, itemKey); }))!;
    }

    long IDictionaryStoreProvider.Count(string dictKey)
    {
        return RedisCall(Db =>
        {
            return Db.HashLength(dictKey);
        });
    }

    async Task<long> IDictionaryStoreProvider.CountAsync(string dictKey)
    {
        return await RedisCallAsync(async Db =>
        {
            return await Db.HashLengthAsync(dictKey);
        });
    }

    IDictionary<string, byte[]> IDictionaryStoreProvider.GetDictionary(string dictKey)
    {
        return RedisCall(Db =>
        {
            return Db.HashGetAll(dictKey).ToDictionary(rv => (string)rv.Name!, rv => (byte[])rv.Value!);
        })!;
    }

    async Task<IDictionary<string, byte[]>> IDictionaryStoreProvider.GetDictionaryAsync(string dictKey)
    {
        return (await RedisCallAsync(async Db =>
        {
            return (await Db.HashGetAllAsync(dictKey)).ToDictionary(rv => (string)rv.Name!, rv => (byte[])rv.Value!);
        }))!;
    }

    IList<string> IDictionaryStoreProvider.Keys(string dictKey)
    {
        return RedisCall(Db =>
        {
            return Db.HashKeys(dictKey).ToStringArray();
        })!;
    }

    async Task<IList<string>> IDictionaryStoreProvider.KeysAsync(string dictKey)
    {
        return (await RedisCallAsync(async Db =>
        {
            return (await Db.HashKeysAsync(dictKey)).ToStringArray();
        }))!;
    }

    IList<byte[]> IDictionaryStoreProvider.Values(string dictKey)
    {
        return RedisCall(Db =>
        {
            return Db.HashValues(dictKey).Select(rv => (byte[])rv!).ToList();
        })!;
    }

    async Task<IList<byte[]>> IDictionaryStoreProvider.ValuesAsync(string dictKey)
    {
        return (await RedisCallAsync(async Db =>
        {
            return (await Db.HashValuesAsync(dictKey)).Select(rv => (byte[])rv!).ToList();
        }))!;
    }

    long IDictionaryStoreProvider.Increment(string dictKey, string itemKey, long value, TimeSpan? expire)
    {
        return RedisCall(Db =>
        {
            if (expire != null)
                return ExecuteTransaction(Db, tran => tran.HashIncrementAsync(dictKey, itemKey, value), dictKey, expire);

            return Db.HashIncrement(dictKey, itemKey, value);
        });
    }

    async Task<long> IDictionaryStoreProvider.IncrementAsync(string dictKey, string itemKey, long value, TimeSpan? expire)
    {
        return (await RedisCallAsync(async Db =>
        {
            if (expire != null)
                return await ExecuteTransactionAsync(Db, tran => tran.HashIncrementAsync(dictKey, itemKey, value), dictKey, expire).ConfigureAwait(false);

            return await Db.HashIncrementAsync(dictKey, itemKey, value).ConfigureAwait(false);
        }))!;
    }

    long IDictionaryStoreProvider.SizeInBytes(string dictKey, string itemKey)
    {
        return RedisCall(Db =>
        {
            var result = Db.Execute("HSTRLEN", dictKey, itemKey);
            return result.IsNull ? 0 : (long)result;
        });
    }

    async Task<long> IDictionaryStoreProvider.SizeInBytesAsync(string dictKey, string itemKey)
    {
        return await RedisCallAsync(async Db =>
        {
            var result = await Db.ExecuteAsync("HSTRLEN", dictKey, itemKey);
            return result.IsNull ? 0 : (long)result;
        });
    }

    async Task<long> IDictionaryStoreProvider.RenameKeyAsync(string dictKey, string oldKey, string newKey)
    {
        return await RedisCallAsync(async Db =>
        {
            var result = await Db.ScriptEvaluateAsync(@"
local value = redis.call('HGET', KEYS[1], ARGV[1])
redis.call('HSET', KEYS[1], ARGV[2], value)
return redis.call('HDEL', KEYS[1], ARGV[1])
", keys: new RedisKey[] { dictKey }, values: new RedisValue[] { oldKey, newKey });
            return result.IsNull ? 0 : (int)result;
        });
    }

    long IDictionaryStoreProvider.RenameKey(string dictKey, string oldKey, string newKey)
    {
        return RedisCall(Db =>
        {
            var result = Db.ScriptEvaluate(@"
local value = redis.call('HGET', KEYS[1], ARGV[1])
redis.call('HSET', KEYS[1], ARGV[2], value)
return redis.call('HDEL', KEYS[1], ARGV[1])
", keys: new RedisKey[] { dictKey }, values: new RedisValue[] { oldKey, newKey });
            return result.IsNull ? 0 : (int)result;
        });
    }
}
