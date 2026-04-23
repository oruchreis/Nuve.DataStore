namespace Nuve.DataStore.Internal;

internal sealed class DataStoreConnectionRegistration
{
    public string Name { get; init; } = default!;

    public string ProviderName { get; init; } = default!;

    public string RootNamespace { get; init; } = string.Empty;

    public int? CompressBiggerThan { get; init; }

    public bool IsDefault { get; init; }

    public bool FromConfiguration { get; init; }
}
