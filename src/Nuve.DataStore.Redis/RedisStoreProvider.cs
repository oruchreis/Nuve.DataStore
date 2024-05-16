using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using StackExchange.Redis;

namespace Nuve.DataStore.Redis;

public partial class RedisStoreProvider : IDataStoreProvider
{
    private static readonly ConcurrentDictionary<string, ConcurrentQueue<ConnectionMultiplexer>> _connectionPools = new();

    private string? _connectionString;
    private ConcurrentQueue<ConnectionMultiplexer>? _connectionPool;
    
    protected T RedisCall<T>(Func<IDatabase, T> callFunction, int retryCount = 0, List<Exception>? exceptions = null)
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
            if (conn != null)
            {
                conn.Dispose();
                conn = null;
            }

            exceptions ??= new List<Exception>();

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

    protected void RedisCall(Action<IDatabase> callFunction, int retryCount = 0, List<Exception>? exceptions = null)
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

    protected async Task<T> RedisCallAsync<T>(Func<IDatabase, Task<T>> callFunctionAsync, int retryCount = 0, List<Exception>? exceptions = null)
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

    protected async Task RedisCallAsync(Func<IDatabase, Task> callFunctionAsync, int retryCount = 0, List<Exception>? exceptions = null)
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

    public virtual void Initialize(string connectionString, IDataStoreProfiler profiler)
    {
        _connectionString = connectionString;
        _connectionPool = _connectionPools.GetOrAdd(connectionString, cs => new ConcurrentQueue<ConnectionMultiplexer>());
    }

    public virtual Task InitializeAsync(string connectionString, IDataStoreProfiler profiler)
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
        try
        {
            RedisCall(Db =>
            {
                using (Db.AcquireLock(lockKey, waitTimeout, slidingExpire))
                {
                    action();
                }
            });
        }
        catch (TimeoutException e)
        {
            if (!skipWhenTimeout)
                action();
            if (throwWhenTimeout)
                ExceptionDispatchInfo.Capture(e).Throw();
        }
    }

    async Task IDataStoreProvider.LockAsync(string lockKey, TimeSpan waitTimeout, Func<Task> action, TimeSpan slidingExpire, bool skipWhenTimeout, bool throwWhenTimeout)
    {
        try
        {
            await RedisCallAsync(async Db =>
            {
                using (await Db.AcquireLockAsync(lockKey, waitTimeout, slidingExpire))
                {
                    await action();
                }
            });
        }
        catch (TimeoutException e)
        {
            if (!skipWhenTimeout)
                await action();
            if (throwWhenTimeout)
                ExceptionDispatchInfo.Capture(e).Throw();
        }
    }
}
