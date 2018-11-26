using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aerospike.Client;

namespace Nuve.DataStore.Aerospike
{
    public partial class AerospikeStoreProvider : IKeyValueStoreProvider
    {
        async Task<string> IKeyValueStoreProvider.GetAsync(string key)
        {
            return (await Client.Get(null, CancellationToken.None, key.ToKey(Namespace)))?.GetString("value");
        }

        async Task<IDictionary<string, string>> IKeyValueStoreProvider.GetAllAsync(params string[] keys)
        {
            var values = (await Client.Get(null, CancellationToken.None, keys.ToKeys(Namespace)))
                .Select(r => r?.GetString("value")).Where(r => r != null);
            return keys.Zip(values, (k, v) => new {k, v}).ToDictionary(kv => kv.k, kv => kv.v);
        }

        async Task<bool> IKeyValueStoreProvider.SetAsync(string key, string entity, bool overwrite)
        {
            await Client.Put(new WritePolicy(ClientPolicy.writePolicyDefault) {recordExistsAction = overwrite ? RecordExistsAction.REPLACE : RecordExistsAction.CREATE_ONLY},
                CancellationToken.None, key.ToKey(Namespace), entity.ToBin("value"));
            return true;
        }

        async Task<bool> IKeyValueStoreProvider.SetAllAsync(IDictionary<string, string> keyValues, bool overwrite)
        {
            foreach (var keyValue in keyValues)
            {
                await ((IKeyValueStoreProvider) this).SetAsync(keyValue.Key, keyValue.Value, overwrite);
            }
            return true;
        }

        async Task<string> IKeyValueStoreProvider.ExchangeAsync(string key, string value)
        {
            var oldValue = await ((IKeyValueStoreProvider) this).GetAsync(key);
            await ((IKeyValueStoreProvider) this).SetAsync(key, value, true);
            return oldValue;
        }

        async Task<long> IKeyValueStoreProvider.AppendStringAsync(string key, string value)
        {
            await Client.Append(null, CancellationToken.None, key.ToKey(Namespace), value.ToBin("value"));
            return 0;//todo: yeni oluşan stringin boyu
        }

        async Task<string> IKeyValueStoreProvider.SubStringAsync(string key, long start, long end)
        {
            return (await ((IKeyValueStoreProvider) this).GetAsync(key)).Substring((int)start, (int)end-(int)start);
        }

        async Task<long> IKeyValueStoreProvider.OverwriteStringAsync(string key, long offset, string value)
        {
            var oldValue = await ((IKeyValueStoreProvider) this).GetAsync(key);
            var newValue = oldValue.Substring(0, (int) offset) + value + (offset + value.Length > oldValue.Length ? "" : oldValue.Substring((int) offset + value.Length));
            await ((IKeyValueStoreProvider) this).SetAsync(key, newValue, true);
            return newValue.Length;
        }

        async Task<long> IKeyValueStoreProvider.SizeInBytesAsync(string key)
        {
            //todo: Aeurospike desteklemiyor. Burası function ile halledilecek.
            return (await ((IKeyValueStoreProvider) this).GetAsync(key)).Length;
        }

        async Task<bool> IKeyValueStoreProvider.ContainsAsync(string key)
        {
            return await Client.Exists(null, CancellationToken.None, key.ToKey(Namespace));
        }

        async Task<bool> IKeyValueStoreProvider.RenameAsync(string oldKey, string newKey)
        {
            //todo: Aeurospike desteklemiyor. Burası function ile halledilecek.
            var value = await ((IKeyValueStoreProvider) this).GetAsync(oldKey);
            await RemoveAsync(oldKey);
            await ((IKeyValueStoreProvider)this).SetAsync(newKey, value, true);
            return true;
        }

        async Task<long> IKeyValueStoreProvider.IncrementAsync(string key, long amount)
        {
            //todo: Aeurospike desteklemiyor. Burası function ile halledilecek.
            var value = long.Parse(await ((IKeyValueStoreProvider)this).GetAsync(key));
            value += amount;
            await ((IKeyValueStoreProvider)this).SetAsync(key, value.ToString(), true);
            return value;
        }

