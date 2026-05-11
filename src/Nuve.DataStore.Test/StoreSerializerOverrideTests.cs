using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Nuve.DataStore.Serializer.JsonNet;

namespace Nuve.DataStore.Test;

[TestClass]
public class StoreSerializerOverrideTests
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
    public void KeyValueStore_ShouldUse_CtorSerializerOverride_Sync()
    {
        var rootNamespace = Bootstrap.NewRootNamespace("kv-serializer-sync");

        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(rootNamespace: rootNamespace);
        serviceProvider.InitializeDataStore();

        var serializer = new ContextJsonSerializer();
        var store = new KeyValueStore(serializer: serializer);
        var key = "user:1";
        var expected = new TestPayload { Id = 1, Name = "john" };

        Assert.IsTrue(store.Set(key, expected));
        var actual = store.Get<TestPayload>(key);
        Assert.IsNotNull(actual);
        Assert.AreEqual(expected.Id, actual.Id);
        Assert.AreEqual(expected.Name, actual.Name);

        var provider = (IKeyValueStoreProvider)DataStoreAccess.GetDefaultProvider();
        var raw = provider.Get($"{rootNamespace}:{key}");
        CollectionAssert.AreEqual(ContextJsonSerializer.HeaderPrefix, raw.Take(ContextJsonSerializer.HeaderPrefix.Length).ToArray());
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task KeyValueStore_ShouldUse_CtorSerializerOverride_Async()
    {
        var rootNamespace = Bootstrap.NewRootNamespace("kv-serializer-async");

        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(rootNamespace: rootNamespace);
        await serviceProvider.InitializeDataStoreAsync();

        var serializer = new ContextJsonSerializer();
        var store = new KeyValueStore(serializer: serializer);
        var key = "user:1";
        var expected = new TestPayload { Id = 1, Name = "john" };

        Assert.IsTrue(await store.SetAsync(key, expected));
        var actual = await store.GetAsync<TestPayload>(key);
        Assert.IsNotNull(actual);
        Assert.AreEqual(expected.Id, actual.Id);
        Assert.AreEqual(expected.Name, actual.Name);

        var provider = (IKeyValueStoreProvider)DataStoreAccess.GetDefaultProvider();
        var raw = await provider.GetAsync($"{rootNamespace}:{key}");
        CollectionAssert.AreEqual(ContextJsonSerializer.HeaderPrefix, raw.Take(ContextJsonSerializer.HeaderPrefix.Length).ToArray());
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void DictionaryStore_ShouldUse_CtorSerializerOverride_Sync()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("dictionary-serializer-sync"));
        serviceProvider.InitializeDataStore();

        var serializer = new ContextJsonSerializer();
        var store = new DictionaryStore<TestPayload>("users", serializer: serializer);
        var expected = new TestPayload { Id = 2, Name = "mary" };

        Assert.IsTrue(store.Set("user:1", expected));
        var actual = store.Get("user:1");
        Assert.IsNotNull(actual);
        Assert.AreEqual(expected.Id, actual.Id);
        Assert.AreEqual(expected.Name, actual.Name);

        var provider = (IDictionaryStoreProvider)DataStoreAccess.GetDefaultProvider();
        var raw = provider.Get(store.MasterKey, "user:1");
        CollectionAssert.AreEqual(ContextJsonSerializer.HeaderPrefix, raw.Take(ContextJsonSerializer.HeaderPrefix.Length).ToArray());
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task DictionaryStore_ShouldUse_CtorSerializerOverride_Async()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("dictionary-serializer-async"));
        await serviceProvider.InitializeDataStoreAsync();

        var serializer = new ContextJsonSerializer();
        var store = new DictionaryStore<TestPayload>("users", serializer: serializer);
        var expected = new TestPayload { Id = 2, Name = "mary" };

        Assert.IsTrue(await store.SetAsync("user:1", expected));
        var actual = await store.GetAsync("user:1");
        Assert.IsNotNull(actual);
        Assert.AreEqual(expected.Id, actual.Id);
        Assert.AreEqual(expected.Name, actual.Name);

        var provider = (IDictionaryStoreProvider)DataStoreAccess.GetDefaultProvider();
        var raw = await provider.GetAsync(store.MasterKey, "user:1");
        CollectionAssert.AreEqual(ContextJsonSerializer.HeaderPrefix, raw.Take(ContextJsonSerializer.HeaderPrefix.Length).ToArray());
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void HashStore_ShouldUse_CtorSerializerOverride_Sync()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("hash-serializer-sync"));
        serviceProvider.InitializeDataStore();

        var serializer = new ContextJsonSerializer();
        var store = new HashStore("users", serializer: serializer);
        var expected = new TestPayload { Id = 3, Name = "alice" };

        Assert.IsTrue(store.Set("user:1", expected));
        var actual = store.Get<TestPayload>("user:1");
        Assert.IsNotNull(actual);
        Assert.AreEqual(expected.Id, actual.Id);
        Assert.AreEqual(expected.Name, actual.Name);

        var provider = (IDictionaryStoreProvider)DataStoreAccess.GetDefaultProvider();
        var raw = provider.Get(store.MasterKey, "user:1");
        CollectionAssert.AreEqual(ContextJsonSerializer.HeaderPrefix, raw.Take(ContextJsonSerializer.HeaderPrefix.Length).ToArray());
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task HashStore_ShouldUse_CtorSerializerOverride_Async()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("hash-serializer-async"));
        await serviceProvider.InitializeDataStoreAsync();

        var serializer = new ContextJsonSerializer();
        var store = new HashStore("users", serializer: serializer);
        var expected = new TestPayload { Id = 3, Name = "alice" };

        Assert.IsTrue(await store.SetAsync("user:1", expected));
        var actual = await store.GetAsync<TestPayload>("user:1");
        Assert.IsNotNull(actual);
        Assert.AreEqual(expected.Id, actual.Id);
        Assert.AreEqual(expected.Name, actual.Name);

        var provider = (IDictionaryStoreProvider)DataStoreAccess.GetDefaultProvider();
        var raw = await provider.GetAsync(store.MasterKey, "user:1");
        CollectionAssert.AreEqual(ContextJsonSerializer.HeaderPrefix, raw.Take(ContextJsonSerializer.HeaderPrefix.Length).ToArray());
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void HashSetStore_ShouldUse_CtorSerializerOverride_Sync()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("hashset-serializer-sync"));
        serviceProvider.InitializeDataStore();

        var serializer = new ContextJsonSerializer();
        var store = new HashSetStore<TestPayload>("tags", serializer: serializer);
        var expected = new TestPayload { Id = 4, Name = "redis" };

        Assert.AreEqual(1, store.Add(expected));
        Assert.IsTrue(store.Contains(expected));

        var provider = (IHashSetStoreProvider)DataStoreAccess.GetDefaultProvider();
        var rawValues = provider.GetHashSet(store.MasterKey).ToArray();
        Assert.AreEqual(1, rawValues.Length);
        CollectionAssert.AreEqual(ContextJsonSerializer.HeaderPrefix, rawValues[0].Take(ContextJsonSerializer.HeaderPrefix.Length).ToArray());
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task HashSetStore_ShouldUse_CtorSerializerOverride_Async()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("hashset-serializer-async"));
        await serviceProvider.InitializeDataStoreAsync();

        var serializer = new ContextJsonSerializer();
        var store = new HashSetStore<TestPayload>("tags", serializer: serializer);
        var expected = new TestPayload { Id = 4, Name = "redis" };

        Assert.AreEqual(1, await store.AddAsync(expected));
        Assert.IsTrue(await store.ContainsAsync(expected));

        var provider = (IHashSetStoreProvider)DataStoreAccess.GetDefaultProvider();
        var rawValues = (await provider.GetHashSetAsync(store.MasterKey)).ToArray();
        Assert.AreEqual(1, rawValues.Length);
        CollectionAssert.AreEqual(ContextJsonSerializer.HeaderPrefix, rawValues[0].Take(ContextJsonSerializer.HeaderPrefix.Length).ToArray());
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void LinkedListStore_ShouldUse_CtorSerializerOverride_Sync()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("linkedlist-serializer-sync"));
        serviceProvider.InitializeDataStore();

        var serializer = new ContextJsonSerializer();
        var store = new LinkedListStore<TestPayload>("queue", serializer: serializer);
        var expected = new TestPayload { Id = 5, Name = "job1" };

        Assert.AreEqual(1, store.AddLast(expected));
        var actual = store.Get(0);
        Assert.IsNotNull(actual);
        Assert.AreEqual(expected.Id, actual.Id);
        Assert.AreEqual(expected.Name, actual.Name);

        var provider = (ILinkedListStoreProvider)DataStoreAccess.GetDefaultProvider();
        var raw = provider.Get(store.MasterKey, 0);
        CollectionAssert.AreEqual(ContextJsonSerializer.HeaderPrefix, raw.Take(ContextJsonSerializer.HeaderPrefix.Length).ToArray());
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task LinkedListStore_ShouldUse_CtorSerializerOverride_Async()
    {
        using var serviceProvider = Bootstrap.BuildRedisServiceProvider(
            rootNamespace: Bootstrap.NewRootNamespace("linkedlist-serializer-async"));
        await serviceProvider.InitializeDataStoreAsync();

        var serializer = new ContextJsonSerializer();
        var store = new LinkedListStore<TestPayload>("queue", serializer: serializer);
        var expected = new TestPayload { Id = 5, Name = "job1" };

        Assert.AreEqual(1, await store.AddLastAsync(expected));
        var actual = await store.GetAsync(0);
        Assert.IsNotNull(actual);
        Assert.AreEqual(expected.Id, actual.Id);
        Assert.AreEqual(expected.Name, actual.Name);

        var provider = (ILinkedListStoreProvider)DataStoreAccess.GetDefaultProvider();
        var raw = await provider.GetAsync(store.MasterKey, 0);
        CollectionAssert.AreEqual(ContextJsonSerializer.HeaderPrefix, raw.Take(ContextJsonSerializer.HeaderPrefix.Length).ToArray());
    }

    private sealed class TestPayload
    {
        public int Id { get; set; }

        public string? Name { get; set; }
    }

    public class ContextJsonSerializer : IDataStoreSerializer
    {
        private static readonly byte[] VersionBytes = Encoding.ASCII.GetBytes("_#1");
        private const byte UncompressedFlag = (byte)'U';

        public static readonly byte[] HeaderPrefix = [.. VersionBytes, UncompressedFlag];

        public byte[] Serialize<TObject>(TObject? obj)
        {
            return Pack(SerializeJson(obj));
        }

        public TObject? Deserialize<TObject>(byte[]? byteArray)
        {
            var json = Unpack(byteArray);
            if (string.IsNullOrEmpty(json))
                return default;

            return JsonConvert.DeserializeObject<TObject>(json, CreateSerializerSettings());
        }

        public byte[] Serialize(object? objectToSerialize, Type type)
        {
            return Pack(SerializeJson(objectToSerialize));
        }

        public object? Deserialize(byte[]? byteArray, Type type)
        {
            var json = Unpack(byteArray);
            if (string.IsNullOrEmpty(json))
                return default;

            return JsonConvert.DeserializeObject(json, type, CreateSerializerSettings());
        }

        private static byte[] SerializeJson(object? value)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value, CreateSerializerSettings()));
        }

        private static JsonSerializerSettings CreateSerializerSettings()
        {
            return new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                ObjectCreationHandling = ObjectCreationHandling.Replace,
                ContractResolver = new NoConstructorCreationContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            };
        }

        private static byte[] Pack(byte[] payload)
        {
            var output = new byte[HeaderPrefix.Length + payload.Length];
            Buffer.BlockCopy(HeaderPrefix, 0, output, 0, HeaderPrefix.Length);
            Buffer.BlockCopy(payload, 0, output, HeaderPrefix.Length, payload.Length);
            return output;
        }

        private static string? Unpack(byte[]? payload)
        {
            if (payload == null || payload.Length == 0)
                return default;

            if (payload.Length >= HeaderPrefix.Length &&
                payload.Take(HeaderPrefix.Length).SequenceEqual(HeaderPrefix))
            {
                return Encoding.UTF8.GetString(payload, HeaderPrefix.Length, payload.Length - HeaderPrefix.Length);
            }

            return Encoding.UTF8.GetString(payload);
        }
    }
}
