using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Nuve.DataStore.Test;

[TestClass]
public class StoreCrudTests
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
    [TestCategory("Integration")]
    public void KeyValueStore_ShouldPerform_Sync_Crud()
    {
        var rootNamespace = Bootstrap.NewRootNamespace("kv-sync-crud");

        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(rootNamespace: rootNamespace);
        serviceProvider.InitializeDataStore();

        var store = new KeyValueStore();
        var key = "user:1";

        Assert.IsTrue(store.Set(key, "john"));
        Assert.AreEqual("john", store.Get<string>(key));
        Assert.IsTrue(store.Contains(key));
        Assert.AreEqual(1, store.Count($"{rootNamespace}:*"));
        Assert.IsTrue(store.Remove(key));
        Assert.IsFalse(store.Contains(key));
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task KeyValueStore_ShouldPerform_Async_Crud()
    {
        var rootNamespace = Bootstrap.NewRootNamespace("kv-async-crud");

        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(rootNamespace: rootNamespace);
        await serviceProvider.InitializeDataStoreAsync();

        var store = new KeyValueStore();
        var key = "user:1";

        Assert.IsTrue(await store.SetAsync(key, "john"));
        Assert.AreEqual("john", await store.GetAsync<string>(key));
        Assert.IsTrue(await store.ContainsAsync(key));
        Assert.AreEqual(1, await store.CountAsync($"{rootNamespace}:*"));
        Assert.IsTrue(await store.RemoveAsync(key));
        Assert.IsFalse(await store.ContainsAsync(key));
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void DictionaryStore_ShouldPerform_Sync_Crud()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("dictionary-sync-crud"));
        serviceProvider.InitializeDataStore();

        var store = new DictionaryStore<string>("users");

        Assert.IsTrue(store.Set("user:1", "john"));
        Assert.AreEqual("john", store.Get("user:1"));
        Assert.IsTrue(store.ContainsKey("user:1"));
        Assert.AreEqual(1, store.Count());
        Assert.AreEqual(1, store.Remove("user:1"));
        Assert.AreEqual(0, store.Count());
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task DictionaryStore_ShouldPerform_Async_Crud()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("dictionary-async-crud"));
        await serviceProvider.InitializeDataStoreAsync();

        var store = new DictionaryStore<string>("users");

        Assert.IsTrue(await store.SetAsync("user:1", "john"));
        Assert.AreEqual("john", await store.GetAsync("user:1"));
        Assert.IsTrue(await store.ContainsKeyAsync("user:1"));
        Assert.AreEqual(1, await store.CountAsync());
        Assert.AreEqual(1, await store.RemoveAsync("user:1"));
        Assert.AreEqual(0, await store.CountAsync());
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void HashStore_ShouldPerform_Sync_Crud()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("hash-sync-crud"));
        serviceProvider.InitializeDataStore();

        var store = new HashStore("users");

        Assert.IsTrue(store.Set("name", "john"));
        Assert.AreEqual("john", store.Get<string>("name"));
        Assert.IsTrue(store.ContainsKey("name"));
        Assert.AreEqual(1, store.Count());
        Assert.AreEqual(1, store.Remove("name"));
        Assert.AreEqual(0, store.Count());
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task HashStore_ShouldPerform_Async_Crud()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("hash-async-crud"));
        await serviceProvider.InitializeDataStoreAsync();

        var store = new HashStore("users");

        Assert.IsTrue(await store.SetAsync("name", "john"));
        Assert.AreEqual("john", await store.GetAsync<string>("name"));
        Assert.IsTrue(await store.ContainsKeyAsync("name"));
        Assert.AreEqual(1, await store.CountAsync());
        Assert.AreEqual(1, await store.RemoveAsync("name"));
        Assert.AreEqual(0, await store.CountAsync());
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void HashSetStore_ShouldPerform_Sync_Crud()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("hashset-sync-crud"));
        serviceProvider.InitializeDataStore();

        var store = new HashSetStore<string>("tags");

        Assert.AreEqual(2, store.Add("redis", "cache"));
        Assert.IsTrue(store.Contains("redis"));
        Assert.AreEqual(2, store.Count());
        Assert.AreEqual(1, store.Remove("redis"));
        Assert.IsFalse(store.Contains("redis"));
        Assert.AreEqual(1, store.Count());
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task HashSetStore_ShouldPerform_Async_Crud()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("hashset-async-crud"));
        await serviceProvider.InitializeDataStoreAsync();

        var store = new HashSetStore<string>("tags");

        Assert.AreEqual(2, await store.AddAsync("redis", "cache"));
        Assert.IsTrue(await store.ContainsAsync("redis"));
        Assert.AreEqual(2, await store.CountAsync());
        Assert.AreEqual(1, await store.RemoveAsync("redis"));
        Assert.IsFalse(await store.ContainsAsync("redis"));
        Assert.AreEqual(1, await store.CountAsync());
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void LinkedListStore_ShouldPerform_Sync_Crud()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("linkedlist-sync-crud"));
        serviceProvider.InitializeDataStore();

        var store = new LinkedListStore<string>("queue");

        Assert.AreEqual(2, store.AddLast("job1", "job2"));
        Assert.AreEqual("job1", store.Get(0));
        Assert.AreEqual(2, store.Count());
        Assert.AreEqual("job1", store.RemoveFirst());
        Assert.AreEqual(1, store.Count());
        Assert.AreEqual("job2", store.Get(0));
        Assert.IsTrue(store.Clear());
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task LinkedListStore_ShouldPerform_Async_Crud()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("linkedlist-async-crud"));
        await serviceProvider.InitializeDataStoreAsync();

        var store = new LinkedListStore<string>("queue");

        Assert.AreEqual(2, await store.AddLastAsync("job1", "job2"));
        Assert.AreEqual("job1", await store.GetAsync(0));
        Assert.AreEqual(2, await store.CountAsync());
        Assert.AreEqual("job1", await store.RemoveFirstAsync());
        Assert.AreEqual(1, await store.CountAsync());
        Assert.AreEqual("job2", await store.GetAsync(0));
        Assert.IsTrue(await store.ClearAsync());
    }
}
