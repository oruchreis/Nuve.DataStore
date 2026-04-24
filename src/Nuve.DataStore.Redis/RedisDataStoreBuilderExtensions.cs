using Microsoft.Extensions.Logging.Abstractions;
using Nuve.DataStore.Internal;

namespace Nuve.DataStore.Redis;

public static class RedisDataStoreBuilderExtensions
{
    public static IDataStoreBuilder AddRedisDataStoreProvider(
        this IDataStoreBuilder builder,
        string providerName = "redis")
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNullOrWhiteSpace(providerName);

        var registrationStore = DataStoreServiceCollectionExtensions.GetOrAddRegistrationStore(builder.Services);

        registrationStore.AddOrReplaceProvider(
            new DataStoreProviderRegistration
            {
                Name = providerName,
                ProviderType = typeof(RedisStoreProvider),
                FromConfiguration = false
            },
            NullLogger.Instance,
            throwIfAlreadyRegisteredFromCode: false);

        return builder;
    }
}
