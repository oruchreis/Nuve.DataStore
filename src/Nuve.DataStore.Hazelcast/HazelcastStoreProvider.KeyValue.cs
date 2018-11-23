using Hazelcast.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Nuve.DataStore.Hazelcast
{
    public partial class HazelcastStoreProvider : IKeyValueStoreProvider
    {
        private const string _keyValueMapKey = "__default";

        protected IMap<string, string> DefaultMap;

        long IKeyValueStoreProvider.AppendString(string key, string value)
        {
            DefaultMap.Set(key, DefaultMap.Get(key) + value);
            return 1;
        }

        async Task<long> IKeyValueStoreProvider.AppendStringAsync(string key, string value)
        {
            DefaultMap.Set(key, await DefaultMap.GetAsync(key) + value);
            return 1;
        }

        bool IKeyValueStoreProvider.Contains(string key)
        {
            return DefaultMap.ContainsKey(key);
        }

        Task<bool> IKeyValueStoreProvider.ContainsAsync(string key)
        {
            return Task.FromResult(DefaultMap.ContainsKey(key));
        }

        long IKeyValueStoreProvider.Decrement(string key, long amount)
        {
            var i = 0L;
            long.TryParse(DefaultMap.Get(key), out i);
            var newValue = i - amount;
            DefaultMap.Set(key, newValue.ToString());
            return newValue;
        }


        async Task<long> IKeyValueStoreProvider.DecrementAsync(string key, long amount)
        {
            var i = 0L;
            long.TryParse(await DefaultMap.GetAsync(key), out i);
            var newValue = i - amount;
            DefaultMap.Set(key, newValue.ToString());
            return newValue;
        }

        string IKeyValueStoreProvider.Exchange(string key, string value)
        {
            return DefaultMap.Put(key, value);
        }

        async Task<string> IKeyValueStoreProvider.ExchangeAsync(string key, string value)
        {
            return await DefaultMap.PutAsync(key, value);
        }

        string IKeyValueStoreProvider.Get(string key)
        {
            return DefaultMap.Get(key);
        }

        IDictionary<string, string> IKeyValueStoreProvider.GetAll(params string[] keys)
        {
            return DefaultMap.GetAll(keys);
        }

        Task<IDictionary<string, string>> IKeyValueStoreProvider.GetAllAsync(params string[] keys)
        {
            return Task.FromResult(DefaultMap.GetAll(keys));
        }

        Task<string> IKeyValueStoreProvider.GetAsync(string key)
        {
            return DefaultMap.GetAsync(key);
        }

        long IKeyValueStoreProvider.Increment(string key, long amount)
        {
            var i = 0L;
            long.TryParse(DefaultMap.Get(key), out i);
            var newValue = i + amount;
            DefaultMap.Set(key, newValue.ToString());
            return newValue;
        }

        async Task<long> IKeyValueStoreProvider.IncrementAsync(string key, long amount)
        {
            var i = 0L;
            long.TryParse(await DefaultMap.GetAsync(key), out i);
            var newValue = i + amount;
            DefaultMap.Set(key, newValue.ToString());
            return newValue;
        }

        long IKeyValueStoreProvider.OverwriteString(string key, long offset, string value)
        {
            var str = DefaultMap.Get(key);
            str = str.Substring(0, (int)offset) + value + (offset + value.Length < str.Length ? str.Substring((int)offset + value.Length) : "");
            DefaultMap.Set(key, str);
            return str.Length;
        }

        async Task<long> IKeyValueStoreProvider.OverwriteStringAsync(string key, long offset, string value)
        {
            var str = await DefaultMap.GetAsync(key);
            str = str.Substring(0, (int)offset) + value + (offset + value.Length < str.Length ? str.Substring((int)offset + value.Length) : "");
            DefaultMap.Set(key, str);
            return str.Length;
        }

        bool IKeyValueStoreProvider.Rename(string oldKey, string newKey)
        {
            var value = DefaultMap.Get(oldKey);
            DefaultMap.Set(newKey, value);
            DefaultMap.Delete(oldKey);
            return true;
        }

        async Task<bool> IKeyValueStoreProvider.RenameAsync(string oldKey, string newKey)
        {
            var value = await DefaultMap.GetAsync(oldKey);
            DefaultMap.Set(newKey, value);
            DefaultMap.Delete(oldKey);
            return true;
        }

        bool IKeyValueStoreProvider.Set(string key, string entity, bool overwrite)
        {
            if (overwrite)
                DefaultMap.Set(key, entity);
            else 
                DefaultMap.PutIfAbsent(key, entity);
            return true;
        }

        bool IKeyValueStoreProvider.SetAll(IDictionary<string, string> keyValues, bool overwrite)
        {
            if (overwrite)
                DefaultMap.PutAll(keyValues);
            else
            {
                foreach (var kv in keyValues)
                {
                    DefaultMap.PutIfAbsent(kv.Key, kv.Value);
                }                
            }
            return true;
        }

        Task<bool> IKeyValueStoreProvider.SetAllAsync(IDictionary<string, string> keyValues, bool overwrite)
        {
            return Task.FromResult(((IKeyValueStoreProvider)this).SetAll(keyValues, overwrite));
        }

        Task<bool> IKeyValueStoreProvider.SetAsync(string key, string entity, bool overwrite)
        {
            return Task.FromResult(((IKeyValueStoreProvider)this).Set(key, entity, overwrite));
        }

        long IKeyValueStoreProvider.SizeInBytes(string key)
        {
            return DefaultMap.GetEntryView(key).GetCost();
        }

        Task<long> IKeyValueStoreProvider.SizeInBytesAsync(string key)
        {
            return Task.FromResult(DefaultMap.GetEntryView(key).GetCost());
        }

        string IKeyValueStoreProvider.SubString(string key, long start, long end)
        {
            return DefaultMap.Get(key).Substring((int)start, (int)end);
        }

        async Task<string> IKeyValueStoreProvider.SubStringAsync(string key, long start, long end)
        {
            return (await DefaultMap.GetAsync(key)).Substring((int)start, (int)end);
        }
    }
}
