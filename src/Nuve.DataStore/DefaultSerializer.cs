using System;
using System.Linq;
using System.Reflection;

namespace Nuve.DataStore
{
    internal class DefaultSerializer : IDataStoreSerializer
    {
        private readonly MethodInfo _serializeMethod;
        private readonly MethodInfo _deserializeMethod;
        public DefaultSerializer()
        {
            var type = Type.GetType("Newtonsoft.Json.JsonConvert,Newtonsoft.Json", false) ??
                throw new InvalidOperationException("Couldn't find default serializer. " +
                                    "Set a default serializer to dataStore/defaultSerializer in the config or " +
                                    "set the DefaultSerializer property of DataStoreManager once.");

            _serializeMethod = type.GetMethods() //SerializeObject(Object, Type, JsonSerializerSettings)
                .Where(m => m.Name == "SerializeObject")
                .Select(m => new
                {
                    Method = m,
                    Params = m.GetParameters()
                })
                .Where(x => x.Params.Length == 3
                            && x.Params[1].ParameterType == typeof(Type))
                .Select(x => x.Method)
                .First();
            _deserializeMethod = type.GetMethods() //DeserializeObject(String, Type)
                .Where(m => m.Name == "DeserializeObject")
                .Select(m => new
                {
                    Method = m,
                    Params = m.GetParameters(),
                    Args = m.GetGenericArguments()
                })
                .Where(x => x.Params.Length == 2
                            && x.Args.Length == 0
                            && x.Params[1].ParameterType == typeof(Type))
                .Select(x => x.Method)
                .First();
        }

        public byte[] Serialize<T>(T? objectToSerialize)
        {
            return (byte[])_serializeMethod.Invoke(null, new object?[] { objectToSerialize, typeof(T) });
        }

        public T? Deserialize<T>(byte[]? serializedObject)
        {
            return (T)_deserializeMethod.Invoke(null, new object?[] { serializedObject, typeof(T), null });
        }

        public byte[] Serialize(object? objectToSerialize, Type type)
        {
            return (byte[])_serializeMethod.Invoke(null, new[] { objectToSerialize, type });
        }

        public object? Deserialize(byte[]? serializedObject, Type type)
        {
            return _deserializeMethod.Invoke(null, new object?[] { serializedObject, type, null });
        }
    }
}
