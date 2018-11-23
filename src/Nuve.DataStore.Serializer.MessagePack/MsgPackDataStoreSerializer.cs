using MessagePack;
using System;

namespace Nuve.DataStore.Serializer.MessagePack
{
    public class MessagePackDataStoreSerializer: IDataStoreSerializer
    {
        static MessagePackDataStoreSerializer()
        {
            MessagePackSerializer.SetDefaultResolver(global::MessagePack.Resolvers.TypelessContractlessStandardResolver.Instance);
        }

        public string Serialize<T>(T objectToSerialize)
        {
              return LZ4MessagePackSerializer.Serialize(objectToSerialize);
        }

        public T Deserialize<T>(string serializedObject)
        {
            var serializer = MessagePackSerializer.Get<T>();
            return serializer.UnpackSingleObject(Ascii85.Decode(serializedObject));
        }

        public string Serialize(object objectToSerialize)
        {
            var serializer = MessagePackSerializer.Get(objectToSerialize.GetType());
            return Ascii85.Encode(serializer.PackSingleObject(objectToSerialize));
        }

        public object Deserialize(string serializedObject, Type type)
        {
            var serializer = MessagePackSerializer.Get(type);
            return serializer.UnpackSingleObject(Ascii85.Decode(serializedObject)); 
        }
    }
}
