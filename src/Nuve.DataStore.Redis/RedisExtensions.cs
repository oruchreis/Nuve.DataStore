using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

[assembly: InternalsVisibleTo("Nuve.DataStore.Test")]
namespace Nuve.DataStore.Redis;

/// <summary>
/// Extensions added to the normal ServiceStack Redis definitions...
/// </summary>
public static class RedisExtension
{
    /// <summary>
    /// Extended version of the normal lock method with expiration. Locked keys will be persistent unless they are deleted. We add expiration to lock keys for this purpose.
    /// </summary>
    /// <param name="redis"></param>
    /// <param name="key"></param>
    /// <param name="timeout"></param>
    /// <param name="slidingExpire"></param>
    /// <returns></returns>
    public static IDisposable AcquireLock(this IDatabase redis, string key, TimeSpan timeout, TimeSpan slidingExpire)
    {
        var locker = new Lock(redis, key, timeout, slidingExpire);
        try
        {
            locker.TryAcquireLock();
        }
        catch (Exception)
        {
            locker.Dispose();
            throw;
        }
        return locker;
    }

    public static async Task<IDisposable> AcquireLockAsync(this IDatabase redis, string key, TimeSpan timeout, TimeSpan slidingExpire)
    {
        var locker = new Lock(redis, key, timeout, slidingExpire);
        try
        {
            await locker.TryAcquireLockAsync();
        }
        catch (Exception)
        {
            locker.Dispose();
            throw;
        }
        return locker;
    }

    internal sealed class Lock : IDisposable
    {
        internal const int CheckSlidingExpirationMs = 500;
        private static readonly CancellationTokenSource _shutdownToken = new CancellationTokenSource();
        static Lock()
        {
            _slidingExpirationThread.Start();
            AppDomain.CurrentDomain.ProcessExit += (s, e) => _shutdownToken.Cancel();
        }

        private static readonly Thread _slidingExpirationThread = new Thread(SlidingExpirationWorker) { IsBackground = true, Name = "RedisLockSlidingExpiration" };
        private static readonly ConcurrentDictionary<Lock, object> _locks = new ConcurrentDictionary<Lock, object>();
        private static readonly EventWaitHandle _waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

        private static void SlidingExpirationWorker()
        {
            while (!_shutdownToken.IsCancellationRequested)
            {
                _waitHandle.WaitOne();

                foreach (var kv in _locks)
                {
                    if (_shutdownToken.IsCancellationRequested)
                        break;
                    var lockItem = kv.Key;
                    var syncObj = kv.Value;
                    lock (syncObj)
                    {
                        bool reachedHalfLife = lockItem.LockAchieved != null &&
                            DateTimeOffset.UtcNow > lockItem.LockAchieved.Value.Add(TimeSpan.FromTicks(lockItem.SlidingExpire.Ticks / 2));
                        if (_locks.ContainsKey(lockItem) && reachedHalfLife)
                        {
                            try
                            {
                                lockItem._redis.LockExtend(lockItem.Key, lockItem.Token, lockItem.SlidingExpire);
                                lockItem.LockAchieved = DateTimeOffset.UtcNow;
                            }
                            catch (Exception e)
                            {
                                Debug.WriteLine(e);
                            }
                        }
                    }
                }

                Thread.Sleep(CheckSlidingExpirationMs);
                if (_locks.IsEmpty)
                    _waitHandle.Reset();
            }


            _shutdownToken.Dispose();
        }

        private static readonly TimeSpan _sleepTime = TimeSpan.FromMilliseconds(200);
        private readonly IDatabase _redis;
        internal readonly string Key;
        internal readonly TimeSpan Timeout;
        internal readonly TimeSpan SlidingExpire;
        internal readonly string Token = Guid.NewGuid().ToString();
        internal DateTimeOffset? LockAchieved;

        public Lock(IDatabase redis, string key, TimeSpan timeout, TimeSpan slidingExpire)
        {
            _redis = redis;
            Key = key;
            Timeout = timeout;
            if (slidingExpire.TotalMilliseconds <= CheckSlidingExpirationMs * 2)
                throw new ArgumentException("Sliding expiration must be greater than 1000ms.", nameof(slidingExpire));
            SlidingExpire = slidingExpire;
            _locks[this] = new object();
            _waitHandle.Set();
        }

        public void TryAcquireLock()
        {
            var lockAchieved = false;
            try
            {
                lockAchieved = _redis.LockTake(Key, Token, SlidingExpire);
                if (lockAchieved)
                    return;
                using var cts = new CancellationTokenSource(Timeout);
                var timedout = false;
                cts.Token.Register(() => timedout = true);
                var loopCount = 1;
                while (!lockAchieved && !cts.IsCancellationRequested)
                {
#if NET48
                Thread.Sleep(TimeSpan.FromTicks(_sleepTime.Ticks * Math.Min(loopCount, 10))); //waiting maximum 10 times of _sleepTime
#else
                    Thread.Sleep(_sleepTime * Math.Min(loopCount, 10)); //waiting maximum 10 times of _sleepTime
#endif
                    lockAchieved = _redis.LockTake(Key, Token, SlidingExpire);
                    loopCount++;
                }
                if (timedout && !lockAchieved)
                    throw new TimeoutException($"The key '{Key}' has remained locked during timeout.")
                    {
                        Data = {
                            ["Connection"] = _redis.Multiplexer.Configuration,
                            ["Key"] = Key,
                            ["Retry Count"] = loopCount,
                            ["Timeout"] = Timeout
                        }
                    };
            }
            finally
            {
                if (lockAchieved)
                    LockAchieved = DateTimeOffset.UtcNow;
            }
        }

        public async Task TryAcquireLockAsync()
        {
            var lockAchieved = false;
            try
            {
                lockAchieved = await _redis.LockTakeAsync(Key, Token, SlidingExpire);
                if (lockAchieved)
                    return;
                using var cts = new CancellationTokenSource(Timeout);
                var timedout = false;
                cts.Token.Register(() => timedout = true);
                var loopCount = 1;
                while (!lockAchieved && !cts.IsCancellationRequested)
                {
#if NET48
                await Task.Delay(TimeSpan.FromTicks(_sleepTime.Ticks * Math.Min(loopCount, 10))); //waiting maximum 10 times of _sleepTime
#else
                    await Task.Delay(_sleepTime * Math.Min(loopCount, 10)); //waiting maximum 10 times of _sleepTime
#endif
                    lockAchieved = await _redis.LockTakeAsync(Key, Token, SlidingExpire);
                    loopCount++;
                }
                if (timedout && !lockAchieved)
                    throw new TimeoutException($"The key '{Key}' has remained locked during timeout.")
                    {
                        Data = {
                            ["Connection"] = _redis.Multiplexer.Configuration,
                            ["Key"] = Key,
                            ["Retry Count"] = loopCount,
                            ["Timeout"] = Timeout
                        }
                    };
            }
            finally
            {
                if (lockAchieved)
                    LockAchieved = DateTimeOffset.UtcNow;
            }
        }

        public void Dispose()
        {
            _locks.TryRemove(this, out var syncObj);
            lock (syncObj!)
            {
                _redis.LockRelease(Key, Token);
            }
        }
    }
}
