using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetJSON;
using Netj = NetJSON.NetJSON;

namespace Nuve.DataStore.Serializer.NetJson
{
    public class NetJsonDataStoreSerializer: IDataStoreSerializer
    {
        static NetJsonDataStoreSerializer()
        {
            Netj.IncludeFields = true;
            Netj.CaseSensitive = true;
            Netj.DateFormat = NetJSONDateFormat.JsonNetISO;
            Netj.IncludeTypeInformation = true;
            Netj.TimeZoneFormat = NetJSONTimeZoneFormat.Local;
            Netj.UseEnumString = true;
        }

        public string Serialize<T>(T objectToSerialize)
        {
            return Netj.Serialize(objectToSerialize);
        }

        public T Deserialize<T>(string serializedObject)
        {
            return Netj.Deserialize<T>(serializedObject);
        }

        public string Serialize(object objectToSerialize)
        {
            return Netj.Serialize(objectToSerialize);
        }

        public object Deserialize(string serializedObject, Type type)
        {
            return Netj.Deserialize(type, serializedObject);
        }
    }
}
