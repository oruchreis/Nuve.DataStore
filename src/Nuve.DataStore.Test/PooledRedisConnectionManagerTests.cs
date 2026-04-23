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
public class PooledRedisConnectionManagerTests
{
    [TestMethod]
    public void AcquireRelease_ShouldReuseMultiplexer()
    {
        var options = new ConnectionOptions
        {
            ConnectionString = RedisTestHelpers.GetRedisConnectionString(),
            ConnectionMode = ConnectionMode.Pooled,
            MaxPoolSize = 2,
            PoolWaitTimeout = TimeSpan.FromMilliseconds(500)
        };

        using var manager = new PooledRedisConnectionManager(options);

        ConnectionMultiplexer firstMux;
        using (var lease1 = manager.Acquire())
        {
            firstMux = lease1.Multiplexer;
        }

        using var lease2 = manager.Acquire();
        Assert.AreSame(firstMux, lease2.Multiplexer);
    }

    [TestMethod]
    public void Acquire_WhenPoolExhausted_ShouldThrowTimeoutException()
    {
        var options = new ConnectionOptions
        {
            ConnectionString = RedisTestHelpers.GetRedisConnectionString(),
            ConnectionMode = ConnectionMode.Pooled,
            MaxPoolSize = 1,
            PoolWaitTimeout = TimeSpan.FromMilliseconds(200)
        };

        using var manager = new PooledRedisConnectionManager(options);
        using var lease1 = manager.Acquire();

        Assert.ThrowsException<TimeoutException>(() =>
        {
            using var lease2 = manager.Acquire();
        });
    }

    [TestMethod]
    public async Task AcquireAsync_WhenLeaseReturned_ShouldProceed()
    {
        var options = new ConnectionOptions
        {
            ConnectionString = RedisTestHelpers.GetRedisConnectionString(),
            ConnectionMode = ConnectionMode.Pooled,
            MaxPoolSize = 1,
            PoolWaitTimeout = TimeSpan.FromSeconds(2)
        };

        using var manager = new PooledRedisConnectionManager(options);

        var lease1 = manager.Acquire();

        var waitingTask = Task.Run(async () =>
        {
            await Task.Delay(300).ConfigureAwait(false);
            lease1.Dispose();
        });

        await using var lease2 = await manager.AcquireAsync().ConfigureAwait(false);
        Assert.IsNotNull(lease2.Multiplexer);

        await waitingTask.ConfigureAwait(false);
    }

    [TestMethod]
    public void DifferentLiveLeases_ShouldNotShareSameMultiplexer_WhenPoolSizeAllows()
    {
        var options = new ConnectionOptions
        {
            ConnectionString = RedisTestHelpers.GetRedisConnectionString(),
            ConnectionMode = ConnectionMode.Pooled,
            MaxPoolSize = 2,
            PoolWaitTimeout = TimeSpan.FromMilliseconds(500)
        };

        using var manager = new PooledRedisConnectionManager(options);

        using var lease1 = manager.Acquire();
        using var lease2 = manager.Acquire();

        Assert.AreNotSame(lease1.Multiplexer, lease2.Multiplexer);
    }
}