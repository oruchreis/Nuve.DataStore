using System;
using Ceras;

namespace Nuve.DataStore.Serializer.Ceras
{
    public class CerasDataStoreSerializer : IDataStoreSerializer
    {
        static CerasDataStoreSerializer()
        {
           CerasBufferPool.Pool = new CerasDefaultBufferPool();
        }

        public CerasDataStoreSerializer()
            : this(null)
        {

        }

        protected readonly SerializerConfig Settings;

        public CerasDataStoreSerializer(object settings = null)
        {
            Settings = settings as SerializerConfig;
        }

        public virtual byte[] Serialize<T>(T objectToSerialize)
        {
            var serializer = new CerasSerializer(Settings);
            return serializer.Serialize(objectToSerialize);
        }

        public virtual T Deserialize<T>(byte[] serializedObject)
        {
            if (serializedObject == null)
                return default(T);
            var serializer = new CerasSerializer(Settings);
            return serializer.Deserialize<T>(serializedObject);
        }

        public virtual byte[] Serialize(object objectToSerialize)
        {
            return Serialize<object>(objectToSerialize);
        }

        public virtual object Deserialize(byte[] serializedObject, Type type)
        {
            return Deserialize<object>(serializedObject);
        }
    }
}
