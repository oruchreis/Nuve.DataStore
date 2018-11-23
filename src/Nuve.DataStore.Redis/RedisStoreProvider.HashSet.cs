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
            return Db.KeyExists(hashSetKey);
        }

        async Task<bool> IHashSetStoreProvider.IsExistsAsync(string hashSetKey)
        {
            return await Db.KeyExistsAsync(hashSetKey);
        }

        long IHashSetStoreProvider.Add(string hashSetKey, params byte[][] values)
        {
            return Db.SetAdd(hashSetKey, values.Select(item => (RedisValue)item).ToArray());
        }

        async Task<long> IHashSetStoreProvider.AddAsync(string hashSetKey, params byte[][] values)
        {
            return await Db.SetAddAsync(hashSetKey, values.Select(item => (RedisValue)item).ToArray());
        }

        long IHashSetStoreProvider.Count(string hashSetKey)
        {
            return Db.SetLength(hashSetKey);
        }

        async Task<long> IHashSetStoreProvider.CountAsync(string hashSetKey)
        {
            return await Db.SetLengthAsync(hashSetKey);
        }

        HashSet<byte[]> IHashSetStoreProvider.Difference(string hashSetKey, params string[] compareHashSetKeys)
        {
            var keys = new List<RedisKey> { hashSetKey };
            keys.AddRange(compareHashSetKeys.Select(item => (RedisKey)item));
            return new HashSet<byte[]>((Db.SetCombine(SetOperation.Difference, keys.ToArray())).Select(rv => (byte[])rv));
        }

        async Task<HashSet<byte[]>> IHashSetStoreProvider.DifferenceAsync(string hashSetKey, params string[] compareHashSetKeys)
        {
            var keys = new List<RedisKey> { hashSetKey };
            keys.AddRange(compareHashSetKeys.Select(item => (RedisKey)item));
            return new HashSet<byte[]>((await Db.SetCombineAsync(SetOperation.Difference, keys.ToArray())).Select(rv => (byte[])rv));
        }

        long IHashSetStoreProvider.DifferenceToNewSet(string hashSetKey, string newHashSetKey, params string[] compareHashSetKeys)
        {
            var keys = new List<RedisKey> { hashSetKey };
            keys.AddRange(compareHashSetKeys.Select(item => (RedisKey)item));
            return Db.SetCombineAndStore(SetOperation.Difference, newHashSetKey, keys.ToArray());
        }

        async Task<long> IHashSetStoreProvider.DifferenceToNewSetAsync(string hashSetKey, string newHashSetKey, params string[] compareHashSetKeys)
        {
            var keys = new List<RedisKey> { hashSetKey };
            keys.AddRange(compareHashSetKeys.Select(item => (RedisKey)item));
            return await Db.SetCombineAndStoreAsync(SetOperation.Difference, newHashSetKey, keys.ToArray());
        }

        HashSet<byte[]> IHashSetStoreProvider.Intersection(string hashSetKey, params string[] compareHashSetKeys)
        {
            var keys = new List<RedisKey> { hashSetKey };
            keys.AddRange(compareHashSetKeys.Select(item => (RedisKey)item));
            return new HashSet<byte[]>((Db.SetCombine(SetOperation.Intersect, keys.ToArray())).Select(rv => (byte[])rv));
        }

        async Task<HashSet<byte[]>> IHashSetStoreProvider.IntersectionAsync(string hashSetKey, params string[] compareHashSetKeys)
        {
            var keys = new List<RedisKey> { hashSetKey };
            keys.AddRange(compareHashSetKeys.Select(item => (RedisKey)item));
            return new HashSet<byte[]>((await Db.SetCombineAsync(SetOperation.Intersect, keys.ToArray())).Select(rv => (byte[])rv));
        }

        long IHashSetStoreProvider.IntersectionToNewSet(string hashSetKey, string newHashSetKey, params string[] compareHashSetKeys)
        {
            var keys = new List<RedisKey> { hashSetKey };
            keys.AddRange(compareHashSetKeys.Select(item => (RedisKey)item));
            return Db.SetCombineAndStore(SetOperation.Intersect, newHashSetKey, keys.ToArray());
        }

        async Task<long> IHashSetStoreProvider.IntersectionToNewSetAsync(string hashSetKey, string newHashSetKey, params string[] compareHashSetKeys)
        {
            var keys = new List<RedisKey> { hashSetKey };
            keys.AddRange(compareHashSetKeys.Select(item => (RedisKey)item));
            return await Db.SetCombineAndStoreAsync(SetOperation.Intersect, newHashSetKey, keys.ToArray());
        }

        HashSet<byte[]> IHashSetStoreProvider.Union(params string[] hashSetKeys)
        {
            return new HashSet<byte[]>((Db.SetCombine(SetOperation.Union,
                hashSetKeys.Select(item => (RedisKey)item).ToArray())).Select(rv => (byte[])rv));
        }

        async Task<HashSet<byte[]>> IHashSetStoreProvider.UnionAsync(params string[] hashSetKeys)
        {
            return new HashSet<byte[]>((await Db.SetCombineAsync(SetOperation.Union,
                hashSetKeys.Select(item => (RedisKey)item).ToArray())).Select(rv => (byte[])rv));
        }

        long IHashSetStoreProvider.UnionToNewSet(string newHashSetKey, params string[] hashSetKeys)
        {
            return Db.SetCombineAndStore(SetOperation.Union, newHashSetKey,
                hashSetKeys.Select(item => (RedisKey)item).ToArray());
        }

        async Task<long> IHashSetStoreProvider.UnionToNewSetAsync(string newHashSetKey, params string[] hashSetKeys)
        {
            return await Db.SetCombineAndStoreAsync(SetOperation.Union, newHashSetKey,
                hashSetKeys.Select(item => (RedisKey)item).ToArray());
        }

        bool IHashSetStoreProvider.Contains(string hashSetKey, byte[] value)
        {
            return Db.SetContains(hashSetKey, value);
        }

        async Task<bool> IHashSetStoreProvider.ContainsAsync(string hashSetKey, byte[] value)
        {
            return await Db.SetContainsAsync(hashSetKey, value);
        }

        HashSet<byte[]> IHashSetStoreProvider.GetHashSet(string hashSetKey)
        {
            return new HashSet<byte[]>((Db.SetMembers(hashSetKey)).Select(rv => (byte[])rv));
        }

        async Task<HashSet<byte[]>> IHashSetStoreProvider.GetHashSetAsync(string hashSetKey)
        {
            return new HashSet<byte[]>((await Db.SetMembersAsync(hashSetKey)).Select(rv => (byte[])rv));
        }

        bool IHashSetStoreProvider.MoveValue(string hashSetKey, string destHashSetKey, byte[] value)
        {
            return Db.SetMove(hashSetKey, destHashSetKey, value);
        }

        async Task<bool> IHashSetStoreProvider.MoveValueAsync(string hashSetKey, string destHashSetKey, byte[] value)
        {
            return await Db.SetMoveAsync(hashSetKey, destHashSetKey, value);
        }

        long IHashSetStoreProvider.Remove(string hashSetKey, params byte[][] values)
        {
            return Db.SetRemove(hashSetKey, values.Select(item => (RedisValue)item).ToArray());
        }

        async Task<long> IHashSetStoreProvider.RemoveAsync(string hashSetKey, params byte[][] values)
        {
            return await Db.SetRemoveAsync(hashSetKey, values.Select(item => (RedisValue)item).ToArray());
        }
    }
}