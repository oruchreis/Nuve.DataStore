using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StackExchange.Redis;
using System.Linq;

namespace Nuve.DataStore.Redis
{
    public partial class RedisStoreProvider : ILinkedListStoreProvider
    {
        bool ILinkedListStoreProvider.IsExists(string listKey)
        {
            return Db.KeyExists(listKey);
        }

        async Task<bool> ILinkedListStoreProvider.IsExistsAsync(string listKey)
        {
            return await Db.KeyExistsAsync(listKey);
        }

        byte[] ILinkedListStoreProvider.Get(string listKey, long index)
        {
            return Db.ListGetByIndex(listKey, index);
        }

        async Task<byte[]> ILinkedListStoreProvider.GetAsync(string listKey, long index)
        {
            return await Db.ListGetByIndexAsync(listKey, index);
        }

        IList<byte[]> ILinkedListStoreProvider.GetRange(string listKey, long start, long end)
        {
            return Db.ListRange(listKey, start, end).Select(rv => (byte[])rv).ToList();
        }

        async Task<IList<byte[]>> ILinkedListStoreProvider.GetRangeAsync(string listKey, long start, long end)
        {
            return (await Db.ListRangeAsync(listKey, start, end)).Select(rv => (byte[])rv).ToList();
        }

        void ILinkedListStoreProvider.Set(string listKey, long index, byte[] value)
        {
            Db.ListSetByIndex(listKey, index, value);
        }

        async Task ILinkedListStoreProvider.SetAsync(string listKey, long index, byte[] value)
        {
            await Db.ListSetByIndexAsync(listKey, index, value);
        }

        long ILinkedListStoreProvider.AddFirst(string listKey, params byte[][] value)
        {
            return Db.ListLeftPush(listKey, value.Select(item => (RedisValue) item).ToArray());
        }

        async Task<long> ILinkedListStoreProvider.AddFirstAsync(string listKey, params byte[][] value)
        {
            return await Db.ListLeftPushAsync(listKey, value.Select(item => (RedisValue) item).ToArray());
        }

        long ILinkedListStoreProvider.AddLast(string listKey, params byte[][] value)
        {
            return Db.ListRightPush(listKey, value.Select(item => (RedisValue) item).ToArray());
        }

        async Task<long> ILinkedListStoreProvider.AddLastAsync(string listKey, params byte[][] value)
        {
            return await Db.ListRightPushAsync(listKey, value.Select(item => (RedisValue) item).ToArray());
        }

        long ILinkedListStoreProvider.AddAfter(string listKey, byte[] pivot, byte[] value)
        {
            return Db.ListInsertAfter(listKey, pivot, value);
        }

        async Task<long> ILinkedListStoreProvider.AddAfterAsync(string listKey, byte[] pivot, byte[] value)
        {
            return await Db.ListInsertAfterAsync(listKey, pivot, value);
        }

        long ILinkedListStoreProvider.AddBefore(string listKey, byte[] pivot, byte[] value)
        {
            return Db.ListInsertBefore(listKey, pivot, value);
        }

        async Task<long> ILinkedListStoreProvider.AddBeforeAsync(string listKey, byte[] pivot, byte[] value)
        {
            return await Db.ListInsertBeforeAsync(listKey, pivot, value);
        }

        long ILinkedListStoreProvider.Count(string listKey)
        {
            return Db.ListLength(listKey);
        }

        async Task<long> ILinkedListStoreProvider.CountAsync(string listKey)
        {
            return await Db.ListLengthAsync(listKey);
        }

        byte[] ILinkedListStoreProvider.RemoveFirst(string listKey)
        {
            return Db.ListLeftPop(listKey);
        }

        async Task<byte[]> ILinkedListStoreProvider.RemoveFirstAsync(string listKey)
        {
            return await Db.ListLeftPopAsync(listKey);
        }

        byte[] ILinkedListStoreProvider.RemoveLast(string listKey)
        {
            return Db.ListRightPop(listKey);
        }

        async Task<byte[]> ILinkedListStoreProvider.RemoveLastAsync(string listKey)
        {
            return await Db.ListRightPopAsync(listKey);
        }

        long ILinkedListStoreProvider.Remove(string listKey, byte[] value)
        {
            return Db.ListRemove(listKey, value);
        }

        async Task<long> ILinkedListStoreProvider.RemoveAsync(string listKey, byte[] value)
        {
            return await Db.ListRemoveAsync(listKey, value);
        }

        void ILinkedListStoreProvider.Trim(string listKey, long start, long end)
        {
            Db.ListTrim(listKey, start, end);
        }

        async Task ILinkedListStoreProvider.TrimAsync(string listKey, long start, long end)
        {
            await Db.ListTrimAsync(listKey, start, end);
        }
    }
}