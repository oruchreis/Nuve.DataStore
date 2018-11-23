using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nuve.DataStore
{
    /// <summary>
    /// Represents a serializer interface that is used by DataStore
    /// </summary>
    public interface IDataStoreSerializer
    {
        byte[] Serialize<T>(T objectToSerialize);
        T Deserialize<T>(byte[] serializedObject);
        byte[] Serialize(object objectToSerialize);
        object Deserialize(byte[] serializedObject, Type type);
    }
}
