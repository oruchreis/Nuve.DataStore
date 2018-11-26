using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using fastJSON;

namespace Nuve.DataStore.Serializer.FastJson
{
    public class FastJsonDataStoreSerializer: IDataStoreSerializer
    {
        private readonly JSONParameters _parameters;

        public FastJsonDataStoreSerializer()
            :this(null)
        {
            
        }

        public FastJsonDataStoreSerializer(object settings = null)
        {
            _parameters = (settings as JSONParameters) ?? new JSONParameters
                                                          {
                                                              ParametricConstructorOverride = true,
                                                              DateTimeMilliseconds = true,
                                                              ShowReadOnlyProperties = true
                                                          };
            JSON.RegisterCustomType(typeof(TimeSpan), data => ((TimeSpan)data).Ticks.ToString(), data => new TimeSpan(long.Parse(data)));
        }

        public string Serialize<T>(T objectToSerialize)
        {
            return JSON.ToJSON(objectToSerialize, _parameters);
        }

        public T Deserialize<T>(string serializedObject)
        {
            return JSON.ToObject<T>(serializedObject, _parameters);
        }

        public string Serialize(object objectToSerialize)
        {
            return JSON.ToJSON(objectToSerialize, _parameters);
        }

        public object Deserialize(string serializedObject, Type type)
        {
            return JSON.ToObject(serializedObject, type);
        }
    }
}
