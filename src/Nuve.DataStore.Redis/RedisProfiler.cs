using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Nuve.DataStore.Redis
{
    public class RedisProfiler: IProfiler
    {
        private readonly IDataStoreProfiler _profiler;
        private readonly ConnectionMultiplexer _redis;
        public RedisProfiler(ConnectionMultiplexer cm, IDataStoreProfiler profiler)
        {
            _profiler = profiler;
            _redis = cm;
        }

        public object GetContext()
        {
            return _profiler == null ? new object() : _profiler.GetContext();
        }

        public async Task<T> Profile<T>(Func<Task<T>> func, string key, [CallerMemberName] string method = null)
        {
            return await Profile(func, () => key, method);
        }

        public async Task<T> Profile<T>(Func<Task<T>> func, Func<string> getKey, [CallerMemberName] string method = null)
        {
            method = string.Format("Redis.{0}", method);
            var startTime = default(DateTime);
            object ctx = null;
            string key = null;
            if (_profiler != null)
            {
                key = getKey();
                ctx = _profiler.Begin(method, key);
                //if (ctx != null)
                //    _redis.BeginProfiling(ctx);
                startTime = DateTime.Now;
            }
            try
            {
                return await func();
            }
            finally
            {
                if (ctx != null)
                {
                    /*var profiledCommands = _redis.FinishProfiling(ctx).OrderBy(pc => pc.CommandCreated).ToList();
                    var result = new DataStoreProfileResult
                                 {
                                     Method = method,
                                     Key = key
                                 };
                    if (profiledCommands.Any())
                    {
                        result.StartTime = profiledCommands.First().CommandCreated;
                        result.EndTime = result.StartTime + new TimeSpan(profiledCommands.Sum(pc => pc.ElapsedTime.Ticks));
                    }                    
                    _profiler.Finish(ctx, result);*/
                    _profiler.Finish(ctx, new DataStoreProfileResult
                                          {
                                              Method = method,
                                              Key = key,
                                              StartTime = startTime,
                                              EndTime = DateTime.Now
                                          });
                }                
            }
        }

        public void Profile(Action action, string key, [CallerMemberName] string method = null)
        {
            Profile(() =>
            {
                action();
                return 0;
            }, key, method);
        }


        public T Profile<T>(Func<T> func, string key, [CallerMemberName] string method = null)
        {
            return Profile(func, () => key, method);
        }

        public void Profile(Action action, Func<string> getKey, [CallerMemberName] string method = null)
        {
            Profile(() =>
                    {
                        action();
                        return 0;
                    }, getKey, method);
        }

        public T Profile<T>(Func<T> func, Func<string> getKey, [CallerMemberName] string method = null)
        {
            method = string.Format("Redis.{0}", method);
            var startTime = default(DateTime);
            object ctx = null;
            string key = null;
            if (_profiler != null)
            {
                key = getKey();
                ctx = _profiler.Begin(method, key);
                //if (ctx != null)
                //    _redis.BeginProfiling(ctx);
                startTime = DateTime.Now;
            }
            try
            {
                return func();
            }
            finally
            {
                if (ctx != null)
                {
                    /*var profiledCommands = _redis.FinishProfiling(ctx).OrderBy(pc => pc.CommandCreated).ToList();
                    var result = new DataStoreProfileResult
                                 {
                                     Method = method,
                                     Key = key
                                 };
                    if (profiledCommands.Any())
                    {
                        result.StartTime = profiledCommands.First().CommandCreated;
                        result.EndTime = result.StartTime + new TimeSpan(profiledCommands.Sum(pc => pc.ElapsedTime.Ticks));
                    }                    
                    _profiler.Finish(ctx, result);*/
                    _profiler.Finish(ctx, new DataStoreProfileResult
                    {
                        Method = method,
                        Key = key,
                        StartTime = startTime,
                        EndTime = DateTime.Now
                    });
                }
            }
        }
    }
}
