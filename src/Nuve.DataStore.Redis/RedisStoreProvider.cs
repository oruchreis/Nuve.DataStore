using Nuve.DataStore.Internal;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

#if TEST
[assembly: InternalsVisibleTo("Nuve.DataStore.Test")]
#endif

namespace Nuve.DataStore.Redis;

/// <summary>
/// Provides a Redis-based implementation of the <see cref="IDataStoreProvider"/> interface,
/// supporting key-value, hash, set, sorted set, and list operations with connection pooling,
/// retry logic, and distributed locking capabilities.
/// 
/// <para>
/// <b>Features:</b>
/// <list type="bullet">
///   <item>Manages Redis connections using a pool per connection string for efficient reuse.</item>
///   <item>Implements synchronous and asynchronous Redis command execution with automatic retries on transient errors.</item>
///   <item>Supports distributed locking mechanisms for concurrency control using <see cref="RedisDataStoreLock"/>.</item>
///   <item>Provides methods for key type detection, expiration management, and key removal.</item>
///   <item>Integrates with <see cref="IDataStoreProfiler"/> for optional profiling support.</item>
/// </list>
/// </para>
/// 
/// <para>
/// <b>Thread Safety:</b> This class is thread-safe and designed for concurrent use across multiple threads.
/// </para>
/// 
/// <para>
/// <b>Usage:</b> Call <see cref="Initialize"/> or <see cref="InitializeAsync"/> before using any data store operations.
/// </para>
/// </summary>
public partial class RedisStoreProvider : IDataStoreProvider
{
    private ConnectionOptions? _options;
    private IRedisConnectionManager? _connectionManager;
    private string? _connectionString;
    private RedisServerCapabilities? _capabilities;

    /// <summary>
    /// Capabilities of the connected server. Reports <see cref="RedisServerCapabilities.Unknown"/>
    /// while the server has not answered yet, so the portable command path is used until it does.
    /// </summary>
    public RedisServerCapabilities Capabilities =>
        Volatile.Read(ref _capabilities) ?? RedisServerCapabilities.Unknown;

#if TEST
    /// <summary>
    /// Replaces the detected capabilities so a topology that is not available locally,
    /// such as one that refuses cross-slot commands, can still be exercised by tests.
    /// </summary>
    internal void OverrideCapabilitiesForTests(bool crossSlotMultiKey)
    {
        var detected = Capabilities;
        Volatile.Write(ref _capabilities, new RedisServerCapabilities(
            detected.ServerVersion,
            detected.ServerType,
            detected.SupportsScripting,
            detected.SupportsSetKeepTtl,
            crossSlotMultiKey));
    }
#endif

    private RedisServerCapabilities EnsureCapabilities(IConnectionMultiplexer multiplexer)
    {
        var cached = Volatile.Read(ref _capabilities);
        if (cached != null)
            return cached;

        var resolved = TryResolveCapabilities(multiplexer);
        if (resolved == null)
            return RedisServerCapabilities.Unknown;

        Volatile.Write(ref _capabilities, resolved);
        return resolved;
    }

    private static RedisServerCapabilities? TryResolveCapabilities(IConnectionMultiplexer multiplexer)
    {
        foreach (var endpoint in multiplexer.GetEndPoints())
        {
            try
            {
                var server = multiplexer.GetServer(endpoint);
                if (!server.IsConnected)
                    continue;

                var features = server.Features;
                return new RedisServerCapabilities(
                    server.Version,
                    server.ServerType,
                    features.Scripting,
                    features.SetKeepTtl,
                    ProbeCrossSlotMultiKey(multiplexer));
            }
            catch (Exception)
            {
                // An endpoint that cannot answer must not stop the remaining ones from being asked.
            }
        }

        return null;
    }

    /// <summary>
    /// Hash tags chosen so the two keys land on different slots, which is what makes this a
    /// cross-slot write. <c>ProbeKeysShouldBeOnDifferentSlots</c> guards that property.
    /// </summary>
    internal static readonly RedisKey CrossSlotProbeFirstKey = "{nuve-datastore-probe-a}:__crossslot__";
    internal static readonly RedisKey CrossSlotProbeSecondKey = "{nuve-datastore-probe-b}:__crossslot__";

