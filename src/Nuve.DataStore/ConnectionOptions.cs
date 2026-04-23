using System;
using System.Collections.Generic;
using System.Text;

namespace Nuve.DataStore;

public sealed class ConnectionOptions
{
    public string ConnectionString { get; set; } = default!;
    public ConnectionMode ConnectionMode { get; set; } = ConnectionMode.Shared;
    public int RetryCount { get; set; } = 5;

    // Pooled mode
    public int MaxPoolSize { get; set; } = 8;
    public TimeSpan PoolWaitTimeout { get; set; } = TimeSpan.FromSeconds(2);

    // Shared mode
    public TimeSpan BackgroundProbeMinInterval { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan HealthCheckTimeout { get; set; } = TimeSpan.FromSeconds(2);
    public TimeSpan SwapDisposeDelay { get; set; } = TimeSpan.FromSeconds(5);
}