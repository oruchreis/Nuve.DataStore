using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Nuve.DataStore.Serializer.JsonNet
{
    public class BsonNetDataStoreSerializer: JsonNetDataStoreSerializer
    {
        public override byte[] Serialize(object objectToSerialize)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BsonDataWriter(ms))
            {
                var serializer = JsonSerializer.Create(Settings);
                serializer.Serialize(writer, objectToSerialize);
                return ms.ToArray();
            }
        }

        public override byte[] Serialize<T>(T objectToSerialize)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BsonDataWriter(ms))
            {
                var serializer = JsonSerializer.Create(Settings);
                serializer.Serialize(writer, objectToSerialize);
                return ms.ToArray();
            }
        }

        public override object Deserialize(byte[] serializedObject, Type type)
        {
            if (serializedObject == null)
                return type.IsValueType ? Activator.CreateInstance(type) : null;
            using (var ms = new MemoryStream(serializedObject))
            using (var reader = new BsonDataReader(ms))
            {
                var serializer = JsonSerializer.Create(Settings);
                return serializer.Deserialize(reader, type);
            }
        }

        public override T Deserialize<T>(byte[] serializedObject)
        {
            if (serializedObject == null)
                return default(T);
            using (var ms = new MemoryStream(serializedObject))
            using (var reader = new BsonDataReader(ms))
            {
                var serializer = JsonSerializer.Create(Settings);
                return serializer.Deserialize<T>(reader);
            }
        }
    }
}
