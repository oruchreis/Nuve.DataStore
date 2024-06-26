﻿using System.Collections;
using Nuve.DataStore.Helpers;

namespace Nuve.DataStore;

/// <summary>
/// A store structure that allows set operations similar to <see cref="HashSet{T}"/>.
/// </summary>
/// <typeparam name="TValue"></typeparam>
public sealed class HashSetStore<TValue> : DataStoreBase, ISet<TValue?>, IReadOnlyCollection<TValue?>
{
    private readonly IHashSetStoreProvider _hashSetStoreProvider;
    private static readonly string _valueName = typeof(TValue).GetFriendlyName().Replace('.', '_');
    private static readonly string _typeName = typeof(HashSetStore<TValue>).GetFriendlyName();

    /// <summary>
    /// HashSet value holding store structure.
    /// </summary>
    /// <param name="masterKey">Under which key this dictionary will be stored</param>
    /// <param name="connectionName">Connection name defined in the config</param>
    /// <param name="defaultExpire">Default expire time.</param>
    /// <param name="autoPing">Should Ping be automatically performed on each operation?</param>
    /// <param name="namespaceSeperator">Separator used to separate namespaces. Default is ":".</param>
    /// <param name="overrideRootNamespace">Used to change the root namespace defined in the connection.</param>
    /// <param name="serializer">Set this if you want to use a different serializer instead of the default serializer.</param>
    /// <param name="profiler">Used to profile only the methods of this data store. The global profiler registered in <see cref="DataStoreManager"/> is used whether it is set or not.</param>
    public HashSetStore(string masterKey, string? connectionName = null, TimeSpan? defaultExpire = null, bool autoPing = false,
        string? namespaceSeperator = null, string? overrideRootNamespace = null, IDataStoreSerializer? serializer = null, IDataStoreCompressor? compressor = null,
        IDataStoreProfiler? profiler = null,
        int? compressBiggerThan = null) :
        base(connectionName, defaultExpire, autoPing, namespaceSeperator, overrideRootNamespace, serializer, compressor, profiler, compressBiggerThan)
    {
        _hashSetStoreProvider = Provider as IHashSetStoreProvider
            ?? throw new InvalidOperationException($"The provider with connection '{connectionName}' doesn't support HashSet operations. " +
                "The provider must implement IHashSetStoreProvider interface to use HashSetStore");

        MasterKey = JoinWithRootNamespace(string.Format("{0}<{1}>",
            masterKey,
            _valueName));//It is mandatory to add the type name. Because lists with different types cannot be deserialized in the same masterKey.
    }

    internal override string TypeName
    {
        get { return _typeName; }
    }

    /// <summary>
    /// The full path where the store is held.
    /// </summary>
    public readonly string MasterKey;

    /// <summary>
    /// Is this store exists?
    /// </summary>
    /// <returns></returns>
    public async Task<bool> IsExistsAsync()
    {
        using (new ProfileScope(this, MasterKey))
        {
            return await _hashSetStoreProvider.IsExistsAsync(MasterKey);
        }
    }

    /// <summary>
    /// Is this store exists?
    /// </summary>
    /// <returns></returns>
    public bool IsExists()
    {
        using (new ProfileScope(this, MasterKey))
        {
            return _hashSetStoreProvider.IsExists(MasterKey);
        }
    }

    /// <summary>
    /// If the <see cref="DataStoreBase.DefaultExpire"/> property of the store is set, it resets the expire time to this value.
    /// </summary>
    /// <returns></returns>
    public async Task<bool> PingAsync()
    {
        return await base.PingAsync(MasterKey);
    }

    /// <summary>
    /// If the <see cref="DataStoreBase.DefaultExpire"/> property of the store is set, it resets the expire time to this value.
    /// </summary>
    /// <returns></returns>
    public bool Ping()
    {
        return base.Ping(MasterKey);
    }

