using Nuve.DataStore.Helpers;

namespace Nuve.DataStore;

/// <summary>
/// A store structure that holds key-value pairs.
/// <remarks>Data is persistent when no expiration is provided. This class is thread-safe.</remarks>
/// </summary>
public class KeyValueStore : DataStoreBase
{
    private readonly IKeyValueStoreProvider _keyValueStoreProvider;
    private static readonly string _typeName = typeof(KeyValueStore).GetFriendlyName();

    /// <summary>
    /// Key-Value store structure that holds key-value pairs.
    /// </summary>
    /// <param name="connectionName">Connection name defined in the config.</param>
    /// <param name="defaultExpire">Default expiration time.</param>
    /// <param name="autoPing">Should <see cref="Ping"/> be automatically called for each operation?</param>
    /// <param name="namespaceSeperator">Separator used to separate namespaces. Default is ":".</param>
    /// <param name="overrideRootNamespace">Used to change the root namespace defined in the connection.</param>
    /// <param name="serializer">Set this if you want to use a custom serializer instead of the default one.</param>
    /// <param name="profiler">Used to profile only the methods of this data store. The global profiler registered in <see cref="DataStoreManager"/> is used whether it is set or not.</param>
    public KeyValueStore(string? connectionName = null, TimeSpan? defaultExpire = null, bool autoPing = false,
        string? namespaceSeperator = null, string? overrideRootNamespace = null, IDataStoreSerializer? serializer = null, IDataStoreCompressor? compressor = null,
        IDataStoreProfiler? profiler = null,
        int? compressBiggerThan = null) :
        base(connectionName, defaultExpire, autoPing, namespaceSeperator, overrideRootNamespace, serializer, compressor, profiler, compressBiggerThan)
    {
        _keyValueStoreProvider = Provider as IKeyValueStoreProvider
            ?? throw new InvalidOperationException($"The provider with connection '{connectionName}' doesn't support Key-Value operations. " +
                "The provider must implement IKeyValueStoreProvider interface to use KeyValueStore");
    }

    internal override string TypeName => _typeName;

    private T CheckAutoPing<T>(string key, Func<T> func)
    {
        var result = func();
        if (AutoPing)
            Task.Run(async () =>
            {
                try
                {
                    await PingAsync(key);
                }
                catch (Exception e)
                {
                    //intentionally supressed
                }
            });
        return result;
    }

    private T CheckAutoPing<T>(IEnumerable<string> keys, Func<T> func)
    {
        var result = func();
        if (AutoPing)
            Task.Run(async () =>
            {
                foreach (var key in keys)
                {
                    try
                    {
                        await PingAsync(key);
                    }
                    catch (Exception e)
                    {
                        //intentionally supressed
                    }
                }
            });
        return result;
    }
    /// <summary>
    /// Retrieves data based on a specific key.
    /// </summary>
    /// <param name="key">The key of the data.</param>
    /// <returns>The retrieved data.</returns>
    public T? Get<T>(string key)
    {
        using (new ProfileScope(this, key))
        {
            return CheckAutoPing(key,
                () => SingleResult<T>(_keyValueStoreProvider.Get(JoinWithRootNamespace(key))));
        }
    }

    /// <summary>
    /// Retrieves data based on a specific key asynchronously.
    /// </summary>
    /// <param name="key">The key of the data.</param>
    /// <returns>The retrieved data.</returns>
    public async Task<T?> GetAsync<T>(string key)
    {
        using (new ProfileScope(this, key))
        {
            return await CheckAutoPing(key,
                async () => SingleResult<T>(await _keyValueStoreProvider.GetAsync(JoinWithRootNamespace(key))));
        }
    }

