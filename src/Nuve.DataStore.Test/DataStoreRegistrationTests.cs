using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nuve.DataStore.Redis;

namespace Nuve.DataStore.Test;

[TestClass]
public class DataStoreRegistrationTests
{
    [TestInitialize]
    public void TestInitialize()
    {
        DataStoreRuntime.ResetForTests();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        DataStoreRuntime.ResetForTests();
    }

    [TestMethod]
    public void AddRedisDataStoreProvider_Throws_For_Duplicate_Provider_Name()
    {
        var services = new ServiceCollection();

        var builder = services.AddDataStore();

        builder.AddRedisDataStoreProvider(
            providerName: "Redis",
            options: new ConnectionOptions
            {
                ConnectionString = RedisTestHelpers.GetRedisConnectionString()
            });

        Assert.ThrowsException<InvalidOperationException>(() =>
        {
            builder.AddRedisDataStoreProvider(
                providerName: "Redis",
                options: new ConnectionOptions
                {
                    ConnectionString = RedisTestHelpers.GetRedisConnectionString()
                });
        });
    }

    [TestMethod]
    public void AddConnection_Throws_For_Duplicate_Connection_Name()
    {
        var services = new ServiceCollection();

        var builder = services
            .AddDataStore()
            .AddRedisDataStoreProvider(
                providerName: "Redis",
                options: new ConnectionOptions
                {
                    ConnectionString = RedisTestHelpers.GetRedisConnectionString()
                });

        builder.AddConnection(
            name: "cache",
            provider: "Redis",
            rootNamespace: "one");

        Assert.ThrowsException<InvalidOperationException>(() =>
        {
            builder.AddConnection(
                name: "cache",
                provider: "Redis",
                rootNamespace: "two");
        });
    }

    [TestMethod]
    public void InitializeDataStore_Throws_When_Connection_References_Unknown_Provider()
    {
        var services = new ServiceCollection();

        services
            .AddDataStore()
            .AddDefaultConnection(
                provider: "MissingProvider",
                rootNamespace: "test");

        using var serviceProvider = services.BuildServiceProvider();

        var ex = Assert.ThrowsException<InvalidOperationException>(() =>
        {
            serviceProvider.InitializeDataStore();
        });

        StringAssert.Contains(ex.Message, "MissingProvider");
    }
}