using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aerospike.Client;

namespace Nuve.DataStore.Aerospike
{
    public partial class AerospikeStoreProvider: IDictionaryStoreProvider
    {
        async Task<bool> IDictionaryStoreProvider.IsExistsAsync(string dictKey)
        {
            return await Client.Exists(null, CancellationToken.None, dictKey.ToKey(Namespace));
        }

        async Task<string> IDictionaryStoreProvider.GetAsync(string dictKey, string itemKey)
        {
            return (await Client.Get(null, CancellationToken.None, dictKey.ToKey(Namespace)))?.GetString(itemKey);
        }

        async Task<IDictionary<string, string>> IDictionaryStoreProvider.GetAsync(string dictKey, params string[] itemKeys)
        {
            var record = await Client.Get(null, CancellationToken.None, dictKey.ToKey(Namespace));
            return record?.bins.ToDictionary(b => b.Key, b => b.Value.ToString());
        }

        async Task<bool> IDictionaryStoreProvider.SetAsync(string dictKey, string itemKey, string itemValue, bool overwrite)
        {
            if (!overwrite)
            {
                var provider = (IDictionaryStoreProvider) this;
                if (await provider.ContainsAsync(dictKey, itemKey))
                    return false;
            }
                
            await Client.Put(null, 
                CancellationToken.None, dictKey.ToKey(Namespace), itemValue.ToBin(itemKey));
            return true;
        }

        async Task IDictionaryStoreProvider.SetAsync(string dictKey, IDictionary<string, string> keyValues)
        {
            await Client.Put(null, CancellationToken.None, dictKey.ToKey(Namespace), keyValues.ToBins());
        }

        async Task<long> IDictionaryStoreProvider.RemoveAsync(string dictKey, params string[] itemKeys)
        {
            await Client.Put(null, CancellationToken.None, dictKey.ToKey(Namespace), itemKeys.ToNullBins());
            return itemKeys.Length;
        }

        async Task<bool> IDictionaryStoreProvider.ContainsAsync(string dictKey, string itemKey)
        {
            return (await Client.Get(null, CancellationToken.None, dictKey.ToKey(Namespace)))?.bins.ContainsKey(itemKey) ?? false;
        }

        async Task<long> IDictionaryStoreProvider.CountAsync(string dictKey)
        {
            return (await Client.Get(null, CancellationToken.None, dictKey.ToKey(Namespace)))?.bins.Count ?? 0;
        }

        async Task<IDictionary<string, string>> IDictionaryStoreProvider.GetDictionaryAsync(string dictKey)
        {
            return (await Client.Get(null, CancellationToken.None, dictKey.ToKey(Namespace)))?.bins.ToDictionary(b => b.Key, b => b.Value.ToString());
        }

        async Task<IList<string>> IDictionaryStoreProvider.KeysAsync(string dictKey)
        {
            return (await Client.Get(null, CancellationToken.None, dictKey.ToKey(Namespace)))?.bins.Keys.ToList();
        }

        async Task<IList<string>> IDictionaryStoreProvider.ValuesAsync(string dictKey)
        {
            return (await Client.Get(null, CancellationToken.None, dictKey.ToKey(Namespace)))?.bins.Values.Select(v => v.ToString()).ToList();
        }

        async Task<long> IDictionaryStoreProvider.IncrementAsync(string dictKey, string itemKey, long value)
        {
            var newValue = long.Parse(await ((IDictionaryStoreProvider) this).GetAsync(dictKey, itemKey)) + value;
            await ((IDictionaryStoreProvider) this).SetAsync(dictKey, itemKey, newValue.ToString(), true);
            return newValue;
        }

        async Task<long> IDictionaryStoreProvider.SizeInBytesAsync(string dictKey, string itemKey)
        {
            return (await ((IDictionaryStoreProvider) this).GetAsync(dictKey, itemKey)).Length;
        }

        bool IDictionaryStoreProvider.IsExists(string dictKey)
        {
            return Client.Exists(null, dictKey.ToKey(Namespace));
        }

        string IDictionaryStoreProvider.Get(string dictKey, string itemKey)
        {
            return Client.Get(null, dictKey.ToKey(Namespace))?.GetString(itemKey);
        }

        IDictionary<string, string> IDictionaryStoreProvider.Get(string dictKey, params string[] itemKeys)
        {
            var record = Client.Get(null, dictKey.ToKey(Namespace));
            return record?.bins.ToDictionary(b => b.Key, b => b.Value.ToString());
        }

        bool IDictionaryStoreProvider.Set(string dictKey, string itemKey, string itemValue, bool overwrite)
        {
            if (!overwrite)
            {
                var provider = (IDictionaryStoreProvider)this;
                if (provider.Contains(dictKey, itemKey))
                    return false;
            }

            Client.Put(null, 
                dictKey.ToKey(Namespace), itemValue.ToBin(itemKey));
            return true;
        }

        void IDictionaryStoreProvider.Set(string dictKey, IDictionary<string, string> keyValues)
        {
            Client.Put(null, dictKey.ToKey(Namespace), keyValues.ToBins());
        }

        long IDictionaryStoreProvider.Remove(string dictKey, params string[] itemKeys)
        {
            Client.Put(null, dictKey.ToKey(Namespace), itemKeys.ToNullBins());
            return itemKeys.Length;
        }

        bool IDictionaryStoreProvider.Contains(string dictKey, string itemKey)
        {
            return Client.Get(null, dictKey.ToKey(Namespace))?.bins.ContainsKey(itemKey) ?? false;
        }

        long IDictionaryStoreProvider.Count(string dictKey)
        {
            return Client.Get(null, dictKey.ToKey(Namespace))?.bins.Count ?? 0;
        }

        IDictionary<string, string> IDictionaryStoreProvider.GetDictionary(string dictKey)
        {
            return Client.Get(null, dictKey.ToKey(Namespace))?.bins.ToDictionary(b => b.Key, b => b.Value.ToString());
        }

        IList<string> IDictionaryStoreProvider.Keys(string dictKey)
        {
            return Client.Get(null, dictKey.ToKey(Namespace))?.bins.Keys.ToList();
        }

        IList<string> IDictionaryStoreProvider.Values(string dictKey)
        {
            return Client.Get(null, dictKey.ToKey(Namespace))?.bins.Values.Select(v => v.ToString()).ToList();
        }

        long IDictionaryStoreProvider.Increment(string dictKey, string itemKey, long value)
        {
            var newValue = long.Parse(((IDictionaryStoreProvider)this).Get(dictKey, itemKey) ?? "0") + value;
            ((IDictionaryStoreProvider)this).Set(dictKey, itemKey, newValue.ToString(), true);
            return newValue;
        }

        long IDictionaryStoreProvider.SizeInBytes(string dictKey, string itemKey)
        {
            return ((IDictionaryStoreProvider)this).Get(dictKey, itemKey)?.Length ?? 0;
        }
    }
}
