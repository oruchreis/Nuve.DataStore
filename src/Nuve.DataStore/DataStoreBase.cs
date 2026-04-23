using Nuve.DataStore.Internal;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Nuve.DataStore;

/// <summary>
/// Base class for DataStore structures.
/// </summary>
public abstract class DataStoreBase
{
    private readonly IDataStoreSerializer? _serializerOverride;
    private readonly IDataStoreCompressor? _compressorOverride;
    private readonly string? _connectionName;
    private readonly string? _overrideRootNamespace;
    private readonly int? _overrideCompressBiggerThan;

    private DataStoreConnectionContext? _context;

    internal readonly IDataStoreProfiler? Profiler;

    /// <summary>
    /// Base class for all DataStore structures.
    /// </summary>
    /// <param name="connectionName">Connection name defined in the configuration or code registration.</param>
    /// <param name="defaultExpire">Default expiration time.</param>
    /// <param name="autoPing">Should Ping be automatically called for each operation?</param>
    /// <param name="namespaceSeperator">Separator used to separate namespaces. Default is ":".</param>
    /// <param name="overrideRootNamespace">Used to override the root namespace defined in the connection.</param>
    /// <param name="serializer">Set this if you want to use a different serializer instead of the default one.</param>
    /// <param name="compressor">Set this if you want to use a different compressor instead of the default one.</param>
    /// <param name="profiler">Used to profile only the methods of this data store. The global profiler is used whether it is set or not.</param>
    /// <param name="compressBiggerThan">Overrides the connection compression threshold.</param>
    protected DataStoreBase(
        string? connectionName = null,
        TimeSpan? defaultExpire = null,
        bool autoPing = false,
        string? namespaceSeperator = null,
        string? overrideRootNamespace = null,
        IDataStoreSerializer? serializer = null,
        IDataStoreCompressor? compressor = null,
        IDataStoreProfiler? profiler = null,
        int? compressBiggerThan = null)
    {
        _connectionName = connectionName;
        _overrideRootNamespace = overrideRootNamespace;
        _overrideCompressBiggerThan = compressBiggerThan;
        _serializerOverride = serializer;
        _compressorOverride = compressor;

        DefaultExpire = defaultExpire ?? TimeSpan.Zero;
        AutoPing = autoPing;
        NamespaceSeperator = namespaceSeperator ?? ":";
        Profiler = profiler;
    }

    /// <summary>
    /// Default expiration time.
    /// </summary>
    public TimeSpan DefaultExpire { get; }

    private volatile bool _autoPing;
    /// <summary>
    /// Should <see cref="Ping"/> be automatically called for each operation?
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

    public string NamespaceSeperator { get; }

    protected IDataStoreSerializer Serializer =>
        _serializerOverride ?? DataStoreRuntime.Manager.DefaultSerializer;

    protected IDataStoreCompressor Compressor =>
        _compressorOverride ?? DataStoreRuntime.Manager.DefaultCompressor;

    protected IDataStoreProvider Provider => GetContext().Provider;

    protected string RootNamespace => GetContext().RootNamespace;

    protected int? CompressBiggerThan => GetContext().CompressBiggerThan;
    
