using StackExchange.Redis;
using System.Threading;

namespace Nuve.DataStore.Redis;


/// <summary>
/// Normal servicestack redis tanımlamasına bizim eklediğimiz extension'lar...
/// </summary>
public static class RedisExtension
{
    /// <summary>
    /// Normal lock metoduna expire eklenmiş hali. Kilitli keyler silinmediği sürece kalıcıdırlar. Bunun için lock key'lerine de expire ekliyoruz.
    /// </summary>
    /// <param name="client"></param>
    /// <param name="key"></param>
    /// <param name="timeout"></param>
    /// <param name="lockExpire"></param>
    /// <returns></returns>
    public static IDisposable AcquireLock(this IDatabase redis, string key, TimeSpan timeout, TimeSpan lockExpire)
    {
        var locker = new Lock(redis, key, timeout, lockExpire);
        locker.TryAcquireLock();
        return locker;
    }

    public static async Task<IDisposable> AcquireLockAsync(this IDatabase redis, string key, TimeSpan timeout, TimeSpan lockExpire)
    {
        var locker = new Lock(redis, key, timeout, lockExpire);
        await locker.TryAcquireLockAsync();
        return locker;
    }

    private sealed class Lock : IDisposable
    {
        private static readonly TimeSpan _sleepTime = TimeSpan.FromMilliseconds(200);
        private readonly IDatabase _redis;
        private readonly string _key;
        private readonly TimeSpan _timeout;
        private readonly TimeSpan _lockExpire;
        private readonly string _token = Guid.NewGuid().ToString();
        public Lock(IDatabase redis, string key, TimeSpan timeout, TimeSpan lockExpire)
        {
            _redis = redis;
            _key = key;
            _timeout = timeout;
            _lockExpire = lockExpire;
        }

        public void TryAcquireLock()
        {
            var lockAchieved = _redis.LockTake(_key, _token, _lockExpire);
            if (lockAchieved)
                return;
            using var cts = new CancellationTokenSource(_timeout);
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
                lockAchieved = _redis.LockTake(_key, _token, _lockExpire);
                loopCount++;
            }
            if (timedout && !lockAchieved)
                throw new TimeoutException($"The key '{_key}' has remained locked during timeout.")
                {
                    Data = {
                        ["Connection"] = _redis.Multiplexer.Configuration,
                        ["Key"] = _key,
                        ["Retry Count"] = loopCount,
                        ["Timeout"] = _timeout
                    }
                };
        }

        public async Task TryAcquireLockAsync()
        {
            var lockAchieved = await _redis.LockTakeAsync(_key, _token, _lockExpire);
            if (lockAchieved)
                return;
            using var cts = new CancellationTokenSource(_timeout);
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
                lockAchieved = await _redis.LockTakeAsync(_key, _token, _lockExpire);
                loopCount++;
            }
            if (timedout && !lockAchieved)
                throw new TimeoutException($"The key '{_key}' has remained locked during timeout.")
                {
                    Data = {
                        ["Connection"] = _redis.Multiplexer.Configuration,
                        ["Key"] = _key,
                        ["Retry Count"] = loopCount,
                        ["Timeout"] = _timeout
                    }
                };
        }

        public void Dispose()
        {
            _redis.LockRelease(_key, _token);
        }
    }
}