    /// <summary>
    /// Retrieves data based on a specific key asynchronously.
    /// </summary>
    /// <param name="key">The key of the data.</param>
    /// <param name="type">The type of the data.</param>
    /// <returns>The retrieved data.</returns>
    public async Task<object?> GetAsync(string key, Type type)
    {
        using (new ProfileScope(this, key))
        {
            return await CheckAutoPing(key,
                async () => SingleResult(await _keyValueStoreProvider.GetAsync(JoinWithRootNamespace(key)), type));
        }
    }

    /// <summary>
    /// Retrieves data based on a specific key.
    /// </summary>
    /// <param name="key">The key of the data.</param>
    /// <param name="type">The type of the data.</param>
    /// <returns>The retrieved data.</returns>
    public object? Get(string key, Type type)
    {
        using (new ProfileScope(this, key))
        {
            return CheckAutoPing(key,
                () => SingleResult(_keyValueStoreProvider.Get(JoinWithRootNamespace(key)), type));
        }
    }

    /// <summary>
    /// Retrieves all keys at once.
    /// </summary>
    /// <typeparam name="T">The type of the data.</typeparam>
    /// <param name="keys">The keys to retrieve.</param>
    /// <returns>A dictionary containing the retrieved data.</returns>
    public IDictionary<string, T?> Get<T>(params string[] keys)
    {
        using (new ProfileScope(this, string.Join(",", keys)))
        {
            return CheckAutoPing(keys,
                () => DictionaryResult<T>(_keyValueStoreProvider.GetAll(JoinWithRootNamespace(keys))));
        }
    }

    /// <summary>
    /// Retrieves all keys at once asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the data.</typeparam>
    /// <param name="keys">The keys to retrieve.</param>
    /// <returns>A dictionary containing the retrieved data.</returns>
    public async Task<IDictionary<string, T?>> GetAsync<T>(params string[] keys)
    {
        using (new ProfileScope(this, string.Join(",", keys)))
        {
            return await CheckAutoPing(keys,
                async () => DictionaryResult<T>(await _keyValueStoreProvider.GetAllAsync(JoinWithRootNamespace(keys))));
        }
    }

    /// <summary>
    /// Used to retrieve all keys at once.
    /// </summary>
    /// <param name="keysTypes">The dictionary containing the keys and their corresponding types.</param>
    /// <returns>A dictionary containing the keys and their corresponding values.</returns>
    public async Task<IDictionary<string, object?>> GetAsync(IDictionary<string, Type> keysTypes)
    {
        using (new ProfileScope(this, string.Join(",", keysTypes.Keys)))
        {
            return await CheckAutoPing(keysTypes.Keys,
                async () => DictionaryResult(await _keyValueStoreProvider.GetAllAsync(JoinWithRootNamespace(keysTypes.Keys)), keysTypes));
        }
    }

    /// <summary>
    /// Used to retrieve all keys at once.
    /// </summary>
    /// <param name="keysTypes">The dictionary containing the keys and their corresponding types.</param>
    /// <returns>A dictionary containing the keys and their corresponding values.</returns>
    public IDictionary<string, object?> Get(IDictionary<string, Type> keysTypes)
    {
        using (new ProfileScope(this, string.Join(",", keysTypes.Keys)))
        {
            return CheckAutoPing(keysTypes.Keys,
                () => DictionaryResult(_keyValueStoreProvider.GetAll(JoinWithRootNamespace(keysTypes.Keys)), keysTypes));
        }
    }

    /// <summary>
    /// Saves the data based on a specific key.
    /// </summary>
    /// <param name="key">The key of the data.</param>
    /// <param name="entity">The data.</param>
    /// <param name="overwrite">If set to false, it does not overwrite when the key already exists.</param>
    public bool Set<T>(string key, T? entity, bool overwrite = true)
    {
        using (new ProfileScope(this, key))
        {
            var result = CheckAutoPing(key,
                () => _keyValueStoreProvider.Set(JoinWithRootNamespace(key), AsValue(entity), overwrite));
            if (result && DefaultExpire != TimeSpan.Zero)
                SetExpire(key, DefaultExpire);
            return result;
        }
    }

