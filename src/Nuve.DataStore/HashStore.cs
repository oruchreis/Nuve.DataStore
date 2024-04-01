using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nuve.DataStore.Helpers;

namespace Nuve.DataStore;

/// <summary>
/// Çok tipli Dictionary yapısı. <see cref="DictionaryStore{TValue}"/>'dan farklı olarak her bir işlemde tip belirtilmesi gerekmektedir.
/// </summary>
public sealed class HashStore : DataStoreBase
{
    private readonly IDictionaryStoreProvider _dictionaryStoreProvider;
    private static readonly string _typeName = typeof(HashStore).GetFriendlyName();

    /// <summary>
    /// Dictionary değer tutan store yapısı. 
    /// </summary>
    /// <param name="masterKey">Bu dictionary hangi key altında saklanacak</param>
    /// <param name="connectionName">Config'de tanımlı bağlantı ismi</param>
    /// <param name="defaultExpire">Varsayılan expire süresi.</param>
    /// <param name="autoPing">Her işlemde otomatik olarak Ping yapılsın mı?</param>
    /// <param name="namespaceSeperator">Namespace'leri ayırmak için kullanılan ayraç. Varsayılan olarak ":"dir. </param>
    /// <param name="overrideRootNamespace">Bağlantıya tanımlı root alan adını değiştirmek için kullanılır.</param>
    /// <param name="serializer">Varsayılan serializer yerine başka bir serializer kullanmak istiyorsanız bunu setleyin.</param>
    /// <param name="profiler">Özel olarak sadece bu data store'un metodlarını profile etmek için kullanılır. 
    /// Setlense de setlenmese de <see cref="DataStoreManager"/>'a kayıtlı global profiler kullanılır.</param>
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
    /// Store yapısının tutulduğu tam yol.
    /// </summary>
    public readonly string MasterKey;

