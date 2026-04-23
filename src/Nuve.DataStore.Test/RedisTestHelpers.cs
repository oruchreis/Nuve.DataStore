using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nuve.DataStore.Redis;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Nuve.DataStore.Test;

internal static class RedisTestHelpers
{
    public static string GetRedisConnectionString()
    {
        return Environment.GetEnvironmentVariable("REDIS_TEST_CONNECTION")
               ?? "localhost:6379,abortConnect=false";
    }

    public static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout, TimeSpan? pollInterval = null)
    {
        var start = DateTime.UtcNow;
        var delay = pollInterval ?? TimeSpan.FromMilliseconds(100);

        while (DateTime.UtcNow - start < timeout)
        {
            if (condition())
                return;

            await Task.Delay(delay).ConfigureAwait(false);
        }

        Assert.Fail("Condition was not satisfied within timeout.");
    }

    public static T GetPrivateField<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Field '{fieldName}' not found on type '{instance.GetType().FullName}'.");
        return (T)field!.GetValue(instance)!;
    }

    public static T GetPrivateProperty<T>(object instance, string propertyName)
    {
        var prop = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(prop, $"Property '{propertyName}' not found on type '{instance.GetType().FullName}'.");
        return (T)prop!.GetValue(instance)!;
    }

    public static ConnectionMultiplexer GetSharedMultiplexer(object sharedManager)
    {
        return GetPrivateField<ConnectionMultiplexer>(sharedManager, "_shared");
    }

    public static int GetBackgroundProbeScheduled(object sharedManager)
    {
        return GetPrivateField<int>(sharedManager, "_backgroundProbeScheduled");
    }

    public static long GetLastProbeTicks(object sharedManager)
    {
        return GetPrivateField<long>(sharedManager, "_lastProbeTicks");
    }

    public static IRedisConnectionManager GetConnectionManager(RedisStoreProvider provider)
    {
        return GetPrivateField<IRedisConnectionManager>(provider, "_connectionManager");
    }

    public static async Task<IDatabase> CreateDatabaseAsync()
    {
        var mux = await ConnectionMultiplexer.ConnectAsync(GetRedisConnectionString()).ConfigureAwait(false);
        return mux.GetDatabase();
    }
}