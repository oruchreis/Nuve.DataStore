using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Nuve.DataStore.Redis;

/// <summary>
/// Represents a distributed lock using Redis as the underlying storage.
/// </summary>
public sealed class RedisDataStoreLock : DataStoreLock
{
    internal const int CheckSlidingExpirationMs = 500;
    private static readonly CancellationTokenSource _shutdownToken = new CancellationTokenSource();
    static RedisDataStoreLock()
    {
        _slidingExpirationThread.Start();
        AppDomain.CurrentDomain.ProcessExit += (s, e) => _shutdownToken.Cancel();
    }

    private static readonly Thread _slidingExpirationThread = new(SlidingExpirationWorker) { IsBackground = true, Name = "RedisLockSlidingExpiration" };
    private static readonly ConcurrentDictionary<string, RedisDataStoreLock> _locks = new();
    private static readonly EventWaitHandle _waitUntilHasAnyLock = new AutoResetEvent(false);

    private static void SlidingExpirationWorker()
    {
        while (!_shutdownToken.IsCancellationRequested)
        {
            _waitUntilHasAnyLock.WaitOne();

            foreach (var kv in _locks)
            {
                if (_shutdownToken.IsCancellationRequested)
                    break;
                var lockItem = kv.Value;

                bool reachedHalfLife = lockItem.LockAchieved != null && 
                    DateTimeOffset.UtcNow > lockItem.LockAchieved.Value.Add(TimeSpan.FromTicks(lockItem.SlidingExpire.Ticks / 2));
                if (_locks.ContainsKey(lockItem.Key) && reachedHalfLife)
                {
                    try
                    {
                        lockItem.Extend(lockItem.SlidingExpire);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e);
                    }
                }
            }

            Thread.Sleep(CheckSlidingExpirationMs);
            if (!_locks.IsEmpty)
                _waitUntilHasAnyLock.Set();
        }


        _shutdownToken.Dispose();
    }

    private static readonly TimeSpan _sleepTime = TimeSpan.FromMilliseconds(200);
    private readonly RedisStoreProvider _provider;
    internal readonly string Key;
    private readonly CancellationToken _waitCancelToken;
    private readonly bool _throwWhenTimeout;
    internal readonly TimeSpan SlidingExpire;
    internal readonly string Token = Guid.NewGuid().ToString();
    /// <inheritdoc />
    public override DateTimeOffset? LockAchieved { get; protected set; }
    private SemaphoreSlim _syncObj = new(1, 1);

    internal RedisDataStoreLock(RedisStoreProvider provider, string key, TimeSpan slidingExpire, bool throwWhenTimeout, CancellationToken waitCancelToken)
    {
        _provider = provider;
        Key = key;
        _waitCancelToken = waitCancelToken;
        _throwWhenTimeout = throwWhenTimeout;
        if (slidingExpire.TotalMilliseconds <= CheckSlidingExpirationMs * 2)
            throw new ArgumentException("Sliding expiration must be greater than 1000ms.", nameof(slidingExpire));
        SlidingExpire = slidingExpire;
    }

    internal bool TryAcquireLock()
    {
        var lockAchieved = false;
        try
        {
            _provider.RedisCall(redis =>
            {
                lockAchieved = redis.LockTake(Key, Token, SlidingExpire);
            });
            if (lockAchieved)
                return true;
            var loopCount = 1;
            while (!lockAchieved && !_waitCancelToken.IsCancellationRequested)
            {
#if NET48
                Thread.Sleep(TimeSpan.FromTicks(_sleepTime.Ticks * Math.Min(loopCount, 10))); //waiting maximum 10 times of _sleepTime
#else
                Thread.Sleep(_sleepTime * Math.Min(loopCount, 10)); //waiting maximum 10 times of _sleepTime
#endif
                _provider.RedisCall(redis =>
                {
                    lockAchieved = redis.LockTake(Key, Token, SlidingExpire);
                });
                loopCount++;
            }
            if (_waitCancelToken.IsCancellationRequested && !lockAchieved)
            {
                if (!_throwWhenTimeout)
                    return false;
                throw new TimeoutException($"The key '{Key}' has remained locked during timeout.")
                {
                    Data = {
                            ["Key"] = Key,
                            ["Retry Count"] = loopCount
                        }
                };
            }
        }
        finally
        {
            if (lockAchieved)
            {
                LockAchieved = DateTimeOffset.UtcNow;
                _locks[Key] = this;
                _waitUntilHasAnyLock.Set();
            }
        }
        return lockAchieved;
    }

    internal async Task<bool> TryAcquireLockAsync()
    {
        var lockAchieved = false;
        try
        {
            await _provider.RedisCallAsync(async redis =>
            {
                lockAchieved = await redis.LockTakeAsync(Key, Token, SlidingExpire);
            });
            if (lockAchieved)
                return true;
            var loopCount = 1;
            while (!lockAchieved && !_waitCancelToken.IsCancellationRequested)
            {
#if NET48
                await Task.Delay(TimeSpan.FromTicks(_sleepTime.Ticks * Math.Min(loopCount, 10))); //waiting maximum 10 times of _sleepTime
#else
                await Task.Delay(_sleepTime * Math.Min(loopCount, 10)); //waiting maximum 10 times of _sleepTime
#endif
                await _provider.RedisCallAsync(async redis =>
                {
                    lockAchieved = await redis.LockTakeAsync(Key, Token, SlidingExpire);
                });
                loopCount++;
            }
            if (_waitCancelToken.IsCancellationRequested && !lockAchieved)
            {
                if (!_throwWhenTimeout)
                    return false;
                throw new TimeoutException($"The key '{Key}' has remained locked during timeout.")
                {
                    Data = {
                            ["Key"] = Key,
                            ["Retry Count"] = loopCount
                        }
                };
            }
        }
        finally
        {
            if (lockAchieved)
            {
                LockAchieved = DateTimeOffset.UtcNow;
                _locks[Key] = this;
                _waitUntilHasAnyLock.Set();
            }
        }

        return lockAchieved;
    }
    /// <inheritdoc />
    public override void Dispose()
    {
        Release();
    }

