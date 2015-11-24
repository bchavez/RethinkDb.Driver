using System;

#if DNX
using System.Reflection;
#endif

namespace RethinkDb.Driver.Utils
{
    internal static class ExtensionsForType
    {
#if DNX
        public static bool IsSubclassOf(this Type type, Type other)
        {
            return type.GetTypeInfo().IsSubclassOf(other);
        }
#endif

        public static bool IsGenericType(this Type type)
        {
#if DNX
            return type.GetTypeInfo().IsGenericType;
#else
            return type.IsGenericType;
#endif
        }
    }
}