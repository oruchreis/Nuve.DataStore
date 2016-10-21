using System;
using Newtonsoft.Json;

namespace Nuve.DataStore.Serializer.JsonNet
{
    public class JsonNetDataStoreSerializer : IDataStoreSerializer
    {
        private readonly JsonSerializerSettings _jsonSerializerSettings;

        public JsonNetDataStoreSerializer()
            :this(null)
        {
            
        }

        public JsonNetDataStoreSerializer(object settings = null)
        {
            _jsonSerializerSettings = (settings as JsonSerializerSettings) ??
                                      new JsonSerializerSettings
                                      {
                                          TypeNameHandling = TypeNameHandling.Auto, //bunun ile $type etiketi eklenip polimorfik objelere izin veriliyor.
                                          ObjectCreationHandling = ObjectCreationHandling.Replace, // bu olmazsa ctor'daki default değerlere ekleme yapar.
                                          ContractResolver = new NoConstructorCreationContractResolver()
                                      };
        }

        public string Serialize<T>(T objectToSerialize)
        {
            return JsonConvert.SerializeObject(objectToSerialize, _jsonSerializerSettings);
        }

        public T Deserialize<T>(string serializedObject)
        {
            if (string.IsNullOrEmpty(serializedObject))
                return default(T);
            return JsonConvert.DeserializeObject<T>(serializedObject, _jsonSerializerSettings);
        }

        public string Serialize(object objectToSerialize)
        {
            return JsonConvert.SerializeObject(objectToSerialize, _jsonSerializerSettings);
        }

        public object Deserialize(string serializedObject, Type type)
        {
            if (string.IsNullOrEmpty(serializedObject))
            {
                if (type.IsValueType)
                {
                    return Activator.CreateInstance(type);
                }
                return null;
            }
            return JsonConvert.DeserializeObject(serializedObject, type, _jsonSerializerSettings);
        }
    }
}
