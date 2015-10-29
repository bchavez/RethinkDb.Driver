using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace RethinkDb.Driver.Ast
{
    internal static class ExtensionHelper
    {
        public static JObject ToJObject(this object anonType)
        {
            return anonType == null ? null : JObject.FromObject(anonType);
        }

        public static IDictionary<string, object> ToDict(this object anonType)
        {
            return anonType == null ? null : PropertyHelper.ObjectToDictionary(anonType);
        }
    }
}
