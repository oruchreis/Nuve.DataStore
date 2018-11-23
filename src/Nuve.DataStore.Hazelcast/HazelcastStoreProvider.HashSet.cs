using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Nuve.DataStore.Hazelcast
{
    public partial class HazelcastStoreProvider : IHashSetStoreProvider
    {
        long IHashSetStoreProvider.Add(string hashSetKey, params string[] values)
        {
            throw new NotImplementedException();
        }

        Task<long> IHashSetStoreProvider.AddAsync(string hashSetKey, params string[] values)
        {
            throw new NotImplementedException();
        }

        bool IHashSetStoreProvider.Contains(string hashSetKey, string value)
        {
            throw new NotImplementedException();
        }

        Task<bool> IHashSetStoreProvider.ContainsAsync(string hashSetKey, string value)
        {
            throw new NotImplementedException();
        }

        long IHashSetStoreProvider.Count(string hashSetKey)
        {
            throw new NotImplementedException();
        }

        Task<long> IHashSetStoreProvider.CountAsync(string hashSetKey)
        {
            throw new NotImplementedException();
        }

        HashSet<string> IHashSetStoreProvider.Difference(string hashSetKey, params string[] compareHashSetKeys)
        {
            throw new NotImplementedException();
        }

        Task<HashSet<string>> IHashSetStoreProvider.DifferenceAsync(string hashSetKey, params string[] compareHashSetKeys)
        {
            throw new NotImplementedException();
        }

        long IHashSetStoreProvider.DifferenceToNewSet(string hashSetKey, string newHashSetKey, params string[] compareHashSetKeys)
        {
            throw new NotImplementedException();
        }

        Task<long> IHashSetStoreProvider.DifferenceToNewSetAsync(string hashSetKey, string newHashSetKey, params string[] compareHashSetKeys)
        {
            throw new NotImplementedException();
        }

        HashSet<string> IHashSetStoreProvider.GetHashSet(string hashSetKey)
        {
            throw new NotImplementedException();
        }

        Task<HashSet<string>> IHashSetStoreProvider.GetHashSetAsync(string hashSetKey)
        {
            throw new NotImplementedException();
        }

        HashSet<string> IHashSetStoreProvider.Intersection(string hashSetKey, params string[] compareHashSetKeys)
        {
            throw new NotImplementedException();
        }

        Task<HashSet<string>> IHashSetStoreProvider.IntersectionAsync(string hashSetKey, params string[] compareHashSetKeys)
        {
            throw new NotImplementedException();
        }

        long IHashSetStoreProvider.IntersectionToNewSet(string hashSetKey, string newHashSetKey, params string[] compareHashSetKeys)
        {
            throw new NotImplementedException();
        }

        Task<long> IHashSetStoreProvider.IntersectionToNewSetAsync(string hashSetKey, string newHashSetKey, params string[] compareHashSetKeys)
        {
            throw new NotImplementedException();
        }

        bool IHashSetStoreProvider.IsExists(string hashSetKey)
        {
            throw new NotImplementedException();
        }

        Task<bool> IHashSetStoreProvider.IsExistsAsync(string hashSetKey)
        {
            throw new NotImplementedException();
        }

        bool IHashSetStoreProvider.MoveValue(string hashSetKey, string destHashSetKey, string value)
        {
            throw new NotImplementedException();
        }

        Task<bool> IHashSetStoreProvider.MoveValueAsync(string hashSetKey, string destHashSetKey, string value)
        {
            throw new NotImplementedException();
        }

        long IHashSetStoreProvider.Remove(string hashSetKey, params string[] values)
        {
            throw new NotImplementedException();
        }

        Task<long> IHashSetStoreProvider.RemoveAsync(string hashSetKey, params string[] values)
        {
            throw new NotImplementedException();
        }

        HashSet<string> IHashSetStoreProvider.Union(params string[] hashSetKeys)
        {
            throw new NotImplementedException();
        }

        Task<HashSet<string>> IHashSetStoreProvider.UnionAsync(params string[] hashSetKeys)
        {
            throw new NotImplementedException();
        }

        long IHashSetStoreProvider.UnionToNewSet(string newHashSetKey, params string[] hashSetKeys)
        {
            throw new NotImplementedException();
        }

        Task<long> IHashSetStoreProvider.UnionToNewSetAsync(string newHashSetKey, params string[] hashSetKeys)
        {
            throw new NotImplementedException();
        }
    }
}
