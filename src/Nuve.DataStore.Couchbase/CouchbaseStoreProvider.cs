using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Nuve.Data.DataStore.Couchbase
{
    internal class CouchbaseLocker : IDisposable
    {
        private readonly string _lockKey;
        private readonly CouchbaseClient _client;
        private readonly ulong _cas;
        public CouchbaseLocker(CouchbaseClient client, string lockKey, TimeSpan lockingExpire)
        {
            _client = client;
            _lockKey = lockKey;
            _cas = _client.GetWithLock(lockKey, lockingExpire).Cas;
        }

        public void Dispose()
        {
            _client.Unlock(_lockKey, _cas);
        }
    }

    internal class CouchbaseKeyMetadata
    {
        public List<string> Children { get; set; }
        public TimeSpan TimeToLive { get; set; }

        public CouchbaseKeyMetadata()
        {
            Children = new List<string>();
        }
    }

    public class CouchbaseStoreProvider<T> : IDataStoreProvider<T>
    {
        private readonly static ReaderWriterLockSlim _clientLocker = new ReaderWriterLockSlim();
        private static CouchbaseClient _client;

        public static CouchbaseClient Client
        {
            get
            {
                _clientLocker.EnterReadLock();
                try
                {
                    return _client;
                }
                finally
                {
                    _clientLocker.ExitReadLock();
                }
            }
        }

        public CouchbaseStoreProvider(params string[] connectionStrings)
        {
            if (Client == null)
            {
                _clientLocker.EnterWriteLock();
                try
                {
                    var connectionString = connectionStrings.FirstOrDefault();
                    var bucketParts = connectionString.Split('|');

                    var config = new CouchbaseClientConfiguration
                    {
                        Bucket = bucketParts[0],
                        BucketPassword = bucketParts[1]
                    };
                    foreach (var url in bucketParts[2].Split(','))
                    {
                        config.Urls.Add(new Uri(url));
                    }

                    _client = new CouchbaseClient(config);
                }
                finally
                {
                    _clientLocker.ExitWriteLock();
                }
            }

            DefaultLockWaitTimeout = TimeSpan.FromSeconds(5);
            DefaultLockingExpire = TimeSpan.FromSeconds(10);
            NamespaceSeperator = ":";
        }

        public T this[string key]
        {
            get { return Get(key); }
            set { Set(key, value); }
        }

        public T Get(string key)
        {
            return Client.Get<T>(key);
        }

        public IDictionary<string, T> GetAll(params string[] keys)
        {
            return Client.Get(keys).ToDictionary(kv => kv.Key, kv => (T)kv.Value);
        }

        private void RemoveKeyFromMetadata(string key)
        {
            var keyParts = key.Split(new[] { NamespaceSeperator }, StringSplitOptions.None);
            if (keyParts.Count() <= 1)
                return;
            var parentKey = string.Join(NamespaceSeperator, keyParts.Take(keyParts.Count() - 1));
            var metadataKey = string.Format("{0}{1}__metadata", parentKey, NamespaceSeperator);
            var metadata = Client.KeyExists(metadataKey) ? Client.Get<CouchbaseKeyMetadata>(metadataKey) : new CouchbaseKeyMetadata();
            metadata.Children.Remove(key);
            Client.Store(StoreMode.Set, metadataKey, metadata);
        }

        private void AddKeyToMetadata(string key)
        {
            var keyParts = key.Split(new[] { NamespaceSeperator }, StringSplitOptions.None);
            if (keyParts.Count() <= 1)
                return;
            var parentKey = string.Join(NamespaceSeperator, keyParts.Take(keyParts.Count() - 1));
            var metadataKey = string.Format("{0}{1}__metadata", parentKey, NamespaceSeperator);
            var metadata = Client.KeyExists(metadataKey) ? Client.Get<CouchbaseKeyMetadata>(metadataKey) : new CouchbaseKeyMetadata();
            metadata.Children.Add(key);
            Client.Store(StoreMode.Set, metadataKey, metadata);
        }

        private CouchbaseKeyMetadata GetKeyMetadata(string parentKey)
        {
            var metadataKey = string.Format("{0}{1}__metadata", parentKey, NamespaceSeperator);
            return Client.Get<CouchbaseKeyMetadata>(metadataKey) ?? new CouchbaseKeyMetadata();
        }

        private void SetKeyMetadata(string parentKey, CouchbaseKeyMetadata metadata)
        {
            var metadataKey = string.Format("{0}{1}__metadata", parentKey, NamespaceSeperator);
            Client.Store(StoreMode.Set, metadataKey, metadata);
        }

        public bool Set(string key, T entity)
        {
            if (!Client.KeyExists(key))
            {
                AddKeyToMetadata(key);
            }
            return Client.Store(StoreMode.Set, key, entity);
        }

        public void SetList(IDictionary<string, T> keyValues)
        {
            foreach (var keyValue in keyValues)
            {
                Set(keyValue.Key, keyValue.Value);
            }
        }

        public bool SetExpire(string key, TimeSpan expire)
        {
            Client.Touch(key, expire);
            var metadata = GetKeyMetadata(key);
            metadata.TimeToLive = expire;
            SetKeyMetadata(key, metadata);
            return true;
        }

        public TimeSpan GetTimeToLive(string key)
        {
            return GetKeyMetadata(key).TimeToLive;
        }

        public void SavePermanently()
        {
            //couchbase'de bu otomatik yapılıyor.
        }

        public string NamespaceSeperator { get; set; }
        public bool Remove(string key)
        {
            if (Client.KeyExists(key))
            {
                RemoveKeyFromMetadata(key);
            }
            return Client.Remove(key);
        }

        public bool Contains(string key)
        {
            return Client.KeyExists(key);
        }

        public long GetChildrenCount(string parentKey)
        {
            return GetKeyMetadata(parentKey).Children.Count;
        }

        public List<string> GetChildKeys(string parentKey)
        {
            return GetKeyMetadata(parentKey).Children;
        }

        public List<string> SearchKey(string keyPattern)
        {
            return GetKeyMetadata(keyPattern).Children;
        }

        public void Rename(string oldKey, string newKey)
        {
            var value = Get(oldKey);
            Remove(oldKey);
            Set(newKey, value);
        }

        public long Increment(string key)
        {
            return (long)Client.Increment(key, 0, 1);
        }

        public long Decrement(string key)
        {
            return (long)Client.Decrement(key, 0, 1);
        }

        public IDisposable Lock(string lockKey, TimeSpan waitTimeout, TimeSpan lockingExpire)
        {
            return new CouchbaseLocker(Client, lockKey, lockingExpire);
        }

        public TimeSpan DefaultLockWaitTimeout { get; set; }
        public TimeSpan DefaultLockingExpire { get; set; }
        public Dictionary<string, string> DebugInformation { get; private set; }
    }
}
