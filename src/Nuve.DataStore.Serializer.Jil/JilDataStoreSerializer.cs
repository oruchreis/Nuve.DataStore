using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jil;

namespace Nuve.DataStore.Serializer.Jil
{
    public class JilDataStoreSerializer : IDataStoreSerializer
    {
        private readonly Options _options;

        public JilDataStoreSerializer()
            :this(null)
        {
        }

        public JilDataStoreSerializer(object options = null)
        {
            _options = (options as Options) ?? new Options(includeInherited: true, dateFormat: DateTimeFormat.ISO8601);
        }

        public string Serialize<T>(T objectToSerialize)
        {
            return JSON.SerializeDynamic(objectToSerialize, _options);
        }

        public T Deserialize<T>(string serializedObject)
        {
            return JSON.Deserialize<T>(serializedObject, _options);
        }

        public string Serialize(object objectToSerialize)
        {
            return JSON.SerializeDynamic(objectToSerialize, _options);
        }

        public object Deserialize(string serializedObject, Type type)
        {
            return JSON.Deserialize(serializedObject, type, _options);
        }
    }
}
