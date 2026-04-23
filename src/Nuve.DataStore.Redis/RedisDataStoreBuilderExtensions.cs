using Microsoft.Extensions.Logging.Abstractions;
using Nuve.DataStore.Internal;

namespace Nuve.DataStore.Redis;

public static class RedisDataStoreBuilderExtensions
{
    public static IDataStoreBuilder AddRedisDataStoreProvider(
        this IDataStoreBuilder builder,
        string providerName = "redis",
        ConnectionOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(builder);

        var registrationStore = DataStoreServiceCollectionExtensions.GetOrAddRegistrationStore(builder.Services);

        registrationStore.AddOrReplaceProvider(
            new DataStoreProviderRegistration
            {
                Name = providerName,
                ProviderType = typeof(RedisStoreProvider),
                Options = options ?? new ConnectionOptions(),
                FromConfiguration = false
            },
            NullLogger.Instance,
            throwIfAlreadyRegisteredFromCode: true);

        return builder;
    }
}