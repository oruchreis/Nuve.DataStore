using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nuve.DataStore.Aerospike
{
    public partial class AerospikeStoreProvider: IHashSetStoreProvider
    {
        async Task<bool> IHashSetStoreProvider.IsExistsAsync(string hashSetKey)
        {
            return await Client.Exists(null, CancellationToken.None, hashSetKey.ToKey(Namespace));
        }

        async Task<long> IHashSetStoreProvider.AddAsync(string hashSetKey, params string[] values)
        {
            await Client.Put(null, CancellationToken.None, hashSetKey.ToKey(Namespace), values.ToBins(""));
            return values.Length;
        }

        async Task<long> IHashSetStoreProvider.CountAsync(string hashSetKey)
        {
            return (await Client.Get(null, CancellationToken.None, hashSetKey.ToKey(Namespace)))?.bins.Count ?? 0;
        }

        async Task<HashSet<string>> IHashSetStoreProvider.DifferenceAsync(string hashSetKey, params string[] compareHashSetKeys)
        {
            var provider = (IHashSetStoreProvider) this;
            var hashSet = await provider.GetHashSetAsync(hashSetKey);
			var compareList = new List<string>();
            foreach (var compareHashSetKey in compareHashSetKeys)
            {
                compareList.AddRange(await provider.GetHashSetAsync(compareHashSetKey));
            }
            hashSet.ExceptWith(compareList);
            return hashSet;
        }

        async Task<long> IHashSetStoreProvider.DifferenceToNewSetAsync(string hashSetKey, string newHashSetKey, params string[] compareHashSetKeys)
        {
            var provider = (IHashSetStoreProvider)this;
            var newHashSet = await provider.DifferenceAsync(hashSetKey, compareHashSetKeys);
            return await provider.AddAsync(newHashSetKey, newHashSet.ToArray());
        }

        async Task<HashSet<string>> IHashSetStoreProvider.IntersectionAsync(string hashSetKey, params string[] compareHashSetKeys)
        {
            var provider = (IHashSetStoreProvider)this;
            var hashSet = await provider.GetHashSetAsync(hashSetKey);
            var compareList = new List<string>();
            foreach (var compareHashSetKey in compareHashSetKeys)
            {
                compareList.AddRange(await provider.GetHashSetAsync(compareHashSetKey));
            }
            hashSet.IntersectWith(compareList);
            return hashSet;
        }

        async Task<long> IHashSetStoreProvider.IntersectionToNewSetAsync(string hashSetKey, string newHashSetKey, params string[] compareHashSetKeys)
        {
            var provider = (IHashSetStoreProvider)this;
            var newHashSet = await provider.IntersectionAsync(hashSetKey, compareHashSetKeys);
            return await provider.AddAsync(newHashSetKey, newHashSet.ToArray());
        }

        async Task<HashSet<string>> IHashSetStoreProvider.UnionAsync(params string[] hashSetKeys)
        {
            var provider = (IHashSetStoreProvider)this;
            var union = new HashSet<string>();
            foreach (var hashSetKey in hashSetKeys)
            {
                union.UnionWith(await provider.GetHashSetAsync(hashSetKey));
            }            
            return union;
        }

        async Task<long> IHashSetStoreProvider.UnionToNewSetAsync(string newHashSetKey, params string[] hashSetKeys)
        {
            var provider = (IHashSetStoreProvider)this;
            var newHashSet = await provider.UnionAsync(hashSetKeys);
            return await provider.AddAsync(newHashSetKey, newHashSet.ToArray());
        }

        async Task<bool> IHashSetStoreProvider.ContainsAsync(string hashSetKey, string value)
        {
            return (await Client.Get(null, CancellationToken.None, hashSetKey.ToKey(Namespace)))?.bins.ContainsKey(value) ?? false;
        }

        async Task<HashSet<string>> IHashSetStoreProvider.GetHashSetAsync(string hashSetKey)
        {
            return new HashSet<string>((await Client.Get(null, CancellationToken.None, hashSetKey.ToKey(Namespace)))?.bins.Keys.AsEnumerable() ?? new string[0]);
        }

        async Task<bool> IHashSetStoreProvider.MoveValueAsync(string hashSetKey, string destHashSetKey, string value)
        {
            var provider = (IHashSetStoreProvider)this;
            await provider.RemoveAsync(hashSetKey, value);
            await provider.AddAsync(destHashSetKey, value);
            return true;
        }

        async Task<long> IHashSetStoreProvider.RemoveAsync(string hashSetKey, params string[] values)
        {
            await Client.Put(null, CancellationToken.None, hashSetKey.ToKey(Namespace), values.ToNullBins());
            return values.Length;
        }

        bool IHashSetStoreProvider.IsExists(string hashSetKey)
        {
            return Client.Exists(null, hashSetKey.ToKey(Namespace));
        }

        long IHashSetStoreProvider.Add(string hashSetKey, params string[] values)
        {
            Client.Put(null, hashSetKey.ToKey(Namespace), values.ToBins(""));
            return values.Length;
        }

        long IHashSetStoreProvider.Count(string hashSetKey)
        {
            return Client.Get(null, hashSetKey.ToKey(Namespace))?.bins.Count ?? 0;
        }

        HashSet<string> IHashSetStoreProvider.Difference(string hashSetKey, params string[] compareHashSetKeys)
        {
            var provider = (IHashSetStoreProvider)this;
            var hashSet = provider.GetHashSet(hashSetKey);
            var compareList = new List<string>();
            foreach (var compareHashSetKey in compareHashSetKeys)
            {
                compareList.AddRange(provider.GetHashSet(compareHashSetKey));
            }
            hashSet.ExceptWith(compareList);
            return hashSet;
        }

        long IHashSetStoreProvider.DifferenceToNewSet(string hashSetKey, string newHashSetKey, params string[] compareHashSetKeys)
        {
            var provider = (IHashSetStoreProvider)this;
            var newHashSet = provider.Difference(hashSetKey, compareHashSetKeys);
            return provider.Add(newHashSetKey, newHashSet.ToArray());
        }

        HashSet<string> IHashSetStoreProvider.Intersection(string hashSetKey, params string[] compareHashSetKeys)
        {
            var provider = (IHashSetStoreProvider)this;
            var hashSet = provider.GetHashSet(hashSetKey);
            var compareList = new List<string>();
            foreach (var compareHashSetKey in compareHashSetKeys)
            {
                compareList.AddRange(provider.GetHashSet(compareHashSetKey));
            }
            hashSet.IntersectWith(compareList);
            return hashSet;
        }

        long IHashSetStoreProvider.IntersectionToNewSet(string hashSetKey, string newHashSetKey, params string[] compareHashSetKeys)
        {
            var provider = (IHashSetStoreProvider)this;
            var newHashSet = provider.Intersection(hashSetKey, compareHashSetKeys);
            return provider.Add(newHashSetKey, newHashSet.ToArray());
        }

        HashSet<string> IHashSetStoreProvider.Union(params string[] hashSetKeys)
        {
            var provider = (IHashSetStoreProvider)this;
            var union = new HashSet<string>();
            foreach (var hashSetKey in hashSetKeys)
            {
                union.UnionWith(provider.GetHashSet(hashSetKey));
            }
            return union;
        }

        long IHashSetStoreProvider.UnionToNewSet(string newHashSetKey, params string[] hashSetKeys)
        {
            var provider = (IHashSetStoreProvider)this;
            var newHashSet = provider.Union(hashSetKeys);
            return provider.Add(newHashSetKey, newHashSet.ToArray());
        }

        bool IHashSetStoreProvider.Contains(string hashSetKey, string value)
        {
            return Client.Get(null, hashSetKey.ToKey(Namespace))?.bins.ContainsKey(value) ?? false;
        }

        HashSet<string> IHashSetStoreProvider.GetHashSet(string hashSetKey)
        {
            return new HashSet<string>(Client.Get(null, hashSetKey.ToKey(Namespace))?.bins.Keys.AsEnumerable() ?? new string[0]);
        }

        bool IHashSetStoreProvider.MoveValue(string hashSetKey, string destHashSetKey, string value)
        {
            var provider = (IHashSetStoreProvider)this;
            provider.Remove(hashSetKey, value);
            provider.Add(destHashSetKey, value);
            return true;
        }

        long IHashSetStoreProvider.Remove(string hashSetKey, params string[] values)
        {
            Client.Put(null, hashSetKey.ToKey(Namespace), values.ToNullBins());
            return values.Length;
        }
    }
}
