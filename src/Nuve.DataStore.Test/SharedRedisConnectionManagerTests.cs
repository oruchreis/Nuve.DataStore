using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nuve.DataStore.Redis;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nuve.DataStore.Test;

[TestClass]
public class SharedRedisConnectionManagerTests
{
    [TestMethod]
    public void Acquire_ShouldReturnSameMultiplexer_ForMultipleCalls()
    {
        var options = new ConnectionOptions
        {
            ConnectionString = RedisTestHelpers.GetRedisConnectionString(),
            ConnectionMode = ConnectionMode.Shared,
            BackgroundProbeMinInterval = TimeSpan.FromMilliseconds(200),
            HealthCheckTimeout = TimeSpan.FromSeconds(1),
            SwapDisposeDelay = TimeSpan.FromMilliseconds(100)
        };

        using var manager = new SharedRedisConnectionManager(options);

        using var lease1 = manager.Acquire();
        using var lease2 = manager.Acquire();

        Assert.IsNotNull(lease1.Multiplexer);
        Assert.IsNotNull(lease2.Multiplexer);
        Assert.AreSame(lease1.Multiplexer, lease2.Multiplexer);
    }

    [TestMethod]
    public void ReportTimeout_ShouldNotReplaceSharedMultiplexer()
    {
        var options = new ConnectionOptions
        {
            ConnectionString = RedisTestHelpers.GetRedisConnectionString(),
            ConnectionMode = ConnectionMode.Shared
        };

        using var manager = new SharedRedisConnectionManager(options);

        using var lease = manager.Acquire();
        var before = lease.Multiplexer;

        manager.ReportTimeout(before, new RedisTimeoutException("synthetic timeout", CommandStatus.Unknown));

        using var lease2 = manager.Acquire();
        var after = lease2.Multiplexer;

        Assert.AreSame(before, after);
    }

    [TestMethod]
    public async Task ReportConnectionFailure_ShouldScheduleBackgroundProbe_WithoutBlockingAcquire()
    {
        var options = new ConnectionOptions
        {
            ConnectionString = RedisTestHelpers.GetRedisConnectionString(),
            ConnectionMode = ConnectionMode.Shared,
            BackgroundProbeMinInterval = TimeSpan.FromMilliseconds(100),
            HealthCheckTimeout = TimeSpan.FromMilliseconds(300),
            SwapDisposeDelay = TimeSpan.FromMilliseconds(100)
        };

        using var manager = new SharedRedisConnectionManager(options);

        using var lease = manager.Acquire();
        var mux = lease.Multiplexer;

        var started = DateTime.UtcNow;
        manager.ReportConnectionFailure(mux, new RedisConnectionException(ConnectionFailureType.SocketFailure, "synthetic failure"));

        using var nextLease = manager.Acquire();
        var elapsed = DateTime.UtcNow - started;

        Assert.IsNotNull(nextLease.Multiplexer);
        Assert.IsTrue(elapsed < TimeSpan.FromSeconds(1), "Acquire path appears to be blocked.");

        await RedisTestHelpers.WaitUntilAsync(
            () => RedisTestHelpers.GetBackgroundProbeScheduled(manager) == 0,
            TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public void ReportConnectionFailure_ShouldBeThrottled()
    {
        var options = new ConnectionOptions
        {
            ConnectionString = RedisTestHelpers.GetRedisConnectionString(),
            ConnectionMode = ConnectionMode.Shared,
            BackgroundProbeMinInterval = TimeSpan.FromSeconds(5)
        };

        using var manager = new SharedRedisConnectionManager(options);

        using var lease = manager.Acquire();
        var mux = lease.Multiplexer;

        manager.ReportConnectionFailure(mux, new RedisConnectionException(ConnectionFailureType.SocketFailure, "first"));
        var firstTicks = RedisTestHelpers.GetLastProbeTicks(manager);

        manager.ReportConnectionFailure(mux, new RedisConnectionException(ConnectionFailureType.SocketFailure, "second"));
        var secondTicks = RedisTestHelpers.GetLastProbeTicks(manager);

        Assert.AreEqual(firstTicks, secondTicks);
    }

    [TestMethod]
    public async Task ConcurrentAcquire_ShouldShareSingleMultiplexer()
    {
        var options = new ConnectionOptions
        {
            ConnectionString = RedisTestHelpers.GetRedisConnectionString(),
            ConnectionMode = ConnectionMode.Shared
        };

        using var manager = new SharedRedisConnectionManager(options);

        var tasks = Enumerable.Range(0, 32)
            .Select(_ => Task.Run(() =>
            {
                using var lease = manager.Acquire();
                return lease.Multiplexer;
            }))
            .ToArray();

        await Task.WhenAll(tasks).ConfigureAwait(false);

        var first = tasks[0].Result;
        foreach (var task in tasks)
            Assert.AreSame(first, task.Result);
    }
}