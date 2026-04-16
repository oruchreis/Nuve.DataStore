using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

[assembly: InternalsVisibleTo("Nuve.DataStore.Test")]
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
    private static readonly ConcurrentDictionary<string, ConcurrentQueue<ConnectionMultiplexer>> _connectionPools = new();

    private string? _connectionString;
    private ConcurrentQueue<ConnectionMultiplexer>? _connectionPool;

    internal T RedisCall<T>(Func<IDatabase, T> callFunction, int retryCount = 0, List<Exception>? exceptions = null)
    {
        if (_connectionString == null || _connectionPool == null)
            throw new InvalidOperationException("The provider hasn't initialized yet.");

        ConnectionMultiplexer? conn = null;
        try
        {
            if (!_connectionPool.TryDequeue(out conn))
            {
                conn = ConnectionMultiplexer.Connect(_connectionString);
            }
            return callFunction(conn.GetDatabase())!;
        }
        catch (Exception e) when (
            e is RedisServerException ||
            e is RedisTimeoutException ||
            e is RedisConnectionException
            )
        {
            conn?.Dispose();
            conn = null;

            exceptions ??= [];

            exceptions.Add(e);

            if (retryCount < 5)
                return RedisCall(callFunction, retryCount + 1, exceptions);
            else
                throw new AggregateException($"Retry limit exceeded.", exceptions);
        }
        finally
        {
            if (conn != null)
                _connectionPool.Enqueue(conn);
        }
    }

    internal void RedisCall(Action<IDatabase> callFunction, int retryCount = 0, List<Exception>? exceptions = null)
    {
        if (_connectionString == null || _connectionPool == null)
            throw new InvalidOperationException("The provider hasn't initialized yet.");

        ConnectionMultiplexer? conn = null;
        try
        {
            if (!_connectionPool.TryDequeue(out conn))
            {
                conn = ConnectionMultiplexer.Connect(_connectionString);
            }
            callFunction(conn.GetDatabase());
        }
        catch (Exception e) when (
            e is RedisServerException ||
            e is RedisTimeoutException ||
            e is RedisConnectionException
            )
        {
            if (conn != null)
            {
                conn.Dispose();
                conn = null;
            }

            exceptions ??= new List<Exception>();

            exceptions.Add(e);

            if (retryCount < 5)
                RedisCall(callFunction, retryCount + 1, exceptions);
            else
                throw new AggregateException($"Retry limit exceeded.", exceptions);
        }
        finally
        {
            if (conn != null)
                _connectionPool.Enqueue(conn);
        }
    }

    internal async Task<T> RedisCallAsync<T>(Func<IDatabase, Task<T>> callFunctionAsync, int retryCount = 0, List<Exception>? exceptions = null)
    {
        if (_connectionString == null || _connectionPool == null)
            throw new InvalidOperationException("The provider hasn't initialized yet.");

        ConnectionMultiplexer? conn = null;
        try
        {
            if (!_connectionPool.TryDequeue(out conn))
            {
                conn = await ConnectionMultiplexer.ConnectAsync(_connectionString);
            }
            return (await callFunctionAsync(conn.GetDatabase()))!;
        }
        catch (Exception e) when (
            e is RedisServerException ||
            e is RedisTimeoutException ||
            e is RedisConnectionException
            )
        {
            if (conn != null)
            {
                conn.Dispose();
                conn = null;
            }

            exceptions ??= new List<Exception>();

            exceptions.Add(e);

            if (retryCount < 5)
                return await RedisCallAsync(callFunctionAsync, retryCount + 1, exceptions);
            else
                throw new AggregateException($"Retry limit exceeded.", exceptions);
        }
        finally
        {
            if (conn != null)
                _connectionPool.Enqueue(conn);
        }
    }

    internal async Task RedisCallAsync(Func<IDatabase, Task> callFunctionAsync, int retryCount = 0, List<Exception>? exceptions = null)
    {
        if (_connectionString == null || _connectionPool == null)
            throw new InvalidOperationException("The provider hasn't initialized yet.");

        ConnectionMultiplexer? conn = null;
        try
        {
            if (!_connectionPool.TryDequeue(out conn))
            {
                conn = await ConnectionMultiplexer.ConnectAsync(_connectionString);
            }
            await callFunctionAsync(conn.GetDatabase());
        }
        catch (Exception e) when (
            e is RedisServerException ||
            e is RedisTimeoutException ||
            e is RedisConnectionException
            )
        {
            if (conn != null)
            {
                conn.Dispose();
                conn = null;
            }

            exceptions ??= new List<Exception>();

            exceptions.Add(e);

            if (retryCount < 5)
                await RedisCallAsync(callFunctionAsync, retryCount + 1, exceptions);
            else
                throw new AggregateException($"Retry limit exceeded.", exceptions);
        }
        finally
        {
            if (conn != null)
                _connectionPool.Enqueue(conn);
        }
    }

    /// <summary>
    /// Initializes the Redis store provider with the specified connection string and optional profiler.
    /// </summary>
    /// <param name="connectionString">The connection string used to establish connections to the Redis server. Cannot be null.</param>
    /// <param name="profiler">An optional profiler instance used to monitor data store operations, or null if profiling is not required.</param>
    public virtual void Initialize(string connectionString, IDataStoreProfiler? profiler)
    {
        _connectionString = connectionString;
        _connectionPool = _connectionPools.GetOrAdd(connectionString, cs => new ConcurrentQueue<ConnectionMultiplexer>());
    }

    /// <summary>
    /// Asynchronously initializes the data store provider using the specified connection string and optional profiler.
    /// </summary>
    /// <param name="connectionString">The connection string used to configure the data store provider. Cannot be null or empty.</param>
    /// <param name="profiler">An optional profiler instance for monitoring data store operations; or null if profiling is not required.</param>
    /// <returns>A completed task that represents the asynchronous initialization operation.</returns>
    public virtual Task InitializeAsync(string connectionString, IDataStoreProfiler? profiler)
    {
        Initialize(connectionString, profiler);

        return Task.CompletedTask;
    }

    StoreKeyType IDataStoreProvider.GetKeyType(string key)
    {
        return RedisCall(Db =>
        {
            var redisType = Db.KeyType(key);
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
        return await RedisCallAsync(async Db =>
        {
            var redisType = await Db.KeyTypeAsync(key);
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

    TimeSpan? IDataStoreProvider.GetExpire(string key)
    {
        return RedisCall(Db =>
        {
            return Db.KeyTimeToLive(key);
        });
    }

    async Task<TimeSpan?> IDataStoreProvider.GetExpireAsync(string key)
    {
        return await RedisCallAsync(async Db =>
        {
            return await Db.KeyTimeToLiveAsync(key);
        });
    }

    bool IDataStoreProvider.SetExpire(string key, TimeSpan expire)
    {
        return RedisCall(Db =>
        {
            return Db.KeyExpire(key, expire);
        });
    }

    async Task<bool> IDataStoreProvider.SetExpireAsync(string key, TimeSpan expire)
    {
        return await RedisCallAsync(async Db =>
        {
            return await Db.KeyExpireAsync(key, expire);
        });
    }

    bool IDataStoreProvider.Remove(string key)
    {
        return RedisCall(Db =>
        {
            return Db.KeyDelete(key);
        });
    }

    async Task<bool> IDataStoreProvider.RemoveAsync(string key)
    {
        return await RedisCallAsync(async Db =>
        {
            return await Db.KeyDeleteAsync(key);
        });
    }

    void IDataStoreProvider.Lock(string lockKey, TimeSpan waitTimeout, Action action, TimeSpan slidingExpire, bool skipWhenTimeout, bool throwWhenTimeout)
    {
        using var cts = new CancellationTokenSource(waitTimeout);
        if (TryAcquireLock(lockKey, slidingExpire, throwWhenTimeout, cts.Token, out var lockItem))
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

    DataStoreLock? IDataStoreProvider.AcquireLock(string lockKey, CancellationToken waitCancelToken, TimeSpan slidingExpire, bool throwWhenTimeout)
    {
        TryAcquireLock(lockKey, slidingExpire, throwWhenTimeout, waitCancelToken, out var lockItem);

        return lockItem;
    }

    async Task IDataStoreProvider.LockAsync(string lockKey, TimeSpan waitTimeout, Func<Task> action, TimeSpan slidingExpire, bool skipWhenTimeout, bool throwWhenTimeout)
    {
        using var cts = new CancellationTokenSource(waitTimeout);
        var lockItem = await TryAcquireLockAsync(lockKey, slidingExpire, throwWhenTimeout, cts.Token);
        if (lockItem != null)
        {
            using (lockItem)
            {
                await action();
            }
        }
        else
        {
            if (!skipWhenTimeout)
                await action();
        }
    }

    async Task<DataStoreLock?> IDataStoreProvider.AcquireLockAsync(string lockKey, CancellationToken waitCancelToken, TimeSpan slidingExpire, bool throwWhenTimeout)
    {
        return await TryAcquireLockAsync(lockKey, slidingExpire, throwWhenTimeout, waitCancelToken);
    }

    private bool TryAcquireLock(string key, TimeSpan slidingExpire, bool throwWhenTimeout, CancellationToken waitCancelToken, out RedisDataStoreLock? locker)
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
                var fencingToken = IncrementFencingToken(key);
                locker.SetFencingToken(fencingToken);
            }
            catch
            {
                locker.Release();
                throw;
            }
        }
        catch (Exception)
        {
            locker?.Dispose();
            locker = null;
            return false;
        }
        return true;
    }

    private async Task<RedisDataStoreLock?> TryAcquireLockAsync(string key, TimeSpan slidingExpire, bool throwWhenTimeout, CancellationToken waitCancelToken)
    {
        var locker = new RedisDataStoreLock(this, key, slidingExpire, throwWhenTimeout, waitCancelToken);
        try
        {
            if (!await locker.TryAcquireLockAsync())
            {
                locker.Dispose();
                return null;
            }

            try
            {
                var fencingToken = await IncrementFencingTokenAsync(key);
                locker.SetFencingToken(fencingToken);
            }
            catch
            {
                await locker.ReleaseAsync();
                throw;
            }
        }
        catch (Exception)
        {
            locker?.Dispose();
            return null;
        }
        return locker;
    }

    private long IncrementFencingToken(string key)
    {
        long fencingToken = 0;

        RedisCall(redis =>
        {
            fencingToken = redis.StringIncrement($"{key}:__fencing__");
        });

        return fencingToken;
    }

    private async Task<long> IncrementFencingTokenAsync(string key)
    {
        long fencingToken = 0;

        await RedisCallAsync(async redis =>
        {
            fencingToken = await redis.StringIncrementAsync($"{key}:__fencing__");
        });

        return fencingToken;
    }
}
