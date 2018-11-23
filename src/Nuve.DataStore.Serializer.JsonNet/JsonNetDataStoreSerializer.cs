using System;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;

namespace Nuve.DataStore.Serializer.JsonNet
{
    public class JsonNetDataStoreSerializer : IDataStoreSerializer
    {
        protected readonly JsonSerializerSettings Settings;

        public JsonNetDataStoreSerializer()
            :this(null)
        {
            
        }

        public JsonNetDataStoreSerializer(object settings = null)
        {
            Settings = (settings as JsonSerializerSettings) ??
                                      new JsonSerializerSettings
                                      {
                                          TypeNameHandling = TypeNameHandling.Auto, //bunun ile $type etiketi eklenip polimorfik objelere izin veriliyor.
                                          ObjectCreationHandling = ObjectCreationHandling.Replace, // bu olmazsa ctor'daki default değerlere ekleme yapar.
                                          ContractResolver = new NoConstructorCreationContractResolver(),
                                          ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                                          PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                                          
                                      };
        }

        public virtual byte[] Serialize<T>(T objectToSerialize)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(objectToSerialize, Settings));
        }

        public virtual T Deserialize<T>(byte[] serializedObject)
        {
            if (serializedObject == null)
                return default(T);
            var str = Encoding.UTF8.GetString(serializedObject);
            if (string.IsNullOrEmpty(str))
                return default(T);
            return JsonConvert.DeserializeObject<T>(str, Settings);
        }

        public virtual byte[] Serialize(object objectToSerialize)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(objectToSerialize, Settings));
        }

        public virtual object Deserialize(byte[] serializedObject, Type type)
        {
            if (serializedObject == null)
                return type.IsValueType ? Activator.CreateInstance(type) : null;
            var str = Encoding.UTF8.GetString(serializedObject);
            if (string.IsNullOrEmpty(str))
                return type.IsValueType ? Activator.CreateInstance(type) : null;
            return JsonConvert.DeserializeObject(str, type, Settings);
        }
    }
}
