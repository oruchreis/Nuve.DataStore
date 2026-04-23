namespace Nuve.DataStore.Internal;

internal readonly struct DataStoreConnectionContext
{
    public DataStoreConnectionContext(
        IDataStoreProvider provider,
        string rootNamespace,
        int? compressBiggerThan)
    {
        Provider = provider ?? throw new ArgumentNullException(nameof(provider));
        RootNamespace = rootNamespace ?? string.Empty;
        CompressBiggerThan = compressBiggerThan;
    }

    public IDataStoreProvider Provider { get; }

    public string RootNamespace { get; }

    public int? CompressBiggerThan { get; }
}