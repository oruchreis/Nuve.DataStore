using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nuve.DataStore
{
    public interface IHashSetStoreProvider
    {
        Task<bool> IsExistsAsync(string hashSetKey);

        Task<long> AddAsync(string hashSetKey, params byte[][] values);

        Task<long> CountAsync(string hashSetKey);

        Task<HashSet<byte[]>> DifferenceAsync(string hashSetKey, params string[] compareHashSetKeys);

        Task<long> DifferenceToNewSetAsync(string hashSetKey, string newHashSetKey, params string[] compareHashSetKeys);

        Task<HashSet<byte[]>> IntersectionAsync(string hashSetKey, params string[] compareHashSetKeys);

        Task<long> IntersectionToNewSetAsync(string hashSetKey, string newHashSetKey, params string[] compareHashSetKeys);

        Task<HashSet<byte[]>> UnionAsync(params string[] hashSetKeys);

        Task<long> UnionToNewSetAsync(string newHashSetKey, params string[] hashSetKeys);

        Task<bool> ContainsAsync(string hashSetKey, byte[] value);

        Task<HashSet<byte[]>> GetHashSetAsync(string hashSetKey);

        Task<bool> MoveValueAsync(string hashSetKey, string destHashSetKey, byte[] value);

        Task<long> RemoveAsync(string hashSetKey, params byte[][] values);


        bool IsExists(string hashSetKey);

        long Add(string hashSetKey, params byte[][] values);

        long Count(string hashSetKey);

        HashSet<byte[]> Difference(string hashSetKey, params string[] compareHashSetKeys);

        long DifferenceToNewSet(string hashSetKey, string newHashSetKey, params string[] compareHashSetKeys);

        HashSet<byte[]> Intersection(string hashSetKey, params string[] compareHashSetKeys);

        long IntersectionToNewSet(string hashSetKey, string newHashSetKey, params string[] compareHashSetKeys);

        HashSet<byte[]> Union(params string[] hashSetKeys);

        long UnionToNewSet(string newHashSetKey, params string[] hashSetKeys);

        bool Contains(string hashSetKey, byte[] value);

        HashSet<byte[]> GetHashSet(string hashSetKey);

        bool MoveValue(string hashSetKey, string destHashSetKey, byte[] value);

        long Remove(string hashSetKey, params byte[][] values);
    }
}