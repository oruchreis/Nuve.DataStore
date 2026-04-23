namespace Nuve.DataStore.Internal;

internal sealed class DataStoreProviderRegistration
{
    public string Name { get; init; } = default!;

    public Type ProviderType { get; init; } = default!;

    public ConnectionOptions Options { get; init; } = default!;

    public bool FromConfiguration { get; init; }
}

