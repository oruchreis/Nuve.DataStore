using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

#if NET452
using Nuve.DataStore.Configuration;
using System.Configuration;
#endif

namespace Nuve.DataStore
{
    /// <summary>
    /// DataStore'un kullandığı provider'ları ve bunların bağlantılarını kontrol eder.
    /// </summary>
    /// <remarks>
    /// <para>Varsayılan olarak ilk erişimde uygulamanın config'inde tanımlı provider ve bağlantıları oluşturur.</para>
    /// <para>Talep olursa yeni provider'lar ve bağlantılar kaydedilir.</para>
    /// <para>Provider'lar ve bağlantılar tekil olması gerekir. Bu yüzden config dışındaki provider ve bağlantı tanımlamalarının bir defaya mahsus yapılması gerekmektedir.</para>
    /// <para>Önceden tanımlı provider ve bağlantılar tanımlanmaya çalışılırsa hata fırlatılır 
    /// çünkü tanımlama işlemleri diğer çağırımları kilitlemektedir ve her yerde kullanılmasının önüne geçilmesi gerekmektedir.</para>
    /// </remarks>
    public static class DataStoreManager
    {
        private static readonly ReaderWriterLockSlim _providerTypeLocker = new ReaderWriterLockSlim();
        private static readonly Dictionary<string, Type> _providerTypes = new Dictionary<string, Type>();        

        private static readonly ReaderWriterLockSlim _providerLocker = new ReaderWriterLockSlim();
        private static readonly Dictionary<string, IDataStoreProvider> _providers = new Dictionary<string, IDataStoreProvider>();
        private static readonly Dictionary<string, string> _rootNamespaces = new Dictionary<string, string>();
        private static string _defaultConnection;

        private static Type GetType(string typeStr)
        {
#if NET452
            var typeParts = typeStr.Split(',');
            var className = typeParts[0];
            var assembly = Assembly.GetExecutingAssembly();
            if (typeParts.Length > 1)
            {
                assembly = Assembly.Load(typeParts[1].Trim());
            }

            return assembly.GetType(className, true, false);
#endif
#if NETSTANDARD1_6
            return Type.GetType(typeStr);
#endif
        }

        /// <summary>
        /// Yeni bir provider tipini kaydeder. Provider tipi en azından <see cref="IDataStoreProvider"/> tanımlamalıdır.
        /// </summary>
        /// <param name="providerName">Provider'a ait isim, önceden kayıtlı olmamalıdır.</param>
        /// <param name="providerTypeString">Provider tipine ait string. "ClassPath, Assembly" şeklinde olmalı.</param>
        /// <exception cref="TypeLoadException">Eğer tipe ait assembly bulunamazsa veya tip <see cref="IDataStoreProvider"/> arayüzünü tanımlamıyorsa bu hata fırlatılır.</exception>
        public static void RegisterProvider(string providerName, string providerTypeString)
        {
            if (DefaultSerializer == null)
                DefaultSerializer = new DefaultSerializer();

            _providerTypeLocker.EnterWriteLock();
            try
            {
                if (_providerTypes.ContainsKey(providerName))
                    throw new Exception(string.Format("The provider '{0}' is already defined.", providerName));

                Type providerType;
                try
                {                    
                    providerType = GetType(providerTypeString);
                }
                catch (Exception e)
                {
                    throw new TypeLoadException(string.Format("Failed to load type '{0}'!", providerTypeString), e);
                }

                if (!typeof(IDataStoreProvider).IsAssignableFrom(providerType))
                    throw new TypeLoadException(string.Format("Invalid type '{0}'. '{0}' must implement IDataStoreProvider.", providerTypeString));

                _providerTypes.Add(providerName, providerType);
            }
            finally
            {
                _providerTypeLocker.ExitWriteLock();
            }
        }

