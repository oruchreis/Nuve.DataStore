using System;
using System.Collections.Concurrent;
using Ceras;

namespace Nuve.DataStore.Serializer.Ceras
{
    public class CerasDataStoreSerializer : IDataStoreSerializer
    {
        static CerasDataStoreSerializer()
        {
            CerasBufferPool.Pool = new CerasDefaultBufferPool();
        }

        private static readonly ConcurrentDictionary<SerializerConfig, ConcurrentQueue<CerasSerializer>> _serializerPools = new ConcurrentDictionary<SerializerConfig, ConcurrentQueue<CerasSerializer>>();

        public CerasDataStoreSerializer()
            : this(null)
        {

        }

        private readonly SerializerConfig _serializerConfig;
        private readonly ConcurrentQueue<CerasSerializer> _serializerPool = new ConcurrentQueue<CerasSerializer>();

        public CerasDataStoreSerializer(object staticSettings = null)
        {
            _serializerConfig = staticSettings as SerializerConfig ?? new SerializerConfig() { };
            _serializerPool = _serializerPools.GetOrAdd(_serializerConfig, config =>
            {
                return new ConcurrentQueue<CerasSerializer>();
            });
        }

        public virtual byte[] Serialize<T>(T objectToSerialize)
        {
            CerasSerializer serializer = null;
            try
            {
                if (!_serializerPool.TryDequeue(out serializer))
                {
                    serializer = new CerasSerializer(_serializerConfig);
                }

                return serializer.Serialize(objectToSerialize);
            }
            finally
            {
                if (serializer != null)
                    _serializerPool.Enqueue(serializer);
            }
        }

        public virtual T Deserialize<T>(byte[] serializedObject)
        {
            if (serializedObject == null)
                return default;


            CerasSerializer serializer = null;
            try
            {
                if (!_serializerPool.TryDequeue(out serializer))
                {
                    serializer = new CerasSerializer(_serializerConfig);
                }

                return serializer.Deserialize<T>(serializedObject);
            }
            finally
            {
                if (serializer != null)
                    _serializerPool.Enqueue(serializer);
            }
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
