using System.Reflection;

#if NET48
using Nuve.DataStore.Configuration;
using System.Configuration;
#endif

namespace Nuve.DataStore;

/// <summary>
/// <summary>
/// Controls the providers used by DataStore and their connections.
/// </summary>
/// <remarks>
/// <para>By default, it creates the providers and connections defined in the application's config on the first access.</para>
/// <para>New providers and connections can be registered upon request.</para>
/// <para>Providers and connections must be unique. Therefore, any provider and connection definitions outside the config must be made only once.</para>
/// <para>If previously defined providers and connections are attempted to be defined again, an exception is thrown because the definition operations lock other invocations and prevent them from being used everywhere.</para>
/// </remarks>
public static class DataStoreManager
{
    private static readonly ReaderWriterLockSlim _providerTypeLocker = new();
    private static readonly Dictionary<string, Type> _providerTypes = new();

    private static readonly ReaderWriterLockSlim _providerLocker = new();
    private static readonly Dictionary<string, IDataStoreProvider> _providers = new();
    private static readonly Dictionary<string, string> _rootNamespaces = new();
    private static readonly Dictionary<string, int?> _compressBiggerThans = new();
    private static string? _defaultConnection;

    private static Type GetType(string typeStr)
    {
        var typeParts = typeStr.Split(',');
        var className = typeParts[0];
        var assembly = Assembly.GetExecutingAssembly();
        if (typeParts.Length > 1)
        {
            assembly = Assembly.Load(typeParts[1].Trim());
        }

        return assembly.GetType(className, true, false);
    }

    /// <summary>
    /// Registers a new provider type. The provider type must implement at least <see cref="IDataStoreProvider"/>.
    /// </summary>
    /// <param name="providerName">The name of the provider, it should not be previously registered.</param>
    /// <param name="providerTypeString">The string representation of the provider type. It should be in the format "ClassPath, Assembly".</param>
    /// <exception cref="TypeLoadException">Thrown if the assembly for the type cannot be found or if the type does not implement <see cref="IDataStoreProvider"/>.</exception>
    public static void RegisterProvider(string providerName, string providerTypeString)
    {
        _providerTypeLocker.EnterUpgradeableReadLock();
        try
        {
            if (_providerTypes.ContainsKey(providerName))
                throw new ArgumentException(string.Format("The provider '{0}' is already defined.", providerName), nameof(providerName));

            Type providerType;
            try
            {
                providerType = GetType(providerTypeString);
            }
            catch (Exception e)
            {
                throw new TypeLoadException(string.Format("Failed to load type '{0}'!", providerTypeString), e);
            }

            RegisterProvider(providerName, providerType);
        }
        finally
        {
            _providerTypeLocker.ExitUpgradeableReadLock();
        }
    }

    /// <summary>
    /// Registers a new provider type. The provider type must implement at least <see cref="IDataStoreProvider"/>.
    /// </summary>
    /// <param name="providerName">The name of the provider, it should not be previously registered.</param>
    /// <param name="providerType">The provider type.</param>
    /// <exception cref="TypeLoadException">Thrown if the assembly for the type cannot be found or if the type does not implement <see cref="IDataStoreProvider"/>.</exception>
    public static void RegisterProvider(string providerName, Type providerType)
    {
        _providerTypeLocker.EnterWriteLock();
        try
        {
            if (_providerTypes.ContainsKey(providerName))
                throw new ArgumentException(string.Format("The provider '{0}' is already defined.", providerName), nameof(providerName));         

            if (!typeof(IDataStoreProvider).IsAssignableFrom(providerType))
                throw new TypeLoadException(string.Format("Invalid type '{0}'. '{0}' must implement IDataStoreProvider.", providerType.FullName));

            _providerTypes.Add(providerName, providerType);
        }
        finally
        {
            _providerTypeLocker.ExitWriteLock();
        }
    }

