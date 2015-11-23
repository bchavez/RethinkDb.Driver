using System;
using System.Reflection;
using System.Threading.Tasks;

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

    public static class ExtensionsForTask
    {
        public static T RunSync<T>(this Task<T> task)
        {
            task.Wait();
            return task.Result;
        }
    }
}