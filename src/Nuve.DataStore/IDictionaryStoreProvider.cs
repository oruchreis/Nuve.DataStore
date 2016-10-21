using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nuve.DataStore
{
    /// <summary>
    /// Represents a store provider that supports dictionary operations.
    /// </summary>
    public interface IDictionaryStoreProvider
    {
        Task<bool> IsExistsAsync(string dictKey);

        Task<string> GetAsync(string dictKey, string itemKey);

        Task<IDictionary<string, string>> GetAsync(string dictKey, params string[] itemKeys);

        Task<bool> SetAsync(string dictKey, string itemKey, string itemValue, bool overwrite);

        Task SetAsync(string dictKey, IDictionary<string, string> keyValues);

        Task<long> RemoveAsync(string dictKey, params string[] itemKeys);

        Task<bool> ContainsAsync(string dictKey, string itemKey);

        Task<long> CountAsync(string dictKey);

        Task<IDictionary<string, string>> GetDictionaryAsync(string dictKey);

        Task<IList<string>> KeysAsync(string dictKey);

        Task<IList<string>> ValuesAsync(string dictKey);

        Task<long> IncrementAsync(string dictKey, string itemKey, long value);

        Task<long> SizeInBytesAsync(string dictKey, string itemKey);

        bool IsExists(string dictKey);

        string Get(string dictKey, string itemKey);

        IDictionary<string, string> Get(string dictKey, params string[] itemKeys);

        bool Set(string dictKey, string itemKey, string itemValue, bool overwrite);

        void Set(string dictKey, IDictionary<string, string> keyValues);

        long Remove(string dictKey, params string[] itemKeys);

        bool Contains(string dictKey, string itemKey);

        long Count(string dictKey);

        IDictionary<string, string> GetDictionary(string dictKey);

        IList<string> Keys(string dictKey);

        IList<string> Values(string dictKey);

        long Increment(string dictKey, string itemKey, long value);

        long SizeInBytes(string dictKey, string itemKey);
    }
}