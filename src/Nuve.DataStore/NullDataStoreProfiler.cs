namespace Nuve.DataStore;

internal class NullDataStoreProfiler: IDataStoreProfiler
{
    public object? Begin(string method, string? key)
    {
        return null;
    }

    public void Finish(object? context, params DataStoreProfileResult[] results)
    {
        //Intentionally left blank
    }

    public object? GetContext()
    {
        return null;
    }
}
