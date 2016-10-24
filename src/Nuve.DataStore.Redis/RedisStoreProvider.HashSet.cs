using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Nuve.DataStore.Redis
{
    public partial class RedisStoreProvider : IHashSetStoreProvider
    {
        bool IHashSetStoreProvider.IsExists(string hashSetKey)
        {
            return Profiler.Profile(() =>Db.KeyExists(hashSetKey), hashSetKey);
        }

        async Task<bool> IHashSetStoreProvider.IsExistsAsync(string hashSetKey)
        {
            return await Profiler.Profile(() => Db.KeyExistsAsync(hashSetKey), hashSetKey);
        }

        long IHashSetStoreProvider.Add(string hashSetKey, params string[] values)
        {
            return Profiler.Profile(() => Db.SetAdd(hashSetKey, values.Select(item => (RedisValue) item).ToArray()), hashSetKey);
        }

        async Task<long> IHashSetStoreProvider.AddAsync(string hashSetKey, params string[] values)
        {
            return await Profiler.Profile(() => Db.SetAddAsync(hashSetKey, values.Select(item => (RedisValue) item).ToArray()), hashSetKey);
        }

        long IHashSetStoreProvider.Count(string hashSetKey)
        {
            return Profiler.Profile(() => Db.SetLength(hashSetKey), hashSetKey);
        }

        async Task<long> IHashSetStoreProvider.CountAsync(string hashSetKey)
        {
            return await Profiler.Profile(() => Db.SetLengthAsync(hashSetKey), hashSetKey);
        }

        HashSet<string> IHashSetStoreProvider.Difference(string hashSetKey, params string[] compareHashSetKeys)
        {
            return Profiler.Profile(() =>
                             {
                                 var keys = new List<RedisKey> {hashSetKey};
                                 keys.AddRange(compareHashSetKeys.Select(item => (RedisKey) item));
                                 return new HashSet<string>((Db.SetCombine(SetOperation.Difference, keys.ToArray())).ToStringArray());
                             }, () => hashSetKey + "|" + string.Join(",", compareHashSetKeys));
        }

        async Task<HashSet<string>> IHashSetStoreProvider.DifferenceAsync(string hashSetKey, params string[] compareHashSetKeys)
        {
            return await Profiler.Profile(async () =>
                                    {
                                        var keys = new List<RedisKey> {hashSetKey};
                                        keys.AddRange(compareHashSetKeys.Select(item => (RedisKey) item));
                                        return new HashSet<string>((await Db.SetCombineAsync(SetOperation.Difference, keys.ToArray())).ToStringArray());
                                    }, () => hashSetKey + "|" + string.Join(",", compareHashSetKeys));
        }

        long IHashSetStoreProvider.DifferenceToNewSet(string hashSetKey, string newHashSetKey, params string[] compareHashSetKeys)
        {
            return Profiler.Profile(() =>
                                    {
                                        var keys = new List<RedisKey> {hashSetKey};
                                        keys.AddRange(compareHashSetKeys.Select(item => (RedisKey) item));
                                        return Db.SetCombineAndStore(SetOperation.Difference, newHashSetKey, keys.ToArray());
                                    }, () => hashSetKey + "|" + newHashSetKey + "|" + string.Join(",", compareHashSetKeys));
        }

        async Task<long> IHashSetStoreProvider.DifferenceToNewSetAsync(string hashSetKey, string newHashSetKey, params string[] compareHashSetKeys)
        {
            return await Profiler.Profile(async () =>
                                                {
                                                    var keys = new List<RedisKey> {hashSetKey};
                                                    keys.AddRange(compareHashSetKeys.Select(item => (RedisKey) item));
                                                    return await Db.SetCombineAndStoreAsync(SetOperation.Difference, newHashSetKey, keys.ToArray());
                                                }, () => hashSetKey + "|" + newHashSetKey + "|" + string.Join(",", compareHashSetKeys));
        }

        HashSet<string> IHashSetStoreProvider.Intersection(string hashSetKey, params string[] compareHashSetKeys)
        {
            return Profiler.Profile(() =>
                                    {
                                        var keys = new List<RedisKey> {hashSetKey};
                                        keys.AddRange(compareHashSetKeys.Select(item => (RedisKey) item));
                                        return new HashSet<string>((Db.SetCombine(SetOperation.Intersect, keys.ToArray())).ToStringArray());
                                    }, () => hashSetKey + "|" + string.Join(",", compareHashSetKeys));
        }

        async Task<HashSet<string>> IHashSetStoreProvider.IntersectionAsync(string hashSetKey, params string[] compareHashSetKeys)
        {
            return await Profiler.Profile(async () =>
                                                {
                                                    var keys = new List<RedisKey> {hashSetKey};
                                                    keys.AddRange(compareHashSetKeys.Select(item => (RedisKey) item));
                                                    return new HashSet<string>((await Db.SetCombineAsync(SetOperation.Intersect, keys.ToArray())).ToStringArray());
                                                }, () => hashSetKey + "|" + string.Join(",", compareHashSetKeys));
        }

        long IHashSetStoreProvider.IntersectionToNewSet(string hashSetKey, string newHashSetKey, params string[] compareHashSetKeys)
        {
            return Profiler.Profile(() =>
                                    {
                                        var keys = new List<RedisKey> {hashSetKey};
                                        keys.AddRange(compareHashSetKeys.Select(item => (RedisKey) item));
                                        return Db.SetCombineAndStore(SetOperation.Intersect, newHashSetKey, keys.ToArray());
                                    }, () => hashSetKey + "|" + newHashSetKey + "|" + string.Join(",", compareHashSetKeys));
        }

        async Task<long> IHashSetStoreProvider.IntersectionToNewSetAsync(string hashSetKey, string newHashSetKey, params string[] compareHashSetKeys)
        {
            return await Profiler.Profile(async () =>
                                                {
                                                    var keys = new List<RedisKey> {hashSetKey};
                                                    keys.AddRange(compareHashSetKeys.Select(item => (RedisKey) item));
                                                    return await Db.SetCombineAndStoreAsync(SetOperation.Intersect, newHashSetKey, keys.ToArray());
                                                }, () => hashSetKey + "|" + newHashSetKey + "|" + string.Join(",", compareHashSetKeys));
        }

        HashSet<string> IHashSetStoreProvider.Union(params string[] hashSetKeys)
        {
            return Profiler.Profile(() => new HashSet<string>((Db.SetCombine(SetOperation.Union,
                hashSetKeys.Select(item => (RedisKey) item).ToArray())).ToStringArray()),
                () => string.Join(",", hashSetKeys));
        }

        async Task<HashSet<string>> IHashSetStoreProvider.UnionAsync(params string[] hashSetKeys)
        {
            return await Profiler.Profile(async () => new HashSet<string>((await Db.SetCombineAsync(SetOperation.Union, 
                hashSetKeys.Select(item => (RedisKey) item).ToArray())).ToStringArray()),
                () => string.Join(",", hashSetKeys));
        }

        long IHashSetStoreProvider.UnionToNewSet(string newHashSetKey, params string[] hashSetKeys)
        {
            return Profiler.Profile(() => Db.SetCombineAndStore(SetOperation.Union, newHashSetKey, 
                hashSetKeys.Select(item => (RedisKey) item).ToArray()),
                ()=> newHashSetKey + "|" + string.Join(",", hashSetKeys));
        }

        async Task<long> IHashSetStoreProvider.UnionToNewSetAsync(string newHashSetKey, params string[] hashSetKeys)
        {
           return await Profiler.Profile(() => Db.SetCombineAndStoreAsync(SetOperation.Union, newHashSetKey, 
               hashSetKeys.Select(item => (RedisKey) item).ToArray()),
               () => newHashSetKey + "|" + string.Join(",", hashSetKeys));
        }

        bool IHashSetStoreProvider.Contains(string hashSetKey, string value)
        {
            return Profiler.Profile(() =>Db.SetContains(hashSetKey, value), hashSetKey);
        }

        async Task<bool> IHashSetStoreProvider.ContainsAsync(string hashSetKey, string value)
        {
            return await Profiler.Profile(() =>Db.SetContainsAsync(hashSetKey, value), hashSetKey);
        }

        HashSet<string> IHashSetStoreProvider.GetHashSet(string hashSetKey)
        {
            return Profiler.Profile(() => new HashSet<string>((Db.SetMembers(hashSetKey)).ToStringArray()), hashSetKey);
        }

        async Task<HashSet<string>> IHashSetStoreProvider.GetHashSetAsync(string hashSetKey)
        {
            return await Profiler.Profile(async () =>new HashSet<string>((await Db.SetMembersAsync(hashSetKey)).ToStringArray()), hashSetKey);
        }

        bool IHashSetStoreProvider.MoveValue(string hashSetKey, string destHashSetKey, string value)
        {
            return Profiler.Profile(() => Db.SetMove(hashSetKey, destHashSetKey, value), () => hashSetKey + "|" + destHashSetKey);
        }

        async Task<bool> IHashSetStoreProvider.MoveValueAsync(string hashSetKey, string destHashSetKey, string value)
        {
            return await Profiler.Profile(() => Db.SetMoveAsync(hashSetKey, destHashSetKey, value), () => hashSetKey + "|" + destHashSetKey);
        }

        long IHashSetStoreProvider.Remove(string hashSetKey, params string[] values)
        {
            return Profiler.Profile(() =>Db.SetRemove(hashSetKey, values.Select(item => (RedisValue) item).ToArray()), hashSetKey);
        }

        async Task<long> IHashSetStoreProvider.RemoveAsync(string hashSetKey, params string[] values)
        {
            return await Profiler.Profile(() => Db.SetRemoveAsync(hashSetKey, values.Select(item => (RedisValue) item).ToArray()), hashSetKey);
        }
    }
}