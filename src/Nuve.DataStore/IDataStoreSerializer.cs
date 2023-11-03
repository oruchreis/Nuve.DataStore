namespace Nuve.DataStore;

/// <summary>
/// Represents a serializer interface that is used by DataStore
/// </summary>
public interface IDataStoreSerializer
{
    byte[] Serialize<T>(T? objectToSerialize);
    byte[] Serialize(object? objectToSerialize, Type type);
    T? Deserialize<T>(byte[]? serializedObject);
    object? Deserialize(byte[]? serializedObject, Type type);
}
