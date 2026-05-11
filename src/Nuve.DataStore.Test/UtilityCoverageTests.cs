using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nuve.DataStore.Helpers;
using Nuve.DataStore.Serializer.JsonNet;

namespace Nuve.DataStore.Test;

[TestClass]
public class UtilityCoverageTests
{
    [TestMethod]
    public void TypeHelper_ShouldFormat_Primitives_Generic_And_NestedTypes()
    {
        Assert.AreEqual("int", typeof(int).GetFriendlyName());
        Assert.AreEqual("string", typeof(string).GetFriendlyName());
        Assert.AreEqual("Dictionary<string, int>", typeof(Dictionary<string, int>).GetFriendlyName());
        Assert.AreEqual("UtilityCoverageTests.NestedPayload", typeof(NestedPayload).GetFriendlyName());
    }

    [TestMethod]
    public void ProfilerHelpers_ShouldReturn_ExpectedDefaults()
    {
        var proxy = new InternalProfilerProxy();
        var context = proxy.Begin("method", "key");
#if NET48
        Assert.IsNull(context);
        Assert.IsNull(proxy.GetContext());
#else
        Assert.IsNotNull(context);
        Assert.IsNotNull(proxy.GetContext());
#endif
        proxy.Finish(context, new DataStoreProfileResult
        {
            Method = "M",
            Key = "K",
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow
        });

        var nullProfiler = new NullDataStoreProfiler();
        Assert.IsNull(nullProfiler.Begin("method", "key"));
        Assert.IsNull(nullProfiler.GetContext());
        nullProfiler.Finish(null, []);
    }

    [TestMethod]
    public void BsonSerializer_ShouldRoundTrip_AndHandleNulls()
    {
        var serializer = new BsonNetDataStoreSerializer();
        var payload = new NestedPayload { Id = 9, Name = "nine" };

        var bytes = serializer.Serialize(payload);
        var roundTrip = serializer.Deserialize<NestedPayload>(bytes);
        Assert.IsNotNull(roundTrip);
        Assert.AreEqual(payload.Id, roundTrip.Id);
        Assert.AreEqual(payload.Name, roundTrip.Name);

        var objectBytes = serializer.Serialize((object)payload, typeof(NestedPayload));
        var objectRoundTrip = (NestedPayload?)serializer.Deserialize(objectBytes, typeof(NestedPayload));
        Assert.IsNotNull(objectRoundTrip);
        Assert.AreEqual(payload.Id, objectRoundTrip.Id);
        Assert.AreEqual(payload.Name, objectRoundTrip.Name);

        Assert.AreEqual(0, serializer.Deserialize<int>(null));
        Assert.IsNull(serializer.Deserialize(null, typeof(string)));
    }

    [TestMethod]
    public void DefaultSerializer_ShouldExpose_CurrentReflectionBehavior()
    {
        var serializer = new DefaultSerializer();
        var payload = new NestedPayload { Id = 3, Name = "three" };

        Assert.ThrowsException<System.Reflection.TargetParameterCountException>(() => serializer.Serialize(payload));
        Assert.ThrowsException<System.Reflection.TargetParameterCountException>(() => serializer.Serialize((object)payload, typeof(NestedPayload)));
        Assert.ThrowsException<System.Reflection.TargetParameterCountException>(() => serializer.Deserialize<NestedPayload>(Array.Empty<byte>()));
        Assert.ThrowsException<System.Reflection.TargetParameterCountException>(() => serializer.Deserialize(Array.Empty<byte>(), typeof(NestedPayload)));
    }

    [TestMethod]
    public async Task DataStoreLock_And_ProfileResult_ShouldCover_BaseBehavior()
    {
        var started = DateTime.UtcNow;
        var ended = started.AddSeconds(1);
        var result = new DataStoreProfileResult
        {
            Method = "Set",
            Key = "user:1",
            StartTime = started,
            EndTime = ended
        };

        Assert.AreEqual("Set", result.Method);
        Assert.AreEqual("user:1", result.Key);
        Assert.AreEqual(started, result.StartTime);
        Assert.AreEqual(ended, result.EndTime);

        var lockItem = new FakeDataStoreLock();
        Assert.IsNull(lockItem.OwnerToken);
        Assert.AreEqual(0L, lockItem.FencingToken);

        lockItem.MarkAcquired();
        Assert.IsNotNull(lockItem.LockAchieved);
        Assert.IsTrue(lockItem.Extend());
        Assert.IsTrue(await lockItem.ExtendAsync());
        Assert.IsTrue(await lockItem.GetTtlAsync() > TimeSpan.Zero);
        Assert.IsTrue(lockItem.Release());
        Assert.IsTrue(await lockItem.ReleaseAsync());
        lockItem.Dispose();
#if !NET48
        await lockItem.DisposeAsync();
#endif
        Assert.IsTrue(lockItem.DisposeCalled);
    }

    private sealed class NestedPayload
    {
        public int Id { get; set; }

        public string? Name { get; set; }
    }

    private sealed class FakeDataStoreLock : DataStoreLock
    {
        public bool DisposeCalled { get; private set; }

        public void MarkAcquired()
        {
            LockAchieved = DateTimeOffset.UtcNow;
        }

        public override void Dispose()
        {
            DisposeCalled = true;
        }

#if !NET48
        public override ValueTask DisposeAsync()
        {
            DisposeCalled = true;
            return ValueTask.CompletedTask;
        }
#endif

        public override Task<TimeSpan?> GetTtlAsync()
        {
            return Task.FromResult<TimeSpan?>(TimeSpan.FromSeconds(1));
        }

        public override bool Extend(TimeSpan? expire = null)
        {
            LockAchieved = DateTimeOffset.UtcNow;
            return true;
        }

        public override Task<bool> ExtendAsync(TimeSpan? expire = null)
        {
            LockAchieved = DateTimeOffset.UtcNow;
            return Task.FromResult(true);
        }

        public override bool Release()
        {
            return true;
        }

        public override Task<bool> ReleaseAsync()
        {
            return Task.FromResult(true);
        }
    }
}
