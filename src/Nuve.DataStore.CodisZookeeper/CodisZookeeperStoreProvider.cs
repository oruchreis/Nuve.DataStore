using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Nuve.DataStore.Redis.Zookeeper
{
    public class CodisZookeeperStoreProvider : RedisStoreProvider
    {
        private static readonly ConcurrentDictionary<string, ConnectionPool> _pools = new ConcurrentDictionary<string, ConnectionPool>();

        public CodisZookeeperStoreProvider()
        {
        }

        public override void Initialize(string connectionString, IDataStoreProfiler profiler)
        {
            InitializeAsync(connectionString, profiler).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public override async Task InitializeAsync(string connectionString, IDataStoreProfiler profiler)
        {
            var pool = _pools.GetOrAdd(connectionString,
                cs =>
                {
                    var parts = cs.Split('|');
                    var zookeeperConnStr = parts[0];
                    var redisConnStr = "";
                    if (parts.Length > 0)
                        redisConnStr = parts[1];
                    StackExchange.Redis.ConfigurationOptions redisOptions = null;
                    if (!string.IsNullOrEmpty(redisConnStr))
                        redisOptions = ParseConnectionString(redisConnStr);

                    var zookeeperParts = zookeeperConnStr.Split(',');
                    if (zookeeperParts.Length != 2)
                        throw new InvalidOperationException("Invalid zookeeper connection string");
                    var zookeeperUrl = zookeeperParts[0];
                    var zookeeperPath = zookeeperParts[1];

                    return new ConnectionPool(zookeeperUrl, zookeeperPath, redisOptions);
                });

            //Redis = await pool.GetConnectionAsync();
        }
    }
}
