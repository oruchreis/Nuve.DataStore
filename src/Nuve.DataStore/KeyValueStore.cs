using Nuve.DataStore.Helpers;

namespace Nuve.DataStore;

/// <summary>
/// Key-Value değer tutan store yapısı. 
/// <remarks>Expire verilmediğinde veriler kalıcı olur. Bu sınıf thread-safe'dir.</remarks>
/// </summary>
public class KeyValueStore : DataStoreBase
{
    private readonly IKeyValueStoreProvider _keyValueStoreProvider;
    private static readonly string _typeName = typeof(KeyValueStore).GetFriendlyName();

    /// <summary>
    /// Key-Value değer tutan store yapısı. 
    /// </summary>
    /// <param name="connectionName">Config'de tanımlı bağlantı ismi</param>
    /// <param name="defaultExpire">Varsayılan expire süresi.</param>
    /// <param name="autoPing">Her işlemde otomatik olarak <see cref="Ping"/> yapılsın mı?</param>
    /// <param name="namespaceSeperator">Namespace'leri ayırmak için kullanılan ayraç. Varsayılan olarak ":"dir. </param>
    /// <param name="overrideRootNamespace">Bağlantıya tanımlı root alan adını değiştirmek için kullanılır.</param>
    /// <param name="serializer">Varsayılan serializer yerine başka bir serializer kullanmak istiyorsanız bunu setleyin.</param>
    /// <param name="profiler">Özel olarak sadece bu data store'un metodlarını profile etmek için kullanılır. 
    /// Setlense de setlenmese de <see cref="DataStoreManager"/>'a kayıtlı global profiler kullanılır.</param>
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
            Task.Run(() => Ping(key));
        return result;
    }

    private T CheckAutoPing<T>(IEnumerable<string> keys, Func<T> func)
    {
        var result = func();
        if (AutoPing)
            Task.Run(() => { foreach (var key in keys) Ping(key); });
        return result;
    }

    /// <summary>
    /// Belli bir key'e göre veriyi çeker.
    /// </summary>
    /// <param name="key">Verinin anahtarı</param>
    /// <returns></returns>
    public T? Get<T>(string key)
    {
        using (new ProfileScope(this, key))
        {
            return CheckAutoPing(key,
                () => SingleResult<T>(_keyValueStoreProvider.Get(JoinWithRootNamespace(key))));
        }
    }

    /// <summary>
    /// Belli bir key'e göre veriyi çeker.
    /// </summary>
    /// <param name="key">Verinin anahtarı</param>
    /// <returns></returns>
    public async Task<T?> GetAsync<T>(string key)
    {
        using (new ProfileScope(this, key))
        {
            return await CheckAutoPing(key,
                async () => SingleResult<T>(await _keyValueStoreProvider.GetAsync(JoinWithRootNamespace(key))));
        }
    }

    /// <summary>
    /// Belli bir key'e göre veriyi çeker.
    /// </summary>
    /// <param name="key">Verinin anahtarı</param>
    /// <param name="type"></param>
    /// <returns></returns>
    public async Task<object?> GetAsync(string key, Type type)
    {
        using (new ProfileScope(this, key))
        {
            return await CheckAutoPing(key,
                async () => SingleResult(await _keyValueStoreProvider.GetAsync(JoinWithRootNamespace(key)), type));
        }
    }

    /// <summary>
    /// Belli bir key'e göre veriyi çeker.
    /// </summary>
    /// <param name="key">Verinin anahtarı</param>
    /// <param name="type"></param>
    /// <returns></returns>
    public object? Get(string key, Type type)
    {
        using (new ProfileScope(this, key))
        {
            return CheckAutoPing(key,
                () => SingleResult(_keyValueStoreProvider.Get(JoinWithRootNamespace(key)), type));
        }
    }

    /// <summary>
    /// Tüm keyleri bir anda çekmek için kullanılır.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="keys"></param>
    /// <returns></returns>
    public IDictionary<string, T?> Get<T>(params string[] keys)
    {
        using (new ProfileScope(this, string.Join(",", keys)))
        {
            return CheckAutoPing(keys,
                () => DictionaryResult<T>(_keyValueStoreProvider.GetAll(JoinWithRootNamespace(keys))));
        }
    }

    /// <summary>
    /// Tüm keyleri bir anda çekmek için kullanılır.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="keys"></param>
    /// <returns></returns>
    public async Task<IDictionary<string, T?>> GetAsync<T>(params string[] keys)
    {
        using (new ProfileScope(this, string.Join(",", keys)))
        {
            return await CheckAutoPing(keys,
                async () => DictionaryResult<T>(await _keyValueStoreProvider.GetAllAsync(JoinWithRootNamespace(keys))));
        }
    }

    /// <summary>
    /// Tüm keyleri bir anda çekmek için kullanılır.
    /// </summary>
    /// <param name="keysTypes"></param>
    /// <returns></returns>
    public async Task<IDictionary<string, object?>> GetAsync(IDictionary<string, Type> keysTypes)
    {
        using (new ProfileScope(this, string.Join(",", keysTypes.Keys)))
        {
            return await CheckAutoPing(keysTypes.Keys,
                async () => DictionaryResult(await _keyValueStoreProvider.GetAllAsync(JoinWithRootNamespace(keysTypes.Keys)), keysTypes));
        }
    }

    /// <summary>
    /// Tüm keyleri bir anda çekmek için kullanılır.
    /// </summary>
    /// <param name="keysTypes"></param>
    /// <returns></returns>
    public IDictionary<string, object?> Get(IDictionary<string, Type> keysTypes)
    {
        using (new ProfileScope(this, string.Join(",", keysTypes.Keys)))
        {
            return CheckAutoPing(keysTypes.Keys,
                () => DictionaryResult(_keyValueStoreProvider.GetAll(JoinWithRootNamespace(keysTypes.Keys)), keysTypes));
        }
    }

    /// <summary>
    /// Belli bir key'e göre veriyi kaydeder.
    /// </summary>
    /// <param name="key">Verinin anahtarı</param>
    /// <param name="entity">Veri</param>
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
    /// Belli bir key'e göre veriyi kaydeder.
    /// </summary>
    /// <param name="key">Verinin anahtarı</param>
    /// <param name="entity">Veri</param>
    /// <param name="overwrite">False setlenirse, key var olduğunda üzerine yazmaz.</param>
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
    /// Birden fazla veri kaydetmek için kullanılır.
    /// </summary>
    /// <param name="keyValues">Verinin anahtarları ve keyleri</param>
    /// <param name="overwrite"></param>
    public bool Set<T>(IDictionary<string, T?> keyValues, bool overwrite = true)
    {
        using (new ProfileScope(this, string.Join(",", keyValues.Keys)))
        {
            var result = CheckAutoPing(keyValues.Keys,
                () => _keyValueStoreProvider.SetAll(AsKeyValue(JoinWithRootNamespace(keyValues)), overwrite));
            if (result && DefaultExpire != TimeSpan.Zero)
                foreach (var key in keyValues.Keys)
                {
                    SetExpire(key, DefaultExpire);
                }
            return result;
        }
    }

    /// <summary>
    /// Birden fazla veri kaydetmek için kullanılır.
    /// </summary>
    /// <param name="keyValues">Verinin anahtarları ve keyleri</param>
    /// <param name="overwrite"></param>
    public async Task<bool> SetAsync<T>(IDictionary<string, T?> keyValues, bool overwrite = true)
    {
        using (new ProfileScope(this, string.Join(",", keyValues.Keys)))
        {
            var result = await CheckAutoPing(keyValues.Keys,
                async () => await _keyValueStoreProvider.SetAllAsync(AsKeyValue(JoinWithRootNamespace(keyValues)), overwrite));
            if (result && DefaultExpire != TimeSpan.Zero)
                foreach (var key in keyValues.Keys)
                {
                    await SetExpireAsync(key, DefaultExpire);
                }
            return result;
        }
    }

    /// <summary>
    /// Verilen değeri key'e yazar ve çıktı olarak eski değeri döner.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="value">Yeni değer</param>
    /// <returns>Eski değer</returns>
    public T? Exchange<T>(string key, T? value)
    {
        using (new ProfileScope(this, key))
        {
            return CheckAutoPing(key,
                () => SingleResult<T>(_keyValueStoreProvider.Exchange(JoinWithRootNamespace(key), AsValue(value))));
        }
    }

    /// <summary>
    /// Verilen değeri key'e yazar ve çıktı olarak eski değeri döner.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="value">Yeni değer</param>
    /// <returns>Eski değer</returns>
    public async Task<T?> ExchangeAsync<T>(string key, T? value)
    {
        using (new ProfileScope(this, key))
        {
            return await CheckAutoPing(key,
                async () => SingleResult<T>(await _keyValueStoreProvider.ExchangeAsync(JoinWithRootNamespace(key), AsValue(value))));
        }
    }

    /// <summary>
    /// Bir verinin ne zaman sonra silineceğini belirler.
    /// </summary>
    /// <param name="key">Verinin anahtarı</param>
    /// <param name="expire">Verinin süresi ne zaman dolacak?</param>
    public bool SetExpire(string key, TimeSpan expire)
    {
        using (new ProfileScope(this, key))
        {
            return Provider.SetExpire(JoinWithRootNamespace(key), expire);
        }
    }

    /// <summary>
    /// Bir verinin ne zaman sonra silineceğini belirler.
    /// </summary>
    /// <param name="key">Verinin anahtarı</param>
    /// <param name="expire">Verinin süresi ne zaman dolacak?</param>
    public async Task<bool> SetExpireAsync(string key, TimeSpan expire)
    {
        using (new ProfileScope(this, key))
        {
            return await Provider.SetExpireAsync(JoinWithRootNamespace(key), expire);
        }
    }

    /// <summary>
    /// Bir verinin ne kadar süresi kaldığını dönderir.
    /// </summary>
    /// <param name="key">Verinin anahtarı</param>
    /// <returns></returns>
    public TimeSpan? GetExpire(string key)
    {
        using (new ProfileScope(this, key))
        {
            return Provider.GetExpire(JoinWithRootNamespace(key));
        }
    }

    /// <summary>
    /// Bir verinin ne kadar süresi kaldığını dönderir.
    /// </summary>
    /// <param name="key">Verinin anahtarı</param>
    /// <returns></returns>
    public async Task<TimeSpan?> GetExpireAsync(string key)
    {
        using (new ProfileScope(this, key))
        {
            return await Provider.GetExpireAsync(JoinWithRootNamespace(key));
        }
    }

    /// <summary>
    /// Bir key'in expire süresini varsayılan expire süresine sıfırlar.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public new bool Ping(string key)
    {
        return base.Ping(JoinWithRootNamespace(key));
    }

    /// <summary>
    /// Bir key'in expire süresini varsayılan expire süresine sıfırlar.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public new async Task<bool> PingAsync(string key)
    {
        return await base.PingAsync(JoinWithRootNamespace(key));
    }

    /// <summary>
    /// Key'i store'dan kaldırır.
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
    /// Key'i store'dan kaldırır.
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
    /// Bir key olup olmadığını döner.
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
    /// Bir key olup olmadığını döner.
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
    /// Bir key'in ismini değiştirir.
    /// </summary>
    /// <param name="oldKey"></param>
    /// <param name="newKey"></param>
    /// <returns></returns>
    public bool Rename(string oldKey, string newKey)
    {
        using (new ProfileScope(this, oldKey))
        {
            return _keyValueStoreProvider.Rename(JoinWithRootNamespace(oldKey), JoinWithRootNamespace(newKey));
        }
    }

    /// <summary>
    /// Bir key'in ismini değiştirir.
    /// </summary>
    /// <param name="oldKey"></param>
    /// <param name="newKey"></param>
    /// <returns></returns>
    public async Task<bool> RenameAsync(string oldKey, string newKey)
    {
        using (new ProfileScope(this, oldKey))
        {
            return await _keyValueStoreProvider.RenameAsync(JoinWithRootNamespace(oldKey), JoinWithRootNamespace(newKey));
        }
    }

    /// <summary>
    /// Bir key'deki sayısal değeri artırır.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="amount"></param>
    /// <returns></returns>
    public long Increment(string key, long amount = 1)
    {
        using (new ProfileScope(this, key))
        {
            return CheckAutoPing(key,
                () => _keyValueStoreProvider.Increment(JoinWithRootNamespace(key), amount));
        }
    }

    /// <summary>
    /// Bir key'deki sayısal değeri artırır.
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
    /// Bir key'deki sayısal değeri azaltır.
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
    /// Bir key'deki sayısal değeri azaltır.
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
    /// Bir key'de bulunan string'e eklme yapar.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns>String'in yeni uzunluğu</returns>
    public long AppendString(string key, string value)
    {
        using (new ProfileScope(this, key))
        {
            return CheckAutoPing(key,
                () => _keyValueStoreProvider.AppendString(JoinWithRootNamespace(key), value));
        }
    }

    /// <summary>
    /// Bir key'de bulunan string'e eklme yapar.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns>String'in yeni uzunluğu</returns>
    public async Task<long> AppendStringAsync(string key, string value)
    {
        using (new ProfileScope(this, key))
        {
            return await CheckAutoPing(key,
                async () => await _keyValueStoreProvider.AppendStringAsync(JoinWithRootNamespace(key), value));
        }
    }

    /// <summary>
    /// Belli aralıktaki değeri döner.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public string SubString(string key, long start, long end)
    {
        using (new ProfileScope(this, key))
        {
            return CheckAutoPing(key,
                () => _keyValueStoreProvider.SubString(JoinWithRootNamespace(key), start, end));
        }
    }

    /// <summary>
    /// Belli aralıktaki değeri döner.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public async Task<string> SubStringAsync(string key, long start, long end)
    {
        using (new ProfileScope(this, key))
        {
            return await CheckAutoPing(key,
                async () => await _keyValueStoreProvider.SubStringAsync(JoinWithRootNamespace(key), start, end));
        }
    }

    /// <summary>
    /// Değerde bulunan string'i <paramref name="offset"/>'den itibaren <paramref name="value"/> string'ini yerleştirir.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="offset"></param>
    /// <param name="value"></param>
    /// <returns>Yeni oluşan string'in uzunluğu</returns>
    public long OverwriteString(string key, long offset, string value)
    {
        using (new ProfileScope(this, key))
        {
            return CheckAutoPing(key,
                () => _keyValueStoreProvider.OverwriteString(JoinWithRootNamespace(key), offset, value));
        }
    }

    /// <summary>
    /// Değerde bulunan string'i <paramref name="offset"/>'den itibaren <paramref name="value"/> string'ini yerleştirir.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="offset"></param>
    /// <param name="value"></param>
    /// <returns>Yeni oluşan string'in uzunluğu</returns>
    public async Task<long> OverwriteStringAsync(string key, long offset, string value)
    {
        using (new ProfileScope(this, key))
        {
            return await CheckAutoPing(key,
                async () => await _keyValueStoreProvider.OverwriteStringAsync(JoinWithRootNamespace(key), offset, value));
        }
    }

    /// <summary>
    /// Key'in büyüklüğünü byte cinsinden döner.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public long SizeInBytes(string key)
    {
        using (new ProfileScope(this, key))
        {
            return _keyValueStoreProvider.SizeInBytes(JoinWithRootNamespace(key));
        }
    }

    /// <summary>
    /// Key'in büyüklüğünü byte cinsinden döner.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public async Task<long> SizeInBytesAsync(string key)
    {
        using (new ProfileScope(this, key))
        {
            return await _keyValueStoreProvider.SizeInBytesAsync(JoinWithRootNamespace(key));
        }
    }

    /// <summary>
    /// Bir key kilitli değilse <paramref name="lockerExpire"/> süresi kadar kilitler veya kilitli ise <paramref name="waitTimeout"/> kadar kilidin açılmasını bekler. 
    /// Sürü bitimininde işlemi gerçekleştirmeden devam eder.
    /// </summary>
    /// <param name="lockerKey"></param>
    /// <param name="waitTimeout"></param>
    /// <param name="lockerExpire"></param>
    /// <param name="action"></param>
    /// <param name="skipWhenTimeout">Timeout olduğunda çalıştırılacak olan aksiyon geçilsin mi?</param>
    /// <param name="throwWhenTimeout">Timeout olduğunda <see cref="TimeoutException"/> fırlatılsın mı?</param>
    public void Lock(string lockerKey, TimeSpan waitTimeout, TimeSpan lockerExpire, Action action, bool skipWhenTimeout = true, bool throwWhenTimeout = false)
    {
        using (new ProfileScope(this, lockerKey))
        {
            Provider.Lock(JoinWithRootNamespace(lockerKey), waitTimeout, lockerExpire, action, skipWhenTimeout, throwWhenTimeout);
        }
    }

    /// <summary>
    /// Bir key kilitli değilse <paramref name="lockerExpire"/> süresi kadar kilitler veya kilitli ise <paramref name="waitTimeout"/> kadar kilidin açılmasını bekler. 
    /// Sürü bitimininde işlemi gerçekleştirmeden devam eder.
    /// </summary>
    /// <param name="lockerKey"></param>
    /// <param name="waitTimeout"></param>
    /// <param name="lockerExpire"></param>
    /// <param name="actionAsync"></param>
    /// <param name="skipWhenTimeout">Timeout olduğunda çalıştırılacak olan aksiyon geçilsin mi?</param>
    /// <param name="throwWhenTimeout">Timeout olduğunda <see cref="TimeoutException"/> fırlatılsın mı?</param>
    public async Task LockAsync(string lockerKey, TimeSpan waitTimeout, TimeSpan lockerExpire, Func<Task> actionAsync, bool skipWhenTimeout = true, bool throwWhenTimeout = false)
    {
        using (new ProfileScope(this, lockerKey))
        {
            await Provider.LockAsync(JoinWithRootNamespace(lockerKey), waitTimeout, lockerExpire, actionAsync, skipWhenTimeout, throwWhenTimeout);
        }
    }
}
