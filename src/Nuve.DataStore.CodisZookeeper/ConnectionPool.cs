using org.apache.zookeeper;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Nuve.DataStore.Redis.Zookeeper
{
    class ConnectionPool
    {        
        private readonly ZooKeeper _zooKeeper;
        public ConnectionPool(string zookeeperUrl, string path, ConfigurationOptions configurationOptions)
        {
            _zooKeeper = new ZooKeeper(zookeeperUrl, 10000, new ConnectionWatcher(), true);
        }

        private class ConnectionWatcher: Watcher
        {
            public override async Task process(WatchedEvent e)
            {
                //if (e.)
            }
        }
    }
}
