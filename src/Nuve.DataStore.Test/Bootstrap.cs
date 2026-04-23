using Microsoft.Extensions.DependencyInjection;
using Nuve.DataStore.Redis;
using Nuve.DataStore.Serializer.JsonNet;

namespace Nuve.DataStore.Test;

public static class Bootstrap
{
    public static ServiceProvider BuildRedisServiceProvider(
        string rootNamespace,
        string defaultConnectionName = "redis",
        string providerName = "Redis",
        string? connectionString = null)
    {
        var services = new ServiceCollection();

        services
            .AddDataStore()
            .AddDataStoreSerializer<JsonNetDataStoreSerializer>()
            .AddRedisDataStoreProvider(
                providerName: providerName,
                options: new ConnectionOptions
                {
                    ConnectionString = connectionString ?? RedisTestHelpers.GetRedisConnectionString(),
                    ConnectionMode = ConnectionMode.Shared
                })
            .AddDefaultConnection(
                provider: providerName,
                rootNamespace: rootNamespace)
            .AddConnection(
                name: defaultConnectionName,
                provider: providerName,
                rootNamespace: rootNamespace);

        return services.BuildServiceProvider();
    }

    public static ServiceProvider BuildRedisServiceProvider(
        Action<IDataStoreBuilder> configure,
        string providerName = "Redis",
        string? connectionString = null)
    {
        var services = new ServiceCollection();

        var builder = services
            .AddDataStore()
            .AddDataStoreSerializer<JsonNetDataStoreSerializer>()
            .AddRedisDataStoreProvider(
                providerName: providerName,
                options: new ConnectionOptions
                {
                    ConnectionString = connectionString ?? RedisTestHelpers.GetRedisConnectionString(),
                    ConnectionMode = ConnectionMode.Shared
                });

        configure(builder);

        return services.BuildServiceProvider();
    }

    public static string NewRootNamespace(string prefix)
    {
        return $"{prefix}:{Guid.NewGuid():N}";
    }
}

public static class DataStoreAccess
{
    public static IDataStoreProvider GetDefaultProvider()
    {
        var context = DataStoreRuntime.Manager.GetConnection(null);
        return context.Provider;
    }

    public static (IDataStoreProvider Provider, string RootNamespace, int? CompressBiggerThan) GetConnection(string? connectionName = null)
    {
        var context = DataStoreRuntime.Manager.GetConnection(connectionName);
        return (context.Provider, context.RootNamespace, context.CompressBiggerThan);
    }
}