    private T CheckAutoPing<T>(Func<T> func)
    {
        var result = func();
        if (AutoPing)
            Task.Run(async () =>
            {
                try
                {
                    await PingAsync();
                }
                catch (Exception e)
                {
                    //intentionally supressed
                }
            });
        return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    public async Task<long> AddAsync(params TValue?[] values)
    {
        using (new ProfileScope(this, MasterKey))
        {
            return await CheckAutoPing(async () => await _hashSetStoreProvider.AddAsync(MasterKey, AsValues(values)));
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    public long Add(params TValue?[] values)
    {
        using (new ProfileScope(this, MasterKey))
        {
            return CheckAutoPing(() => _hashSetStoreProvider.Add(MasterKey, AsValues(values)));
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public async Task<long> CountAsync()
    {
        using (new ProfileScope(this, MasterKey))
        {
            return await CheckAutoPing(async () => await _hashSetStoreProvider.CountAsync(MasterKey));
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public long Count()
    {
        using (new ProfileScope(this, MasterKey))
        {
            return CheckAutoPing(() => _hashSetStoreProvider.Count(MasterKey));
        }
    }

    /// <summary>
    /// Sets the difference with the existing elements using the given values.
    /// </summary>
    /// <param name="values">The values to set the difference with.</param>
    /// <returns>The number of elements in the resulting set.</returns>
    public async Task<long> DifferenceWithAsync(params TValue?[] values)
    {
        using (new ProfileScope(this, MasterKey))
        {
            return await CheckAutoPing(async () =>
            {
                //todo: lock
                var tempKey = $"{MasterKey}__temp_{Guid.NewGuid()}";

                await _hashSetStoreProvider.AddAsync(tempKey, AsValues(values));
                var result = await DifferenceToNewSetAsync(MasterKey, tempKey);
                await Provider.RemoveAsync(tempKey);
                return result;
            });
        }
    }

    /// <summary>
    /// Sets the difference with the existing elements using the given values.
    /// </summary>
    /// <param name="values">The values to set the difference with.</param>
    /// <returns>The number of elements in the resulting set.</returns>
    public long DifferenceWith(params TValue?[] values)
    {
        using (new ProfileScope(this, MasterKey))
        {
            return CheckAutoPing(() =>
            {
                //todo: lock
                var tempKey = $"{MasterKey}__temp_{Guid.NewGuid()}";

                _hashSetStoreProvider.Add(tempKey, AsValues(values));
                var result = DifferenceToNewSet(MasterKey, tempKey);
                Provider.Remove(tempKey);
                return result;
            });
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    public async Task<long> SymmetricDifferenceWithAsync(params TValue?[] values)
    {
        using (new ProfileScope(this, MasterKey))
        {
            return await CheckAutoPing(async () =>
                          {
                              //todo: lock
                              var tempKey1 = $"{MasterKey}__temp_{Guid.NewGuid()}_1";
                              var tempKey2 = $"{MasterKey}__temp_{Guid.NewGuid()}_2";
                              await _hashSetStoreProvider.AddAsync(tempKey1, AsValues(values));
                              //simetrik olduğu için ilk önce sağ tarafın yani tempe göre farkı alalım ve saklayalım.
                              await _hashSetStoreProvider.DifferenceToNewSetAsync(tempKey1, tempKey2, MasterKey);
                              //mevcut set'e farkını alalım ve saklayalım.
                              await DifferenceToNewSetAsync(MasterKey, tempKey1);
                              //bu iksiini birleştirip saklayalım
                              var result = await _hashSetStoreProvider.UnionToNewSetAsync(MasterKey, MasterKey, tempKey2);
                              await Provider.RemoveAsync(tempKey1);
                              await Provider.RemoveAsync(tempKey2);
                              return result;
                          });
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    public long SymmetricDifferenceWith(params TValue?[] values)
    {
        using (new ProfileScope(this, MasterKey))
        {
            return CheckAutoPing(() =>
                          {
                              //todo: lock
                              var tempKey1 = $"{MasterKey}__temp_{Guid.NewGuid()}_1";
                              var tempKey2 = $"{MasterKey}__temp_{Guid.NewGuid()}_2";
                              _hashSetStoreProvider.Add(tempKey1, AsValues(values));
                              //simetrik olduğu için ilk önce sağ tarafın yani tempe göre farkı alalım ve saklayalım.
                              _hashSetStoreProvider.DifferenceToNewSet(tempKey1, tempKey2, MasterKey);
                              //mevcut set'e farkını alalım ve saklayalım.
                              DifferenceToNewSet(MasterKey, tempKey1);
                              //bu iksiini birleştirip saklayalım
                              var result = _hashSetStoreProvider.UnionToNewSet(MasterKey, MasterKey, tempKey2);
                              Provider.Remove(tempKey1);
                              Provider.Remove(tempKey2);
                              return result;
                          });
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="compareHashSetKeys"></param>
    /// <returns></returns>
    public async Task<HashSet<TValue?>> DifferenceToHashSetAsync(params string[] compareHashSetKeys)
    {
        using (new ProfileScope(this, MasterKey))
        {
            return await CheckAutoPing(async () => HashSetResult<TValue>(await _hashSetStoreProvider.DifferenceAsync(MasterKey, compareHashSetKeys)));
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="compareHashSetKeys"></param>
    /// <returns></returns>
    public HashSet<TValue?> DifferenceToHashSet(params string[] compareHashSetKeys)
    {
        using (new ProfileScope(this, MasterKey))
        {
            return CheckAutoPing(() => HashSetResult<TValue?>(_hashSetStoreProvider.Difference(MasterKey, compareHashSetKeys)));
        }
    }

    /// <summary>
    /// Takes the difference and creates a new set as specified by <paramref name="newHashSetKey"/>.
    /// </summary>
    /// <param name="newHashSetKey">The key of the new set to be created.</param>
    /// <param name="compareHashSetKeys">The keys of the sets to compare.</param>
    /// <returns>The number of elements in the difference.</returns>
    public async Task<long> DifferenceToNewSetAsync(string newHashSetKey, params string[] compareHashSetKeys)
    {
        using (new ProfileScope(this, MasterKey))
        {
            return await CheckAutoPing(async () => await _hashSetStoreProvider.DifferenceToNewSetAsync(MasterKey, newHashSetKey, compareHashSetKeys));
        }
    }

    /// <summary>
    /// Takes the difference and creates a new set as specified by <paramref name="newHashSetKey"/>.
    /// </summary>
    /// <param name="newHashSetKey">The key of the new set to be created.</param>
    /// <param name="compareHashSetKeys">The keys of the sets to compare.</param>
    /// <returns>The number of elements in the difference.</returns>
    public long DifferenceToNewSet(string newHashSetKey, params string[] compareHashSetKeys)
    {
        using (new ProfileScope(this, MasterKey))
        {
            return CheckAutoPing(() => _hashSetStoreProvider.DifferenceToNewSet(MasterKey, newHashSetKey, compareHashSetKeys));
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    public async Task<long> IntersectWithAsync(params TValue?[] values)
    {
        using (new ProfileScope(this, MasterKey))
        {
            return await CheckAutoPing(async () =>
                                       {
                                           //todo: lock
                                           var tempKey = $"{MasterKey}__temp_{Guid.NewGuid()}";
                                           await _hashSetStoreProvider.AddAsync(tempKey, AsValues(values));
                                           var result = await IntersectToNewSetAsync(MasterKey, MasterKey, tempKey);
                                           await Provider.RemoveAsync(tempKey);
                                           return result;
                                       });
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    public long IntersectWith(params TValue?[] values)
    {
        using (new ProfileScope(this, MasterKey))
        {
            return CheckAutoPing(() =>
                          {
                              //todo: lock
                              var tempKey = $"{MasterKey}__temp_{Guid.NewGuid()}";
                              _hashSetStoreProvider.Add(tempKey, AsValues(values));
                              var result = IntersectToNewSet(MasterKey, MasterKey, tempKey);
                              Provider.Remove(tempKey);
                              return result;
                          });
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="compareHashSetKeys"></param>
    /// <returns></returns>
    public async Task<HashSet<TValue?>> IntersectToHashSetAsync(params string[] compareHashSetKeys)
    {
        using (new ProfileScope(this, MasterKey))
        {
            return await CheckAutoPing(async () => HashSetResult<TValue?>(await _hashSetStoreProvider.IntersectionAsync(MasterKey, compareHashSetKeys)));
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="compareHashSetKeys"></param>
    /// <returns></returns>
    public HashSet<TValue?> IntersectToHashSet(params string[] compareHashSetKeys)
    {
        using (new ProfileScope(this, MasterKey))
        {
            return CheckAutoPing(() => HashSetResult<TValue?>(_hashSetStoreProvider.Intersection(MasterKey, compareHashSetKeys)));
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="newHashSetKey"></param>
    /// <param name="compareHashSetKeys"></param>
    /// <returns></returns>
    public async Task<long> IntersectToNewSetAsync(string newHashSetKey, params string[] compareHashSetKeys)
    {
        using (new ProfileScope(this, MasterKey))
        {
            return await CheckAutoPing(async () => await _hashSetStoreProvider.IntersectionToNewSetAsync(MasterKey, newHashSetKey, compareHashSetKeys));
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="newHashSetKey"></param>
    /// <param name="compareHashSetKeys"></param>
    /// <returns></returns>
    public long IntersectToNewSet(string newHashSetKey, params string[] compareHashSetKeys)
    {
        using (new ProfileScope(this, MasterKey))
        {
            return CheckAutoPing(() => _hashSetStoreProvider.IntersectionToNewSet(MasterKey, newHashSetKey, compareHashSetKeys));
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="hashSetKeys"></param>
    /// <returns></returns>
    public async Task<HashSet<TValue?>> UnionToHashSetAsync(params string[] hashSetKeys)
    {
        using (new ProfileScope(this, MasterKey))
        {
            return await CheckAutoPing(async () =>
                                       {
                                           var keys = new List<string> { MasterKey };
                                           keys.AddRange(hashSetKeys);
                                           return HashSetResult<TValue?>(await _hashSetStoreProvider.UnionAsync(keys.ToArray()));
                                       });
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="hashSetKeys"></param>
    /// <returns></returns>
    public HashSet<TValue?> UnionToHashSet(params string[] hashSetKeys)
    {
        using (new ProfileScope(this, MasterKey))
        {
            return CheckAutoPing(() =>
                                 {
                                     var keys = new List<string> { MasterKey };
                                     keys.AddRange(hashSetKeys);
                                     return HashSetResult<TValue?>(_hashSetStoreProvider.Union(keys.ToArray()));
                                 });
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="newHashSetKey"></param>
    /// <param name="hashSetKeys"></param>
    /// <returns></returns>
    public async Task<long> UnionToNewSetAsync(string newHashSetKey, params string[] hashSetKeys)
    {
        using (new ProfileScope(this, MasterKey))
        {
            return await CheckAutoPing(async () =>
                                       {
                                           var keys = new List<string> { MasterKey };
                                           keys.AddRange(hashSetKeys);
                                           return await _hashSetStoreProvider.UnionToNewSetAsync(newHashSetKey, keys.ToArray());
                                       });
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="newHashSetKey"></param>
    /// <param name="hashSetKeys"></param>
    /// <returns></returns>
    public long UnionToNewSet(string newHashSetKey, params string[] hashSetKeys)
    {
        using (new ProfileScope(this, MasterKey))
        {
            return CheckAutoPing(() =>
                                 {
                                     var keys = new List<string> { MasterKey };
                                     keys.AddRange(hashSetKeys);
                                     return _hashSetStoreProvider.UnionToNewSet(newHashSetKey, keys.ToArray());
                                 });
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    public async Task<bool> ContainsAsync(params TValue?[] values)
    {
        using (new ProfileScope(this, MasterKey))
        {
            return await CheckAutoPing(async () =>
                                       {
                                           var results = await Task.WhenAll(values.Select(async value => await _hashSetStoreProvider.ContainsAsync(MasterKey, AsValue(value))));
                                           return results.All(r => r);
                                       });
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    public bool Contains(params TValue?[] values)
    {
        using (new ProfileScope(this, MasterKey))
        {
            return CheckAutoPing(() => values.Select(value => _hashSetStoreProvider.Contains(MasterKey, AsValue(value))).All(r => r));
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="otherHashSetKey"></param>
    /// <returns></returns>
    public async Task<bool> IsSubsetOfAsync(string otherHashSetKey)
    {
        using (new ProfileScope(this, MasterKey))
        {
            return await CheckAutoPing(async () =>
                                       {
                                           //todo: lock
                                           var tempKey = $"{MasterKey}__temp_{Guid.NewGuid()}";
                                           //kesişimi bir yer kaydedelim.
                                           await _hashSetStoreProvider.IntersectionToNewSetAsync(otherHashSetKey, tempKey, MasterKey);
                                           //eğer kesişimin sayısı ile bizim sayımız eşitse alt kümedir.
                                           var result = await CountAsync() == await _hashSetStoreProvider.CountAsync(tempKey);
                                           await Provider.RemoveAsync(tempKey);
                                           return result;
                                       });
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="otherHashSetKey"></param>
    /// <returns></returns>
    public bool IsSubsetOf(string otherHashSetKey)
    {
        using (new ProfileScope(this, MasterKey))
        {
            return CheckAutoPing(() =>
                                 {
                                     //todo: lock
                                     var tempKey = $"{MasterKey}__temp_{Guid.NewGuid()}";
                                     //kesişimi bir yer kaydedelim.
                                     _hashSetStoreProvider.IntersectionToNewSet(otherHashSetKey, tempKey, MasterKey);
                                     //eğer kesişimin sayısı ile bizim sayımız eşitse alt kümedir.
                                     var result = Count() == _hashSetStoreProvider.Count(tempKey);
                                     Provider.Remove(tempKey);
                                     return result;
                                 });
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="otherHashSetKey"></param>
    /// <returns></returns>
    public async Task<bool> IsSupersetOfAsync(string otherHashSetKey)
    {
        using (new ProfileScope(this, MasterKey))
        {
            return await CheckAutoPing(async () =>
                                       {
                                           //todo: lock
                                           var tempKey = $"{MasterKey}__temp_{Guid.NewGuid()}";
                                           //kesişimi bir yer kaydedelim.
                                           await _hashSetStoreProvider.IntersectionToNewSetAsync(MasterKey, tempKey, otherHashSetKey);
                                           //eğer kesişimin sayısı ile otherhash'in sayısı eşitse alt kümedir.
                                           var result = await _hashSetStoreProvider.CountAsync(otherHashSetKey) == await _hashSetStoreProvider.CountAsync(tempKey);
                                           await Provider.RemoveAsync(tempKey);
                                           return result;
                                       });
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="otherHashSetKey"></param>
    /// <returns></returns>
    public bool IsSupersetOf(string otherHashSetKey)
    {
        using (new ProfileScope(this, MasterKey))
        {
            return CheckAutoPing(() =>
                                 {
                                     //todo: lock
                                     var tempKey = $"{MasterKey}__temp_{Guid.NewGuid()}";
                                     //kesişimi bir yer kaydedelim.
                                     _hashSetStoreProvider.IntersectionToNewSet(MasterKey, tempKey, otherHashSetKey);
                                     //eğer kesişimin sayısı ile otherhash'in sayısı eşitse alt kümedir.
                                     var result = _hashSetStoreProvider.Count(otherHashSetKey) == _hashSetStoreProvider.Count(tempKey);
                                     Provider.Remove(tempKey);
                                     return result;
                                 });
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="otherHashSetKey"></param>
    /// <returns></returns>
    public async Task<bool> OverlapsAsync(string otherHashSetKey)
    {
        using (new ProfileScope(this, MasterKey))
        {
            return await CheckAutoPing(async () =>
                                       {
                                           var tempKey = $"{MasterKey}__temp_{Guid.NewGuid()}";
                                           //kesişimi bir yer kaydedelim.
                                           await _hashSetStoreProvider.IntersectionToNewSetAsync(MasterKey, tempKey, otherHashSetKey);
                                           var result = await _hashSetStoreProvider.CountAsync(tempKey) > 0;
                                           await Provider.RemoveAsync(tempKey);
                                           return result;
                                       });
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="otherHashSetKey"></param>
    /// <returns></returns>
    public bool Overlaps(string otherHashSetKey)
    {
        using (new ProfileScope(this, MasterKey))
        {
            return CheckAutoPing(() =>
                          {
                              var tempKey = $"{MasterKey}__temp_{Guid.NewGuid()}";
                              //kesişimi bir yer kaydedelim.
                              _hashSetStoreProvider.IntersectionToNewSet(MasterKey, tempKey, otherHashSetKey);
                              var result = _hashSetStoreProvider.Count(tempKey) > 0;
                              Provider.Remove(tempKey);
                              return result;
                          });
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public async Task<HashSet<TValue?>> GetHashSetAsync()
    {
        using (new ProfileScope(this, MasterKey))
        {
            return await CheckAutoPing(async () => HashSetResult<TValue?>(await _hashSetStoreProvider.GetHashSetAsync(MasterKey)));
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public HashSet<TValue?> GetHashSet()
    {
        using (new ProfileScope(this, MasterKey))
        {
            return CheckAutoPing(() => HashSetResult<TValue?>(_hashSetStoreProvider.GetHashSet(MasterKey)));
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="destinationHashSetKey"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public async Task<bool> MoveValueAsync(string destinationHashSetKey, TValue? value)
    {
        using (new ProfileScope(this, MasterKey))
        {
            return await CheckAutoPing(async () => await _hashSetStoreProvider.MoveValueAsync(MasterKey, destinationHashSetKey, AsValue(value)));
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="destinationHashSetKey"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool MoveValue(string destinationHashSetKey, TValue? value)
    {
        using (new ProfileScope(this, MasterKey))
        {
            return CheckAutoPing(() => _hashSetStoreProvider.MoveValue(MasterKey, destinationHashSetKey, AsValue(value)));
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    public async Task<long> RemoveAsync(params TValue?[] values)
    {
        using (new ProfileScope(this, MasterKey))
        {
            return await CheckAutoPing(async () => await _hashSetStoreProvider.RemoveAsync(MasterKey, AsValues(values)));
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    public long Remove(params TValue?[] values)
    {
        using (new ProfileScope(this, MasterKey))
        {
            return CheckAutoPing(() => _hashSetStoreProvider.Remove(MasterKey, AsValues(values)));
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public async Task<bool> ClearAsync()
    {
        using (new ProfileScope(this, MasterKey))
        {
            return await Provider.RemoveAsync(MasterKey);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public bool Clear()
    {
        using (new ProfileScope(this, MasterKey))
        {
            return Provider.Remove(MasterKey);
        }
    }

    /// <summary>
    /// Creates a lock based on the given key.
    /// </summary>
    /// <param name="key">Which key to lock</param>
    /// <param name="waitTimeout"></param>
    /// <param name="action"></param>
    /// <param name="skipWhenTimeout">Should the action be skipped when a timeout occurs?</param>
    /// <param name="throwWhenTimeout">Should a <see cref="TimeoutException"/> be thrown when a timeout occurs?</param>
    public void Lock(string key, TimeSpan waitTimeout, Action action, bool skipWhenTimeout = true, bool throwWhenTimeout = false, TimeSpan? slidingExpire = null)
    {
        var lockKey = $"{MasterKey}_locker_{NamespaceSeperator}{key}";
        using (new ProfileScope(this, lockKey))
        {
            Provider.Lock(lockKey, waitTimeout, action, slidingExpire ?? TimeSpan.FromSeconds(30), skipWhenTimeout, throwWhenTimeout);
        }
    }

    /// <summary>
    /// Creates a lock based on the given key.
    /// </summary>
    /// <param name="key">Which key to lock</param>
    /// <param name="waitTimeout"></param>
    /// <param name="action"></param>
    /// <param name="skipWhenTimeout">Should the action be skipped when a timeout occurs?</param>
    /// <param name="throwWhenTimeout">Should a <see cref="TimeoutException"/> be thrown when a timeout occurs?</param>
    public async Task LockAsync(string key, TimeSpan waitTimeout, Func<Task> action, bool skipWhenTimeout = true, bool throwWhenTimeout = false, TimeSpan? slidingExpire = null)
    {
        var lockKey = $"{MasterKey}_locker_{NamespaceSeperator}{key}";
        using (new ProfileScope(this, lockKey))
        {
            await Provider.LockAsync(lockKey, waitTimeout, action, slidingExpire ?? TimeSpan.FromSeconds(30), skipWhenTimeout, throwWhenTimeout);
        }
    }

    #region Interfaces
    [Obsolete("This method enumerates all items.")]
    public IEnumerator<TValue?> GetEnumerator()
    {
        return GetHashSet().GetEnumerator();
    }

    [Obsolete("This method enumerates all items.")]
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    void ICollection<TValue?>.Add(TValue? item)
    {
        Add(item);
    }

    bool ISet<TValue?>.Add(TValue? item)
    {
        return Add(item) > 0;
    }

    public void UnionWith(IEnumerable<TValue?> other)
    {
        Add(other.ToArray());
    }

    void ISet<TValue?>.IntersectWith(IEnumerable<TValue?> other)
    {
        IntersectWith(other.ToArray());
    }

    void ISet<TValue?>.ExceptWith(IEnumerable<TValue?> other)
    {
        DifferenceWith(other.ToArray());
    }

    void ISet<TValue?>.SymmetricExceptWith(IEnumerable<TValue?> other)
    {
        SymmetricDifferenceWith(other.ToArray());
    }

    public bool IsSubsetOf(IEnumerable<TValue?> other)
    {
        var hashSetStore = other as HashSetStore<TValue?>;
        if (hashSetStore != null)
        {
            return IsSubsetOf(hashSetStore.MasterKey);
        }

        var hashSet = other as HashSet<TValue?> ?? new HashSet<TValue?>(other);
        return hashSet.IsSubsetOf(GetHashSet());
    }

    public bool IsSupersetOf(IEnumerable<TValue?> other)
    {
        var hashSetStore = other as HashSetStore<TValue?>;
        if (hashSetStore != null)
        {
            return IsSupersetOf(hashSetStore.MasterKey);
        }

        return Contains(other.ToArray());
    }

    public bool IsProperSupersetOf(IEnumerable<TValue?> other)
    {
        var hashSetStore = other as HashSetStore<TValue?>;
        var collection = other as ICollection<TValue?>;
        if (hashSetStore != null && hashSetStore.Count() == Count())
            return false;
        else if (collection != null && collection.Count == Count())
            return false;
        else if (other.Count() == Count())
            return false;

        return IsSupersetOf(other);
    }

    public bool IsProperSubsetOf(IEnumerable<TValue?> other)
    {
        var hashSetStore = other as HashSetStore<TValue?>;
        var collection = other as ICollection<TValue?>;
        if (hashSetStore != null && hashSetStore.Count() == Count())
            return false;
        else if (collection != null && collection.Count == Count())
            return false;
        else if (other.Count() == Count())
            return false;

        return IsSubsetOf(other);
    }

    public bool Overlaps(IEnumerable<TValue?> other)
    {
        var hashSetStore = other as HashSetStore<TValue?>;
        if (hashSetStore != null)
            return Overlaps(hashSetStore.MasterKey);

        return other.Any(i => Contains(i));
    }

    public bool SetEquals(IEnumerable<TValue?> other)
    {
        return IsSubsetOf(other) && IsSupersetOf(other);
    }

    void ICollection<TValue?>.Clear()
    {
        Clear();
    }

    bool ICollection<TValue?>.Contains(TValue? item)
    {
        return Contains(item);
    }

    [Obsolete("This method enumerates all items.")]
    public void CopyTo(TValue?[] array, int arrayIndex)
    {
        GetHashSet().CopyTo(array, arrayIndex);
    }

    bool ICollection<TValue?>.Remove(TValue? item)
    {
        return Remove(item) > 0;
    }

    int IReadOnlyCollection<TValue?>.Count
    {
        get { return (int)Count(); }
    }

    int ICollection<TValue?>.Count
    {
        get { return (int)Count(); }
    }

    public bool IsReadOnly
    {
        get { return false; }
    }
    #endregion
}
