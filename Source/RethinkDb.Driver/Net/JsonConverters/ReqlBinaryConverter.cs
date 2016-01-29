using System;
using Newtonsoft.Json;

namespace RethinkDb.Driver.Net.JsonConverters
{
    public class ReqlBinaryConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject(); //convert to $reql_type$
            writer.WritePropertyName(Converter.PseudoTypeKey);
            writer.WriteValue(Converter.Binary);
            writer.WritePropertyName("data");

            writer.WriteValue(Convert.ToBase64String((byte[])value));
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartObject)
            {
                var msg = string.Join(" ",
                    $"The JSON representation of binary data (byte[]) when parsing the server response is not a {Converter.PseudoTypeKey}:{Converter.Binary} object.",
                    $"This happens if your JSON document contains binary data (byte[]) in some other format (like a base64 string only) rather than a native RethinkDB pseudo type {Converter.PseudoTypeKey}:{Converter.Binary} object.",
                    $"If you are overriding the default Ser/Deserialization process, you need to make sure byte[] is a native {Converter.PseudoTypeKey}:{Converter.Binary} objects before using the built-in {nameof(ReqlBinaryConverter)}.",
                    "See https://rethinkdb.com/docs/data-types/ for more information about how binary data is represented in RethinkDB.");
                throw new JsonSerializationException(msg);
            }

            reader.ReadAndAssertProperty(Converter.PseudoTypeKey);
            var reql_type = reader.ReadAsString();
            if( reql_type != Converter.Binary )
            {
                throw new JsonSerializationException($"Expected {Converter.PseudoTypeKey} should be {Converter.Binary} but got {reql_type}.");
            }

            reader.ReadAndAssertProperty("data");

            var data = reader.ReadAsBytes();

            //realign and get out of the pseudo type
            //one more post read to align out of { reql_type:BINARY, data:""} 
            reader.ReadAndAssert();

            return data;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(byte[]);
        }
    }
}