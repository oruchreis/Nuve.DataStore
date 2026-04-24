using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nuve.DataStore.Configuration;
using Nuve.DataStore.Internal;

namespace Nuve.DataStore;

public static class DataStoreServiceCollectionExtensions
{
    public static IDataStoreBuilder AddDataStore(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        ThrowHelper.ThrowIfNull(services);

        services.TryAddSingleton<IDataStoreSerializer, DefaultSerializer>();
        services.TryAddSingleton<IDataStoreCompressor, DeflateCompressor>();
        services.TryAddSingleton<IDataStoreProfiler, NullDataStoreProfiler>();

        var registrationStore = GetOrAddRegistrationStore(services);

        services.TryAddSingleton<DataStoreManager>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<DataStoreManager>>();
            var resolvedConfiguration = configuration ?? serviceProvider.GetService<IConfiguration>();
            var finalizedConnections = registrationStore.CreateFinalConnections(
                BuildConfigurationConnections(resolvedConfiguration),
                BuildLegacyConfigurationConnections());

            return new DataStoreManager(
                serviceProvider,
                registrationStore.Providers,
                registrationStore.Serializers,
                finalizedConnections,
                serviceProvider.GetRequiredService<IDataStoreSerializer>(),
                serviceProvider.GetRequiredService<IDataStoreCompressor>(),
                serviceProvider.GetRequiredService<IDataStoreProfiler>(),
                logger);
        });

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(NullLogger<>)));

        return new DataStoreBuilder(services);
    }

    private static IReadOnlyCollection<DataStoreConnectionRegistration> BuildConfigurationConnections(
        IConfiguration? configuration)
    {
        if (configuration == null)
            return Array.Empty<DataStoreConnectionRegistration>();

        var section = configuration.GetSection("DataStore");
        if (!section.Exists())
            return Array.Empty<DataStoreConnectionRegistration>();

        var options = section.Get<DataStoreOptions>();
        if (options?.Connections == null)
            return Array.Empty<DataStoreConnectionRegistration>();

        var registrations = new List<DataStoreConnectionRegistration>(options.Connections.Count);

        foreach (var connectionEntry in options.Connections)
        {
            var connection = connectionEntry.Value;
            var connectionName = connection.IsDefault
                ? DataStoreConstants.DefaultConnectionName
                : connectionEntry.Key;

            if (string.IsNullOrWhiteSpace(connectionName))
                continue;

            registrations.Add(new DataStoreConnectionRegistration
            {
                Name = connectionName,
                ProviderName = connection.Provider,
                Options = CreateConnectionOptions(connection),
                SerializerName = connection.Serializer,
                RootNamespace = connection.RootNamespace ?? string.Empty,
                CompressBiggerThan = connection.CompressBiggerThan,
                IsDefault = connection.IsDefault,
                FromConfiguration = true
            });
        }

        return registrations;
    }

#if NET48
    private static IReadOnlyCollection<DataStoreConnectionRegistration> BuildLegacyConfigurationConnections()
    {
        var config = DataStoreConfigurationSection.GetConfiguration();
        if (config?.Connections == null)
            return Array.Empty<DataStoreConnectionRegistration>();

        var registrations = new List<DataStoreConnectionRegistration>();

        foreach (ConnectionConfigurationElement connection in config.Connections)
        {
            var connectionName = connection.IsDefault
                ? DataStoreConstants.DefaultConnectionName
                : connection.Name;

            if (string.IsNullOrWhiteSpace(connectionName))
                continue;

            registrations.Add(new DataStoreConnectionRegistration
            {
                Name = connectionName,
                ProviderName = connection.Provider,
                Options = CreateConnectionOptions(connection),
                SerializerName = connection.Serializer,
                RootNamespace = connection.Namespace ?? string.Empty,
                CompressBiggerThan = connection.CompressBiggerThan,
                IsDefault = connection.IsDefault,
                FromConfiguration = true
            });
        }

        return registrations;
    }
#else
    private static IReadOnlyCollection<DataStoreConnectionRegistration> BuildLegacyConfigurationConnections()
    {
        return Array.Empty<DataStoreConnectionRegistration>();
    }
#endif

    internal static DataStoreRegistrationStore GetOrAddRegistrationStore(IServiceCollection services)
    {
        ThrowHelper.ThrowIfNull(services);

        foreach (var descriptor in services)
        {
            if (descriptor.ServiceType != typeof(DataStoreRegistrationStore))
                continue;

            if (descriptor.ImplementationInstance is DataStoreRegistrationStore existingInstance)
                return existingInstance;
        }

        var created = new DataStoreRegistrationStore();
        services.AddSingleton(created);
        return created;
    }

    internal static ILogger GetBootstrapLogger(IServiceCollection services)
    {
        return NullLogger.Instance;
    }

    private static ConnectionOptions CreateConnectionOptions(DataStoreConnectionDefinitionOptions connection)
    {
        return new ConnectionOptions
        {
            ConnectionString = connection.ConnectionString,
            ConnectionMode = connection.ConnectionMode,
            RetryCount = connection.RetryCount,
            MaxPoolSize = connection.MaxPoolSize,
            PoolWaitTimeout = connection.PoolWaitTimeout,
            BackgroundProbeMinInterval = connection.BackgroundProbeMinInterval,
            HealthCheckTimeout = connection.HealthCheckTimeout,
            SwapDisposeDelay = connection.SwapDisposeDelay
        };
    }

#if NET48
    private static ConnectionOptions CreateConnectionOptions(ConnectionConfigurationElement connection)
    {
        return new ConnectionOptions
        {
            ConnectionString = connection.ConnectionString,
            ConnectionMode = connection.ConnectionMode,
            RetryCount = connection.RetryCount ?? 5,
            MaxPoolSize = connection.MaxPoolSize ?? 8,
            PoolWaitTimeout = connection.PoolWaitTimeout ?? TimeSpan.FromSeconds(2),
            BackgroundProbeMinInterval = connection.BackgroundProbeMinInterval ?? TimeSpan.FromSeconds(5),
            HealthCheckTimeout = connection.HealthCheckTimeout ?? TimeSpan.FromSeconds(2),
            SwapDisposeDelay = connection.SwapDisposeDelay ?? TimeSpan.FromSeconds(5)
        };
    }
#endif
}
