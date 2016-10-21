using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
            return new Lock(redis, key, timeout, lockExpire);
        }

        private class Lock : IDisposable
        {
            private readonly IDatabase _redis;
            private readonly string _key;
            private readonly string _token;
            public Lock(IDatabase redis, string key, TimeSpan timeout, TimeSpan lockExpire)
            {
                _redis = redis;
                _key = key;
                if (_redis.LockQuery(_key).HasValue)
                {
                    var stopWatch = new Stopwatch();
                    stopWatch.Start();
                    while (_redis.LockQuery(_key).HasValue)
                    {
                        if (stopWatch.Elapsed >= timeout)
                            throw new TimeoutException(string.Format("{0} anahtarı timeout süresince kilitli kaldı.", _key));
                    }
                }

                _token = Guid.NewGuid().ToString();
                redis.LockTake(_key, _token, lockExpire);
            }

            public void Dispose()
            {
                _redis.LockRelease(_key, _token);
            }
        }
    }
}
