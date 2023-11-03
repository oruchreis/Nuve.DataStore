using System.Runtime.CompilerServices;

namespace Nuve.DataStore;

internal sealed class ProfileScope : IDisposable
{
    public ProfileScope(DataStoreBase store, string? key, [CallerMemberName]string? method = null)
    {
#if NET48
        DataStoreProfiler = store.Profiler;
        var parent = InternalProfileManager.Current;
        ParentScope = parent;
        InternalProfileManager.Current = this;

        method = string.Format("{0}.{1}", store.TypeName, method);

        object? localContext = null;
        object? globalContext = null;
        if (DataStoreProfiler != null)
        {
            localContext = DataStoreProfiler.Begin(method, key);
        }

        if (DataStoreManager.GlobalProfiler != null)
        {
            globalContext = DataStoreManager.GlobalProfiler.Begin(method, key);
        }

        ProfilerContext = new ProfilerContext(globalContext, localContext);
#endif
    }

    public ProfileScope? ParentScope { get; set; }
    public IDataStoreProfiler? DataStoreProfiler { get; set; }
    public ProfilerContext ProfilerContext { get; set; }
    private readonly List<DataStoreProfileResult> _profileResults = new();
    private readonly ReaderWriterLockSlim _profileResultsLocker = new();
    public void AddProfileResults(IEnumerable<DataStoreProfileResult> results)
    {
#if NET48
        _profileResultsLocker.EnterWriteLock();
        try
        {
            _profileResults.AddRange(results);
        }
        finally
        {
            _profileResultsLocker.ExitWriteLock();
        }
#endif
    }

    /// <summary>
    /// Bu scope'u bitirir.
    /// </summary>
    public void Dispose()
    {
#if NET48
        _profileResultsLocker.EnterReadLock();
        try
        {
            if (DataStoreProfiler != null && ProfilerContext.LocalContext != null)
            {
                InternalProfileManager.Current?.DataStoreProfiler?.Finish(ProfilerContext.LocalContext, _profileResults.ToArray());
            }

            if (DataStoreManager.GlobalProfiler != null && ProfilerContext.GlobalContext != null)
            {
                // _profileResults.ToArray() ile tekrardan array oluşturuyoruz, referans içermesin diye.
                DataStoreManager.GlobalProfiler.Finish(ProfilerContext.GlobalContext, _profileResults.ToArray());
            }
        }
        finally
        {
            _profileResultsLocker.ExitReadLock();
        }

        InternalProfileManager.Current = ParentScope;
#endif
    }
}
#if NET48
[Serializable]
internal sealed class ProfileScopeWrapper : MarshalByRefObject
{
    [NonSerialized]
    internal readonly ProfileScope Scope;

    internal ProfileScopeWrapper(ProfileScope scope)
    {
        Scope = scope;
    }
}
#endif
