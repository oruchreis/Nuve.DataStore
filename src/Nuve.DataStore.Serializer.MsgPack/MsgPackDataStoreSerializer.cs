using System;
using System.IO;
using System.Text;
using MsgPack;
using MsgPack.Serialization;

namespace Nuve.DataStore.Serializer.MsgPack
{
    public class MsgPackDataStoreSerializer: IDataStoreSerializer
    {
        static MsgPackDataStoreSerializer()
        {
            SerializationContext.Default.DefaultDateTimeConversionMethod = DateTimeConversionMethod.Native;
        }

        public string Serialize<T>(T objectToSerialize)
        {
            var serializer = MessagePackSerializer.Get<T>();
            return Ascii85.Encode(serializer.PackSingleObject(objectToSerialize));           
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
