using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aerospike.Client;

namespace Nuve.DataStore.Aerospike
{
    public partial class AerospikeStoreProvider : IDataStoreProvider
    {
        private static readonly ConcurrentDictionary<string, AerospikeClient> _clients = new ConcurrentDictionary<string, AerospikeClient>();
        private static readonly ConcurrentDictionary<string, AsyncClient> _asyncClients = new ConcurrentDictionary<string, AsyncClient>();
        protected AsyncClient Client;
        protected AsyncClientPolicy ClientPolicy;
        protected string Namespace;

        public void Initialize(string connectionString, IDataStoreProfiler profiler)
        {
            ClientPolicy = new AsyncClientPolicy
                           {
                               readPolicyDefault =
                               {
                                   timeout = 5000,
                                   maxRetries = 50,
                                   consistencyLevel = ConsistencyLevel.CONSISTENCY_ONE,
                                   sleepBetweenRetries = 10,
                                   sendKey = true,
                               },
                               writePolicyDefault =
                               {
                                   timeout = 5000,
                                   maxRetries = 50,
                                   sleepBetweenRetries = 10,
                                   sendKey = true,
                                   recordExistsAction = RecordExistsAction.UPDATE,
                                   expiration = -1
                               },
                               batchPolicyDefault =
                               {
                                   consistencyLevel = ConsistencyLevel.CONSISTENCY_ONE,
                                   sendKey = true,
                                   timeout = 5000,
                                   maxRetries = 50,
                                   sleepBetweenRetries = 10
                               }
                           };
            Client = _asyncClients.GetOrAdd(connectionString,
                connStr =>
                {
                    var parameters = connectionString.Split(';').Select(p => p.Split('=')).ToDictionary(p => p[0], p => p[1]);
                    Namespace = parameters["namespace"];
                    return new AsyncClient(ClientPolicy,
                               parameters["hosts"].Split(',').Select(h => h.Split(':')).Select(ip => new Host(ip[0], int.Parse(ip[1]))).ToArray());
                });
        }

        public StoreKeyType GetKeyType(string key)
        {
            return StoreKeyType.KeyValue; //todo: burada bir şeyler yap.
        }

        public Task<StoreKeyType> GetKeyTypeAsync(string key)
        {
            return Task.FromResult(StoreKeyType.KeyValue);
        }

        public TimeSpan? GetExpire(string key)
        {
            return TimeSpan.FromSeconds(Client.GetHeader(null, key.ToKey(Namespace)).TimeToLive);
        }

        public async Task<TimeSpan?> GetExpireAsync(string key)
        {
            return TimeSpan.FromSeconds((await Client.GetHeader(null, CancellationToken.None, key.ToKey(Namespace))).TimeToLive);
        }

        public bool SetExpire(string key, TimeSpan expire)
        {
            try
            {
                Client.Touch(new WritePolicy(ClientPolicy.writePolicyDefault)
                {
                    expiration = (int)expire.TotalSeconds,
                    recordExistsAction = RecordExistsAction.UPDATE
                }, key.ToKey(Namespace));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> SetExpireAsync(string key, TimeSpan expire)
        {
            try
            {
                await Client.Touch(new WritePolicy(ClientPolicy.writePolicyDefault)
                {
                    expiration = (int)expire.TotalSeconds,
                    recordExistsAction = RecordExistsAction.UPDATE
                }, CancellationToken.None, key.ToKey(Namespace));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool Remove(string key)
        {
            return Client.Delete(null, key.ToKey(Namespace));
        }

        public async Task<bool> RemoveAsync(string key)
        {
            return await Client.Delete(null, CancellationToken.None, key.ToKey(Namespace));
        }

        public void Lock(string lockKey, TimeSpan waitTimeout, TimeSpan lockerExpire, Action action, bool skipWhenTimeout = true, bool throwWhenTimeout = false)
        {
            //todo: direk destek yok, taklit yapılacak.
        }
    }
}