    /// <summary>
    /// Saves the data based on a specific key.
    /// </summary>
    /// <param name="key">The key of the data.</param>
    /// <param name="entity">The data.</param>
    /// <param name="overwrite">If set to false, it does not overwrite when the key already exists.</param>
    public async Task<bool> SetAsync<T>(string key, T? entity, bool overwrite = true)
    {
        using (new ProfileScope(this, key))
        {
            var result = await CheckAutoPing(key,
                async () => await _keyValueStoreProvider.SetAsync(JoinWithRootNamespace(key), AsValue(entity), overwrite));
            if (result && DefaultExpire != TimeSpan.Zero)
                await SetExpireAsync(key, DefaultExpire);
            return result;
        }
    }

    /// <summary>
    /// Used to save multiple data.
    /// </summary>
    /// <param name="keyValues">The keys and values of the data.</param>
    /// <param name="overwrite">Whether to overwrite existing data with the same keys.</param>
    public bool Set<T>(IDictionary<string, T?> keyValues, bool overwrite = true, bool serializeParallel = false, ParallelOptions? parallelOptions = null)
    {
        using (new ProfileScope(this, string.Join(",", keyValues.Keys)))
        {
            var result = CheckAutoPing(keyValues.Keys,
                () => _keyValueStoreProvider.SetAll(AsKeyValue(JoinWithRootNamespace(keyValues), serializeParallel, parallelOptions), overwrite));
            if (result && DefaultExpire != TimeSpan.Zero)
                foreach (var key in keyValues.Keys)
                {
                    SetExpire(key, DefaultExpire);
                }
            return result;
        }
    }

    /// <summary>
    /// Used to save multiple data.
    /// </summary>
    /// <param name="keyValues">The keys and values of the data.</param>
    /// <param name="overwrite">Whether to overwrite existing data with the same keys.</param>
    public async Task<bool> SetAsync<T>(IDictionary<string, T?> keyValues, bool overwrite = true, bool serializeParallel = false, ParallelOptions? parallelOptions = null)
    {
        using (new ProfileScope(this, string.Join(",", keyValues.Keys)))
        {
            var result = await CheckAutoPing(keyValues.Keys,
                async () => await _keyValueStoreProvider.SetAllAsync(AsKeyValue(JoinWithRootNamespace(keyValues), serializeParallel, parallelOptions), overwrite));
            if (result && DefaultExpire != TimeSpan.Zero)
                foreach (var key in keyValues.Keys)
                {
                    await SetExpireAsync(key, DefaultExpire);
                }
            return result;
        }
    }

    /// <summary>
    /// Writes the given value to the key and returns the old value as the output.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key">The key.</param>
    /// <param name="value">The new value.</param>
    /// <returns>The old value.</returns>
    public T? Exchange<T>(string key, T? value)
    {
        using (new ProfileScope(this, key))
        {
            return CheckAutoPing(key,
                () => SingleResult<T>(_keyValueStoreProvider.Exchange(JoinWithRootNamespace(key), AsValue(value))));
        }
    }

    /// <summary>
    /// Writes the given value to the key and returns the old value as the output.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key">The key.</param>
    /// <param name="value">The new value.</param>
    /// <returns>The old value.</returns>
    public async Task<T?> ExchangeAsync<T>(string key, T? value)
    {
        using (new ProfileScope(this, key))
        {
            return await CheckAutoPing(key,
                async () => SingleResult<T>(await _keyValueStoreProvider.ExchangeAsync(JoinWithRootNamespace(key), AsValue(value))));
        }
    }

    /// <summary>
    /// Determines when a data will expire.
    /// </summary>
    /// <param name="key">The key of the data.</param>
    /// <param name="expire">When will the data expire?</param>
    public bool SetExpire(string key, TimeSpan expire)
    {
        using (new ProfileScope(this, key))
        {
            return Provider.SetExpire(JoinWithRootNamespace(key), expire);
        }
    }