    /// <summary>
    /// Measures whether one write may span several slots by attempting exactly that. Deriving it from
    /// the endpoint is not enough, so the server is asked to perform the operation in question and the
    /// refusal is taken as the answer. A refusal for any other reason is equally safe to act on,
    /// because grouping keys per slot works on every deployment.
    /// </summary>
    private static bool ProbeCrossSlotMultiKey(IConnectionMultiplexer multiplexer)
    {
        var database = multiplexer.GetDatabase();

        try
        {
            return database.StringSet(
            [
                new(CrossSlotProbeFirstKey, RedisValue.EmptyString),
                new(CrossSlotProbeSecondKey, RedisValue.EmptyString)
            ]);
        }
        catch (Exception)
        {
            return false;
        }
        finally
        {
            // Deleted one by one, since a multi-key delete would hit the same restriction being probed.
            TryDeleteProbeKey(database, CrossSlotProbeFirstKey);
            TryDeleteProbeKey(database, CrossSlotProbeSecondKey);
        }
    }

    private static void TryDeleteProbeKey(IDatabase database, RedisKey key)
    {
        try
        {
            database.KeyDelete(key);
        }
        catch (Exception)
        {
            // intentionally supressed
        }
    }

    /// <summary>
    /// Fails with a message naming the offending keys when the server refuses a command that spans
    /// slots. Unlike a plain multi-key read or write, a set combination cannot be split per slot,
    /// so the caller has to place the keys together on purpose.
    /// </summary>
    private void EnsureKeysShareOneSlot(IDatabase database, string operation, params string[] keys)
    {
        if (keys.Length <= 1 || EnsureCapabilities(database.Multiplexer).SupportsCrossSlotMultiKey)
            return;

        var placements = keys.Distinct().Select(key => new { Key = key, Slot = RedisHashSlot.Of(key) }).ToArray();
        var slotCount = placements.Select(placement => placement.Slot).Distinct().Count();
        if (slotCount <= 1)
            return;

        var detail = string.Join(", ", placements.Select(placement => $"'{placement.Key}' is on slot {placement.Slot}"));

        throw new InvalidOperationException(
            $"{operation} requires every key to be on a single cluster slot, but the given keys are spread over {slotCount} slots: {detail}. " +
            "This server rejects a command that spans slots, and a set combination cannot be split per slot the way a plain " +
            "multi-key read or write can. Store the keys together by giving them a shared hash tag, " +
            "for example '{tenant-1}:orders' and '{tenant-1}:orders-archive' both live on the slot of 'tenant-1'.");
    }

    internal T RedisCall<T>(Func<IDatabase, T> callFunction, int retryCount = 0, List<Exception>? exceptions = null)
    {
        if (_connectionManager == null || _options == null)
            throw new InvalidOperationException("The provider hasn't initialized yet.");

        IRedisConnectionLease? lease = null;
        try
        {
            lease = _connectionManager.Acquire();
            return callFunction(lease.Multiplexer.GetDatabase())!;
        }
        catch (Exception e) when (
            e is RedisTimeoutException ||
            e is TimeoutException)
        {
            if (lease != null)
                _connectionManager.ReportTimeout(lease.Multiplexer, e);

            exceptions ??= [];
            exceptions.Add(e);

            if (retryCount < _options.RetryCount)
                return RedisCall(callFunction, retryCount + 1, exceptions);

            throw new AggregateException("Retry limit exceeded.", exceptions);
        }
        catch (Exception e) when (
            e is RedisConnectionException ||
            e is SocketException)
        {
            if (lease != null)
                _connectionManager.ReportConnectionFailure(lease.Multiplexer, e);

            exceptions ??= [];
            exceptions.Add(e);

            if (retryCount < _options.RetryCount)
                return RedisCall(callFunction, retryCount + 1, exceptions);

            throw new AggregateException("Retry limit exceeded.", exceptions);
        }
        finally
        {
            lease?.Dispose();
        }
    }

    internal void RedisCall(Action<IDatabase> callFunction, int retryCount = 0, List<Exception>? exceptions = null)
    {
        if (_connectionManager == null || _options == null)
            throw new InvalidOperationException("The provider hasn't initialized yet.");

        IRedisConnectionLease? lease = null;
        try
        {
            lease = _connectionManager.Acquire();
            callFunction(lease.Multiplexer.GetDatabase());
        }
        catch (Exception e) when (
            e is RedisTimeoutException ||
            e is TimeoutException)
        {
            if (lease != null)
                _connectionManager.ReportTimeout(lease.Multiplexer, e);

            exceptions ??= [];
            exceptions.Add(e);

            if (retryCount < _options.RetryCount)
                RedisCall(callFunction, retryCount + 1, exceptions);
            else
                throw new AggregateException("Retry limit exceeded.", exceptions);
        }
        catch (Exception e) when (
            e is RedisConnectionException ||
            e is SocketException)
        {
            if (lease != null)
                _connectionManager.ReportConnectionFailure(lease.Multiplexer, e);

            exceptions ??= [];
            exceptions.Add(e);

            if (retryCount < _options.RetryCount)
                RedisCall(callFunction, retryCount + 1, exceptions);
            else
                throw new AggregateException("Retry limit exceeded.", exceptions);
        }
        finally
        {
            lease?.Dispose();
        }
    }

