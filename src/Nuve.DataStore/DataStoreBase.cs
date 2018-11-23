using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nuve.DataStore
{
    /// <summary>
    /// DataStore yapıları için base sınıf.
    /// </summary>
    public abstract class DataStoreBase
    {
        private readonly IDataStoreSerializer _serializer;
        private readonly string _rootNameSpace;
        protected readonly IDataStoreProvider Provider;
        internal readonly IDataStoreProfiler Profiler;
        private int? _compressBiggerThan;

        /// <summary>
        /// Tüm DataStore yapıları için base sınıf.
        /// </summary>
        /// <param name="connectionName">Config'de tanımlı bağlantı ismi</param>
        /// <param name="defaultExpire">Varsayılan expire süresi.</param>
        /// <param name="autoPing">Her işlemde otomatik olarak <see cref="Ping"/> yapılsın mı?</param>
        /// <param name="namespaceSeperator">Namespace'leri ayırmak için kullanılan ayraç. Varsayılan olarak ":"dir. </param>
        /// <param name="overrideRootNamespace">Bağlantıya tanımlı root alan adını değiştirmek için kullanılır.</param>
        /// <param name="serializer">Varsayılan serializer yerine başka bir serializer kullanmak istiyorsanız bunu setleyin.</param>
        /// <param name="profiler">Özel olarak sadece bu data store'un metodlarını profile etmek için kullanılır. 
        /// Setlense de setlenmese de <see cref="DataStoreManager"/>'a kayıtlı global profiler kullanılır.</param>
        protected DataStoreBase(string connectionName = null, TimeSpan? defaultExpire = null, bool autoPing = false,
            string namespaceSeperator = null, string overrideRootNamespace = null, IDataStoreSerializer serializer = null, IDataStoreProfiler profiler = null,
            int? compressBiggerThan = null)
        {
            DefaultExpire = defaultExpire ?? TimeSpan.Zero;
            AutoPing = autoPing;
            NamespaceSeperator = namespaceSeperator ?? ":";
            DataStoreManager.GetProvider(connectionName, out Provider, out _rootNameSpace, out int? defaultCompressBiggerThan);
            if (overrideRootNamespace != null)
                _rootNameSpace = overrideRootNamespace;
            _serializer = serializer ?? DataStoreManager.DefaultSerializer;
            Profiler = profiler;
            _compressBiggerThan = compressBiggerThan ?? defaultCompressBiggerThan;
        }

        private long _defaultExpire;
        /// <summary>
        /// Varsayılan expire süresi.
        /// </summary>
        public TimeSpan DefaultExpire
        {
            get
            {
                return TimeSpan.FromTicks(Interlocked.Read(ref _defaultExpire));
            }
            set
            {
                Interlocked.Exchange(ref _defaultExpire, value.Ticks);
            }
        }

        private volatile bool _autoPing;
        /// <summary>
        /// Her işlemde otomatik olarak <see cref="Ping"/> yapılsın mı?
        /// </summary>
        public bool AutoPing
        {
            get
            {
                return _autoPing;
            }
            set
            {
                _autoPing = value;
            }
        }

        /// <summary>
        /// Bir key'in expire süresini varsayılan expire süresine sıfırlar.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        protected virtual async Task<bool> PingAsync(string key)
        {
            var defaultExpire = DefaultExpire;
            if (defaultExpire == TimeSpan.Zero)
                return await Task.FromResult(false);
            using (new ProfileScope(this, key))
            {
                return await Provider.SetExpireAsync(key, defaultExpire);
            }
        }

        /// <summary>
        /// Bir key'in expire süresini varsayılan expire süresine sıfırlar.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        protected virtual bool Ping(string key)
        {
            var defaultExpire = DefaultExpire;
            if (defaultExpire == TimeSpan.Zero)
                return false;
            using (new ProfileScope(this, key))
            {
                return Provider.SetExpire(key, defaultExpire);
            }
        }

        /// <summary>
        /// Namespace'in ayracı
        /// </summary>
        public string NamespaceSeperator { get; private set; }

        /// <summary>
        /// Root namespace ile birleştirme işlemleri için ortak nokta.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        [DebuggerStepThrough]
        protected virtual string JoinWithRootNamespace(string path)
        {
            return string.IsNullOrEmpty(_rootNameSpace)
                ? path
                : string.Join(NamespaceSeperator, _rootNameSpace, path);
        }

        /// <summary>
        /// Root namespace ile birleştirme işlemleri için ortak nokta.
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        [DebuggerStepThrough]
        protected virtual string[] JoinWithRootNamespace(params string[] paths)
        {
            return JoinWithRootNamespace(paths.AsEnumerable());
        }

        /// <summary>
        /// Root namespace ile birleştirme işlemleri için ortak nokta.
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        [DebuggerStepThrough]
        protected virtual string[] JoinWithRootNamespace(IEnumerable<string> paths)
        {
            return paths.Select(JoinWithRootNamespace).ToArray();
        }

        /// <summary>
        /// Root namespace ile birleştirme işlemleri için ortak nokta.
        /// </summary>
        /// <param name="pathsWithValues"></param>
        /// <returns></returns>
        [DebuggerStepThrough]
        protected virtual IDictionary<string, T> JoinWithRootNamespace<T>(IDictionary<string, T> pathsWithValues)
        {
            return pathsWithValues.ToDictionary(kv => JoinWithRootNamespace(kv.Key), kv => kv.Value);
        }

        private static readonly byte[] _compressSignature = Encoding.ASCII.GetBytes("__Compressed_D__");
        private const CompressionLevel _compressionLevel = CompressionLevel.Optimal;

        private byte[] CompressIfNeeded(byte[] uncompressed)
        {
            if (_compressBiggerThan == null || uncompressed.Length < _compressBiggerThan)
                return uncompressed;

            using (var cs = new MemoryStream())
            {
                cs.Write(_compressSignature, 0, _compressSignature.Length);
                using (var ds = new DeflateStream(cs, _compressionLevel))
                {
                    ds.Write(uncompressed, 0, uncompressed.Length);
                    ds.Close();
                }

                return cs.ToArray();
            }
        }

        private byte[] DecompressIfNeeded(byte[] compressed)
        {
            if (compressed == null)
                return null;

            for (int i = 0; i < _compressSignature.Length; i++)
            {
                if (compressed[i] != _compressSignature[i])
                {
                    return compressed; //çünkü signature yok ve aslında uncompressed.
                }
            }

            var compressedData = new byte[compressed.Length - _compressSignature.Length];
            Array.Copy(compressed, _compressSignature.Length, compressedData, 0, compressedData.Length);
            using(var uncompressedStream = new MemoryStream())
            using (var compressedStream = new MemoryStream(compressedData))
            {
                using (var ds = new DeflateStream(compressedStream, CompressionMode.Decompress))
                {
                    ds.CopyTo(uncompressedStream);
                    ds.Close();

                    return uncompressedStream.ToArray();
                }
            }
        }

        /// <summary>
        /// Tek değeri deserialize etmek için yardımcı metod.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="result"></param>
        /// <returns></returns>
        [DebuggerStepThrough]
        protected T SingleResult<T>(byte[] result)
        {
            return result is T sameType ? sameType : _serializer.Deserialize<T>(DecompressIfNeeded(result));
        }

        /// <summary>
        /// Tek değeri deserialize etmek için yardımcı metod.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        [DebuggerStepThrough]
        protected object SingleResult(byte[] result, Type type)
        {
            return type == typeof(byte[]) ? result : _serializer.Deserialize(DecompressIfNeeded(result), type);
        }

        /// <summary>
        /// Dictionary olarak deserialize etmek için yardımcı metod.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="result"></param>
        /// <returns></returns>
        [DebuggerStepThrough]
        protected IDictionary<string, T> DictionaryResult<T>(IDictionary<string, byte[]> result)
        {
            if (result is IDictionary<string, T> sameType)
                return sameType;

            var dic = new Dictionary<string, T>();
            foreach (var kv in result)
            {
                dic[kv.Key] = _serializer.Deserialize<T>(DecompressIfNeeded(kv.Value));
            }
            return dic;
        }

        /// <summary>
        /// Dictionary olarak deserialize etmek için yardımcı metod.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="types"></param>
        /// <returns></returns>
        [DebuggerStepThrough]
        protected IDictionary<string, object> DictionaryResult(IDictionary<string, byte[]> result, IDictionary<string, Type> types)
        {
            var dic = new Dictionary<string, object>();
            foreach (var kv in result)
            {
                dic[kv.Key] = types[kv.Key] == typeof(byte[]) ? kv.Value : _serializer.Deserialize(DecompressIfNeeded(kv.Value), types[kv.Key]);
            }
            return dic;
        }

        /// <summary>
        /// List olarak deserialize etmek için yardımcı metod.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="result"></param>
        /// <returns></returns>
        [DebuggerStepThrough]
        protected IList<T> ListResult<T>(IList<byte[]> result)
        {
            if (result is IList<T> sameType)
                return sameType;

            var list = new List<T>();
            foreach (var item in result)
            {
                list.Add(_serializer.Deserialize<T>(DecompressIfNeeded(item)));
            }
            return list;
        }

        /// <summary>
        /// HashSet olarak deserialize etmek için yardımcı metod.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="result"></param>
        /// <returns></returns>
        [DebuggerStepThrough]
        protected HashSet<T> HashSetResult<T>(HashSet<byte[]> result)
        {
            if (result is HashSet<T> sameType)
                return sameType;

            var hashSet = new HashSet<T>();
            foreach (var item in result)
            {
                hashSet.Add(_serializer.Deserialize<T>(DecompressIfNeeded(item)));
            }
            return hashSet;
        }

        /// <summary>
        /// Provider'a parametreleri serialize edip yollamak için yardımcı metod.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        [DebuggerStepThrough]
        protected byte[] AsValue<T>(T value)
        {
            if (value is byte[] sameType)
                return sameType;

            if (typeof(T) == typeof(object))
                return CompressIfNeeded(_serializer.Serialize((object)value));
            return CompressIfNeeded(_serializer.Serialize(value));
        }

        /// <summary>
        /// Provider'a parametreleri serialize edip yollamak için yardımcı metod.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <returns></returns>
        [DebuggerStepThrough]
        protected byte[][] AsValues<T>(IList<T> values)
        {
            if (values is byte[][] sameType)
                return sameType;
            return values.Select(v => AsValue(v)).ToArray();
        }

        /// <summary>
        /// Provider'a parametreleri serialize edip yollamak için yardımcı metod.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="keyValues"></param>
        /// <returns></returns>
        [DebuggerStepThrough]
        protected IDictionary<string, byte[]> AsKeyValue<T>(IDictionary<string, T> keyValues)
        {
            var dic = new Dictionary<string, byte[]>();
            foreach (var kv in keyValues)
            {
                dic[kv.Key] = AsValue(kv.Value);
            }
            return dic;
        }

        internal abstract string TypeName { get; }
    }
}