    /// <summary>
    /// Resets the expiration time of a key to the default expiration time.
    /// </summary>
    /// <param name="key">The key to reset the expiration time for.</param>
    /// <returns>Returns a task that represents the asynchronous operation. The task result is a boolean value indicating whether the expiration time was successfully reset.</returns>
    protected virtual async Task<bool> PingAsync(string key)
    {
        if (DefaultExpire == TimeSpan.Zero)
            return await Task.FromResult(false).ConfigureAwait(false);

        using (new ProfileScope(this, key))
        {
            return await Provider.SetExpireAsync(key, DefaultExpire).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Resets the expiration time of a key to the default expiration time.
    /// </summary>
    /// <param name="key">The key to reset the expiration time for.</param>
    /// <returns>Returns a boolean value indicating whether the expiration time was successfully reset.</returns>
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
    /// Common point for joining with the root namespace.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    [DebuggerStepThrough]
    protected virtual string JoinWithRootNamespace(string path)
    {
        var rootNamespace = RootNamespace;
        return string.IsNullOrEmpty(rootNamespace)
            ? path
            : string.Join(NamespaceSeperator, rootNamespace, path);
    }

    /// <summary>
    /// Common point for joining with the root namespace.
    /// </summary>
    /// <param name="paths"></param>
    /// <returns></returns>
    [DebuggerStepThrough]
    protected virtual string[] JoinWithRootNamespace(params string[] paths)
    {
        return JoinWithRootNamespace(paths.AsEnumerable());
    }

    /// <summary>
    /// Common point for joining with the root namespace.
    /// </summary>
    /// <param name="paths"></param>
    /// <returns></returns>
    [DebuggerStepThrough]
    protected virtual string[] JoinWithRootNamespace(IEnumerable<string> paths)
    {
        return paths.Select(JoinWithRootNamespace).ToArray();
    }

    /// <summary>
    /// Common point for joining with the root namespace.
    /// </summary>
    /// <param name="pathsWithValues"></param>
    /// <returns></returns>
    [DebuggerStepThrough]
    protected virtual IDictionary<string, T> JoinWithRootNamespace<T>(IDictionary<string, T> pathsWithValues)
    {
        return pathsWithValues.ToDictionary(kv => JoinWithRootNamespace(kv.Key), kv => kv.Value);
    }

    private DataStoreConnectionContext GetContext()
    {
        var cached = _context;
        if (cached.HasValue)
            return cached.Value;

        var context = DataStoreRuntime.Manager.GetConnection(_connectionName);

        if (_overrideRootNamespace != null || _overrideCompressBiggerThan.HasValue)
        {
            context = new DataStoreConnectionContext(
                context.Provider,
                _overrideRootNamespace ?? context.RootNamespace,
                _overrideCompressBiggerThan ?? context.CompressBiggerThan);
        }

        _context = context;
        return context;
    }

    private byte[] CompressIfNeeded(byte[] uncompressed)
    {
        var compressBiggerThan = CompressBiggerThan;
        var compressor = Compressor;

        if (compressBiggerThan == null || uncompressed.Length < compressBiggerThan)
            return uncompressed;

        using var compressedStream = new MemoryStream();
        compressedStream.Write(compressor.Signature, 0, compressor.Signature.Length);
        compressor.Compress(compressedStream, uncompressed);
        return compressedStream.ToArray();
    }

    private byte[] DecompressIfNeeded(byte[] compressed)
    {
        if (compressed == null || compressed.Length == 0)
            return Array.Empty<byte>();

        var compressor = Compressor;

#if NET48
        for (int i = 0; i < compressor.Signature.Length; i++)
        {
            if (compressed[i] != compressor.Signature[i])
            {
                return compressed;
            }
        }

        var compressedData = new byte[compressed.Length - compressor.Signature.Length];
        Array.Copy(compressed, compressor.Signature.Length, compressedData, 0, compressedData.Length);
#else
        var compressedSpanWithSignature = compressed.AsSpan();
        if (compressedSpanWithSignature.Length < compressor.Signature.Length ||
            !compressedSpanWithSignature[..compressor.Signature.Length].SequenceEqual(compressor.Signature.AsSpan()))
        {
            return compressed;
        }

        var compressedData = compressedSpanWithSignature[compressor.Signature.Length..].ToArray();
#endif

        using var uncompressedStream = new MemoryStream();
        compressor.Decompress(uncompressedStream, compressedData);
        return uncompressedStream.ToArray();
    }

    /// <summary>
    /// Helper method for deserializing a single value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="result"></param>
    /// <returns></returns>
    [DebuggerStepThrough]
    protected T? SingleResult<T>(byte[] result)
    {
        return result is T sameType
            ? sameType
            : Serializer.Deserialize<T>(DecompressIfNeeded(result));
    }

    /// <summary>
    /// Helper method for deserializing a single value.
    /// </summary>
    /// <param name="result"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    [DebuggerStepThrough]
    protected object? SingleResult(byte[] result, Type type)
    {
        return type == typeof(byte[])
            ? result
            : Serializer.Deserialize(DecompressIfNeeded(result), type);
    }

    /// <summary>
    /// Helper method for deserializing a dictionary.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="result"></param>
    /// <returns></returns>
    [DebuggerStepThrough]
    protected IDictionary<string, T?> DictionaryResult<T>(IDictionary<string, byte[]> result)
    {
        if (result is IDictionary<string, T?> sameType)
            return sameType;

        var dic = new Dictionary<string, T?>();
        foreach (var kv in result)
        {
            dic[kv.Key] = Serializer.Deserialize<T>(DecompressIfNeeded(kv.Value));
        }
        return dic;
    }

    /// <summary>
    /// Helper method for deserializing a dictionary.
    /// </summary>
    /// <param name="result">The dictionary to deserialize.</param>
    /// <param name="types">The types of the values in the dictionary.</param>
    /// <returns>Returns a dictionary with deserialized values.</returns>
    [DebuggerStepThrough]
    protected IDictionary<string, object?> DictionaryResult(IDictionary<string, byte[]> result, IDictionary<string, Type> types)
    {
        var dic = new Dictionary<string, object?>();
        foreach (var kv in result)
        {
            dic[kv.Key] = types[kv.Key] == typeof(byte[]) ? kv.Value : Serializer.Deserialize(DecompressIfNeeded(kv.Value), types[kv.Key]);
        }
        return dic;
    }

    /// <summary>
    /// Helper method for deserializing a list.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the list.</typeparam>
    /// <param name="result">The list to deserialize.</param>
    /// <returns>Returns a list with deserialized elements.</returns>
    [DebuggerStepThrough]
    protected IList<T?> ListResult<T>(IList<byte[]> result)
    {
        if (result is IList<T?> sameType)
            return sameType;

        var list = new List<T?>();
        foreach (var item in result)
        {
            list.Add(Serializer.Deserialize<T>(DecompressIfNeeded(item)));
        }
        return list;
    }

    /// <summary>
    /// Helper method for deserializing a HashSet.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the HashSet.</typeparam>
    /// <param name="result">The HashSet to deserialize.</param>
    /// <returns>Returns a HashSet with deserialized elements.</returns>
    [DebuggerStepThrough]
    protected HashSet<T?> HashSetResult<T>(HashSet<byte[]> result)
    {
        if (result is HashSet<T?> sameType)
            return sameType;

        var hashSet = new HashSet<T?>();
        foreach (var item in result)
        {
            hashSet.Add(Serializer.Deserialize<T>(DecompressIfNeeded(item)));
        }
        return hashSet;
    }

    /// <summary>
    /// Helper method for serializing a value to be sent to the Provider.
    /// </summary>
    /// <typeparam name="T">The type of the value to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <returns>Returns the serialized value as a byte array.</returns>
    [DebuggerStepThrough]
    protected byte[] AsValue<T>(T? value)
    {
        if (value is byte[] sameType)
            return sameType;

        if (typeof(T) == typeof(object))
            return CompressIfNeeded(Serializer.Serialize((object?)value));
        return CompressIfNeeded(Serializer.Serialize(value));
    }

    /// <summary>
    /// Helper method for serializing and sending parameters to the Provider.
    /// </summary>
    /// <typeparam name="T">The type of the values to serialize.</typeparam>
    /// <param name="values">The values to serialize.</param>
    /// <returns>Returns a two-dimensional byte array containing the serialized values.</returns>
    [DebuggerStepThrough]
    protected byte[][] AsValues<T>(IList<T?> values)
    {
        if (values is byte[][] sameType)
            return sameType;
        return values.Select(v => AsValue(v)).ToArray();
    }

    /// <summary>
    /// Helper method for serializing and sending parameters to the Provider.
    /// </summary>
    /// <typeparam name="T">The type of the values to serialize.</typeparam>
    /// <param name="keyValues">The key-value pairs to serialize.</param>
    /// <param name="serializeParallel">Indicates whether to serialize the key-value pairs in parallel.</param>
    /// <param name="parallelOptions">Options for parallel execution, such as the maximum degree of parallelism.</param>
    /// <returns>Returns a dictionary where the keys are strings and the values are byte arrays containing the serialized values.</returns>
    [DebuggerStepThrough]
    protected IDictionary<string, byte[]> AsKeyValue<T>(IDictionary<string, T?> keyValues, bool serializeParallel = false, ParallelOptions? parallelOptions = null)
    {
        if (serializeParallel)
        {
            var dic = new ConcurrentDictionary<string, byte[]>();
            Parallel.ForEach(keyValues, parallelOptions ?? new ParallelOptions(), kv =>
            {
                dic[kv.Key] = AsValue(kv.Value);
            });
            return dic;
        }
        else
        {
            var dic = new Dictionary<string, byte[]>();
            foreach (var kv in keyValues)
            {
                dic[kv.Key] = AsValue(kv.Value);
            }
            return dic;
        }
    }

    internal abstract string TypeName { get; }
}
