using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace RethinkDb.Driver.Ast
{
    /// <summary>
    /// Extension  helpers for working with the AST.
    /// </summary>
    public static class ExtensionHelper
    {
        internal static JObject ToJObject(this object anonType)
        {
            return anonType == null ? null : JObject.FromObject(anonType);
        }

        internal static IDictionary<string, object> ToDict(this object anonType)
        {
            return anonType == null ? null : PropertyHelper.ObjectToDictionary(anonType);
        }
        /// <summary>
        /// Uses a collection as parameters for a method call.
        /// </summary>
        /// <param name="args">Same as calling params object[] overload. Instead of specifying each param, ICollection can be used for convenience.</param>
        public static object[] AsParams<T>(this ICollection<T> args)
        {
            return args.OfType<object>().ToArray();
        }
    }
}