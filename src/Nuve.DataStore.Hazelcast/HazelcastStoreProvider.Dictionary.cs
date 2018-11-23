using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Nuve.DataStore.Hazelcast
{
    public partial class HazelcastStoreProvider : IDictionaryStoreProvider
    {
        bool IDictionaryStoreProvider.Contains(string dictKey, string itemKey)
        {
            throw new NotImplementedException();
        }

        Task<bool> IDictionaryStoreProvider.ContainsAsync(string dictKey, string itemKey)
        {
            throw new NotImplementedException();
        }

        long IDictionaryStoreProvider.Count(string dictKey)
        {
            throw new NotImplementedException();
        }

        Task<long> IDictionaryStoreProvider.CountAsync(string dictKey)
        {
            throw new NotImplementedException();
        }

        string IDictionaryStoreProvider.Get(string dictKey, string itemKey)
        {
            throw new NotImplementedException();
        }

        IDictionary<string, string> IDictionaryStoreProvider.Get(string dictKey, params string[] itemKeys)
        {
            throw new NotImplementedException();
        }

        Task<string> IDictionaryStoreProvider.GetAsync(string dictKey, string itemKey)
        {
            throw new NotImplementedException();
        }

        Task<IDictionary<string, string>> IDictionaryStoreProvider.GetAsync(string dictKey, params string[] itemKeys)
        {
            throw new NotImplementedException();
        }

        IDictionary<string, string> IDictionaryStoreProvider.GetDictionary(string dictKey)
        {
            throw new NotImplementedException();
        }

        Task<IDictionary<string, string>> IDictionaryStoreProvider.GetDictionaryAsync(string dictKey)
        {
            throw new NotImplementedException();
        }

        long IDictionaryStoreProvider.Increment(string dictKey, string itemKey, long value)
        {
            throw new NotImplementedException();
        }

        Task<long> IDictionaryStoreProvider.IncrementAsync(string dictKey, string itemKey, long value)
        {
            throw new NotImplementedException();
        }

        bool IDictionaryStoreProvider.IsExists(string dictKey)
        {
            throw new NotImplementedException();
        }

        Task<bool> IDictionaryStoreProvider.IsExistsAsync(string dictKey)
        {
            throw new NotImplementedException();
        }

        IList<string> IDictionaryStoreProvider.Keys(string dictKey)
        {
            throw new NotImplementedException();
        }

        Task<IList<string>> IDictionaryStoreProvider.KeysAsync(string dictKey)
        {
            throw new NotImplementedException();
        }

        long IDictionaryStoreProvider.Remove(string dictKey, params string[] itemKeys)
        {
            throw new NotImplementedException();
        }

        Task<long> IDictionaryStoreProvider.RemoveAsync(string dictKey, params string[] itemKeys)
        {
            throw new NotImplementedException();
        }

        long IDictionaryStoreProvider.RenameKey(string dictKey, string oldKey, string newKey)
        {
            throw new NotImplementedException();
        }

        Task<long> IDictionaryStoreProvider.RenameKeyAsync(string dictKey, string oldKey, string newKey)
        {
            throw new NotImplementedException();
        }

        bool IDictionaryStoreProvider.Set(string dictKey, string itemKey, string itemValue, bool overwrite)
        {
            throw new NotImplementedException();
        }

        void IDictionaryStoreProvider.Set(string dictKey, IDictionary<string, string> keyValues)
        {
            throw new NotImplementedException();
        }

        Task<bool> IDictionaryStoreProvider.SetAsync(string dictKey, string itemKey, string itemValue, bool overwrite)
        {
            throw new NotImplementedException();
        }

        Task IDictionaryStoreProvider.SetAsync(string dictKey, IDictionary<string, string> keyValues)
        {
            throw new NotImplementedException();
        }

        long IDictionaryStoreProvider.SizeInBytes(string dictKey, string itemKey)
        {
            throw new NotImplementedException();
        }

        Task<long> IDictionaryStoreProvider.SizeInBytesAsync(string dictKey, string itemKey)
        {
            throw new NotImplementedException();
        }

        IList<string> IDictionaryStoreProvider.Values(string dictKey)
        {
            throw new NotImplementedException();
        }

        Task<IList<string>> IDictionaryStoreProvider.ValuesAsync(string dictKey)
        {
            throw new NotImplementedException();
        }
    }
}
