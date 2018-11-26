using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aerospike.Client;

namespace Nuve.DataStore.Aerospike
{
    public static class AerospikeHelper
    {
        public static Key ToKey(this string key, string @namespace)
        {
            var firstColonIndex = key.IndexOf(":", StringComparison.InvariantCultureIgnoreCase);
            if (firstColonIndex > -1)
                return new Key(@namespace, key.Substring(0, firstColonIndex), key.Substring(firstColonIndex+1));
            else
                return new Key(@namespace, "Default", key);
        }

        public static Key[] ToKeys(this string[] keys, string @namespace)
        {
            return keys.Select(k => k.ToKey(@namespace)).ToArray();
        }

        public static Bin ToBin(this string value, string name)
        {
            return new Bin(name, value);
        }

        public static Bin[] ToBins(this IDictionary<string, string> keyValues)
        {
            return keyValues.Select(kv => kv.Value.ToBin(kv.Key)).ToArray();
        }

        public static Bin[] ToBins(this string[] keys, string value)
        {
            return keys.Select(value.ToBin).ToArray();
        }

        public static Bin ToNullBin(this string name)
        {
            return Bin.AsNull(name);
        }

        public static Bin[] ToNullBins(this string[] names)
        {
            return names.Select(n => n.ToNullBin()).ToArray();
        }
    }
}
