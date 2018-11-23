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

        Task<byte[]> GetAsync(string dictKey, string itemKey);

        Task<IDictionary<string, byte[]>> GetAsync(string dictKey, params string[] itemKeys);

        Task<bool> SetAsync(string dictKey, string itemKey, byte[] itemValue, bool overwrite);

        Task SetAsync(string dictKey, IDictionary<string, byte[]> keyValues);

        Task<long> RemoveAsync(string dictKey, params string[] itemKeys);

        Task<bool> ContainsAsync(string dictKey, string itemKey);

        Task<long> CountAsync(string dictKey);

        Task<IDictionary<string, byte[]>> GetDictionaryAsync(string dictKey);

        Task<IList<string>> KeysAsync(string dictKey);

        Task<IList<byte[]>> ValuesAsync(string dictKey);

        Task<long> IncrementAsync(string dictKey, string itemKey, long value);

        Task<long> SizeInBytesAsync(string dictKey, string itemKey);

        Task<long> RenameKeyAsync(string dictKey, string oldKey, string newKey);

        bool IsExists(string dictKey);

        byte[] Get(string dictKey, string itemKey);

        IDictionary<string, byte[]> Get(string dictKey, params string[] itemKeys);

        bool Set(string dictKey, string itemKey, byte[] itemValue, bool overwrite);

        void Set(string dictKey, IDictionary<string, byte[]> keyValues);

        long Remove(string dictKey, params string[] itemKeys);

        bool Contains(string dictKey, string itemKey);

        long Count(string dictKey);

        IDictionary<string, byte[]> GetDictionary(string dictKey);

        IList<string> Keys(string dictKey);

        IList<byte[]> Values(string dictKey);

        long Increment(string dictKey, string itemKey, long value);

        long SizeInBytes(string dictKey, string itemKey);

        long RenameKey(string dictKey, string oldKey, string newKey);
    }
}