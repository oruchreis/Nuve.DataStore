using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nuve.DataStore.Configuration;
using Nuve.DataStore.Internal;
#if NET48
using System.Configuration;
#endif

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
            return new DataStoreManager(
                serviceProvider,
                registrationStore.Providers,
                registrationStore.Connections,
                serviceProvider.GetRequiredService<IDataStoreSerializer>(),
                serviceProvider.GetRequiredService<IDataStoreCompressor>(),
                serviceProvider.GetRequiredService<IDataStoreProfiler>(),
                serviceProvider.GetRequiredService<ILogger<DataStoreManager>>());
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

        if (options.DefaultConnection != null)
        {
            GetOrAddRegistrationStore(builder.Services).AddOrReplaceConnection(
                new DataStoreConnectionRegistration
                {
                    Name = "__default__",
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

            GetOrAddRegistrationStore(builder.Services).AddOrReplaceConnection(
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
                    Name = "__default__",
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

    internal static ILogger GetBootstrapLogger(IServiceCollection services)
    {
        return NullLogger.Instance;
    }
}