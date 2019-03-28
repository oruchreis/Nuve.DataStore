using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nuve.DataStore.Redis.Zookeeper;
using System.Threading;

namespace Nuve.DataStore.Test
{
    [TestClass]
    public class CodisZookeeperStoreProviderTest
    {
        [TestMethod]
        public void Connectivity()
        {
            DataStoreManager.DefaultSerializer = new Serializer.JsonNet.JsonNetDataStoreSerializer();
            DataStoreManager.RegisterProvider("Codis", "Nuve.DataStore.Redis.Zookeeper.CodisZookeeperStoreProvider, Nuve.DataStore.CodisZookeeper");            
            DataStoreManager.CreateConnection("Codis", "Codis", "51.145.158.172:2181,/codis3_test/codis-cluster/proxy|");            

            var kv = new KeyValueStore();
            kv.Set("asd", "asd");


            while (true)
            {
                Thread.Sleep(1000);
            }
        }
    }
}
