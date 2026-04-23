namespace Nuve.DataStore.Internal;

internal sealed class DataStoreProviderOptionsRegistration
{
    public string Name { get; set; } = default!;

    public ConnectionOptions Options { get; set; } = default!;

    public bool FromConfiguration { get; set; }
}