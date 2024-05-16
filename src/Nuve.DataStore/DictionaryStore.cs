using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nuve.DataStore.Helpers;

namespace Nuve.DataStore
{
    /// <summary>
    /// DictionaryStore structure that holds values of type <typeparamref name="TValue"/>.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    public sealed class DictionaryStore<TValue> : DataStoreBase, IDictionary<string, TValue?>
    {
        private readonly IDictionaryStoreProvider _dictionaryStoreProvider;
        private static readonly string _valueName = typeof(TValue).GetFriendlyName().Replace('.', '_');
        private static readonly string _typeName = typeof(DictionaryStore<TValue>).GetFriendlyName();

        /// <summary>
        /// DictionaryStore structure that holds values.
        /// </summary>
        /// <param name="masterKey">Under which key this dictionary will be stored</param>
        /// <param name="connectionName">Connection name defined in the config</param>
        /// <param name="defaultExpire">Default expiration time.</param>
        /// <param name="autoPing">Should Ping be automatically performed for each operation?</param>
        /// <param name="namespaceSeperator">Separator used to separate namespaces. Default is ":".</param>
        /// <param name="overrideRootNamespace">Used to change the root namespace defined in the connection.</param>
        /// <param name="serializer">Set this if you want to use a different serializer instead of the default serializer.</param>
        /// <param name="profiler">Used to profile only the methods of this data store. 
        /// Whether set or not, the global profiler registered in <see cref="DataStoreManager"/> will be used.</param>
        public DictionaryStore(string masterKey, string? connectionName = null, TimeSpan? defaultExpire = null, bool autoPing = false,
            string? namespaceSeperator = null, string? overrideRootNamespace = null, IDataStoreSerializer? serializer = null, IDataStoreCompressor? compressor = null,
            IDataStoreProfiler? profiler = null,
            int? compressBiggerThan = null) :
            base(connectionName, defaultExpire, autoPing, namespaceSeperator, overrideRootNamespace, serializer, compressor, profiler, compressBiggerThan)
        {
            _dictionaryStoreProvider = Provider as IDictionaryStoreProvider
                ?? throw new InvalidOperationException($"The provider with connection '{connectionName}' doesn't support Dictionary operations. " +
                    "The provider must implement IDictionaryStoreProvider interface to use DictionaryStore");

            MasterKey = JoinWithRootNamespace($"{masterKey}<{_valueName}>");// It is necessary to add the type name. Because lists of different types cannot be deserialized in the same masterKey.
        }

        internal override string TypeName => _typeName;

        /// <summary>
        /// The full path where the store is held.
        /// </summary>
        public readonly string MasterKey;
        
        /// <summary>
        /// Is this store available?
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
        /// Is this store available?
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
        /// If the <see cref="DataStoreBase.DefaultExpire"/> property of the store is set, it resets the expiration time to this value.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> PingAsync()
        {
            return await base.PingAsync(MasterKey);
        }

        /// <summary>
        /// If the <see cref="DataStoreBase.DefaultExpire"/> property of the store is set, it resets the expiration time to this value.
        /// </summary>
        /// <returns></returns>
        public bool Ping()
        {
            return base.Ping(MasterKey);
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
        /// <returns>The expiration time of the MasterKey.</returns>
        public async Task<TimeSpan?> GetExpireAsync()
        {
            using (new ProfileScope(this, MasterKey))
            {
                return await Provider.GetExpireAsync(MasterKey);
            }
        }

        /// <summary>
        /// Gets the expiration time of the MasterKey.
        /// </summary>
        /// <returns>The expiration time of the MasterKey.</returns>
        public TimeSpan? GetExpire()
        {
            using (new ProfileScope(this, MasterKey))
            {
                return Provider.GetExpire(MasterKey);
            }
        }

        /// <summary>
        /// Clears the store.
        /// </summary>
        /// <returns>True if the store is cleared successfully; otherwise, false.</returns>
        public async Task<bool> ClearAsync()
        {
            using (new ProfileScope(this, MasterKey))
            {
                return await Provider.RemoveAsync(MasterKey);
            }
        }

        /// <summary>
        /// Clears the store.
        /// </summary>
        /// <returns>True if the store is cleared successfully; otherwise, false.</returns>
        public bool Clear()
        {
            using (new ProfileScope(this, MasterKey))
            {
                return Provider.Remove(MasterKey);
            }
        }

        /// <summary>
        /// Returns whether a key exists or not.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns><c>true</c> if the key exists; otherwise, <c>false</c>.</returns>
        public async Task<bool> ContainsKeyAsync(string key)
        {
            using (new ProfileScope(this, key))
            {
                return await CheckAutoPing(async () => await _dictionaryStoreProvider.ContainsAsync(MasterKey, key));
            }
        }

        /// <summary>
        /// Returns whether a key exists or not.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns><c>true</c> if the key exists; otherwise, <c>false</c>.</returns>
        public bool ContainsKey(string key)
        {
            using (new ProfileScope(this, key))
            {
                return CheckAutoPing(() => _dictionaryStoreProvider.Contains(MasterKey, key));
            }
        }

        /// <summary>
        /// Returns whether a key-value pair exists or not.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <param name="value">The value to check.</param>
        /// <returns><c>true</c> if the key-value pair exists; otherwise, <c>false</c>.</returns>
        public async Task<bool> ContainsAsync(string key, TValue? value)
        {
            using (new ProfileScope(this, key))
            {
                return await CheckAutoPing(async () =>
                                           {
                                               var keyExists = await _dictionaryStoreProvider.ContainsAsync(MasterKey, key);

                                               if (!keyExists)
                                                   return await Task.FromResult(false);

                                               return await Task.FromResult(EqualityComparer<TValue?>.Default.Equals(await GetAsync(key), value));
                                           });
            }
        }

        /// <summary>
        /// Returns whether a key-value pair exists or not.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <param name="value">The value to check.</param>
        /// <returns><c>true</c> if the key-value pair exists; otherwise, <c>false</c>.</returns>
        public bool Contains(string key, TValue? value)
        {
            using (new ProfileScope(this, key))
            {
                return CheckAutoPing(() =>
                              {
                                  var keyExists = _dictionaryStoreProvider.Contains(MasterKey, key);

                                  if (!keyExists)
                                      return false;

                                  return EqualityComparer<TValue?>.Default.Equals(Get(key), value);
                              });
            }
        }

        /// <summary>
        /// Copies the content to an array.
        /// </summary>
        /// <param name="array">The array to copy the content to.</param>
        /// <param name="arrayIndex">The starting index of the array.</param>
        public async Task CopyToAsync(KeyValuePair<string, TValue?>[] array, int arrayIndex)
        {
            await CheckAutoPing(async () =>
                          {
                              var dict = await ToDictionaryAsync();
                              dict.CopyTo(array, arrayIndex);
                          });
        }

        /// <summary>
        /// Copies the content to an array.
        /// </summary>
        /// <param name="array">The array to copy the content to.</param>
        /// <param name="arrayIndex">The starting index of the array.</param>
        public void CopyTo(KeyValuePair<string, TValue?>[] array, int arrayIndex)
        {
            CheckAutoPing(() =>
                          {
                              var dict = ToDictionary();
                              dict.CopyTo(array, arrayIndex);
                          });
        }
        /// <summary>
        /// Removes keys from the store.
        /// </summary>
        /// <param name="keys">The keys to remove.</param>
        /// <returns>The number of keys removed.</returns>
        public async Task<long> RemoveAsync(params string[] keys)
        {
            using (new ProfileScope(this, MasterKey))
            {
                return await CheckAutoPing(async () => await _dictionaryStoreProvider.RemoveAsync(MasterKey, keys));
            }
        }

        /// <summary>
        /// Removes keys from the store.
        /// </summary>
        /// <param name="keys">The keys to remove.</param>
        /// <returns>The number of keys removed.</returns>
        public long Remove(params string[] keys)
        {
            using (new ProfileScope(this, MasterKey))
            {
                return CheckAutoPing(() => _dictionaryStoreProvider.Remove(MasterKey, keys));
            }
        }

        /// <summary>
        /// Gets the value of a key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The value associated with the key.</returns>
        public async Task<TValue?> GetAsync(string key)
        {
            using (new ProfileScope(this, key))
            {
                return await CheckAutoPing(async () => SingleResult<TValue?>(await _dictionaryStoreProvider.GetAsync(MasterKey, key)));
            }
        }

        /// <summary>
        /// Gets the value of a key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The value associated with the key.</returns>
        public TValue? Get(string key)
        {
            using (new ProfileScope(this, key))
            {
                return CheckAutoPing(() => SingleResult<TValue?>(_dictionaryStoreProvider.Get(MasterKey, key)));
            }
        }

        /// <summary>
        /// Gets the values of multiple keys. Use this for querying multiple keys.
        /// </summary>
        /// <param name="keys">The keys.</param>
        /// <returns>A dictionary containing the values associated with the keys.</returns>
        public async Task<IDictionary<string, TValue?>> GetAsync(params string[] keys)
        {
            using (new ProfileScope(this, MasterKey))
            {
                return await CheckAutoPing(async () => DictionaryResult<TValue?>(await _dictionaryStoreProvider.GetAsync(MasterKey, keys)));
            }
        }

        /// <summary>
        /// Gets the values of multiple keys. Use this for querying multiple keys.
        /// </summary>
        /// <param name="keys">The keys.</param>
        /// <returns>A dictionary containing the values associated with the keys.</returns>
        public IDictionary<string, TValue?> Get(params string[] keys)
        {
            using (new ProfileScope(this, MasterKey))
            {
                return CheckAutoPing(() => DictionaryResult<TValue?>(_dictionaryStoreProvider.Get(MasterKey, keys)));
            }
        }

        /// <summary>
        /// Sets a value for a key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="overwrite">Should it overwrite an existing value?</param>
        /// <returns>True if the value was set successfully, false otherwise.</returns>
        public async Task<bool> SetAsync(string key, TValue? value, bool overwrite = true)
        {
            using (new ProfileScope(this, key))
            {
                return await CheckAutoPing(async () => await _dictionaryStoreProvider.SetAsync(MasterKey, key, AsValue(value), overwrite));
            }
        }

        /// <summary>
        /// Sets a value for a key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="overwrite">Should it overwrite an existing value?</param>
        /// <returns>True if the value was set successfully, false otherwise.</returns>
        public bool Set(string key, TValue? value, bool overwrite = true)
        {
            using (new ProfileScope(this, key))
            {
                return CheckAutoPing(() => _dictionaryStoreProvider.Set(MasterKey, key, AsValue(value), overwrite));
            }
        }

        /// <summary>
        /// Sets values for multiple keys. If you need to assign values to multiple keys, this method provides faster results.
        /// </summary>
        /// <param name="keyValues">The key-value pairs to set.</param>
        /// <param name="overwrite">Should it overwrite existing values?</param>
        /// <returns></returns>
        public async Task SetAsync(IDictionary<string, TValue?> keyValues, bool overwrite = true, bool serializeParallel = false, ParallelOptions? parallelOptions = null)
        {
            using (new ProfileScope(this, MasterKey))
            {
                await CheckAutoPing(async () => await _dictionaryStoreProvider.SetAsync(MasterKey, AsKeyValue(keyValues, serializeParallel, parallelOptions)));
            }
        }

        /// <summary>
        /// Sets values for multiple keys. If you need to assign values to multiple keys, this method provides faster results.
        /// </summary>
        /// <param name="keyValues">The key-value pairs to set.</param>
        /// <param name="overwrite">Should it overwrite existing values?</param>
        /// <returns></returns>
        public void Set(IDictionary<string, TValue?> keyValues, bool overwrite = true, bool serializeParallel = false, ParallelOptions? parallelOptions = null)
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
        public async Task<long> CountAsync()
        {
            using (new ProfileScope(this, MasterKey))
            {
                return await CheckAutoPing(async () => await _dictionaryStoreProvider.CountAsync(MasterKey));
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
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. The task result contains the content as a <see cref="Dictionary{TKey,TValue}"/>.</returns>
        public async Task<IDictionary<string, TValue?>> ToDictionaryAsync()
        {
            using (new ProfileScope(this, MasterKey))
            {
                return await CheckAutoPing(async () => DictionaryResult<TValue?>(await _dictionaryStoreProvider.GetDictionaryAsync(MasterKey)));
            }
        }

        /// <summary>
        /// Returns the content as a <see cref="Dictionary{TKey,TValue}"/>.
        /// </summary>
        /// <returns>The content as a <see cref="Dictionary{TKey,TValue}"/>.</returns>
        public IDictionary<string, TValue?> ToDictionary()
        {
            using (new ProfileScope(this, MasterKey))
            {
                return CheckAutoPing(() => DictionaryResult<TValue?>(_dictionaryStoreProvider.GetDictionary(MasterKey)));
            }
        }

        /// <summary>
        /// Returns the keys in the store.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. The task result contains the keys in the store.</returns>
        public async Task<IList<string>> KeysAsync()
        {
            using (new ProfileScope(this, MasterKey))
            {
                return await CheckAutoPing(async () => await _dictionaryStoreProvider.KeysAsync(MasterKey));
            }
        }

        /// <summary>
        /// Returns the keys in the store.
        /// </summary>
        /// <returns>The keys in the store.</returns>
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
        /// <returns>A list of values in the store.</returns>
        public async Task<IList<TValue?>> ValuesAsync()
        {
            using (new ProfileScope(this, MasterKey))
            {
                return await CheckAutoPing(async () => ListResult<TValue?>(await _dictionaryStoreProvider.ValuesAsync(MasterKey)));
            }
        }

        /// <summary>
        /// Returns the values in the store.
        /// </summary>
        /// <returns>A list of values in the store.</returns>
        public IList<TValue?> Values()
        {
            using (new ProfileScope(this, MasterKey))
            {
                return CheckAutoPing(() => ListResult<TValue?>(_dictionaryStoreProvider.Values(MasterKey)));
            }
        }

        /// <summary>
        /// Increments the integer value in a key by the specified <paramref name="value"/>.
        /// </summary>
        /// <remarks>By default, it increments by 1.</remarks>
        /// <param name="key">The key of the value to increment.</param>
        /// <param name="value">The value to increment by.</param>
        /// <returns>The incremented value.</returns>
        public async Task<long> IncrementAsync(string key, long value)
        {
            using (new ProfileScope(this, key))
            {
                return await CheckAutoPing(async () => await _dictionaryStoreProvider.IncrementAsync(MasterKey, key, value));
            }
        }

        /// <summary>
        /// Increments the integer value in a key by the specified <paramref name="value"/>.
        /// </summary>
        /// <remarks>By default, it increments by 1.</remarks>
        /// <param name="key">The key of the value to increment.</param>
        /// <param name="value">The value to increment by.</param>
        /// <returns>The incremented value.</returns>
        public long Increment(string key, long value)
        {
            using (new ProfileScope(this, key))
            {
                return CheckAutoPing(() => _dictionaryStoreProvider.Increment(MasterKey, key, value));
            }
        }

        /// <summary>
        /// Returns the total size of the serialized data in a key in bytes.
        /// </summary>
        /// <param name="key">The key of the serialized data.</param>
        /// <returns>The size of the serialized data in bytes.</returns>
        public async Task<long> SizeInBytesAsync(string key)
        {
            using (new ProfileScope(this, key))
            {
                return await CheckAutoPing(async () => await _dictionaryStoreProvider.SizeInBytesAsync(MasterKey, key));
            }
        }

        /// <summary>
        /// Returns the total size of the serialized data in bytes for a given key.
        /// </summary>
        /// <param name="key">The key of the serialized data.</param>
        /// <returns>The size of the serialized data in bytes.</returns>
        public long SizeInBytes(string key)
        {
            using (new ProfileScope(this, key))
            {
                return CheckAutoPing(() => _dictionaryStoreProvider.SizeInBytes(MasterKey, key));
            }
        }

        /// <summary>
        /// Creates a lock for the specified key.
        /// </summary>
        /// <param name="key">The key to create a lock for.</param>
        /// <param name="waitTimeout">The maximum time to wait for the lock.</param>
        /// <param name="action">The action to perform when the lock is acquired.</param>
        /// <param name="skipWhenTimeout">Should the action be skipped when a timeout occurs?</param>
        /// <param name="throwWhenTimeout">Should a TimeoutException be thrown when a timeout occurs?</param>
        public void Lock(string key, TimeSpan waitTimeout, Action action, bool skipWhenTimeout = true, bool throwWhenTimeout = false, TimeSpan? slidingExpire = null)
        {
            var lockKey = $"{MasterKey}_locker_{NamespaceSeperator}{key}";
            using (new ProfileScope(this, lockKey))
            {
                Provider.Lock(lockKey, waitTimeout, action, slidingExpire ?? TimeSpan.FromSeconds(30), skipWhenTimeout, throwWhenTimeout);
            }
        }

        /// <summary>
        /// Creates a lock for the specified key.
        /// </summary>
        /// <param name="key">The key to create a lock for.</param>
        /// <param name="waitTimeout">The maximum time to wait for the lock.</param>
        /// <param name="action">The action to perform when the lock is acquired.</param>
        /// <param name="skipWhenTimeout">Should the action be skipped when a timeout occurs?</param>
        /// <param name="throwWhenTimeout">Should a TimeoutException be thrown when a timeout occurs?</param>
        public async Task LockAsync(string key, TimeSpan waitTimeout, Func<Task> action, bool skipWhenTimeout = true, bool throwWhenTimeout = false, TimeSpan? slidingExpire = null)
        {
            var lockKey = $"{MasterKey}_locker_{NamespaceSeperator}{key}";
            using (new ProfileScope(this, lockKey))
            {
                await Provider.LockAsync(lockKey, waitTimeout, action, slidingExpire ?? TimeSpan.FromSeconds(30), skipWhenTimeout, throwWhenTimeout);
            }
        }

        #region IDictionary<string, TValue>
        public IEnumerator<KeyValuePair<string, TValue?>> GetEnumerator()
        {
            return new DictionaryStoreEnumerator<TValue?>(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<string, TValue?> item)
        {
            Set(item.Key, item.Value);
        }

        public void Add(string key, TValue? value)
        {
            Set(key, value);
        }

        void ICollection<KeyValuePair<string, TValue?>>.Clear()
        {
            Clear();
        }

        public bool Contains(KeyValuePair<string, TValue?> item)
        {
            return Contains(item.Key, item.Value);
        }

        public bool Remove(KeyValuePair<string, TValue?> item)
        {
            if (Contains(item))
            {
                Remove(item.Key);
            }
            return false;
        }

        int ICollection<KeyValuePair<string, TValue?>>.Count
        {
            get
            {
                return (int)Count();
            }
        }

        public bool IsReadOnly => false;

        bool IDictionary<string, TValue?>.Remove(string key)
        {
            return Remove(key) > 0;
        }

        public bool TryGetValue(string key, out TValue? value)
        {
            if (!ContainsKey(key))
            {
                value = default;
                return false;
            }

            value = Get(key);
            return true;
        }

        public TValue? this[string key]
        {
            get { return Get(key); }
            set { Set(key, value); }
        }

        ICollection<string> IDictionary<string, TValue?>.Keys => Keys();

        ICollection<TValue?> IDictionary<string, TValue?>.Values => Values();

        #endregion
    }
}
