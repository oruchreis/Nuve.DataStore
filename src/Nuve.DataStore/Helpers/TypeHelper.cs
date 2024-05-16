using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Nuve.DataStore.Helpers;

internal static class TypeHelper
{
    /// <summary>
    /// Ensures correct printing of names in generic methods.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static string GetFriendlyName(this Type type)
    {
        var prefix = "";
        if (type.IsNested && !type.IsGenericParameter && type.DeclaringType != null)
            prefix = $"{type.DeclaringType.GetFriendlyName()}.";
        if (type == typeof(int))
            return $"{prefix}int";
        else if (type == typeof(short))
            return $"{prefix}short";
        else if (type == typeof(byte))
            return $"{prefix}byte";
        else if (type == typeof(bool))
            return $"{prefix}bool";
        else if (type == typeof(long))
            return $"{prefix}long";
        else if (type == typeof(float))
            return $"{prefix}float";
        else if (type == typeof(double))
            return $"{prefix}double";
        else if (type == typeof(decimal))
            return $"{prefix}decimal";
        else if (type == typeof(string))
            return $"{prefix}string";
        else if (type.GetTypeInfo().IsGenericType)
            return prefix + type.Name.Split('`')[0] + "<" +
                   string.Join(", ", type.GetGenericArguments().Select(GetFriendlyName).ToArray()) + ">";
        else
            return prefix + type.Name;
    }
}
