using Microsoft.Extensions.DependencyInjection;

namespace Nuve.DataStore;

public interface IDataStoreBuilder
{
    IServiceCollection Services { get; }
}

internal sealed class DataStoreBuilder : IDataStoreBuilder
{
    public DataStoreBuilder(IServiceCollection services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public IServiceCollection Services { get; }
}