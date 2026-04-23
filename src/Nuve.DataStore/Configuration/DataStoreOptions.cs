namespace Nuve.DataStore.Configuration;

public sealed class DataStoreOptions
{
    public List<DataStoreProviderDefinitionOptions>? Providers { get; set; }
    public DataStoreConnectionDefinitionOptions? DefaultConnection { get; set; }

    public List<DataStoreConnectionDefinitionOptions>? Connections { get; set; }
}

public sealed class DataStoreProviderDefinitionOptions
{
    public string Name { get; set; } = default!;

    public string ConnectionString { get; set; } = default!;

    public ConnectionMode ConnectionMode { get; set; } = ConnectionMode.Shared;

    public int RetryCount { get; set; } = 5;

    public int MaxPoolSize { get; set; } = 8;

    public TimeSpan PoolWaitTimeout { get; set; } = TimeSpan.FromSeconds(2);

    public TimeSpan BackgroundProbeMinInterval { get; set; } = TimeSpan.FromSeconds(5);

    public TimeSpan HealthCheckTimeout { get; set; } = TimeSpan.FromSeconds(2);

    public TimeSpan SwapDisposeDelay { get; set; } = TimeSpan.FromSeconds(5);
}

public sealed class DataStoreConnectionDefinitionOptions
{
    public string? Name { get; set; }

    public string Provider { get; set; } = default!;

    public string? RootNamespace { get; set; }

    public int? CompressBiggerThan { get; set; }
}