    /// <summary>
    /// Creates a new connection. If a connection with the same name already exists, an exception is thrown.
    /// </summary>
    /// <param name="connectionName">The unique name of the connection.</param>
    /// <param name="providerName">The name of the previously registered provider.</param>
    /// <param name="connectionString">The provider-specific connection string.</param>
    /// <param name="rootNamespace"></param>
    /// <param name="isDefault">Is this the default connection?</param>
    public static void CreateConnection(string connectionName, string providerName, string connectionString,
        string rootNamespace = "", bool isDefault = false, int? compressBiggerThan = null)
    {
        _providerLocker.EnterWriteLock();
        try
        {
            if (_providers.ContainsKey(connectionName))
                throw new ArgumentException(string.Format("There is already a connection with the name '{0}'.", connectionName), nameof(connectionName));

            _providerTypeLocker.EnterReadLock();
            try
            {
                if (!_providerTypes.ContainsKey(providerName))
                    throw new ArgumentException(string.Format("Couldn't find the provider '{0}'", providerName), nameof(providerName));

                var provider = (IDataStoreProvider)Activator.CreateInstance(_providerTypes[providerName])!;
                provider.Initialize(connectionString, new InternalProfilerProxy());
                _providers.Add(connectionName, provider);

                _rootNamespaces[connectionName] = rootNamespace ?? "";
                _compressBiggerThans[connectionName] = compressBiggerThan;

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

    private static Lazy<IDataStoreSerializer> _defaultSerializer;
    private static readonly ReaderWriterLockSlim _defaultSerializerLocker = new ReaderWriterLockSlim();

    /// <summary>
    /// Gets or sets the default serializer to be used in all DataStore operations.
    /// </summary>
    /// <remarks>
    /// It is recommended to perform the modification operations at the beginning of the application because the modification process is subject to a lock.
    /// </remarks>
    public static IDataStoreSerializer DefaultSerializer
    {
        get
        {
            _defaultSerializerLocker.EnterReadLock();
            try
            {
                return _defaultSerializer.Value;
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
                _defaultSerializer = new(() => value);
            }
            finally
            {
                _defaultSerializerLocker.ExitWriteLock();
            }
        }
    }

    private static Lazy<IDataStoreCompressor> _defaultCompressor;
    private static readonly ReaderWriterLockSlim _defaultCompressorLocker = new();

    /// <summary>
    /// Gets or sets the default compressor to be used in all DataStore operations.
    /// </summary>
    /// <remarks>
    /// It is recommended to perform the modification operations at the beginning of the application because the modification process is subject to a lock.
    /// </remarks>
    public static IDataStoreCompressor DefaultCompressor
    {
        get
        {
            _defaultCompressorLocker.EnterReadLock();
            try
            {
                return _defaultCompressor.Value;
            }
            finally
            {
                _defaultCompressorLocker.ExitReadLock();
            }
        }
        set
        {
            _defaultCompressorLocker.EnterWriteLock();
            try
            {
                _defaultCompressor = new(() => value);
            }
            finally
            {
                _defaultCompressorLocker.ExitWriteLock();
            }
        }
    }

    static DataStoreManager()
    {
#if NET48
        var config = DataStoreConfigurationSection.GetConfiguration();
        if (config != null)
        {

            IDataStoreSerializer? serializer = null;
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
                //intentionally left blank
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
                    CreateConnection(connection.Name, connection.ProviderName, connection.ConnectionString, connection.Namespace, connection.IsDefault, connection.CompressBiggerThan);
                }
        }
#endif

        _defaultSerializer ??= new Lazy<IDataStoreSerializer>(() => new DefaultSerializer());
        _defaultCompressor ??= new Lazy<IDataStoreCompressor>(() => new DeflateCompressor());
    }

    internal static void GetProvider(string? connectionName, out IDataStoreProvider provider, out string rootNamespace, out int? compressBiggerThan)
    {
        _providerLocker.EnterReadLock();
        try
        {
            if (connectionName == null || !_providers.ContainsKey(connectionName))
                connectionName = _defaultConnection ?? throw new ArgumentNullException(nameof(connectionName));
            provider = _providers[connectionName];
            rootNamespace = _rootNamespaces[connectionName];
            compressBiggerThan = _compressBiggerThans[connectionName];
        }
        finally
        {
            _providerLocker.ExitReadLock();
        }
    }

    private static readonly ReaderWriterLockSlim _globalProfilerLocker = new();
    private static IDataStoreProfiler _globalProfiler = new NullDataStoreProfiler();
    /// <summary>
    /// Registers the profiler used to profile all datastore structures.
    /// </summary>
    /// <param name="profiler">The profiler to be registered.</param>
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
