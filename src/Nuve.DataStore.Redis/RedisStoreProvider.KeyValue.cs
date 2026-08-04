using StackExchange.Redis;

namespace Nuve.DataStore.Redis;

public partial class RedisStoreProvider : IKeyValueStoreProvider
{
    byte[] IKeyValueStoreProvider.Get(string key)
    {
        return RedisCall(Db =>
        {
            return Db.StringGet(key);
        })!;
    }

    async Task<byte[]> IKeyValueStoreProvider.GetAsync(string key)
    {
        return (await RedisCallAsync(async Db =>
        {
            return await Db.StringGetAsync(key);
        }))!;
    }

    IDictionary<string, byte[]> IKeyValueStoreProvider.GetAll(params string[] keys)
    {
        return RedisCall(Db =>
        {
            var result = new Dictionary<string, byte[]>(keys.Length);

            foreach (var batch in BatchKeysPerRequest(Db, keys))
            {
                var values = Db.StringGet(batch.Select(item => (RedisKey)item).ToArray());
                CollectValues(result, batch, values);
            }

            return result;
        })!;
    }

    async Task<IDictionary<string, byte[]>> IKeyValueStoreProvider.GetAllAsync(params string[] keys)
    {
        return (await RedisCallAsync(async Db =>
        {
            var result = new Dictionary<string, byte[]>(keys.Length);

            foreach (var batch in BatchKeysPerRequest(Db, keys))
            {
                var values = await Db.StringGetAsync(batch.Select(item => (RedisKey)item).ToArray()).ConfigureAwait(false);
                CollectValues(result, batch, values);
            }

            return result;
        }))!;
    }

    private static void CollectValues(Dictionary<string, byte[]> result, string[] keys, RedisValue[] values)
    {
        for (var index = 0; index < keys.Length; index++)
        {
            result[keys[index]] = (byte[])values[index]!;
        }
    }

    /// <summary>
    /// Splits the keys into the batches that may travel in one command. A server that accepts a
    /// cross-slot request takes them all at once; otherwise each slot is requested on its own.
    /// </summary>
    private IEnumerable<string[]> BatchKeysPerRequest(IDatabase database, string[] keys)
    {
        if (keys.Length <= 1 || EnsureCapabilities(database.Multiplexer).SupportsCrossSlotMultiKey)
            return [keys];

        return keys.GroupBy(RedisHashSlot.Of).Select(group => group.ToArray());
    }

    /// <summary>
    /// Splits the pairs into the batches that may travel in one command. A server that accepts a
    /// cross-slot write takes them all at once, keeping the write atomic; otherwise the write is
    /// atomic per slot but no longer as a whole, which is what makes it work on a sharded deployment.
    /// </summary>
    private IEnumerable<KeyValuePair<RedisKey, RedisValue>[]> BatchPairsPerRequest(
        IDatabase database, IDictionary<string, byte[]> keyValues)
    {
        var pairs = keyValues
            .Select(kv => new KeyValuePair<RedisKey, RedisValue>(kv.Key, kv.Value))
            .ToArray();

        if (pairs.Length <= 1 || EnsureCapabilities(database.Multiplexer).SupportsCrossSlotMultiKey)
            return [pairs];

        return pairs.GroupBy(pair => RedisHashSlot.Of(pair.Key!)).Select(group => group.ToArray());
    }

    bool IKeyValueStoreProvider.Set(string key, byte[] entity, bool overwrite, TimeSpan? expire, bool onlyWhenMissing)
    {
        if (expire == null || !onlyWhenMissing)
        {
            return RedisCall(Db =>
            {
                return Db.StringSet(key, entity, expiry: expire, when: overwrite ? When.Always : When.NotExists);
            });
        }

        return RedisCall(Db => AsValueWritten(
            Db.ScriptEvaluate(LuaCommands.SetKeepingTtl, [key], SetKeepingTtlArgs(entity, expire.Value, overwrite))));
    }

    async Task<bool> IKeyValueStoreProvider.SetAsync(string key, byte[] entity, bool overwrite, TimeSpan? expire, bool onlyWhenMissing)
    {
        if (expire == null || !onlyWhenMissing)
        {
            return (await RedisCallAsync(async Db => { return await Db.StringSetAsync(key, entity, expiry: expire, when: overwrite ? When.Always : When.NotExists); }))!;
        }

        return await RedisCallAsync(async Db => AsValueWritten(
            await Db.ScriptEvaluateAsync(LuaCommands.SetKeepingTtl, [key], SetKeepingTtlArgs(entity, expire.Value, overwrite))
                .ConfigureAwait(false))).ConfigureAwait(false);
    }

    private static RedisValue[] SetKeepingTtlArgs(byte[] entity, TimeSpan expire, bool overwrite)
    {
        return [entity, (long)expire.TotalMilliseconds, overwrite ? "0" : "1"];
    }

    private static bool AsValueWritten(RedisResult result)
    {
        return !result.IsNull && (long)result == 1;
    }

