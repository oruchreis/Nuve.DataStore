using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Hadoop.Avro;

namespace Nuve.DataStore.Serializer.Avro
{
    public class AvroDataStoreSerializer: IDataStoreSerializer
    {
        public string Serialize<T>(T objectToSerialize)
        {
            var serializer = AvroSerializer.Create<T>();
            using (var ms = new MemoryStream())
            {
                serializer.Serialize(ms, objectToSerialize);
                ms.Seek(0, SeekOrigin.Begin);
                return Ascii85.Encode(ms.ToArray());
            }
        }

        public T Deserialize<T>(string serializedObject)
        {
            var serializer = AvroSerializer.Create<T>();
            using (var ms = new MemoryStream(Ascii85.Decode(serializedObject)))
            {
                return serializer.Deserialize(ms);
            }
        }

        private static readonly ConcurrentDictionary<Type, MethodInfo> _serializerCreateMethods = new ConcurrentDictionary<Type, MethodInfo>();

        public string Serialize(object objectToSerialize)
        {
            var serializer = _serializerCreateMethods.GetOrAdd(objectToSerialize.GetType(), t => typeof(AvroSerializer).GetMethod("Create").MakeGenericMethod(t)).Invoke(null, null);
            using (var ms = new MemoryStream())
            {
                serializer.GetType().GetMethod("Serialize").Invoke(serializer, new[] {ms, objectToSerialize});
                ms.Seek(0, SeekOrigin.Begin);
                return Ascii85.Encode(ms.ToArray());
            }
        }

        public object Deserialize(string serializedObject, Type type)
        {
            var serializer = _serializerCreateMethods.GetOrAdd(type, t => typeof(AvroSerializer).GetMethod("Create").MakeGenericMethod(t)).Invoke(null, null);
            using (var ms = new MemoryStream(Ascii85.Decode(serializedObject)))
            {
                return serializer.GetType().GetMethod("Deserialize").Invoke(serializer, new object[] {ms});
            }
        }
    }
}
