using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nuve.DataStore
{
    public interface IHashSetStoreProvider
    {
        Task<bool> IsExistsAsync(string hashSetKey);

        Task<long> AddAsync(string hashSetKey, params string[] values);

        Task<long> CountAsync(string hashSetKey);

        Task<HashSet<string>> DifferenceAsync(string hashSetKey, params string[] compareHashSetKeys);

        Task<long> DifferenceToNewSetAsync(string hashSetKey, string newHashSetKey, params string[] compareHashSetKeys);

        Task<HashSet<string>> IntersectionAsync(string hashSetKey, params string[] compareHashSetKeys);

        Task<long> IntersectionToNewSetAsync(string hashSetKey, string newHashSetKey, params string[] compareHashSetKeys);

        Task<HashSet<string>> UnionAsync(params string[] hashSetKeys);

        Task<long> UnionToNewSetAsync(string newHashSetKey, params string[] hashSetKeys);

        Task<bool> ContainsAsync(string hashSetKey, string value);

        Task<HashSet<string>> GetHashSetAsync(string hashSetKey);

        Task<bool> MoveValueAsync(string hashSetKey, string destHashSetKey, string value);

        Task<long> RemoveAsync(string hashSetKey, params string[] values);


        bool IsExists(string hashSetKey);

        long Add(string hashSetKey, params string[] values);

        long Count(string hashSetKey);

        HashSet<string> Difference(string hashSetKey, params string[] compareHashSetKeys);

        long DifferenceToNewSet(string hashSetKey, string newHashSetKey, params string[] compareHashSetKeys);

        HashSet<string> Intersection(string hashSetKey, params string[] compareHashSetKeys);

        long IntersectionToNewSet(string hashSetKey, string newHashSetKey, params string[] compareHashSetKeys);

        HashSet<string> Union(params string[] hashSetKeys);

        long UnionToNewSet(string newHashSetKey, params string[] hashSetKeys);

        bool Contains(string hashSetKey, string value);

        HashSet<string> GetHashSet(string hashSetKey);

        bool MoveValue(string hashSetKey, string destHashSetKey, string value);

        long Remove(string hashSetKey, params string[] values);
    }
}