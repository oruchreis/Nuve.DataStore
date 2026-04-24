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

            return new DataStoreManager(
                serviceProvider,
                registrationStore.Providers,
                registrationStore.Serializers,
                registrationStore.Connections,
                serviceProvider.GetRequiredService<IDataStoreSerializer>(),
                serviceProvider.GetRequiredService<IDataStoreCompressor>(),
                serviceProvider.GetRequiredService<IDataStoreProfiler>(),
                logger);
        });

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(NullLogger<>)));

        var builder = new DataStoreBuilder(services);

        RegisterConfigurationConnections(builder, configuration);

#if NET48
        RegisterLegacyConfigurationConnections(builder);
#endif

        return builder;
    }

    private static void RegisterConfigurationConnections(
        IDataStoreBuilder builder,
        IConfiguration? configuration)
    {
        if (configuration == null)
            return;

        var section = configuration.GetSection("DataStore");
        if (!section.Exists())
            return;

        var options = section.Get<DataStoreOptions>();
        if (options?.Connections == null)
            return;

        var logger = GetBootstrapLogger(builder.Services);
        var registrationStore = GetOrAddRegistrationStore(builder.Services);

        foreach (var connection in options.Connections)
        {
            var connectionName = connection.IsDefault
                ? DataStoreConstants.DefaultConnectionName
                : connection.Name;

            if (string.IsNullOrWhiteSpace(connectionName))
                continue;

            registrationStore.AddOrReplaceConnection(
                new DataStoreConnectionRegistration
                {
                    Name = connectionName,
                    ProviderName = connection.Provider,
                    Options = CreateConnectionOptions(connection),
                    SerializerName = connection.Serializer,
                    RootNamespace = connection.RootNamespace ?? string.Empty,
                    CompressBiggerThan = connection.CompressBiggerThan,
                    IsDefault = connection.IsDefault,
                    FromConfiguration = true
                },
                logger,
                throwIfAlreadyRegisteredFromCode: false);
        }
    }

#if NET48
    private static void RegisterLegacyConfigurationConnections(IDataStoreBuilder builder)
    {
        var logger = GetBootstrapLogger(builder.Services);
        var registrationStore = GetOrAddRegistrationStore(builder.Services);

        var config = DataStoreConfigurationSection.GetConfiguration();
        if (config?.Connections == null)
            return;

        foreach (ConnectionConfigurationElement connection in config.Connections)
        {
            var connectionName = connection.IsDefault
                ? DataStoreConstants.DefaultConnectionName
                : connection.Name;

            if (string.IsNullOrWhiteSpace(connectionName))
                continue;

            registrationStore.AddOrReplaceConnection(
                new DataStoreConnectionRegistration
                {
                    Name = connectionName,
                    ProviderName = connection.Provider,
                    Options = CreateConnectionOptions(connection),
                    SerializerName = connection.Serializer,
                    RootNamespace = connection.Namespace ?? string.Empty,
                    CompressBiggerThan = connection.CompressBiggerThan,
                    IsDefault = connection.IsDefault,
                    FromConfiguration = true
                },
                logger,
                throwIfAlreadyRegisteredFromCode: false);
        }
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
