using Nuve.DataStore.Internal;

namespace Nuve.DataStore;

public static class DataStoreRuntime
{
    private static DataStoreManager? _manager;

    public static bool IsInitialized => Volatile.Read(ref _manager) != null;

    public static DataStoreManager Manager =>
        Volatile.Read(ref _manager)
        ?? throw new InvalidOperationException(
            "The data store runtime has not been initialized. Call InitializeDataStore() or InitializeDataStoreAsync() after building the service provider and before creating any data store instances.");

    internal static void Initialize(DataStoreManager manager)
    {
        ThrowHelper.ThrowIfNull(manager);

        var existing = Interlocked.CompareExchange(ref _manager, manager, null);

        if (existing != null && !ReferenceEquals(existing, manager))
        {
            throw new InvalidOperationException(
                "The data store runtime has already been initialized with a different manager instance.");
        }
    }

#if TEST
    internal static void ResetForTests()
    {
        Volatile.Write(ref _manager, null);
    }
#endif
}