    /// <summary>
    /// Determines when a data will expire.
    /// </summary>
    /// <param name="key">The key of the data.</param>
    /// <param name="expire">When will the data expire?</param>
    public async Task<bool> SetExpireAsync(string key, TimeSpan expire)
    {
        using (new ProfileScope(this, key))
        {
            return await Provider.SetExpireAsync(JoinWithRootNamespace(key), expire);
        }
    }

    /// <summary>
    /// Gets the remaining time until a data expires.
    /// </summary>
    /// <param name="key">The key of the data.</param>
    /// <returns>The remaining time until the data expires.</returns>
    public TimeSpan? GetExpire(string key)
    {
        using (new ProfileScope(this, key))
        {
            return Provider.GetExpire(JoinWithRootNamespace(key));
        }
    }

    /// <summary>
    /// Gets the remaining time until a data expires.
    /// </summary>
    /// <param name="key">The key of the data.</param>
    /// <returns>The remaining time until the data expires.</returns>
    public async Task<TimeSpan?> GetExpireAsync(string key)
    {
        using (new ProfileScope(this, key))
        {
            return await Provider.GetExpireAsync(JoinWithRootNamespace(key));
        }
    }

    /// <summary>
    /// Resets the expire time of a key to the default expire time.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public new bool Ping(string key)
    {
        return base.Ping(JoinWithRootNamespace(key));
    }

    /// <summary>
    /// Resets the expire time of a key to the default expire time.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public new async Task<bool> PingAsync(string key)
    {
        return await base.PingAsync(JoinWithRootNamespace(key));
    }

    /// <summary>
    /// Removes a key from the store.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public bool Remove(string key)
    {
        using (new ProfileScope(this, key))
        {
            return Provider.Remove(JoinWithRootNamespace(key));
        }
    }

    /// <summary>
    /// Removes a key from the store.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public async Task<bool> RemoveAsync(string key)
    {
        using (new ProfileScope(this, key))
        {
            return await Provider.RemoveAsync(JoinWithRootNamespace(key));
        }
    }

    /// <summary>
    /// Checks if a key exists in the store.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public bool Contains(string key)
    {
        using (new ProfileScope(this, key))
        {
            return _keyValueStoreProvider.Contains(JoinWithRootNamespace(key));
        }
    }

    /// <summary>
    /// Checks if a key exists in the store.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public async Task<bool> ContainsAsync(string key)
    {
        using (new ProfileScope(this, key))
        {
            return await _keyValueStoreProvider.ContainsAsync(JoinWithRootNamespace(key));
        }
    }

    /// <summary>
    /// Renames the name of a key.
    /// </summary>
    /// <param name="oldKey">The old key name.</param>
    /// <param name="newKey">The new key name.</param>
    /// <returns>True if the key name is successfully changed; otherwise, false.</returns>
    public bool Rename(string oldKey, string newKey)
    {
        using (new ProfileScope(this, oldKey))
        {
            return _keyValueStoreProvider.Rename(JoinWithRootNamespace(oldKey), JoinWithRootNamespace(newKey));
        }
    }

    /// <summary>
    /// Renames the name of a key asynchronously.
    /// </summary>
    /// <param name="oldKey">The old key name.</param>
    /// <param name="newKey">The new key name.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the key name is successfully changed; otherwise, false.</returns>
    public async Task<bool> RenameAsync(string oldKey, string newKey)
    {
        using (new ProfileScope(this, oldKey))
        {
            return await _keyValueStoreProvider.RenameAsync(JoinWithRootNamespace(oldKey), JoinWithRootNamespace(newKey));
        }
    }

    /// <summary>
    /// Increments the numeric value of a key.
    /// </summary>
    /// <param name="key">The key name.</param>
    /// <param name="amount">The amount by which to increment the value. Default is 1.</param>
    /// <returns>The new value of the key after incrementing.</returns>
    public long Increment(string key, long amount = 1)
    {
        using (new ProfileScope(this, key))
        {
            return CheckAutoPing(key,
                () => _keyValueStoreProvider.Increment(JoinWithRootNamespace(key), amount));
        }
    }