#if !NET48
    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        await ReleaseAsync();
    }
#endif
    
    /// <inheritdoc />
    public override bool Extend(TimeSpan? expire = null)
    {
        try
        {
            _syncObj.Wait();
            if (LockAchieved == null)
                throw new InvalidOperationException("Lock is not acquired yet.");

            var result = false;
            _provider.RedisCall(redis =>
             {
                 result = redis.LockExtend(Key, Token, expire ?? SlidingExpire);
             });
            if (result)
                LockAchieved = DateTimeOffset.UtcNow;
            return result;
        }
        finally
        {
            _syncObj.Release();
        }
    }
    
    /// <inheritdoc />
    public override async Task<bool> ExtendAsync(TimeSpan? expire = null)
    {
        try
        {
            await _syncObj.WaitAsync();
            if (LockAchieved == null)
                throw new InvalidOperationException("Lock is not acquired yet.");
            var result = false;
            await _provider.RedisCallAsync(async redis =>
            {
                result = await redis.LockExtendAsync(Key, Token, expire ?? SlidingExpire);
            });
            if (result)
                LockAchieved = DateTimeOffset.UtcNow;
            return result;
        }
        finally
        {
            _syncObj.Release();
        }
    }
    
    /// <inheritdoc />
    public override bool Release()
    {
        var result = false;
        if (_locks.TryRemove(Key, out var _))
        {
            try
            {
                _syncObj.Wait();
                if (LockAchieved != null)
                {
                    _provider.RedisCall(redis =>
                    {
                        result = redis.LockRelease(Key, Token);
                    });
                }

            }
            finally
            {
                _syncObj.Release();
            }
        }
        return result;
    }

    /// <inheritdoc />
    public override async Task<bool> ReleaseAsync()
    {
        var result = false;
        if (_locks.TryRemove(Key, out var _))
        {
            try
            {
                await _syncObj.WaitAsync();
                if (LockAchieved != null)
                {
                    await _provider.RedisCallAsync(async redis =>
                    {
                        result = await redis.LockReleaseAsync(Key, Token);
                    });
                }
            }
            finally
            {
                _syncObj.Release();
            }
        }
        return result;
    }
}
