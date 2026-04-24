namespace Nuve.DataStore.Configuration;

public sealed class DataStoreOptions
{
    public Dictionary<string, DataStoreConnectionDefinitionOptions>? Connections { get; set; }
}

public sealed class DataStoreConnectionDefinitionOptions
{
    public string Provider { get; set; } = default!;

    public string? Serializer { get; set; }

    public string ConnectionString { get; set; } = default!;

    public ConnectionMode ConnectionMode { get; set; } = ConnectionMode.Shared;

    public int RetryCount { get; set; } = 5;

    public int MaxPoolSize { get; set; } = 8;

    public TimeSpan PoolWaitTimeout { get; set; } = TimeSpan.FromSeconds(2);

    public TimeSpan BackgroundProbeMinInterval { get; set; } = TimeSpan.FromSeconds(5);

    public TimeSpan HealthCheckTimeout { get; set; } = TimeSpan.FromSeconds(2);

    public TimeSpan SwapDisposeDelay { get; set; } = TimeSpan.FromSeconds(5);

    public string? RootNamespace { get; set; }

    public int? CompressBiggerThan { get; set; }

    public bool IsDefault { get; set; }
}
