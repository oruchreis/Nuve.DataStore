namespace Nuve.DataStore.Internal;

internal sealed class DataStoreSerializerRegistration
{
    public string Name { get; init; } = default!;

    public Type? SerializerType { get; init; }

    public IDataStoreSerializer? SerializerInstance { get; init; }
}