    /// <summary>
    /// Bu store mevcut mu?
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
    /// Store'un <see cref="DataStoreBase.DefaultExpire"/> özelliği setli ise expire süresini bu süreye sıfırlar.
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
            Task.Run(() => Ping());
    }

    private T CheckAutoPing<T>(Func<T> func)
    {
        var result = func();
        if (AutoPing)
            Task.Run(() => Ping());
        return result;
    }

    /// <summary>
    /// MasterKey'in geçerlilik süresi
    /// </summary>
    /// <returns></returns>
    public async Task<TimeSpan?> GetExpireAsync()
    {
        using (new ProfileScope(this, MasterKey))
        {
            return await Provider.GetExpireAsync(MasterKey);
        }
    }

    /// <summary>
    /// Ekleme yapar.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="overwrite">Üzerine yazılsın mı?</param>
    /// <returns></returns>
    public async Task<bool> AddAsync<TValue>(string key, TValue value, bool overwrite = true)
    {
        using (new ProfileScope(this, key))
        {
            return await CheckAutoPing(async () => await _dictionaryStoreProvider.SetAsync(MasterKey, key, AsValue(value), overwrite));
        }
    }

    /// <summary>
    /// Store silinir.
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
    /// Bir key'in olup olmadığını döner.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public async Task<bool> ContainsKeyAsync(string key)
    {
        using (new ProfileScope(this, key))
        {
            return await CheckAutoPing(async () => await _dictionaryStoreProvider.ContainsAsync(MasterKey, key));
        }
    }

    /// <summary>
    /// Key-value ikilisinin olup olamdığını döner.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
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
    /// Keyleri store'dan kaldırır.
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
    /// Bir key'in değerini getirir.
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
    /// Bir key'in değerini getirir.
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
    /// Birden fazla key'in değerini getirir. Birden fazla key sorgusu için bunu kullanın.
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
    /// Birden fazla key'in değerini getirir. Birden fazla key sorgusu için bunu kullanın.
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
    /// Bir key'e değer ataması yapar.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="overwrite">Üzerine yazılsın mı?</param>
    /// <returns></returns>
    public async Task<bool> SetAsync<TValue>(string key, TValue value, bool overwrite = true)
    {
        using (new ProfileScope(this, key))
        {
            return await CheckAutoPing(async () => await _dictionaryStoreProvider.SetAsync(MasterKey, key, AsValue(value), overwrite));
        }
    }

    /// <summary>
    /// Birden fazla key'e değer ataması yapar. Birden fazla key'e değer atanacaksa bu daha hızlı sonuç verir.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="keyValues"></param>
    /// <param name="overwrite">Üzerine yazılsın mı?</param>
    /// <returns></returns>
    public async Task SetAsync<TValue>(IDictionary<string, TValue?> keyValues, bool overwrite = true, bool serializeParallel = false, ParallelOptions? parallelOptions = null)
    {
        using (new ProfileScope(this, MasterKey))
        {
            await CheckAutoPing(async () => await _dictionaryStoreProvider.SetAsync(MasterKey, AsKeyValue(keyValues, serializeParallel, parallelOptions)));

        }
    }

    /// <summary>
    /// Store'un eleman sayısını verir.
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
    /// İçeriği <see cref="Dictionary{TKey,TValue}"/> şeklinde çıktı verir.
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
    /// İçeriği <see cref="Dictionary{TKey,TValue}"/> şeklinde çıktı verir.
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
    /// Store'daki keyleri döner.
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
    /// Store'daki değerleri döner.
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
    /// Bir key'deki integer değeri <paramref name="value"/> kadar artırır.
    /// </summary>
    /// <remarks>Varsayalın olarak 1 artırır.</remarks>
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
    /// Bir key'deki serialize edilmiş verinin toplam boyutunu byte cinsinden döner.
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
    /// Verilen key'e göre kilit oluşturur.
    /// </summary>
    /// <param name="key">Hangi key kilitlenecek</param>
    /// <param name="waitTimeout"></param>
    /// <param name="lockerExpire"></param>
    /// <param name="action"></param>
    /// <param name="skipWhenTimeout">Timeout olduğunda çalıştırılacak olan aksiyon geçilsin mi?</param>
    /// <param name="throwWhenTimeout">Timeout olduğunda <see cref="TimeoutException"/> fırlatılsın mı?</param>
    public void Lock(string key, TimeSpan waitTimeout, TimeSpan lockerExpire, Action action, bool skipWhenTimeout = true, bool throwWhenTimeout = false)
    {
        var lockKey = $"{MasterKey}_locker_{NamespaceSeperator}{key}";
        using (new ProfileScope(this, lockKey))
        {
            Provider.Lock(lockKey, waitTimeout, lockerExpire, action, skipWhenTimeout, throwWhenTimeout);
        }
    }

    /// <summary>
    /// Verilen key'e göre kilit oluşturur.
    /// </summary>
    /// <param name="key">Hangi key kilitlenecek</param>
    /// <param name="waitTimeout"></param>
    /// <param name="lockerExpire"></param>
    /// <param name="action"></param>
    /// <param name="skipWhenTimeout">Timeout olduğunda çalıştırılacak olan aksiyon geçilsin mi?</param>
    /// <param name="throwWhenTimeout">Timeout olduğunda <see cref="TimeoutException"/> fırlatılsın mı?</param>
    public async Task LockAsync(string key, TimeSpan waitTimeout, TimeSpan lockerExpire, Func<Task> action, bool skipWhenTimeout = true, bool throwWhenTimeout = false)
    {
        var lockKey = $"{MasterKey}_locker_{NamespaceSeperator}{key}";
        using (new ProfileScope(this, lockKey))
        {
            await Provider.LockAsync(lockKey, waitTimeout, lockerExpire, action, skipWhenTimeout, throwWhenTimeout);
        }
    }

    /// <summary>
    /// Bu store mevcut mu?
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
    /// Store'un <see cref="DataStoreBase.DefaultExpire"/> özelliği setli ise expire süresini bu süreye sıfırlar.
    /// </summary>
    /// <returns></returns>
    public bool Ping()
    {
        return base.Ping(MasterKey);
    }

    /// <summary>
    /// MasterKey'in geçerlilik süresi
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
    /// Ekleme yapar.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="overwrite">Üzerine yazılsın mı?</param>
    /// <returns></returns>
    public bool Add<TValue>(string key, TValue value, bool overwrite = true)
    {
        using (new ProfileScope(this, key))
        {
            return CheckAutoPing(() => _dictionaryStoreProvider.Set(MasterKey, key, AsValue(value), overwrite));
        }
    }

    /// <summary>
    /// Store silinir.
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
    /// Bir key'in olup olmadığını döner.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public bool ContainsKey(string key)
    {
        using (new ProfileScope(this, key))
        {
            return CheckAutoPing(() => _dictionaryStoreProvider.Contains(MasterKey, key));
        }
    }

    /// <summary>
    /// Key-value ikilisinin olup olamdığını döner.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
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
    /// Keyleri store'dan kaldırır.
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
    /// Bir key'in değerini getirir.
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
    /// Bir key'in değerini getirir.
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
    /// Birden fazla key'in değerini getirir. Birden fazla key sorgusu için bunu kullanın.
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
    /// Birden fazla key'in değerini getirir. Birden fazla key sorgusu için bunu kullanın.
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
    /// Bir key'e değer ataması yapar.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="overwrite">Üzerine yazılsın mı?</param>
    /// <returns></returns>
    public bool Set<TValue>(string key, TValue value, bool overwrite = true)
    {
        using (new ProfileScope(this, key))
        {
            return CheckAutoPing(() => _dictionaryStoreProvider.Set(MasterKey, key, AsValue(value), overwrite));
        }
    }

    /// <summary>
    /// Birden fazla key'e değer ataması yapar. Birden fazla key'e değer atanacaksa bu daha hızlı sonuç verir.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="keyValues"></param>
    /// <param name="overwrite">Üzerine yazılsın mı?</param>
    /// <returns></returns>
    public void Set<TValue>(IDictionary<string, TValue?> keyValues, bool overwrite = true, bool serializeParallel = false, ParallelOptions? parallelOptions = null)
    {
        using (new ProfileScope(this, MasterKey))
        {
            CheckAutoPing(() => _dictionaryStoreProvider.Set(MasterKey, AsKeyValue(keyValues, serializeParallel, parallelOptions)));
        }
    }

    /// <summary>
    /// Store'un eleman sayısını verir.
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
    /// İçeriği <see cref="Dictionary{TKey,TValue}"/> şeklinde çıktı verir.
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
    /// İçeriği <see cref="Dictionary{TKey,TValue}"/> şeklinde çıktı verir.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <returns></returns>
    public IDictionary<string, object?> ToDictionary(IDictionary<string, Type> keysTypes)
    {
        using (new ProfileScope(this, MasterKey))
        {
            return CheckAutoPing(() => DictionaryResult(_dictionaryStoreProvider.GetDictionary(MasterKey), keysTypes));
        }
    }

    /// <summary>
    /// Store'daki keyleri döner.
    /// </summary>
    /// <returns></returns>
    public IList<string> Keys()
    {
        using (new ProfileScope(this, MasterKey))
        {
            return CheckAutoPing(() => _dictionaryStoreProvider.Keys(MasterKey));
        }
    }

    /// <summary>
    /// Store'daki değerleri döner.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <returns></returns>
    public IList<TValue?> Values<TValue>()
    {
        using (new ProfileScope(this, MasterKey))
        {
            return CheckAutoPing(() => ListResult<TValue?>(_dictionaryStoreProvider.Values(MasterKey)));
        }
    }

    /// <summary>
    /// Bir key'deki integer değeri <paramref name="value"/> kadar artırır.
    /// </summary>
    /// <remarks>Varsayalın olarak 1 artırır.</remarks>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public long Increment(string key, long value = 1)
    {
        using (new ProfileScope(this, key))
        {
            return CheckAutoPing(() => _dictionaryStoreProvider.Increment(MasterKey, key, value));
        }
    }

    /// <summary>
    /// Bir key'deki serialize edilmiş verinin toplam boyutunu byte cinsinden döner.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
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
