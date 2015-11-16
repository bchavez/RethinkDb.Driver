using System;
using Newtonsoft.Json;
using RethinkDb.Driver.Ast;

namespace RethinkDb.Driver.Net.JsonConverters
{
    public class ReqlDateTimeConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var ast = Util.ToReqlAst(value);
            serializer.Serialize(writer, ast.Build());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if( reader.TokenType != JsonToken.StartObject )
            {
                var msg = string.Join(" ",
                    $"The JSON representation of a DateTime/DateTimeOffset when parsing the server response is not a {Converter.PseudoTypeKey}:{Converter.Time} object.",
                    $"This happens if your JSON document contains DateTime/DateTimeOffsets in some other format (like an ISO8601 string) rather than a native RethinkDB pseudo type {Converter.PseudoTypeKey}:{Converter.Time} object.",
                    $"If you are overriding the default Ser/Deserialization process, you need to make sure DateTime/DateTimeOffset are native {Converter.PseudoTypeKey}:{Converter.Time} objects before using the built-in {nameof(ReqlDateTimeConverter)}.",
                    "See https://rethinkdb.com/docs/data-types/ for more information about how Date and Times are represented in RethinkDB.");
                throw new JsonSerializationException(msg);
            }
            
            reader.ReadAndAssertProperty(Converter.PseudoTypeKey);
            var reql_type = reader.ReadAsString();
            if( reql_type != Converter.Time )
            {
                throw new JsonSerializationException($"Expected {Converter.PseudoTypeKey} should be {Converter.Time} but got {reql_type}.");
            }

            reader.ReadAndAssertProperty("epoch_time");
            var epoch_time = reader.ReadAsDecimal();
            if( epoch_time == null )
            {
                throw new JsonSerializationException($"The {Converter.PseudoTypeKey}:{Converter.Time} object doesn't have an epoch_time value.");
            }

            reader.ReadAndAssertProperty("timezone");
            var timezone = reader.ReadAsString();

            //realign and get out of the pseudo type
            //one more post read to align out of { reql_type:TIME,  .... } 
            reader.ReadAndAssert();

            var tz = TimeSpan.Parse(timezone.Substring(1));
            if( !timezone.StartsWith("+") )
                tz = -tz;

            var epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var dt = epoch + TimeSpan.FromSeconds(Convert.ToDouble(epoch_time.Value));

            var dto = dt.ToOffset(tz);

            if( objectType == typeof(DateTimeOffset) )
                return dto;

            var tzHandle = serializer.DateTimeZoneHandling;

            switch( tzHandle )
            {
                case DateTimeZoneHandling.Local:
                    return dto.LocalDateTime;
                case DateTimeZoneHandling.Utc:
                    return dto.UtcDateTime;
                case DateTimeZoneHandling.Unspecified:
                    return dto.DateTime;
                case DateTimeZoneHandling.RoundtripKind:
                    return dto.Offset == TimeSpan.Zero ? dto.UtcDateTime : dto.LocalDateTime;
                default:
                    throw new JsonSerializationException("Invalid date time handling value.");
            }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DateTime) ||
                   objectType == typeof(DateTimeOffset);
        }

       
    }
}