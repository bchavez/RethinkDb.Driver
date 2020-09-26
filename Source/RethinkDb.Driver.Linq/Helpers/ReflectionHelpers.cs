using System;
using System.Linq;
using System.Reflection;

namespace RethinkDb.Driver.Linq.Helpers
{
    internal static class ReflectionHelpers
    {
        public static bool HasAttribute<T>(this MemberInfo memberInfo) where T : Attribute
        {
            return memberInfo.CustomAttributes.Any(x => x.AttributeType == typeof(T));
        }
    }
}
