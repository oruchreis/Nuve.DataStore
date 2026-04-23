using Microsoft.Extensions.DependencyInjection;
using Nuve.DataStore.Internal;

namespace Nuve.DataStore;

public static class DataStoreServiceProviderExtensions
{
    public static IServiceProvider InitializeDataStore(this IServiceProvider serviceProvider)
    {
        ThrowHelper.ThrowIfNull(serviceProvider);

        var manager = serviceProvider.GetRequiredService<DataStoreManager>();
        manager.InitializeProviders();
        DataStoreRuntime.Initialize(manager);

        return serviceProvider;
    }

    public static async Task<IServiceProvider> InitializeDataStoreAsync(this IServiceProvider serviceProvider)
    {
        ThrowHelper.ThrowIfNull(serviceProvider);

        var manager = serviceProvider.GetRequiredService<DataStoreManager>();
        await manager.InitializeProvidersAsync().ConfigureAwait(false);
        DataStoreRuntime.Initialize(manager);

        return serviceProvider;
    }
}