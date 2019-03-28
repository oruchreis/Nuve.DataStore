using System;
using System.Collections.Generic;
using System.Text;

namespace Nuve.DataStore.Redis.Zookeeper
{
    class CodisProxyMetadata
    {
        //Codis Ver < 3
        public string addr { get; set; }
        public string state { get; set; }

        //Codis Ver > 3
        public string proxy_addr { get; set; }
    }
}
