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
                registrationStore.GetFinalProviders(logger),
                registrationStore.Connections,
                serviceProvider.GetRequiredService<IDataStoreSerializer>(),
                serviceProvider.GetRequiredService<IDataStoreCompressor>(),
                serviceProvider.GetRequiredService<IDataStoreProfiler>(),
                logger);
        });

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(NullLogger<>)));

        var builder = new DataStoreBuilder(services);

        RegisterConfigurationProviders(builder, configuration);
        RegisterConfigurationConnections(builder, configuration);

#if NET48
        RegisterLegacyConfigurationProviders(builder);
        RegisterLegacyConfigurationConnections(builder);
#endif

        return builder;
    }

    private static void RegisterConfigurationProviders(
        IDataStoreBuilder builder,
        IConfiguration? configuration)
    {
        if (configuration == null)
            return;

        var section = configuration.GetSection("DataStore");
        if (!section.Exists())
            return;

        var options = section.Get<DataStoreOptions>();
        if (options?.Providers == null)
            return;

        var logger = GetBootstrapLogger(builder.Services);
        var registrationStore = GetOrAddRegistrationStore(builder.Services);

        foreach (var provider in options.Providers)
        {
            if (string.IsNullOrWhiteSpace(provider.Name))
                continue;

            registrationStore.AddOrReplaceProviderOptionsFromConfiguration(
                provider.Name,
                new ConnectionOptions
                {
                    ConnectionString = provider.ConnectionString,
                    ConnectionMode = provider.ConnectionMode,
                    RetryCount = provider.RetryCount,
                    MaxPoolSize = provider.MaxPoolSize,
                    PoolWaitTimeout = provider.PoolWaitTimeout,
                    BackgroundProbeMinInterval = provider.BackgroundProbeMinInterval,
                    HealthCheckTimeout = provider.HealthCheckTimeout,
                    SwapDisposeDelay = provider.SwapDisposeDelay
                },
                logger);
        }
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
        if (options == null)
            return;

        var logger = GetBootstrapLogger(builder.Services);
        var registrationStore = GetOrAddRegistrationStore(builder.Services);

        if (options.DefaultConnection != null)
        {
            registrationStore.AddOrReplaceConnection(
                new DataStoreConnectionRegistration
                {
                    Name = DataStoreConstants.DefaultConnectionName,
                    ProviderName = options.DefaultConnection.Provider,
                    RootNamespace = options.DefaultConnection.RootNamespace ?? string.Empty,
                    CompressBiggerThan = options.DefaultConnection.CompressBiggerThan,
                    IsDefault = true,
                    FromConfiguration = true
                },
                logger,
                throwIfAlreadyRegisteredFromCode: false);
        }

        if (options.Connections == null)
            return;

        foreach (var connection in options.Connections)
        {
            if (string.IsNullOrWhiteSpace(connection.Name))
                continue;

            registrationStore.AddOrReplaceConnection(
                new DataStoreConnectionRegistration
                {
                    Name = connection.Name,
                    ProviderName = connection.Provider,
                    RootNamespace = connection.RootNamespace ?? string.Empty,
                    CompressBiggerThan = connection.CompressBiggerThan,
                    IsDefault = false,
                    FromConfiguration = true
                },
                logger,
                throwIfAlreadyRegisteredFromCode: false);
        }
    }

#if NET48
    private static void RegisterLegacyConfigurationProviders(IDataStoreBuilder builder)
    {
        var logger = GetBootstrapLogger(builder.Services);
        var registrationStore = GetOrAddRegistrationStore(builder.Services);

        var config = DataStoreConfigurationSection.GetConfiguration();
        if (config?.Providers == null)
            return;

        foreach (ProviderConfigurationElement provider in config.Providers)
        {
            if (string.IsNullOrWhiteSpace(provider.Name))
                continue;

            var connectionOptions = new ConnectionOptions
            {
                ConnectionString = provider.ConnectionString,
                ConnectionMode = provider.ConnectionMode
            };
            if (provider.RetryCount != null)
            {
                connectionOptions.RetryCount = provider.RetryCount.Value;
            }

            if (provider.MaxPoolSize != null)
            {
                connectionOptions.MaxPoolSize = provider.MaxPoolSize.Value;
            }

            if (provider.PoolWaitTimeout != null)
            {
                connectionOptions.PoolWaitTimeout = provider.PoolWaitTimeout.Value;
            }

            if (provider.BackgroundProbeMinInterval != null)
            {
                connectionOptions.BackgroundProbeMinInterval = provider.BackgroundProbeMinInterval.Value;
            }

            if (provider.HealthCheckTimeout != null)
            {
                connectionOptions.HealthCheckTimeout = provider.HealthCheckTimeout.Value;
            }

            if (provider.SwapDisposeDelay != null)
            {
                connectionOptions.SwapDisposeDelay = provider.SwapDisposeDelay.Value;
            }

            registrationStore.AddOrReplaceProviderOptionsFromConfiguration(
                provider.Name,
                connectionOptions,
                logger);
        }
    }

    private static void RegisterLegacyConfigurationConnections(IDataStoreBuilder builder)
    {
        var logger = GetBootstrapLogger(builder.Services);
        var registrationStore = GetOrAddRegistrationStore(builder.Services);

        var config = DataStoreConfigurationSection.GetConfiguration();
        if (config == null)
            return;

        if (config.DefaultConnection != null)
        {
            registrationStore.AddOrReplaceConnection(
                new DataStoreConnectionRegistration
                {
                    Name = DataStoreConstants.DefaultConnectionName,
                    ProviderName = config.DefaultConnection.ProviderName,
                    RootNamespace = config.DefaultConnection.Namespace ?? string.Empty,
                    CompressBiggerThan = config.DefaultConnection.CompressBiggerThan,
                    IsDefault = true,
                    FromConfiguration = true
                },
                logger,
                throwIfAlreadyRegisteredFromCode: false);
        }

        if (config.Connections == null)
            return;

        foreach (ConnectionConfigurationElement connection in config.Connections)
        {
            if (string.IsNullOrWhiteSpace(connection.Name))
                continue;

            registrationStore.AddOrReplaceConnection(
                new DataStoreConnectionRegistration
                {
                    Name = connection.Name,
                    ProviderName = connection.ProviderName,
                    RootNamespace = connection.Namespace ?? string.Empty,
                    CompressBiggerThan = connection.CompressBiggerThan,
                    IsDefault = false,
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
}