    internal async Task<T> RedisCallAsync<T>(Func<IDatabase, Task<T>> callFunctionAsync, int retryCount = 0, List<Exception>? exceptions = null)
    {
        if (_connectionManager == null || _options == null)
            throw new InvalidOperationException("The provider hasn't initialized yet.");

        IRedisConnectionLease? lease = null;
        try
        {
            lease = await _connectionManager.AcquireAsync().ConfigureAwait(false);
            return (await callFunctionAsync(lease.Multiplexer.GetDatabase()).ConfigureAwait(false))!;
        }
        catch (Exception e) when (
            e is RedisTimeoutException ||
            e is TimeoutException)
        {
            if (lease != null)
                _connectionManager.ReportTimeout(lease.Multiplexer, e);

            exceptions ??= [];
            exceptions.Add(e);

            if (retryCount < _options.RetryCount)
                return await RedisCallAsync(callFunctionAsync, retryCount + 1, exceptions).ConfigureAwait(false);

            throw new AggregateException("Retry limit exceeded.", exceptions);
        }
        catch (Exception e) when (
            e is RedisConnectionException ||
            e is SocketException)
        {
            if (lease != null)
                _connectionManager.ReportConnectionFailure(lease.Multiplexer, e);

            exceptions ??= [];
            exceptions.Add(e);

            if (retryCount < _options.RetryCount)
                return await RedisCallAsync(callFunctionAsync, retryCount + 1, exceptions).ConfigureAwait(false);

            throw new AggregateException("Retry limit exceeded.", exceptions);
        }
        finally
        {
            if (lease != null)
                await lease.DisposeAsync().ConfigureAwait(false);
        }
    }

