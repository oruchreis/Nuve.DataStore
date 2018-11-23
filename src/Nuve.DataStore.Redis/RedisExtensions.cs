using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nuve.DataStore.Redis
{

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
            locker.AcquireLock();
            return locker;
        }

        public static async Task<IDisposable> AcquireLockAsync(this IDatabase redis, string key, TimeSpan timeout, TimeSpan lockExpire)
        {
            var locker = new Lock(redis, key, timeout, lockExpire);
            await locker.AcquireLockAsync();
            return locker;
        }

        private class Lock : IDisposable
        {
            private static readonly TimeSpan _sleepTime = TimeSpan.FromMilliseconds(100);
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

            public void AcquireLock()
            {
                var lockAchieved = false;
                var totalTime = TimeSpan.Zero;
                while (!lockAchieved && totalTime < _timeout)
                {
                    lockAchieved = _redis.LockTake(_key, _token, _lockExpire);
                    if (lockAchieved)
                    {
                        continue;
                    }
                    Thread.Sleep(_sleepTime);
                    totalTime += _sleepTime;
                }

                if (!lockAchieved)
                    throw new TimeoutException(string.Format("{0} anahtarı timeout süresince kilitli kaldı.", _key));
            }

            public async Task AcquireLockAsync()
            {
                var lockAchieved = false;
                var totalTime = TimeSpan.Zero;
                while (!lockAchieved && totalTime < _timeout)
                {
                    lockAchieved = await _redis.LockTakeAsync(_key, _token, _lockExpire);
                    if (lockAchieved)
                    {
                        continue;
                    }
                    await Task.Delay(_sleepTime);
                    totalTime += _sleepTime;
                }

                if (!lockAchieved)
                    throw new TimeoutException(string.Format("{0} anahtarı timeout süresince kilitli kaldı.", _key));
            }

            public void Dispose()
            {
                _redis.LockRelease(_key, _token);
            }
        }
    }
}
