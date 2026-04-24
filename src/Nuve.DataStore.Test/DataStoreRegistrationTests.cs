using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
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
    public void AddRedisDataStoreProvider_Keeps_First_Registration_For_Duplicate_Provider_Name()
    {
        var services = new ServiceCollection();
        var builder = services.AddDataStore();

        builder.AddRedisDataStoreProvider("Redis");
        builder.AddRedisDataStoreProvider("Redis");

        var registrationStore = DataStoreServiceCollectionExtensions.GetOrAddRegistrationStore(builder.Services);

        Assert.AreEqual(1, registrationStore.Providers.Count);
    }

    [TestMethod]
    public void AddConnection_Replaces_Duplicate_Connection_Name()
    {
        var services = new ServiceCollection();
        var builder = services
            .AddDataStore()
            .AddRedisDataStoreProvider("Redis");

        builder.AddConnection(
            name: "cache",
            provider: "Redis",
            options: new ConnectionOptions
            {
                ConnectionString = "one"
            },
            rootNamespace: "one");

        builder.AddConnection(
            name: "cache",
            provider: "Redis",
            options: new ConnectionOptions
            {
                ConnectionString = "two"
            },
            rootNamespace: "two");

        var registrationStore = DataStoreServiceCollectionExtensions.GetOrAddRegistrationStore(builder.Services);
        var registration = registrationStore.Connections.Single(x => x.Name == "cache");

        Assert.AreEqual("two", registration.Options.ConnectionString);
        Assert.AreEqual("two", registration.RootNamespace);
    }

    [TestMethod]
    public void InitializeDataStore_Throws_When_Connection_References_Unknown_Provider()
    {
        var services = new ServiceCollection();

        services
            .AddDataStore()
            .AddDefaultConnection(
                provider: "MissingProvider",
                options: new ConnectionOptions
                {
                    ConnectionString = "missing"
                },
                rootNamespace: "test");

        using var serviceProvider = services.BuildServiceProvider();

        var ex = Assert.ThrowsException<InvalidOperationException>(() =>
        {
            serviceProvider.InitializeDataStore();
        });

        StringAssert.Contains(ex.Message, "MissingProvider");
    }

    [TestMethod]
    public void AddConnection_Action_Merges_With_Configuration_Connection_Options()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DataStore:Connections:cache:Provider"] = "Redis",
                ["DataStore:Connections:cache:Serializer"] = "json",
                ["DataStore:Connections:cache:ConnectionString"] = "from-config",
                ["DataStore:Connections:cache:RetryCount"] = "7"
            })
            .Build();

        var services = new ServiceCollection();
        var builder = services
            .AddDataStore(configuration)
            .AddRedisDataStoreProvider("Redis");

        builder.AddConnection(
            name: "cache",
            provider: "Redis",
            configure: options => options.RetryCount = 9);

        var registrationStore = DataStoreServiceCollectionExtensions.GetOrAddRegistrationStore(builder.Services);
        var registration = registrationStore.Connections.Single(x => x.Name == "cache");

        Assert.AreEqual("from-config", registration.Options.ConnectionString);
        Assert.AreEqual(9, registration.Options.RetryCount);
        Assert.AreEqual("json", registration.SerializerName);
    }

    [TestMethod]
    public void Named_Keyed_Configuration_Allows_Partial_Override_From_Later_Provider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DataStore:Connections:Default:IsDefault"] = "true",
                ["DataStore:Connections:Default:Provider"] = "redis",
                ["DataStore:Connections:Default:Serializer"] = "json",
                ["DataStore:Connections:Default:ConnectionString"] = "redis.gordios.local:6379,abortConnect=false",
                ["DataStore:Connections:Default:ConnectionMode"] = "Shared",
                ["DataStore:Connections:Default:RetryCount"] = "5",
                ["DataStore:Connections:Default:MaxPoolSize"] = "8",
                ["DataStore:Connections:Default:PoolWaitTimeout"] = "00:00:02",
                ["DataStore:Connections:Default:BackgroundProbeMinInterval"] = "00:00:05",
                ["DataStore:Connections:Default:HealthCheckTimeout"] = "00:00:02",
                ["DataStore:Connections:Default:SwapDisposeDelay"] = "00:00:05",
                ["DataStore:Connections:Default:RootNamespace"] = "App"
            })
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DataStore:Connections:Default:RootNamespace"] = "Wcf"
            })
            .Build();

        var services = new ServiceCollection();
        var builder = services
            .AddDataStore(configuration)
            .AddRedisDataStoreProvider("redis");

        var registrationStore = DataStoreServiceCollectionExtensions.GetOrAddRegistrationStore(builder.Services);
        var registration = registrationStore.Connections.Single(x => x.Name == DataStoreConstants.DefaultConnectionName);

        Assert.AreEqual("redis", registration.ProviderName);
        Assert.AreEqual("json", registration.SerializerName);
        Assert.AreEqual("redis.gordios.local:6379,abortConnect=false", registration.Options.ConnectionString);
        Assert.AreEqual("Wcf", registration.RootNamespace);
    }
}