        async Task<long> IKeyValueStoreProvider.DecrementAsync(string key, long amount)
        {
            return await ((IKeyValueStoreProvider) this).IncrementAsync(key, -amount);
        }

        string IKeyValueStoreProvider.Get(string key)
        {
            return Client.Get(null, key.ToKey(Namespace))?.GetString("value");
        }

        IDictionary<string, string> IKeyValueStoreProvider.GetAll(params string[] keys)
        {
            var values = Client.Get(null, keys.ToKeys(Namespace)).Select(r => r?.GetString("value")).Where(r => r != null);
            return keys.Zip(values, (k, v) => new { k, v }).ToDictionary(kv => kv.k, kv => kv.v);
        }

        bool IKeyValueStoreProvider.Set(string key, string entity, bool overwrite)
        {
            Client.Put(new WritePolicy(ClientPolicy.writePolicyDefault) {recordExistsAction = overwrite ? RecordExistsAction.REPLACE : RecordExistsAction.CREATE_ONLY},
                key.ToKey(Namespace), entity.ToBin("value"));
            return true;
        }

        bool IKeyValueStoreProvider.SetAll(IDictionary<string, string> keyValues, bool overwrite)
        {
            foreach (var keyValue in keyValues)
            {
                ((IKeyValueStoreProvider)this).Set(keyValue.Key, keyValue.Value, overwrite);
            }
            return true;
        }

        string IKeyValueStoreProvider.Exchange(string key, string value)
        {
            var oldValue = ((IKeyValueStoreProvider)this).Get(key);
            ((IKeyValueStoreProvider)this).Set(key, value, true);
            return oldValue;
        }

        long IKeyValueStoreProvider.AppendString(string key, string value)
        {
            Client.Append(null, key.ToKey(Namespace), value.ToBin("value"));
            return 0;//todo: yeni oluşan stringin boyu
        }

        string IKeyValueStoreProvider.SubString(string key, long start, long end)
        {
            return ((IKeyValueStoreProvider)this).Get(key).Substring((int)start, (int)end - (int)start);
        }

        long IKeyValueStoreProvider.OverwriteString(string key, long offset, string value)
        {
            var oldValue = ((IKeyValueStoreProvider)this).Get(key);
            var newValue = oldValue.Substring(0, (int)offset) + value + (offset + value.Length > oldValue.Length ? "" : oldValue.Substring((int)offset + value.Length));
            ((IKeyValueStoreProvider)this).Set(key, newValue, true);
            return newValue.Length;
        }

        long IKeyValueStoreProvider.SizeInBytes(string key)
        {
            //todo: Aeurospike desteklemiyor. Burası function ile halledilecek.
            return ((IKeyValueStoreProvider)this).Get(key).Length;
        }

        bool IKeyValueStoreProvider.Contains(string key)
        {
            return Client.Exists(null, key.ToKey(Namespace));
        }

        bool IKeyValueStoreProvider.Rename(string oldKey, string newKey)
        {
            //todo: Aeurospike desteklemiyor. Burası function ile halledilecek.
            var value = ((IKeyValueStoreProvider)this).Get(oldKey);
            Remove(oldKey);
            ((IKeyValueStoreProvider)this).Set(newKey, value, true);
            return true;
        }

        long IKeyValueStoreProvider.Increment(string key, long amount)
        {
            //todo: Aeurospike desteklemiyor. Burası function ile halledilecek.
            var value = long.Parse(((IKeyValueStoreProvider)this).Get(key));
            value += amount;
            ((IKeyValueStoreProvider)this).Set(key, value.ToString(), true);
            return value;
        }

        long IKeyValueStoreProvider.Decrement(string key, long amount)
        {
            return ((IKeyValueStoreProvider)this).Increment(key, -amount);
        }
    }
}
