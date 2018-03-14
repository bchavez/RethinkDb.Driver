using System.Reflection;
using RethinkDb.Driver.Net;
using Newtonsoft.Json.Serialization;

namespace RethinkDb.Driver.Utils
{
    /// <summary>
    /// Provides methods that assist in the translation of linq queries.
    /// </summary>
    public static class QueryHelper
    {
        /// <summary>
        /// Gets the name of the database field that corresponds to the given class member.
        /// The mapping can be configured by setting <see cref="Converter.Serializer"/> to a custom Serializer.
        /// </summary>
        public static string GetJsonMemberName(MemberInfo member)
        {
            if ( !(Converter.Serializer?.ContractResolver is DefaultContractResolver contractResolver) )
            {
                return member.Name;
            }

            var namingStrategy = contractResolver.NamingStrategy;
            if ( namingStrategy == null )
            {
                return member.Name;
            }

            return namingStrategy.GetPropertyName( member.Name, false );
        }
    }
}