        /// <summary>
        /// Yeni bir bağlantı oluşturur. Aynı isimde bağlantı varsa hata fırlatılır.
        /// </summary>
        /// <param name="connectionName">Tekil bağlantı ismi</param>
        /// <param name="providerName">Önceden kaydedilmiş provider adı.</param>
        /// <param name="connectionString">Provider'a özgü connection string.</param>
        /// <param name="rootNamespace"></param>
        /// <param name="isDefault">Varsayılan bağlantı bu mu?</param>
        public static void CreateConnection(string connectionName, string providerName, string connectionString, 
            string rootNamespace = "", bool isDefault = false)
        {
            _providerLocker.EnterWriteLock();
            try
            {
                if(_providers.ContainsKey(connectionName))
                    throw new Exception(string.Format("Theres is already a connection with name '{0}'.", connectionName));                

                _providerTypeLocker.EnterReadLock();
                try
                {
                    if (!_providerTypes.ContainsKey(providerName))
                        throw new Exception(string.Format("Couldn't find the provider '{0}'", providerName));

                    var provider = (IDataStoreProvider)Activator.CreateInstance(_providerTypes[providerName]);
                    provider.Initialize(connectionString, new InternalProfilerProxy());
                    _providers.Add(connectionName, provider);

                    _rootNamespaces[connectionName] = rootNamespace ?? "";

                    if (_defaultConnection == null || isDefault)
                        _defaultConnection = connectionName;
                }
                finally
                {
                    _providerTypeLocker.ExitReadLock();
                }                
            }
            finally
            {
                _providerLocker.ExitWriteLock();
            }
        }

        private static IDataStoreSerializer _defaultSerializer;
        private static readonly ReaderWriterLockSlim _defaultSerializerLocker = new ReaderWriterLockSlim();

        /// <summary>
        /// Tüm DataStore işlemlerinde kullanılacak varsayılan serializer'ı getirmek ya da değiştirmek için kullanılır.
        /// </summary>
        /// <remarks>Değiştirme işlemlerini uygulama başlangıcında yapmanız tavsiye olunur. Çünkü değiştirme işlemi lock'a tabidir.</remarks>
        public static IDataStoreSerializer DefaultSerializer
        {
            get
            {
                _defaultSerializerLocker.EnterReadLock();
                try
                {
                    return _defaultSerializer;
                }
                finally
                {
                    _defaultSerializerLocker.ExitReadLock();
                }
            }
            set
            {
                _defaultSerializerLocker.EnterWriteLock();
                try
                {
                    _defaultSerializer = value;
                }
                finally
                {
                    _defaultSerializerLocker.ExitWriteLock();
                }
            }
        }

        static DataStoreManager()
        {
#if NET452
            var config = DataStoreConfigurationSection.GetConfiguration();
            if (config == null) return;

            IDataStoreSerializer serializer = null;
            try
            {
                if (!string.IsNullOrEmpty(config.DefaultSerializer))
                {
                    var serializerType = GetType(config.DefaultSerializer);
                    serializer = Activator.CreateInstance(serializerType) as IDataStoreSerializer;
                }
            }
            catch
            {
            }
            if (serializer != null)
                DefaultSerializer = serializer;

            if (config.Providers != null)
                foreach (NameValueConfigurationElement provider in config.Providers)
                {
                    if (string.IsNullOrEmpty(provider.Name))
                        continue;
                    RegisterProvider(provider.Name, provider.Value);
                }

            if (config.Connections != null)
                foreach (ConnectionConfigurationElement connection in config.Connections)
                {
                    if (string.IsNullOrEmpty(connection.Name))
                        continue;
                    CreateConnection(connection.Name, connection.ProviderName, connection.ConnectionString, connection.Namespace, connection.IsDefault);
                }
#endif
        }

        internal static void GetProvider(string connectionName, out IDataStoreProvider provider, out string rootNamespace)
        {
            _providerLocker.EnterReadLock();
            try
            {
                if (connectionName == null || !_providers.ContainsKey(connectionName))
                    connectionName = _defaultConnection;
                provider = _providers[connectionName];
                rootNamespace = _rootNamespaces[connectionName];
            }
            finally
            {
                _providerLocker.ExitReadLock();
            }
        }

        private static readonly ReaderWriterLockSlim _globalProfilerLocker = new ReaderWriterLockSlim();
        private static IDataStoreProfiler _globalProfiler = new NullDataStoreProfiler();
        /// <summary>
        /// Tüm datastore yapılarını profile etmek için kullanılan profiler'ı kaydeder.
        /// </summary>
        /// <param name="profiler"></param>
        public static void RegisterGlobalProfiler(IDataStoreProfiler profiler)
        {
            _globalProfilerLocker.EnterWriteLock();
            try
            {
                _globalProfiler = profiler;
            }
            finally
            {
                _globalProfilerLocker.ExitWriteLock();
            }
        }

        internal static IDataStoreProfiler GlobalProfiler
        {
            get
            {
                _globalProfilerLocker.EnterReadLock();
                try
                {
                    return _globalProfiler;
                }
                finally
                {
                    _globalProfilerLocker.ExitReadLock();
                }
            }
        }
    }
}
