using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nuve.DataStore.Helpers;

namespace Nuve.DataStore;

/// <summary>
/// Multi-type Dictionary structure. Unlike <see cref="DictionaryStore{TValue}"/>, type must be specified for each operation.
/// </summary>
public sealed class HashStore : DataStoreBase
{
    private readonly IDictionaryStoreProvider _dictionaryStoreProvider;
    private static readonly string _typeName = typeof(HashStore).GetFriendlyName();

    /// <summary>
    /// Store structure that holds dictionary values.
    /// </summary>
    /// <param name="masterKey">Under which key this dictionary will be stored</param>
    /// <param name="connectionName">Connection name defined in the config</param>
    /// <param name="defaultExpire">Default expiration time</param>
    /// <param name="autoPing">Should automatic Ping be performed for each operation?</param>
    /// <param name="namespaceSeperator">Separator used to separate namespaces. Default is ":"</param>
    /// <param name="overrideRootNamespace">Used to change the defined root namespace for the connection</param>
    /// <param name="serializer">Set this if you want to use a different serializer instead of the default one</param>
    /// <param name="profiler">Used to profile only the methods of this data store. The global profiler registered in <see cref="DataStoreManager"/> is used whether it is set or not.</param>
    public HashStore(string masterKey, string? connectionName = null, TimeSpan? defaultExpire = null, bool autoPing = false,
        string? namespaceSeperator = null, string? overrideRootNamespace = null, IDataStoreSerializer? serializer = null, IDataStoreCompressor? compressor = null, IDataStoreProfiler? profiler = null,
        int? compressBiggerThan = null) :
        base(connectionName, defaultExpire, autoPing, namespaceSeperator, overrideRootNamespace, serializer, compressor, profiler, compressBiggerThan)
    {
        _dictionaryStoreProvider = Provider as IDictionaryStoreProvider
            ?? throw new InvalidOperationException(
                $"The provider with connection '{connectionName}' doesn't support Dictionary operations. " +
                "The provider must implement IDictionaryStoreProvider interface to use HashStore");
        MasterKey = JoinWithRootNamespace(masterKey);
    }

    internal override string TypeName => _typeName;

    /// <summary>
    /// The full path where the store is located.
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
            return await _dictionaryStoreProvider.IsExistsAsync(MasterKey);
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

