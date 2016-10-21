using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Nuve.DataStore.Redis
{
    public partial class RedisStoreProvider : ILinkedListStoreProvider
    {
        bool ILinkedListStoreProvider.IsExists(string listKey)
        {
            return Profiler.Profile(() => Db.KeyExists(listKey), listKey);
        }

        async Task<bool> ILinkedListStoreProvider.IsExistsAsync(string listKey)
        {
            return await Profiler.Profile(() => Db.KeyExistsAsync(listKey), listKey);
        }

        string ILinkedListStoreProvider.Get(string listKey, long index)
        {
            return Profiler.Profile(() => Db.ListGetByIndex(listKey, index), listKey);
        }

        async Task<string> ILinkedListStoreProvider.GetAsync(string listKey, long index)
        {
            return await Profiler.Profile(() => Db.ListGetByIndexAsync(listKey, index), listKey);
        }

        IList<string> ILinkedListStoreProvider.GetRange(string listKey, long start, long end)
        {
            return Profiler.Profile(() => Db.ListRange(listKey, start, end).ToStringArray(), listKey);
        }

        async Task<IList<string>> ILinkedListStoreProvider.GetRangeAsync(string listKey, long start, long end)
        {
            return await Profiler.Profile(async () => (await Db.ListRangeAsync(listKey, start, end)).ToStringArray(), listKey);
        }

        void ILinkedListStoreProvider.Set(string listKey, long index, string value)
        {
            Profiler.Profile(() => Db.ListSetByIndex(listKey, index, value), listKey);
        }

        async Task ILinkedListStoreProvider.SetAsync(string listKey, long index, string value)
        {
            await Profiler.Profile(() => Db.ListSetByIndexAsync(listKey, index, value), listKey);
        }

        long ILinkedListStoreProvider.AddFirst(string listKey, params string[] value)
        {
            return Profiler.Profile(() => Db.ListLeftPush(listKey, Array.ConvertAll(value, item => (RedisValue) item)), listKey);
        }

        async Task<long> ILinkedListStoreProvider.AddFirstAsync(string listKey, params string[] value)
        {
            return await Profiler.Profile(() => Db.ListLeftPushAsync(listKey, Array.ConvertAll(value, item => (RedisValue) item)), listKey);
        }

        long ILinkedListStoreProvider.AddLast(string listKey, params string[] value)
        {
            return Profiler.Profile(() => Db.ListRightPush(listKey, Array.ConvertAll(value, item => (RedisValue) item)), listKey);
        }

        async Task<long> ILinkedListStoreProvider.AddLastAsync(string listKey, params string[] value)
        {
            return await Profiler.Profile(() => Db.ListRightPushAsync(listKey, Array.ConvertAll(value, item => (RedisValue) item)), listKey);
        }

        long ILinkedListStoreProvider.AddAfter(string listKey, string pivot, string value)
        {
            return Profiler.Profile(() => Db.ListInsertAfter(listKey, pivot, value), () => listKey + "|" + pivot);
        }

        async Task<long> ILinkedListStoreProvider.AddAfterAsync(string listKey, string pivot, string value)
        {
            return await Profiler.Profile(() => Db.ListInsertAfterAsync(listKey, pivot, value), () => listKey + "|" + pivot);
        }

        long ILinkedListStoreProvider.AddBefore(string listKey, string pivot, string value)
        {
            return Profiler.Profile(() => Db.ListInsertBefore(listKey, pivot, value), () => listKey + "|" + pivot);
        }

        async Task<long> ILinkedListStoreProvider.AddBeforeAsync(string listKey, string pivot, string value)
        {
            return await Profiler.Profile(() => Db.ListInsertBeforeAsync(listKey, pivot, value), () => listKey + "|" + pivot);
        }

        long ILinkedListStoreProvider.Count(string listKey)
        {
            return Profiler.Profile(() => Db.ListLength(listKey), listKey);
        }

        async Task<long> ILinkedListStoreProvider.CountAsync(string listKey)
        {
            return await Profiler.Profile(() => Db.ListLengthAsync(listKey), listKey);
        }

        string ILinkedListStoreProvider.RemoveFirst(string listKey)
        {
            return Profiler.Profile(() => Db.ListLeftPop(listKey), listKey);
        }

        async Task<string> ILinkedListStoreProvider.RemoveFirstAsync(string listKey)
        {
            return await Profiler.Profile(() => Db.ListLeftPopAsync(listKey), listKey);
        }

        string ILinkedListStoreProvider.RemoveLast(string listKey)
        {
            return Profiler.Profile(() => Db.ListRightPop(listKey), listKey);
        }

        async Task<string> ILinkedListStoreProvider.RemoveLastAsync(string listKey)
        {
            return await Profiler.Profile(() => Db.ListRightPopAsync(listKey), listKey);
        }

        long ILinkedListStoreProvider.Remove(string listKey, string value)
        {
            return Profiler.Profile(() => Db.ListRemove(listKey, value), listKey);
        }

        async Task<long> ILinkedListStoreProvider.RemoveAsync(string listKey, string value)
        {
            return await Profiler.Profile(() => Db.ListRemoveAsync(listKey, value), listKey);
        }

        void ILinkedListStoreProvider.Trim(string listKey, long start, long end)
        {
            Profiler.Profile(() => Db.ListTrim(listKey, start, end), listKey);
        }

        async Task ILinkedListStoreProvider.TrimAsync(string listKey, long start, long end)
        {
            await Profiler.Profile(() => Db.ListTrimAsync(listKey, start, end), listKey);
        }
    }
}