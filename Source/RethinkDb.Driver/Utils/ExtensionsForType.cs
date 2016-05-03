using System;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace RethinkDb.Driver.Utils
{
    internal static class ExtensionsForType
    {
        public static bool IsASubclassOf(this Type type, Type other)
        {
            return type.GetTypeInfo().IsSubclassOf(other);
        }
        public static bool IsJToken(this Type type)
        {
            return type.IsASubclassOf(typeof(JToken));
        }

        public static bool IsGenericType(this Type type)
        {
            return type.GetTypeInfo().IsGenericType;
        }

        public static Type BaseType(this Type type)
        {
            return type.GetTypeInfo().BaseType;
        }
    }
}