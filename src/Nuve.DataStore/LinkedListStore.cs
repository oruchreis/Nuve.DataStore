using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nuve.DataStore.Helpers;

namespace Nuve.DataStore
{
    public sealed class LinkedListStore<TValue>: DataStoreBase, ICollection<TValue>, IList<TValue>
    {
        private readonly ILinkedListStoreProvider _linkedListStoreProvider;
        private static readonly string _valueName = typeof(TValue).GetFriendlyName().Replace('.', '_');
        private static readonly string _typeName = typeof(DictionaryStore<TValue>).GetFriendlyName();

        /// <summary>
        /// HashSet değer tutan store yapısı. 
        /// </summary>
        /// <param name="masterKey">Bu dictionary hangi key altında saklanacak</param>
        /// <param name="connectionName">Config'de tanımlı bağlantı ismi</param>
        /// <param name="defaultExpire">Varsayılan expire süresi.</param>
        /// <param name="autoPing">Her işlemde otomatik olarak Ping yapılsın mı?</param>
        /// <param name="namespaceSeperator">Namespace'leri ayırmak için kullanılan ayraç. Varsayılan olarak ":"dir. </param>
        /// <param name="overrideRootNamespace">Bağlantıya tanımlı root alan adını değiştirmek için kullanılır.</param>
        /// <param name="serializer">Varsayılan serializer yerine başka bir serializer kullanmak istiyorsanız bunu setleyin.</param>
        /// <param name="profiler">Özel olarak sadece bu data store'un metodlarını profile etmek için kullanılır. 
        /// Setlense de setlenmese de <see cref="DataStoreManager"/>'a kayıtlı global profiler kullanılır.</param>
        public LinkedListStore(string masterKey, string connectionName = null, TimeSpan? defaultExpire = null, bool autoPing = false,
            string namespaceSeperator = null, string overrideRootNamespace = null, IDataStoreSerializer serializer = null, IDataStoreProfiler profiler = null,
            int? compressBiggerThan = null) :
            base(connectionName, defaultExpire, autoPing, namespaceSeperator, overrideRootNamespace, serializer, profiler, compressBiggerThan)
        {
            _linkedListStoreProvider = Provider as ILinkedListStoreProvider;
            if (_linkedListStoreProvider == null)
                throw new InvalidOperationException(string.Format("The provider with connection '{0}' doesn't support LinkedList operations. " +
                                                                  "The provider must implement ILinkedListStoreProvider interface to use LinkedListStore", connectionName));

            MasterKey = JoinWithRootNamespace(string.Format("{0}<{1}>",
                masterKey,
                _valueName));//tip ismini eklememiz şart. Çünkü aynı masterKey'de farklı tipte olan listeler deserialize olamaz.
        }

        internal override string TypeName
        {
            get { return _typeName; }
        }

        /// <summary>
        /// Store yapısının tutulduğu tam yol.
        /// </summary>
        public readonly string MasterKey;

        /// <summary>
        /// Bu store mevcut mu?
        /// </summary>
        /// <returns></returns>
        public async Task<bool> IsExistsAsync()
        {
            using (new ProfileScope(this,MasterKey))
            {
                return await _linkedListStoreProvider.IsExistsAsync(MasterKey); 
            }
        }

        /// <summary>
        /// Bu store mevcut mu?
        /// </summary>
        /// <returns></returns>
        public bool IsExists()
        {
            using (new ProfileScope(this, MasterKey))
            {
                return _linkedListStoreProvider.IsExists(MasterKey);
            }
        }

        /// <summary>
        /// Store'un <see cref="DataStoreBase.DefaultExpire"/> özelliği setli ise expire süresini bu süreye sıfırlar.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> PingAsync()
        {
            return await base.PingAsync(MasterKey);
        }

        /// <summary>
        /// Store'un <see cref="DataStoreBase.DefaultExpire"/> özelliği setli ise expire süresini bu süreye sıfırlar.
        /// </summary>
        /// <returns></returns>
        public bool Ping()
        {
            return base.Ping(MasterKey);
        }

