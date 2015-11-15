using System;
using Newtonsoft.Json;

namespace RethinkDb.Driver.Net.JsonConverters
{
    internal static class ExtensionsForJsonConverters
    {
        public static void ReadAndAssertProperty(this JsonReader reader, string propertyName)
        {
            ReadAndAssert(reader);
            if ((reader.TokenType != JsonToken.PropertyName) || !string.Equals(reader.Value.ToString(), propertyName, StringComparison.OrdinalIgnoreCase))
            {
                throw new JsonSerializationException($"Expected JSON property '{propertyName}'.");
            }
        }

        public static void ReadAndAssert(this JsonReader reader)
        {
            if (!reader.Read())
            {
                throw new JsonSerializationException("Unexpected end.");
            }
        }
    }
}