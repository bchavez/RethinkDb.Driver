using Newtonsoft.Json.Linq;

namespace RethinkDb.Driver.Ast
{
    internal static class ExtensionHelper
    {
        public static JObject ToJObject(this object anonType)
        {
            return anonType == null ? null : JObject.FromObject(anonType);
        }
    }
}