        private void CheckAutoPing()
        {
            if (AutoPing)
                Task.Run(() => Ping());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public async Task<TValue> GetAsync(long index)
        {
            using (new ProfileScope(this,MasterKey))
            {
                CheckAutoPing();
                return SingleResult<TValue>(await _linkedListStoreProvider.GetAsync(MasterKey, index)); 
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public TValue Get(long index)
        {
            using (new ProfileScope(this, MasterKey))
            {
                CheckAutoPing();
                return SingleResult<TValue>(_linkedListStoreProvider.Get(MasterKey, index));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public async Task<IList<TValue>> GetRangeAsync(long start, long end)
        {
            using (new ProfileScope(this,MasterKey))
            {
                CheckAutoPing();
                return ListResult<TValue>(await _linkedListStoreProvider.GetRangeAsync(MasterKey, start, end)); 
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public IList<TValue> GetRange(long start, long end)
        {
            using (new ProfileScope(this, MasterKey))
            {
                CheckAutoPing();
                return ListResult<TValue>(_linkedListStoreProvider.GetRange(MasterKey, start, end));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task SetAsync(long index, TValue value)
        {
            using (new ProfileScope(this,MasterKey))
            {
                CheckAutoPing();
                await _linkedListStoreProvider.SetAsync(MasterKey, index, AsValue(value)); 
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public void Set(long index, TValue value)
        {
            using (new ProfileScope(this, MasterKey))
            {
                CheckAutoPing();
                _linkedListStoreProvider.Set(MasterKey, index, AsValue(value));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<long> AddFirstAsync(params TValue[] value)
        {
            using (new ProfileScope(this,MasterKey))
            {
                CheckAutoPing();
                return await _linkedListStoreProvider.AddFirstAsync(MasterKey, AsValues(value)); 
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public long AddFirst(params TValue[] value)
        {
            using (new ProfileScope(this, MasterKey))
            {
                CheckAutoPing();
                return _linkedListStoreProvider.AddFirst(MasterKey, AsValues(value));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<long> AddLastAsync(params TValue[] value)
        {
            using (new ProfileScope(this,MasterKey))
            {
                CheckAutoPing();
                return await _linkedListStoreProvider.AddLastAsync(MasterKey, AsValues(value)); 
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public long AddLast(params TValue[] value)
        {
            using (new ProfileScope(this, MasterKey))
            {
                CheckAutoPing();
                return _linkedListStoreProvider.AddLast(MasterKey, AsValues(value));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pivot"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<long> AddAfterAsync(TValue pivot, TValue value)
        {
            using (new ProfileScope(this,MasterKey))
            {
                CheckAutoPing();
                return await _linkedListStoreProvider.AddAfterAsync(MasterKey, AsValue(pivot), AsValue(value)); 
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pivot"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public long AddAfter(TValue pivot, TValue value)
        {
            using (new ProfileScope(this, MasterKey))
            {
                CheckAutoPing();
                return _linkedListStoreProvider.AddAfter(MasterKey, AsValue(pivot), AsValue(value));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pivot"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<long> AddBeforeAsync(TValue pivot, TValue value)
        {
            using (new ProfileScope(this,MasterKey))
            {
                CheckAutoPing();
                return await _linkedListStoreProvider.AddBeforeAsync(MasterKey, AsValue(value), AsValue(value)); 
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pivot"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public long AddBefore(TValue pivot, TValue value)
        {
            using (new ProfileScope(this, MasterKey))
            {
                CheckAutoPing();
                return _linkedListStoreProvider.AddBefore(MasterKey, AsValue(value), AsValue(value));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<long> CountAsync()
        {
            using (new ProfileScope(this,MasterKey))
            {
                CheckAutoPing();
                return await _linkedListStoreProvider.CountAsync(MasterKey); 
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public long Count()
        {
            using (new ProfileScope(this, MasterKey))
            {
                CheckAutoPing();
                return _linkedListStoreProvider.Count(MasterKey);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<TValue> RemoveFirstAsync()
        {
            using (new ProfileScope(this,MasterKey))
            {
                CheckAutoPing();
                return SingleResult<TValue>(await _linkedListStoreProvider.RemoveFirstAsync(MasterKey)); 
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public TValue RemoveFirst()
        {
            using (new ProfileScope(this, MasterKey))
            {
                CheckAutoPing();
                return SingleResult<TValue>(_linkedListStoreProvider.RemoveFirst(MasterKey));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<TValue> RemoveLastAsync()
        {
            using (new ProfileScope(this,MasterKey))
            {
                CheckAutoPing();
                return SingleResult<TValue>(await _linkedListStoreProvider.RemoveLastAsync(MasterKey)); 
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public TValue RemoveLast()
        {
            using (new ProfileScope(this, MasterKey))
            {
                CheckAutoPing();
                return SingleResult<TValue>(_linkedListStoreProvider.RemoveLast(MasterKey));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<long> RemoveAsync(TValue value)
        {
            using (new ProfileScope(this,MasterKey))
            {
                CheckAutoPing();
                return await _linkedListStoreProvider.RemoveAsync(MasterKey, AsValue(value)); 
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public long Remove(TValue value)
        {
            using (new ProfileScope(this, MasterKey))
            {
                CheckAutoPing();
                return _linkedListStoreProvider.Remove(MasterKey, AsValue(value));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public async Task TrimAsync(long start, long end)
        {
            using (new ProfileScope(this,MasterKey))
            {
                CheckAutoPing();
                await _linkedListStoreProvider.TrimAsync(MasterKey, start, end); 
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public void Trim(long start, long end)
        {
            using (new ProfileScope(this, MasterKey))
            {
                CheckAutoPing();
                _linkedListStoreProvider.Trim(MasterKey, start, end);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<bool> ClearAsync()
        {
            using (new ProfileScope(this,MasterKey))
            {
                return await Provider.RemoveAsync(MasterKey); 
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool Clear()
        {
            using (new ProfileScope(this, MasterKey))
            {
                return Provider.Remove(MasterKey);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<bool> InsertAsync(long index, TValue value)
        {
            using (new ProfileScope(this,MasterKey))
            {
                CheckAutoPing();
                var itemAtIndex = await _linkedListStoreProvider.GetAsync(MasterKey, index);
                return await _linkedListStoreProvider.AddBeforeAsync(MasterKey, itemAtIndex, AsValue(value)) > 0; 
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Insert(long index, TValue value)
        {
            using (new ProfileScope(this, MasterKey))
            {
                CheckAutoPing();
                var itemAtIndex = _linkedListStoreProvider.Get(MasterKey, index);
                return _linkedListStoreProvider.AddBefore(MasterKey, itemAtIndex, AsValue(value)) > 0;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public async Task<bool> RemoveAtAsync(long index)
        {
            using (new ProfileScope(this,MasterKey))
            {
                CheckAutoPing();
                var itemAtIndex = await _linkedListStoreProvider.GetAsync(MasterKey, index);
                return await _linkedListStoreProvider.RemoveAsync(MasterKey, itemAtIndex) > 0; 
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool RemoveAt(long index)
        {
            using (new ProfileScope(this, MasterKey))
            {
                CheckAutoPing();
                var itemAtIndex = _linkedListStoreProvider.Get(MasterKey, index);
                return _linkedListStoreProvider.Remove(MasterKey, itemAtIndex) > 0;
            }
        }

        /// <summary>
        /// Verilen key'e göre kilit oluşturur.
        /// </summary>
        /// <param name="key">Hangi key kilitlenecek</param>
        /// <param name="waitTimeout"></param>
        /// <param name="lockerExpire"></param>
        /// <param name="action"></param>
        /// <param name="skipWhenTimeout">Timeout olduğunda çalıştırılacak olan aksiyon geçilsin mi?</param>
        /// <param name="throwWhenTimeout">Timeout olduğunda <see cref="TimeoutException"/> fırlatılsın mı?</param>
        public void Lock(string key, TimeSpan waitTimeout, TimeSpan lockerExpire, Action action, bool skipWhenTimeout = true, bool throwWhenTimeout = false)
        {
            var lockKey = string.Format("{0}_locker_{1}{2}", MasterKey, NamespaceSeperator, key);
            using (new ProfileScope(this, lockKey))
            {
                Provider.Lock(lockKey, waitTimeout, lockerExpire, action, skipWhenTimeout, throwWhenTimeout);
            }
        }

        #region Interfaces
		public IEnumerator<TValue> GetEnumerator()
        {
            return new LinkedListStoreEnumerator<TValue>(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void ICollection<TValue>.Add(TValue item)
        {
            AddLast(item);
        }

        void ICollection<TValue>.Clear()
        {
            Clear();
        }

        [Obsolete("This method enumerates all items.")]
        public bool Contains(TValue item)
        {
            return GetRange(0, -1).Contains(item);
        }

        [Obsolete("This method enumerates all items.")]
        public void CopyTo(TValue[] array, int arrayIndex)
        {
            GetRange(0, -1).CopyTo(array, arrayIndex);
        }

        bool ICollection<TValue>.Remove(TValue item)
        {
            return Remove(item) > 0;
        }

        int ICollection<TValue>.Count
        {
            get { return (int) Count(); } 
        }

        public bool IsReadOnly { get { return false; } }

        [Obsolete("This method enumerates all items.")]
        public int IndexOf(TValue item)
        {
            return GetRange(0, -1).IndexOf(item);
        }

        void IList<TValue>.Insert(int index, TValue item)
        {
            Insert(index, item);
        }

        void IList<TValue>.RemoveAt(int index)
        {
            RemoveAt(index);
        }

        public TValue this[int index]
        {
            get { return Get(index); }
            set { Set(index, value); }
        }
	#endregion
    }
}