    /// <summary>
    /// Increases the numeric value in a key.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="amount"></param>
    /// <returns></returns>
    public async Task<long> IncrementAsync(string key, long amount = 1)
    {
        using (new ProfileScope(this, key))
        {
            return await CheckAutoPing(key,
                async () => await _keyValueStoreProvider.IncrementAsync(JoinWithRootNamespace(key), amount));
        }
    }

    /// <summary>
    /// Decreases the numeric value in a key.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="amount"></param>
    /// <returns></returns>
    public long Decrement(string key, long amount = 1)
    {
        using (new ProfileScope(this, key))
        {
            return CheckAutoPing(key,
                () => _keyValueStoreProvider.Decrement(JoinWithRootNamespace(key), amount));
        }
    }

    /// <summary>
    /// Decreases the numeric value in a key.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="amount"></param>
    /// <returns></returns>
    public async Task<long> DecrementAsync(string key, long amount = 1)
    {
        using (new ProfileScope(this, key))
        {
            return await CheckAutoPing(key,
                async () => await _keyValueStoreProvider.DecrementAsync(JoinWithRootNamespace(key), amount));
        }
    }


    /// <summary>
    /// Appends a string to the value of a key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The string to append.</param>
    /// <returns>The new length of the string.</returns>
    public long AppendString(string key, string value)
    {
        using (new ProfileScope(this, key))
        {
            return CheckAutoPing(key,
                () => _keyValueStoreProvider.AppendString(JoinWithRootNamespace(key), value));
        }
    }

    /// <summary>
    /// Appends a string to the value of a key asynchronously.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The string to append.</param>
    /// <returns>The new length of the string.</returns>
    public async Task<long> AppendStringAsync(string key, string value)
    {
        using (new ProfileScope(this, key))
        {
            return await CheckAutoPing(key,
                async () => await _keyValueStoreProvider.AppendStringAsync(JoinWithRootNamespace(key), value));
        }
    }

    /// <summary>
    /// Returns a substring of a value within a specified range.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="start">The starting index of the substring.</param>
    /// <param name="end">The ending index of the substring.</param>
    /// <returns>The substring.</returns>
    public string SubString(string key, long start, long end)
    {
        using (new ProfileScope(this, key))
        {
            return CheckAutoPing(key,
                () => _keyValueStoreProvider.SubString(JoinWithRootNamespace(key), start, end));
        }
    }

    /// <summary>
    /// Returns a substring of a value within a specified range asynchronously.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="start">The starting index of the substring.</param>
    /// <param name="end">The ending index of the substring.</param>
    /// <returns>The substring.</returns>
    public async Task<string> SubStringAsync(string key, long start, long end)
    {
        using (new ProfileScope(this, key))
        {
            return await CheckAutoPing(key,
                async () => await _keyValueStoreProvider.SubStringAsync(JoinWithRootNamespace(key), start, end));
        }
    }

    /// <summary>
    /// Replaces the string in the value starting from the specified <paramref name="offset"/> with the <paramref name="value"/> string.
    /// </summary>
    /// <param name="key">The key of the value.</param>
    /// <param name="offset">The starting offset.</param>
    /// <param name="value">The string to be inserted.</param>
    /// <returns>The length of the newly created string.</returns>
    public long OverwriteString(string key, long offset, string value)
    {
        using (new ProfileScope(this, key))
        {
            return CheckAutoPing(key,
                () => _keyValueStoreProvider.OverwriteString(JoinWithRootNamespace(key), offset, value));
        }
    }