    internal async Task RedisCallAsync(Func<IDatabase, Task> callFunctionAsync, int retryCount = 0, List<Exception>? exceptions = null)
    {
        if (_connectionManager == null || _options == null)
            throw new InvalidOperationException("The provider hasn't initialized yet.");

        IRedisConnectionLease? lease = null;
        try
        {
            lease = await _connectionManager.AcquireAsync().ConfigureAwait(false);
            await callFunctionAsync(lease.Multiplexer.GetDatabase()).ConfigureAwait(false);
        }
        catch (Exception e) when (
            e is RedisTimeoutException ||
            e is TimeoutException)
        {
            if (lease != null)
                _connectionManager.ReportTimeout(lease.Multiplexer, e);

            exceptions ??= [];
            exceptions.Add(e);

            if (retryCount < _options.RetryCount)
                await RedisCallAsync(callFunctionAsync, retryCount + 1, exceptions).ConfigureAwait(false);
            else
                throw new AggregateException("Retry limit exceeded.", exceptions);
        }
        catch (Exception e) when (
            e is RedisConnectionException ||
            e is SocketException)
        {
            if (lease != null)
                _connectionManager.ReportConnectionFailure(lease.Multiplexer, e);

            exceptions ??= [];
            exceptions.Add(e);

            if (retryCount < _options.RetryCount)
                await RedisCallAsync(callFunctionAsync, retryCount + 1, exceptions).ConfigureAwait(false);
            else
                throw new AggregateException("Retry limit exceeded.", exceptions);
        }
        finally
        {
            if (lease != null)
                await lease.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Queues the expiration command that accompanies a write inside the same transaction.
    /// When <paramref name="onlyWhenMissing"/> is set the expiration must leave an already expiring
    /// key untouched, which servers before Redis 7.0 can only express through a script.
    /// </summary>
    private Task ApplyExpire(IDatabase database, ITransaction transaction, RedisKey key, TimeSpan expire, bool onlyWhenMissing)
    {
        if (!onlyWhenMissing)
            return transaction.KeyExpireAsync(key, expire);

        if (EnsureCapabilities(database.Multiplexer).SupportsExpireOptions)
            return transaction.KeyExpireAsync(key, expire, ExpireWhen.HasNoExpiry);

        return transaction.ScriptEvaluateAsync(LuaCommands.ExpireWhenMissing, [key], [(long)expire.TotalMilliseconds]);
    }

    internal T ExecuteTransaction<T>(IDatabase database, Func<ITransaction, Task<T>> actionFactory, RedisKey key, TimeSpan? expire, bool onlyWhenMissing = false)
    {
        if (expire == null)
            throw new ArgumentNullException(nameof(expire));

        var transaction = database.CreateTransaction();
        var actionTask = actionFactory(transaction);
        var expireTask = ApplyExpire(database, transaction, key, expire.Value, onlyWhenMissing);

        if (!transaction.Execute())
            throw new InvalidOperationException("Redis transaction could not be executed.");

        var actionResult = actionTask.GetAwaiter().GetResult();
        expireTask.GetAwaiter().GetResult();
        return actionResult;
    }

    internal void ExecuteTransaction(IDatabase database, Func<ITransaction, Task> actionFactory, RedisKey key, TimeSpan? expire, bool onlyWhenMissing = false)
    {
        if (expire == null)
            throw new ArgumentNullException(nameof(expire));

        var transaction = database.CreateTransaction();
        var actionTask = actionFactory(transaction);
        var expireTask = ApplyExpire(database, transaction, key, expire.Value, onlyWhenMissing);

        if (!transaction.Execute())
            throw new InvalidOperationException("Redis transaction could not be executed.");

        actionTask.GetAwaiter().GetResult();
        expireTask.GetAwaiter().GetResult();
    }

    internal async Task<T> ExecuteTransactionAsync<T>(IDatabase database, Func<ITransaction, Task<T>> actionFactory, RedisKey key, TimeSpan? expire, bool onlyWhenMissing = false)
    {
        if (expire == null)
            throw new ArgumentNullException(nameof(expire));

        var transaction = database.CreateTransaction();
        var actionTask = actionFactory(transaction);
        var expireTask = ApplyExpire(database, transaction, key, expire.Value, onlyWhenMissing);

        if (!await transaction.ExecuteAsync().ConfigureAwait(false))
            throw new InvalidOperationException("Redis transaction could not be executed.");

        var actionResult = await actionTask.ConfigureAwait(false);
        await expireTask.ConfigureAwait(false);
        return actionResult;
    }

    internal async Task ExecuteTransactionAsync(IDatabase database, Func<ITransaction, Task> actionFactory, RedisKey key, TimeSpan? expire, bool onlyWhenMissing = false)
    {
        if (expire == null)
            throw new ArgumentNullException(nameof(expire));

        var transaction = database.CreateTransaction();
        var actionTask = actionFactory(transaction);
        var expireTask = ApplyExpire(database, transaction, key, expire.Value, onlyWhenMissing);

        if (!await transaction.ExecuteAsync().ConfigureAwait(false))
            throw new InvalidOperationException("Redis transaction could not be executed.");

        await actionTask.ConfigureAwait(false);
        await expireTask.ConfigureAwait(false);
    }

    /// <summary>
    /// Backward-compatible initialize overload.
    /// Defaults to shared mode.
    /// </summary>
    public virtual void Initialize(string connectionString, ConnectionMode connectionMode, IDataStoreProfiler? profiler)
    {
        Initialize(new ConnectionOptions
        {
            ConnectionString = connectionString,
            ConnectionMode = connectionMode
        }, profiler);
    }

    public virtual Task InitializeAsync(string connectionString, ConnectionMode connectionMode, IDataStoreProfiler? profiler)
    {
        Initialize(connectionString, connectionMode, profiler);
        return Task.CompletedTask;
    }

    public virtual void Initialize(ConnectionOptions options, IDataStoreProfiler? profiler)
    {
        ThrowHelper.ThrowIfNull(options);
        ThrowHelper.ThrowIfNullOrWhiteSpace(options.ConnectionString);          

        _options = options;
        _connectionString = options.ConnectionString;

        _connectionManager?.Dispose();

        _capabilities = null;
        _connectionManager = options.ConnectionMode switch
        {
            ConnectionMode.Shared => new SharedRedisConnectionManager(options),
            ConnectionMode.Pooled => new PooledRedisConnectionManager(options),
            _ => throw new ArgumentOutOfRangeException(nameof(options.ConnectionMode))
        };

        ResolveCapabilitiesFromConnection();
    }

    /// <summary>
    /// Asks the server for its capabilities during startup so the first operation does not pay for it.
    /// A server that is briefly unavailable must not fail startup, so the capabilities stay unresolved
    /// and are asked for again on first use.
    /// </summary>
    private void ResolveCapabilitiesFromConnection()
    {
        IRedisConnectionLease? lease = null;
        try
        {
            lease = _connectionManager!.Acquire();
            EnsureCapabilities(lease.Multiplexer);
        }
        catch (Exception)
        {
            // intentionally supressed
        }
        finally
        {
            lease?.Dispose();
        }
    }

    public virtual Task InitializeAsync(ConnectionOptions options, IDataStoreProfiler? profiler)
    {
        Initialize(options, profiler);
        return Task.CompletedTask;
    }

    StoreKeyType IDataStoreProvider.GetKeyType(string key)
    {
        return RedisCall(db =>
        {
            var redisType = db.KeyType(key);
            return redisType switch
            {
                RedisType.List => StoreKeyType.LinkedList,
                RedisType.Hash => StoreKeyType.Dictionary,
                RedisType.SortedSet => StoreKeyType.SortedSet,
                RedisType.Set => StoreKeyType.HashSet,
                _ => StoreKeyType.KeyValue
            };
        });
    }

    async Task<StoreKeyType> IDataStoreProvider.GetKeyTypeAsync(string key)
    {
        return await RedisCallAsync(async db =>
        {
            var redisType = await db.KeyTypeAsync(key).ConfigureAwait(false);
            return redisType switch
            {
                RedisType.List => StoreKeyType.LinkedList,
                RedisType.Hash => StoreKeyType.Dictionary,
                RedisType.SortedSet => StoreKeyType.SortedSet,
                RedisType.Set => StoreKeyType.HashSet,
                _ => StoreKeyType.KeyValue
            };
        }).ConfigureAwait(false);
    }

    TimeSpan? IDataStoreProvider.GetExpire(string key)
    {
        return RedisCall(db => db.KeyTimeToLive(key));
    }

    async Task<TimeSpan?> IDataStoreProvider.GetExpireAsync(string key)
    {
        return await RedisCallAsync(async db => await db.KeyTimeToLiveAsync(key).ConfigureAwait(false)).ConfigureAwait(false);
    }

    bool IDataStoreProvider.SetExpire(string key, TimeSpan expire, bool onlyWhenMissing)
    {
        if (!onlyWhenMissing)
            return RedisCall(db => db.KeyExpire(key, expire));

        return RedisCall(db => EnsureCapabilities(db.Multiplexer).SupportsExpireOptions
            ? db.KeyExpire(key, expire, ExpireWhen.HasNoExpiry)
            : AsExpireApplied(db.ScriptEvaluate(LuaCommands.ExpireWhenMissing, [key], [(long)expire.TotalMilliseconds])));
    }

    async Task<bool> IDataStoreProvider.SetExpireAsync(string key, TimeSpan expire, bool onlyWhenMissing)
    {
        if (!onlyWhenMissing)
            return await RedisCallAsync(async db => await db.KeyExpireAsync(key, expire).ConfigureAwait(false)).ConfigureAwait(false);

        return await RedisCallAsync(async db =>
        {
            if (EnsureCapabilities(db.Multiplexer).SupportsExpireOptions)
                return await db.KeyExpireAsync(key, expire, ExpireWhen.HasNoExpiry).ConfigureAwait(false);

            return AsExpireApplied(
                await db.ScriptEvaluateAsync(LuaCommands.ExpireWhenMissing, [key], [(long)expire.TotalMilliseconds])
                    .ConfigureAwait(false));
        }).ConfigureAwait(false);
    }

    private static bool AsExpireApplied(RedisResult result)
    {
        return !result.IsNull && (long)result == 1;
    }

    bool IDataStoreProvider.Remove(string key)
    {
        return RedisCall(db => db.KeyDelete(key));
    }

    async Task<bool> IDataStoreProvider.RemoveAsync(string key)
    {
        return await RedisCallAsync(async db => await db.KeyDeleteAsync(key).ConfigureAwait(false)).ConfigureAwait(false);
    }

    void IDataStoreProvider.Lock(string lockKey, TimeSpan waitTimeout, Action action, TimeSpan slidingExpire, bool skipWhenTimeout, bool throwWhenTimeout, string? fencingKey, TimeSpan? fencingExpire)
    {
        using var cts = new CancellationTokenSource(waitTimeout);
        if (TryAcquireLock(lockKey, slidingExpire, throwWhenTimeout, cts.Token, out var lockItem, fencingKey, fencingExpire))
        {
            using (lockItem)
            {
                action();
            }
        }
        else
        {
            if (!skipWhenTimeout)
                action();
        }
    }

    DataStoreLock? IDataStoreProvider.AcquireLock(string lockKey, CancellationToken waitCancelToken, TimeSpan slidingExpire, bool throwWhenTimeout, string? fencingKey, TimeSpan? fencingExpire)
    {
        TryAcquireLock(lockKey, slidingExpire, throwWhenTimeout, waitCancelToken, out var lockItem, fencingKey, fencingExpire);
        return lockItem;
    }

    async Task IDataStoreProvider.LockAsync(string lockKey, TimeSpan waitTimeout, Func<Task> action, TimeSpan slidingExpire, bool skipWhenTimeout, bool throwWhenTimeout, string? fencingKey, TimeSpan? fencingExpire)
    {
        using var cts = new CancellationTokenSource(waitTimeout);
        var lockItem = await TryAcquireLockAsync(lockKey, slidingExpire, throwWhenTimeout, cts.Token, fencingKey, fencingExpire).ConfigureAwait(false);
        if (lockItem != null)
        {
            using (lockItem)
            {
                await action().ConfigureAwait(false);
            }
        }
        else
        {
            if (!skipWhenTimeout)
                await action().ConfigureAwait(false);
        }
    }

    async Task<DataStoreLock?> IDataStoreProvider.AcquireLockAsync(string lockKey, CancellationToken waitCancelToken, TimeSpan slidingExpire, bool throwWhenTimeout, string? fencingKey, TimeSpan? fencingExpire)
    {
        return await TryAcquireLockAsync(lockKey, slidingExpire, throwWhenTimeout, waitCancelToken, fencingKey, fencingExpire).ConfigureAwait(false);
    }

    private bool TryAcquireLock(string key, TimeSpan slidingExpire, bool throwWhenTimeout, CancellationToken waitCancelToken, out RedisDataStoreLock? locker, string? fencingKey, TimeSpan? fencingExpire)
    {
        locker = new RedisDataStoreLock(this, key, slidingExpire, throwWhenTimeout, waitCancelToken);
        try
        {
            if (!locker.TryAcquireLock())
            {
                locker.Dispose();
                locker = null;
                return false;
            }

            try
            {
                var fencingToken = IncrementFencingToken(fencingKey ?? $"{key}:__fencing__", fencingExpire);
                locker.SetFencingToken(fencingToken);
            }
            catch
            {
                locker.Release();
                throw;
            }
        }
        catch
        {
            locker?.Dispose();
            locker = null;
            return false;
        }

        return true;
    }

    private async Task<RedisDataStoreLock?> TryAcquireLockAsync(string key, TimeSpan slidingExpire, bool throwWhenTimeout, CancellationToken waitCancelToken, string? fencingKey, TimeSpan? fencingExpire)
    {
        var locker = new RedisDataStoreLock(this, key, slidingExpire, throwWhenTimeout, waitCancelToken);
        try
        {
            if (!await locker.TryAcquireLockAsync().ConfigureAwait(false))
            {
                locker.Dispose();
                return null;
            }

            try
            {
                var fencingToken = await IncrementFencingTokenAsync(fencingKey ?? $"{key}:__fencing__", fencingExpire).ConfigureAwait(false);
                locker.SetFencingToken(fencingToken);
            }
            catch
            {
                await locker.ReleaseAsync().ConfigureAwait(false);
                throw;
            }
        }
        catch
        {
            locker?.Dispose();
            return null;
        }

        return locker;
    }

    private long IncrementFencingToken(string key, TimeSpan? expire)
    {
        long fencingToken = 0;

        if (expire == null)
        {
            RedisCall(redis =>
            {
                fencingToken = redis.StringIncrement(key);
            });
        }
        else
        {
            RedisCall(redis =>
            {
                fencingToken = ExecuteTransaction(redis, tx => tx.StringIncrementAsync(key), key, expire);
            });
        }

        return fencingToken;
    }

    private async Task<long> IncrementFencingTokenAsync(string key, TimeSpan? expire)
    {
        long fencingToken = 0;

        if (expire == null)
        {
            await RedisCallAsync(async redis =>
            {
                fencingToken = await redis.StringIncrementAsync(key).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }
        else
        {
            await RedisCallAsync(async redis =>
            {
                fencingToken = await ExecuteTransactionAsync(redis, tx => tx.StringIncrementAsync(key), key, expire).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        return fencingToken;
    }
}
