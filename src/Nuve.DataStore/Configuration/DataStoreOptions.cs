namespace Nuve.DataStore.Configuration;

public sealed class DataStoreOptions
{
    public DataStoreConnectionDefinitionOptions? DefaultConnection { get; set; }

    public List<DataStoreConnectionDefinitionOptions>? Connections { get; set; }
}

public sealed class DataStoreConnectionDefinitionOptions
{
    public string? Name { get; set; }

    public string Provider { get; set; } = default!;

    public string? RootNamespace { get; set; }

    public int? CompressBiggerThan { get; set; }
}