    private void CheckAutoPing(Action action)
    {
        action();
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
    /// Gets the expiration time of the MasterKey.
    /// </summary>
    /// <returns>The expiration time as a TimeSpan.</returns>
    public async Task<TimeSpan?> GetExpireAsync()
    {
        using (new ProfileScope(this, MasterKey))
        {
            return await Provider.GetExpireAsync(MasterKey);
        }
    }

    /// <summary>
    /// Adds a key-value pair to the store.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <param name="overwrite">Whether to overwrite the existing value if the key already exists.</param>
    /// <returns>True if the addition is successful, otherwise false.</returns>
    public async Task<bool> AddAsync<TValue>(string key, TValue value, bool overwrite = true)
    {
        using (new ProfileScope(this, key))
        {
            return await CheckAutoPing(async () => await _dictionaryStoreProvider.SetAsync(MasterKey, key, AsValue(value), overwrite));
        }
    }

    /// <summary>
    /// Clears the store.
    /// </summary>
    /// <returns>True if the store is cleared successfully, otherwise false.</returns>
    public async Task<bool> ClearAsync()
    {
        using (new ProfileScope(this, MasterKey))
        {
            return await Provider.RemoveAsync(MasterKey);
        }
    }

    /// <summary>
    /// Checks if a key exists in the store.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key exists, otherwise false.</returns>
    public async Task<bool> ContainsKeyAsync(string key)
    {
        using (new ProfileScope(this, key))
        {
            return await CheckAutoPing(async () => await _dictionaryStoreProvider.ContainsAsync(MasterKey, key));
        }
    }

    /// <summary>
    /// Checks if a key-value pair exists in the store.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns>True if the key-value pair exists, otherwise false.</returns>
    public async Task<bool> ContainsAsync<TValue>(string key, TValue value)
    {
        using (new ProfileScope(this, key))
        {
            return await CheckAutoPing(
                async () =>
                {
                    var keyExists = await _dictionaryStoreProvider.ContainsAsync(MasterKey, key);
                    if (!keyExists)
                        return await Task.FromResult(false);
                    return await Task.FromResult(EqualityComparer<TValue?>.Default.Equals(await GetAsync<TValue?>(key), value));
                });
        }
    }
    /// <summary>
    /// Removes keys from the store.
    /// </summary>
    /// <param name="keys"></param>
    /// <returns></returns>
    public async Task<long> RemoveAsync(params string[] keys)
    {
        if (keys == null || keys.Length == 0)
            return 0;
        using (new ProfileScope(this, keys.First()))
        {
            return await CheckAutoPing(async () => await _dictionaryStoreProvider.RemoveAsync(MasterKey, keys));
        }
    }

    /// <summary>
    /// Retrieves the value of a key.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="key"></param>
    /// <returns></returns>
    public async Task<TValue?> GetAsync<TValue>(string key)
    {
        using (new ProfileScope(this, key))
        {
            return await CheckAutoPing(async () => SingleResult<TValue>(await _dictionaryStoreProvider.GetAsync(MasterKey, key)));
        }
    }

    /// <summary>
    /// Retrieves the value of a key.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public async Task<object?> GetAsync(string key, Type type)
    {
        using (new ProfileScope(this, key))
        {
            return await CheckAutoPing(async () => SingleResult(await _dictionaryStoreProvider.GetAsync(MasterKey, key), type));
        }
    }

    /// <summary>
    /// Retrieves the values of multiple keys. Use this for querying multiple keys.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="keys"></param>
    /// <returns></returns>
    public async Task<IDictionary<string, TValue?>> GetAsync<TValue>(params string[] keys)
    {
        using (new ProfileScope(this, MasterKey))
        {
            return await CheckAutoPing(async () => DictionaryResult<TValue?>(await _dictionaryStoreProvider.GetAsync(MasterKey, keys)));
        }
    }

    /// <summary>
    /// Retrieves the values of multiple keys. Use this for querying multiple keys.
    /// </summary>
    /// <param name="keysTypes"></param>
    /// <returns></returns>
    public async Task<IDictionary<string, object?>> GetAsync(IDictionary<string, Type> keysTypes)
    {
        using (new ProfileScope(this, MasterKey))
        {
            return await CheckAutoPing(async () => DictionaryResult(await _dictionaryStoreProvider.GetAsync(MasterKey, keysTypes.Keys.ToArray()), keysTypes));
        }
    }

    /// <summary>
    /// Sets a value for a key.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="overwrite">Should it overwrite?</param>
    /// <returns></returns>
    public async Task<bool> SetAsync<TValue>(string key, TValue value, bool overwrite = true)
    {
        using (new ProfileScope(this, key))
        {
            return await CheckAutoPing(async () => await _dictionaryStoreProvider.SetAsync(MasterKey, key, AsValue(value), overwrite));
        }
    }

    /// <summary>
    /// Assigns values to multiple keys. If multiple keys need to be assigned values, this method provides faster results.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="keyValues"></param>
    /// <param name="overwrite">Should it overwrite?</param>
    /// <returns></returns>
    public async Task SetAsync<TValue>(IDictionary<string, TValue?> keyValues, bool overwrite = true, bool serializeParallel = false, ParallelOptions? parallelOptions = null)
    {
        using (new ProfileScope(this, MasterKey))
        {
            await CheckAutoPing(async () => await _dictionaryStoreProvider.SetAsync(MasterKey, AsKeyValue(keyValues, serializeParallel, parallelOptions)));
        }
    }

    /// <summary>
    /// Returns the number of elements in the store.
    /// </summary>
    /// <returns></returns>
    public async Task<long> CountAsync()
    {
        using (new ProfileScope(this, MasterKey))
        {
            return await CheckAutoPing(async () => await _dictionaryStoreProvider.CountAsync(MasterKey));
        }
    }
    /// <summary>
    /// Returns the content as a <see cref="Dictionary{TKey,TValue}"/>.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <returns></returns>
    public async Task<IDictionary<string, TValue?>> ToDictionaryAsync<TValue>()
    {
        using (new ProfileScope(this, MasterKey))
        {
            return await CheckAutoPing(async () => DictionaryResult<TValue?>(await _dictionaryStoreProvider.GetDictionaryAsync(MasterKey)));
        }
    }

    /// <summary>
    /// Returns the content as a <see cref="Dictionary{TKey,TValue}"/>.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <returns></returns>
    public async Task<IDictionary<string, object?>> ToDictionaryAsync(IDictionary<string, Type> keysTypes)
    {
        using (new ProfileScope(this, MasterKey))
        {
            return await CheckAutoPing(async () => DictionaryResult(await _dictionaryStoreProvider.GetDictionaryAsync(MasterKey), keysTypes));
        }
    }

    /// <summary>
    /// Returns the keys in the store.
    /// </summary>
    /// <returns></returns>
    public async Task<IList<string>> KeysAsync()
    {
        using (new ProfileScope(this, MasterKey))
        {
            return await CheckAutoPing(async () => await _dictionaryStoreProvider.KeysAsync(MasterKey));
        }
    }

    /// <summary>
    /// Returns the values in the store.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <returns></returns>
    public async Task<IList<TValue?>> ValuesAsync<TValue>()
    {
        using (new ProfileScope(this, MasterKey))
        {
            return await CheckAutoPing(async () => ListResult<TValue?>(await _dictionaryStoreProvider.ValuesAsync(MasterKey)));
        }
    }

    /// <summary>
    /// Increments the integer value in a key by the specified <paramref name="value"/>.
    /// </summary>
    /// <remarks>By default, it increments by 1.</remarks>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public async Task<long> IncrementAsync(string key, long value = 1)
    {
        using (new ProfileScope(this, key))
        {
            return await CheckAutoPing(async () => await _dictionaryStoreProvider.IncrementAsync(MasterKey, key, value));
        }
    }

    /// <summary>
    /// Returns the total size of the serialized data in bytes for a key.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public async Task<long> SizeInBytesAsync(string key)
    {
        using (new ProfileScope(this, key))
        {
            return await CheckAutoPing(async () => await _dictionaryStoreProvider.SizeInBytesAsync(MasterKey, key));
        }
    }

    /// <summary>
    /// Creates a lock for the given key.
    /// </summary>
    /// <param name="key">The key to be locked.</param>
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
    /// Creates a lock for the given key.
    /// </summary>
    /// <param name="key">The key to be locked.</param>
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

    /// <summary>
    /// Is this store exists?
    /// </summary>
    /// <returns></returns>
    public bool IsExists()
    {
        using (new ProfileScope(this, MasterKey))
        {
            return _dictionaryStoreProvider.IsExists(MasterKey);
        }
    }

    /// <summary>
    /// Resets the expiration time to the value of <see cref="DataStoreBase.DefaultExpire"/> if set.
    /// </summary>
    /// <returns></returns>
    public bool Ping()
    {
        return base.Ping(MasterKey);
    }

    /// <summary>
    /// Expiration time of the MasterKey.
    /// </summary>
    /// <returns></returns>
    public TimeSpan? GetExpire()
    {
        using (new ProfileScope(this, MasterKey))
        {
            return Provider.GetExpire(MasterKey);
        }
    }

    /// <summary>
    /// Adds an item to the store.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="key">The key of the item.</param>
    /// <param name="value">The value of the item.</param>
    /// <param name="overwrite">Should it overwrite if the item already exists?</param>
    /// <returns>True if the item is added successfully, otherwise false.</returns>
    public bool Add<TValue>(string key, TValue value, bool overwrite = true)
    {
        using (new ProfileScope(this, key))
        {
            return CheckAutoPing(() => _dictionaryStoreProvider.Set(MasterKey, key, AsValue(value), overwrite));
        }
    }

    /// <summary>
    /// Clears the store.
    /// </summary>
    /// <returns>True if the store is cleared successfully, otherwise false.</returns>
    public bool Clear()
    {
        using (new ProfileScope(this, MasterKey))
        {
            return Provider.Remove(MasterKey);
        }
    }

    /// <summary>
    /// Checks if a key exists in the store.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key exists, otherwise false.</returns>
    public bool ContainsKey(string key)
    {
        using (new ProfileScope(this, key))
        {
            return CheckAutoPing(() => _dictionaryStoreProvider.Contains(MasterKey, key));
        }
    }

    /// <summary>
    /// Checks if a key-value pair exists in the store.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="key">The key to check.</param>
    /// <param name="value">The value to check.</param>
    /// <returns>True if the key-value pair exists, otherwise false.</returns>
    public bool Contains<TValue>(string key, TValue value)
    {
        using (new ProfileScope(this, key))
        {
            return CheckAutoPing(() =>
            {
                var keyExists = _dictionaryStoreProvider.Contains(MasterKey, key);
                if (!keyExists)
                    return false;
                return EqualityComparer<TValue?>.Default.Equals(Get<TValue?>(key), value);
            });
        }
    }
    /// <summary>
    /// Removes keys from the store.
    /// </summary>
    /// <param name="keys"></param>
    /// <returns></returns>
    public long Remove(params string[] keys)
    {
        if (keys == null || keys.Length == 0)
            return 0;
        using (new ProfileScope(this, keys.First()))
        {
            return CheckAutoPing(() => _dictionaryStoreProvider.Remove(MasterKey, keys));
        }
    }

    /// <summary>
    /// Retrieves the value of a key.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="key"></param>
    /// <returns></returns>
    public TValue? Get<TValue>(string key)
    {
        using (new ProfileScope(this, key))
        {
            return CheckAutoPing(() => SingleResult<TValue?>(_dictionaryStoreProvider.Get(MasterKey, key)));
        }
    }

    /// <summary>
    /// Retrieves the value of a key.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public object? Get(string key, Type type)
    {
        using (new ProfileScope(this, key))
        {
            return CheckAutoPing(() => SingleResult(_dictionaryStoreProvider.Get(MasterKey, key), type));
        }
    }

    /// <summary>
    /// Retrieves the values of multiple keys. Use this for querying multiple keys.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="keys"></param>
    /// <returns></returns>
    public IDictionary<string, TValue?> Get<TValue>(params string[] keys)
    {
        using (new ProfileScope(this, MasterKey))
        {
            return CheckAutoPing(() => DictionaryResult<TValue?>(_dictionaryStoreProvider.Get(MasterKey, keys)));
        }
    }
    /// <summary>
    /// Retrieves the values of multiple keys. Use this for querying multiple keys.
    /// </summary>
    /// <param name="keysTypes"></param>
    /// <returns></returns>
    public IDictionary<string, object?> Get(IDictionary<string, Type> keysTypes)
    {
        using (new ProfileScope(this, MasterKey))
        {
            return CheckAutoPing(() => DictionaryResult(_dictionaryStoreProvider.Get(MasterKey, keysTypes.Keys.ToArray()), keysTypes));
        }
    }

    /// <summary>
    /// Sets a value for a key.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="overwrite">Should it overwrite?</param>
    /// <returns></returns>
    public bool Set<TValue>(string key, TValue value, bool overwrite = true)
    {
        using (new ProfileScope(this, key))
        {
            return CheckAutoPing(() => _dictionaryStoreProvider.Set(MasterKey, key, AsValue(value), overwrite));
        }
    }

    /// <summary>
    /// Sets values for multiple keys. If you need to set values for multiple keys, this provides faster results.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="keyValues"></param>
    /// <param name="overwrite">Should it overwrite?</param>
    /// <returns></returns>
    public void Set<TValue>(IDictionary<string, TValue?> keyValues, bool overwrite = true, bool serializeParallel = false, ParallelOptions? parallelOptions = null)
    {
        using (new ProfileScope(this, MasterKey))
        {
            CheckAutoPing(() => _dictionaryStoreProvider.Set(MasterKey, AsKeyValue(keyValues, serializeParallel, parallelOptions)));
        }
    }

    /// <summary>
    /// Returns the number of elements in the store.
    /// </summary>
    /// <returns></returns>
    public long Count()
    {
        using (new ProfileScope(this, MasterKey))
        {
            return CheckAutoPing(() => _dictionaryStoreProvider.Count(MasterKey));
        }
    }

    /// <summary>
    /// Returns the content as a <see cref="Dictionary{TKey,TValue}"/>.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <returns></returns>
    public IDictionary<string, TValue?> ToDictionary<TValue>()
    {
        using (new ProfileScope(this, MasterKey))
        {
            return CheckAutoPing(() => DictionaryResult<TValue?>(_dictionaryStoreProvider.GetDictionary(MasterKey)));
        }
    }

    /// <summary>
    /// Returns the content of the store as a dictionary.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <returns>A dictionary containing the keys and values in the store.</returns>
    public IDictionary<string, object?> ToDictionary(IDictionary<string, Type> keysTypes)
    {
        using (new ProfileScope(this, MasterKey))
        {
            return CheckAutoPing(() => DictionaryResult(_dictionaryStoreProvider.GetDictionary(MasterKey), keysTypes));
        }
    }

    /// <summary>
    /// Returns the keys in the store.
    /// </summary>
    /// <returns>A list of keys in the store.</returns>
    public IList<string> Keys()
    {
        using (new ProfileScope(this, MasterKey))
        {
            return CheckAutoPing(() => _dictionaryStoreProvider.Keys(MasterKey));
        }
    }

    /// <summary>
    /// Returns the values in the store.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <returns>A list of values in the store.</returns>
    public IList<TValue?> Values<TValue>()
    {
        using (new ProfileScope(this, MasterKey))
        {
            return CheckAutoPing(() => ListResult<TValue?>(_dictionaryStoreProvider.Values(MasterKey)));
        }
    }

    /// <summary>
    /// Increments the integer value in a key by the specified value.
    /// </summary>
    /// <remarks>By default, it increments by 1.</remarks>
    /// <param name="key">The key of the value to increment.</param>
    /// <param name="value">The value to increment by.</param>
    /// <returns>The incremented value.</returns>
    public long Increment(string key, long value = 1)
    {
        using (new ProfileScope(this, key))
        {
            return CheckAutoPing(() => _dictionaryStoreProvider.Increment(MasterKey, key, value));
        }
    }

    /// <summary>
    /// Returns the total size of the serialized value in bytes for a key.
    /// </summary>
    /// <param name="key">The key of the value to get the size of.</param>
    /// <returns>The size of the value in bytes.</returns>
    public long SizeInBytes(string key)
    {
        using (new ProfileScope(this, key))
        {
            return CheckAutoPing(() => _dictionaryStoreProvider.SizeInBytes(MasterKey, key));
        }
    }

    /// <summary>
    /// Dumps all values into byte array
    /// </summary>
    /// <returns></returns>
    public async Task<byte[]> DumpAsync()
    {
        using (new ProfileScope(this, MasterKey))
        {
            return await CheckAutoPing(async () => AsValue(await _dictionaryStoreProvider.GetDictionaryAsync(MasterKey)));
        }
    }

    /// <summary>
    /// Restores previously dumped byte array into the hash store.
    /// </summary>
    /// <param name="dump"></param>
    /// <returns></returns>
    public async Task RestoreAsync(byte[] dump)
    {
        using (new ProfileScope(this, MasterKey))
        {
            if (dump.Length == 0)
                return;
            await CheckAutoPing(async () => await _dictionaryStoreProvider.SetAsync(MasterKey, SingleResult<Dictionary<string, byte[]>>(dump) 
                ?? throw new InvalidOperationException($"Couldn't deserialize the dumped data, be sure the '{nameof(dump)}' parameter is valid.")));
        }
    }
}
