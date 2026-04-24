namespace Nuve.DataStore.Internal;

internal sealed class DataStoreConnectionRegistration
{
    public string Name { get; init; } = default!;

    public string ProviderName { get; init; } = default!;

    public ConnectionOptions Options { get; init; } = default!;

    public Action<ConnectionOptions>? ConfigureOptions { get; init; }

    public string? SerializerName { get; init; }

    public string? RootNamespace { get; init; }

    public int? CompressBiggerThan { get; init; }

    public bool IsDefault { get; init; }

    public bool FromConfiguration { get; init; }
}
