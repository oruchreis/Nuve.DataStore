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

        _connectionManager = options.ConnectionMode switch
        {
            ConnectionMode.Shared => new SharedRedisConnectionManager(options),
            ConnectionMode.Pooled => new PooledRedisConnectionManager(options),
            _ => throw new ArgumentOutOfRangeException(nameof(options.ConnectionMode))
        };
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

    bool IDataStoreProvider.SetExpire(string key, TimeSpan expire)
    {
        return RedisCall(db => db.KeyExpire(key, expire));
    }

    async Task<bool> IDataStoreProvider.SetExpireAsync(string key, TimeSpan expire)
    {
        return await RedisCallAsync(async db => await db.KeyExpireAsync(key, expire).ConfigureAwait(false)).ConfigureAwait(false);
    }

    bool IDataStoreProvider.Remove(string key)
    {
        return RedisCall(db => db.KeyDelete(key));
    }

    async Task<bool> IDataStoreProvider.RemoveAsync(string key)
    {
        return await RedisCallAsync(async db => await db.KeyDeleteAsync(key).ConfigureAwait(false)).ConfigureAwait(false);
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
        var lockItem = await TryAcquireLockAsync(lockKey, slidingExpire, throwWhenTimeout, cts.Token).ConfigureAwait(false);
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

    async Task<DataStoreLock?> IDataStoreProvider.AcquireLockAsync(string lockKey, CancellationToken waitCancelToken, TimeSpan slidingExpire, bool throwWhenTimeout)
    {
        return await TryAcquireLockAsync(lockKey, slidingExpire, throwWhenTimeout, waitCancelToken).ConfigureAwait(false);
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
        catch
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
            if (!await locker.TryAcquireLockAsync().ConfigureAwait(false))
            {
                locker.Dispose();
                return null;
            }

            try
            {
                var fencingToken = await IncrementFencingTokenAsync(key).ConfigureAwait(false);
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
            fencingToken = await redis.StringIncrementAsync($"{key}:__fencing__").ConfigureAwait(false);
        }).ConfigureAwait(false);

        return fencingToken;
    }
}