using StackExchange.Redis;

namespace Nuve.DataStore.Redis;

public partial class RedisStoreProvider : ILinkedListStoreProvider
{
    bool ILinkedListStoreProvider.IsExists(string listKey)
    {
        return RedisCall(Db =>
        {
            return Db.KeyExists(listKey);
        });
    }

    async Task<bool> ILinkedListStoreProvider.IsExistsAsync(string listKey)
    {
        return (await RedisCallAsync(async Db => { return await Db.KeyExistsAsync(listKey); }))!;
    }

    byte[] ILinkedListStoreProvider.Get(string listKey, long index)
    {
        return RedisCall(Db =>
        {
            return Db.ListGetByIndex(listKey, index);
        })!;
    }

    async Task<byte[]> ILinkedListStoreProvider.GetAsync(string listKey, long index)
    {
        return (await RedisCallAsync(async Db =>
        {
            return await Db.ListGetByIndexAsync(listKey, index);
        }))!;
    }

    IList<byte[]> ILinkedListStoreProvider.GetRange(string listKey, long start, long end)
    {
        return RedisCall(Db =>
        {
            return Db.ListRange(listKey, start, end).Select(rv => (byte[])rv!).ToList();
        })!;
    }

    async Task<IList<byte[]>> ILinkedListStoreProvider.GetRangeAsync(string listKey, long start, long end)
    {
        return (await RedisCallAsync(async Db =>
        {
            return (await Db.ListRangeAsync(listKey, start, end)).Select(rv => (byte[])rv!).ToList();
        }))!;
    }

    void ILinkedListStoreProvider.Set(string listKey, long index, byte[] value)
    {
        RedisCall(Db =>
        {
            Db.ListSetByIndex(listKey, index, value);
        });
    }

    async Task ILinkedListStoreProvider.SetAsync(string listKey, long index, byte[] value)
    {
        await RedisCallAsync(async Db =>
        {
            await Db.ListSetByIndexAsync(listKey, index, value);
        });
    }

    long ILinkedListStoreProvider.AddFirst(string listKey, params byte[][] value)
    {
        return RedisCall(Db =>
        {
            return Db.ListLeftPush(listKey, value.Select(item => (RedisValue)item).ToArray());
        });
    }

    async Task<long> ILinkedListStoreProvider.AddFirstAsync(string listKey, params byte[][] value)
    {
        return (await RedisCallAsync(async Db => { return await Db.ListLeftPushAsync(listKey, value.Select(item => (RedisValue)item).ToArray()); }))!;
    }

    long ILinkedListStoreProvider.AddLast(string listKey, params byte[][] value)
    {
        return RedisCall(Db =>
        {
            return Db.ListRightPush(listKey, value.Select(item => (RedisValue)item).ToArray());
        });
    }

    async Task<long> ILinkedListStoreProvider.AddLastAsync(string listKey, params byte[][] value)
    {
        return (await RedisCallAsync(async Db => { return await Db.ListRightPushAsync(listKey, value.Select(item => (RedisValue)item).ToArray()); }))!;
    }

    long ILinkedListStoreProvider.AddAfter(string listKey, byte[] pivot, byte[] value)
    {
        return RedisCall(Db =>
        {
            return Db.ListInsertAfter(listKey, pivot, value);
        });
    }

    async Task<long> ILinkedListStoreProvider.AddAfterAsync(string listKey, byte[] pivot, byte[] value)
    {
        return (await RedisCallAsync(async Db => { return await Db.ListInsertAfterAsync(listKey, pivot, value); }))!;
    }

    long ILinkedListStoreProvider.AddBefore(string listKey, byte[] pivot, byte[] value)
    {
        return RedisCall(Db =>
        {
            return Db.ListInsertBefore(listKey, pivot, value);
        });
    }

    async Task<long> ILinkedListStoreProvider.AddBeforeAsync(string listKey, byte[] pivot, byte[] value)
    {
        return (await RedisCallAsync(async Db => { return await Db.ListInsertBeforeAsync(listKey, pivot, value); }))!;
    }

    long ILinkedListStoreProvider.Count(string listKey)
    {
        return RedisCall(Db =>
        {
            return Db.ListLength(listKey);
        });
    }

    async Task<long> ILinkedListStoreProvider.CountAsync(string listKey)
    {
        return (await RedisCallAsync(async Db => { return await Db.ListLengthAsync(listKey); }))!;
    }

    byte[] ILinkedListStoreProvider.RemoveFirst(string listKey)
    {
        return RedisCall(Db =>
        {
            return Db.ListLeftPop(listKey);
        })!;
    }

    async Task<byte[]> ILinkedListStoreProvider.RemoveFirstAsync(string listKey)
    {
        return (await RedisCallAsync(async Db => { return await Db.ListLeftPopAsync(listKey); }))!;
    }

    byte[] ILinkedListStoreProvider.RemoveLast(string listKey)
    {
        return RedisCall(Db =>
        {
            return Db.ListRightPop(listKey);
        })!;
    }

    async Task<byte[]> ILinkedListStoreProvider.RemoveLastAsync(string listKey)
    {
        return (await RedisCallAsync(async Db => { return await Db.ListRightPopAsync(listKey); }))!;
    }

    long ILinkedListStoreProvider.Remove(string listKey, byte[] value)
    {
        return RedisCall(Db =>
        {
            return Db.ListRemove(listKey, value);
        });
    }

    async Task<long> ILinkedListStoreProvider.RemoveAsync(string listKey, byte[] value)
    {
        return (await RedisCallAsync(async Db => { return await Db.ListRemoveAsync(listKey, value); }))!;
    }

    void ILinkedListStoreProvider.Trim(string listKey, long start, long end)
    {
        RedisCall(Db =>
        {
            Db.ListTrim(listKey, start, end);
        });
    }

    async Task ILinkedListStoreProvider.TrimAsync(string listKey, long start, long end)
    {
        await RedisCallAsync(async Db =>
        {
            await Db.ListTrimAsync(listKey, start, end);
        });
    }
}