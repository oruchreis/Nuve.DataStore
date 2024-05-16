namespace Nuve.DataStore;

/// <summary>
/// Intermediate profiler set for the provider.
/// All methods here are called in each provider method.
/// This class triggers global and local profilers using the current scope's context.
/// </summary>
internal class InternalProfilerProxy : IDataStoreProfiler
{
    public object? Begin(string method, string? key)
    {
#if EXPERIMENTAL
        object localContext = null;
        object globalContext = null;
        if (InternalProfileManager.Current != null && InternalProfileManager.Current.DataStoreProfiler != null)
        {
            localContext = InternalProfileManager.Current.DataStoreProfiler.Begin(method, key);
        }

        if (DataStoreManager.GlobalProfiler != null)
        {
            globalContext = DataStoreManager.GlobalProfiler.Begin(method, key);                
        }

        return new ProfilerContext(globalContext, localContext);
#endif

#if NET48
        return InternalProfileManager.Current?.ProfilerContext;
#else
        return new object();
#endif
    }

    public void Finish(object? context, params DataStoreProfileResult[] results)
    {
#if EXPERIMENTAL
        if (InternalProfileManager.Current != null && InternalProfileManager.Current.DataStoreProfiler != null)
        {
            InternalProfileManager.Current.DataStoreProfiler.Finish(((ProfilerContext)context).LocalContext, results);
        }

        if (DataStoreManager.GlobalProfiler != null)
        {
            DataStoreManager.GlobalProfiler.Finish(((ProfilerContext)context).GlobalContext, results);
        }
#endif
#if NET48
        InternalProfileManager.Current?.AddProfileResults(results);
#endif
    }

    public object? GetContext()
    {
#if EXPERIMENTAL
        object localContext = null;
        object globalContext = null;
        if (InternalProfileManager.Current != null && InternalProfileManager.Current.DataStoreProfiler != null)
        {
            localContext = InternalProfileManager.Current.DataStoreProfiler.GetContext();
        }

        if (DataStoreManager.GlobalProfiler != null)
        {
            globalContext = DataStoreManager.GlobalProfiler.GetContext();
        }

        return new ProfilerContext(globalContext, localContext);
#endif
#if NET48
        return InternalProfileManager.Current?.ProfilerContext;
#else
        return new object();
#endif
    }
}
