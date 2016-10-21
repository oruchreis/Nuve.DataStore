using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Nuve.DataStore
{
    internal class DefaultSerializer: IDataStoreSerializer
    {
        private readonly MethodInfo _serializeMethod;
        private readonly MethodInfo _deserializeMethod;
        public DefaultSerializer()
        {
            var type = Type.GetType("Newtonsoft.Json.JsonConvert,Newtonsoft.Json", false);
            if (type == null)
                throw new Exception("Couldn't find default serializer. " +
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
                            && x.Params[1].ParameterType == typeof (Type))
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
                            && x.Args.Length == 1
                            && x.Params[1].ParameterType == typeof (Type))
                .Select(x => x.Method)
                .First();
        }

        public string Serialize<T>(T objectToSerialize)
        {
            return (string) _serializeMethod.Invoke(null, new object[] {objectToSerialize, typeof (T)});
        }

        public T Deserialize<T>(string serializedObject)
        {
            return (T) _deserializeMethod.Invoke(null, new object[] {serializedObject, typeof (T), null});
        }

        public string Serialize(object objectToSerialize)
        {
            return (string) _serializeMethod.Invoke(null, new object[] {objectToSerialize, objectToSerialize.GetType()});
        }

        public object Deserialize(string serializedObject, Type type)
        {
            return _deserializeMethod.Invoke(null, new object[] {serializedObject, type, null});
        }
    }
}