    bool IKeyValueStoreProvider.SetAll(IDictionary<string, byte[]> keyValues, bool overwrite)
    {
        var when = overwrite ? When.Always : When.NotExists;

        return RedisCall(Db =>
        {
            var applied = true;

            foreach (var batch in BatchPairsPerRequest(Db, keyValues))
            {
                applied &= Db.StringSet(batch, when);
            }

            return applied;
        });
    }

    async Task<bool> IKeyValueStoreProvider.SetAllAsync(IDictionary<string, byte[]> keyValues, bool overwrite)
    {
        var when = overwrite ? When.Always : When.NotExists;

        return await RedisCallAsync(async Db =>
        {
            var applied = true;

            foreach (var batch in BatchPairsPerRequest(Db, keyValues))
            {
                applied &= await Db.StringSetAsync(batch, when).ConfigureAwait(false);
            }

            return applied;
        });
    }

    byte[] IKeyValueStoreProvider.Exchange(string key, byte[] value)
    {
        return RedisCall(Db =>
        {
            return Db.StringGetSet(key, value);
        })!;
    }

    async Task<byte[]> IKeyValueStoreProvider.ExchangeAsync(string key, byte[] value)
    {
        return (await RedisCallAsync(async Db =>
        {
            return await Db.StringGetSetAsync(key, value);
        }))!;
    }

    long IKeyValueStoreProvider.AppendString(string key, string value)
    {
        return RedisCall(Db =>
        {
            return Db.StringAppend(key, value);
        });
    }

    async Task<long> IKeyValueStoreProvider.AppendStringAsync(string key, string value)
    {
        return (await RedisCallAsync(async Db => { return await Db.StringAppendAsync(key, value); }))!;
    }

    string IKeyValueStoreProvider.SubString(string key, long start, long end)
    {
        return RedisCall(Db =>
        {
            return Db.StringGetRange(key, start, end);
        })!;
    }

    async Task<string> IKeyValueStoreProvider.SubStringAsync(string key, long start, long end)
    {
        return (await RedisCallAsync(async Db =>
        {
            return await Db.StringGetRangeAsync(key, start, end);
        }))!;
    }

    long IKeyValueStoreProvider.OverwriteString(string key, long offset, string value)
    {
        return RedisCall(Db =>
        {
            return (long)Db.StringSetRange(key, offset, value);
        });
    }

    async Task<long> IKeyValueStoreProvider.OverwriteStringAsync(string key, long offset, string value)
    {
        return await RedisCallAsync(async Db =>
        {
            return (long)await Db.StringSetRangeAsync(key, offset, value);
        });
    }

    long IKeyValueStoreProvider.SizeInBytes(string key)
    {
        return RedisCall(Db =>
        {
            return Db.StringLength(key);
        });
    }

    async Task<long> IKeyValueStoreProvider.SizeInBytesAsync(string key)
    {
        return (await RedisCallAsync(async Db => { return await Db.StringLengthAsync(key); }))!;
    }

    bool IKeyValueStoreProvider.Contains(string key)
    {
        return RedisCall(Db =>
        {
            return Db.KeyExists(key);
        });
    }

    async Task<bool> IKeyValueStoreProvider.ContainsAsync(string key)
    {
        return (await RedisCallAsync(async Db => { return await Db.KeyExistsAsync(key); }))!;
    }

    bool IKeyValueStoreProvider.Rename(string oldKey, string newKey)
    {
        return RedisCall(Db =>
        {
            return Db.KeyRename(oldKey, newKey);
        });
    }

    async Task<bool> IKeyValueStoreProvider.RenameAsync(string oldKey, string newKey)
    {
        return await RedisCallAsync(async Db =>
        {
            return await Db.KeyRenameAsync(oldKey, newKey);
        });
    }

    long IKeyValueStoreProvider.Increment(string key, long amount)
    {
        return RedisCall(Db =>
        {
            return Db.StringIncrement(key, amount);
        });
    }

    async Task<long> IKeyValueStoreProvider.IncrementAsync(string key, long amount)
    {
        return (await RedisCallAsync(async Db => { return await Db.StringIncrementAsync(key, amount); }))!;
    }

    long IKeyValueStoreProvider.Decrement(string key, long amount)
    {
        return RedisCall(Db =>
        {
            return Db.StringDecrement(key, amount);
        });
    }

    async Task<long> IKeyValueStoreProvider.DecrementAsync(string key, long amount)
    {
        return (await RedisCallAsync(async Db => { return await Db.StringDecrementAsync(key, amount); }))!;
    }

    long IKeyValueStoreProvider.Count(string pattern)
    {
        return RedisCall(Db =>
        {
            var result = Db.ScriptEvaluateReadOnly(@"return #redis.call('keys', KEYS[1])", keys: [pattern]);
            return result.IsNull ? 0 : (int)result;
        });
    }
    async Task<long> IKeyValueStoreProvider.CountAsync(string pattern)
    {
        return await RedisCallAsync(async Db =>
        {
            var result = await Db.ScriptEvaluateReadOnlyAsync(@"return #redis.call('keys', KEYS[1])", keys: [pattern]);
            return result.IsNull ? 0 : (int)result;
        });
    }
}