    /// <summary>
    /// Replaces the string in the value starting from the specified <paramref name="offset"/> with the <paramref name="value"/> string.
    /// </summary>
    /// <param name="key">The key of the value.</param>
    /// <param name="offset">The starting offset.</param>
    /// <param name="value">The string to be inserted.</param>
    /// <returns>The length of the newly created string.</returns>
    public async Task<long> OverwriteStringAsync(string key, long offset, string value)
    {
        using (new ProfileScope(this, key))
        {
            return await CheckAutoPing(key,
                async () => await _keyValueStoreProvider.OverwriteStringAsync(JoinWithRootNamespace(key), offset, value));
        }
    }

    /// <summary>
    /// Returns the size of the key's value in bytes.
    /// </summary>
    /// <param name="key">The key of the value.</param>
    /// <returns>The size of the value in bytes.</returns>
    public long SizeInBytes(string key)
    {
        using (new ProfileScope(this, key))
        {
            return _keyValueStoreProvider.SizeInBytes(JoinWithRootNamespace(key));
        }
    }

    /// <summary>
    /// Returns the size of the key's value in bytes.
    /// </summary>
    /// <param name="key">The key of the value.</param>
    /// <returns>The size of the value in bytes.</returns>
    public async Task<long> SizeInBytesAsync(string key)
    {
        using (new ProfileScope(this, key))
        {
            return await _keyValueStoreProvider.SizeInBytesAsync(JoinWithRootNamespace(key));
        }
    }

    /// <summary>
    /// If the key is not locked, locks the key for the duration of <paramref name="lockerExpire"/> or waits for the lock to be released for a maximum of <paramref name="waitTimeout"/>.
    /// Continues without performing the operation at the end of the wait period.
    /// </summary>
    /// <param name="lockerKey">The key to lock.</param>
    /// <param name="waitTimeout">The maximum time to wait for the lock to be released.</param>
    /// <param name="action">The action to perform while the key is locked.</param>
    /// <param name="skipWhenTimeout">Should the action be skipped when a timeout occurs?</param>
    /// <param name="throwWhenTimeout">Should a <see cref="TimeoutException"/> be thrown when a timeout occurs?</param>
    /// <param name="slidingExpire">The duration of the lock. If provided, the lock will be automatically released after this duration.</param>
    public void Lock(string lockerKey, TimeSpan waitTimeout, Action action, bool skipWhenTimeout = true, bool throwWhenTimeout = false, TimeSpan? slidingExpire = null)
    {
        using (new ProfileScope(this, lockerKey))
        {
            Provider.Lock(JoinWithRootNamespace(lockerKey), waitTimeout, action, slidingExpire ?? TimeSpan.FromSeconds(30), skipWhenTimeout, throwWhenTimeout);
        }
    }

    /// <summary>
    /// If the key is not locked, locks the key for the duration of <paramref name="lockerExpire"/> or waits for the lock to be released for a maximum of <paramref name="waitTimeout"/>.
    /// Continues without performing the operation at the end of the wait period.
    /// </summary>
    /// <param name="lockerKey">The key to lock.</param>
    /// <param name="waitTimeout">The maximum time to wait for the lock to be released.</param>
    /// <param name="actionAsync">The asynchronous action to perform while the key is locked.</param>
    /// <param name="skipWhenTimeout">Should the action be skipped when a timeout occurs?</param>
    /// <param name="throwWhenTimeout">Should a <see cref="TimeoutException"/> be thrown when a timeout occurs?</param>
    /// <param name="slidingExpire">The duration of the lock. If provided, the lock will be automatically released after this duration.</param>
    public async Task LockAsync(string lockerKey, TimeSpan waitTimeout, Func<Task> actionAsync, bool skipWhenTimeout = true, bool throwWhenTimeout = false, TimeSpan? slidingExpire = null)
    {
        using (new ProfileScope(this, lockerKey))
        {
            await Provider.LockAsync(JoinWithRootNamespace(lockerKey), waitTimeout, actionAsync, slidingExpire ?? TimeSpan.FromSeconds(30), skipWhenTimeout, throwWhenTimeout);
        }
    }
}
