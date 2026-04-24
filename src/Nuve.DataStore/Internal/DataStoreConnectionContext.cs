namespace Nuve.DataStore.Internal;

internal readonly struct DataStoreConnectionContext
{
    public DataStoreConnectionContext(
        IDataStoreProvider provider,
        IDataStoreSerializer serializer,
        string rootNamespace,
        int? compressBiggerThan)
    {
        Provider = provider ?? throw new ArgumentNullException(nameof(provider));
        Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        RootNamespace = rootNamespace ?? string.Empty;
        CompressBiggerThan = compressBiggerThan;
    }

    public IDataStoreProvider Provider { get; }

    public IDataStoreSerializer Serializer { get; }

    public string RootNamespace { get; }

    public int? CompressBiggerThan { get; }
}
