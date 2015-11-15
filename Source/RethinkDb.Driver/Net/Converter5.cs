using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RethinkDb.Driver.Ast;

namespace RethinkDb.Driver.Net
{
    public static class Converter5
    {
        static Converter5()
        {
            DateTimeConverter = new ReqlDateTimeConverter();
            BinaryConverter = new ReqlBinaryConverter();

            Settings = new JsonSerializerSettings()
            {
                Converters = new JsonConverter[]
                    {
                        DateTimeConverter, BinaryConverter
                    }
            };

            Seralizer = JsonSerializer.CreateDefault(Settings);
        }

        public static JsonSerializerSettings Settings;
        public static JsonSerializer Seralizer;
        public static ReqlDateTimeConverter DateTimeConverter;
        public static ReqlBinaryConverter BinaryConverter;


        public const string PseudoTypeKey = "$reql_type$";
        public const string Time = "TIME";
        public const string GroupedData = "GROUPED_DATA";
        public const string Geometry = "GEOMETRY";
        public const string Binary = "BINARY";
    }

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
                    $"The JSON representation of a DateTime/DateTimeOffset when parsing the server response is not a {Converter5.PseudoTypeKey}:{Converter5.Time} object.",
                    $"This happens if your JSON document contains DateTime/DateTimeOffsets in some other format (like an ISO8601 string) rather than a native RethinkDB pseudo type {Converter5.PseudoTypeKey}:{Converter5.Time} object.",
                    $"If you are overriding the default Ser/Deserialization process, you need to make sure DateTime/DateTimeOffset are native {Converter5.PseudoTypeKey}:{Converter5.Time} objects before using the built-in {nameof(ReqlDateTimeConverter)}.",
                    "See https://rethinkdb.com/docs/data-types/ for more information about how Date and Times are represented in RethinkDB.");
                throw new JsonSerializationException(msg);
            }
            
            reader.ReadAndAssertProperty(Converter5.PseudoTypeKey);
            var reql_type = reader.ReadAsString();
            if( reql_type != Converter5.Time )
            {
                throw new JsonSerializationException($"Expected {Converter5.PseudoTypeKey} should be {Converter5.Time} but got {reql_type}.");
            }

            reader.ReadAndAssertProperty("epoch_time");
            var epoch_time = reader.ReadAsDecimal();
            if( epoch_time == null )
            {
                throw new JsonSerializationException("The $reql_type$:TIME object doesn't have an epoch_time value.");
            }

            reader.ReadAndAssertProperty("timezone");
            var timezone = reader.ReadAsString();

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

    public class ReqlBinaryConverter :
#if DNX
        JsonConverter
#else
        BinaryConverter
#endif
    {
        private bool useInternal = false;
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject(); //convert to $reql_type$
            writer.WritePropertyName(Converter5.PseudoTypeKey);
            writer.WriteValue(Converter5.Binary);
            writer.WritePropertyName("data");
            if (useInternal)
            {
#if !DNX
                base.WriteJson(writer, value, serializer);
#endif
            }
            else
            {
                writer.WriteValue(value);
            }
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartObject)
            {
                var msg = string.Join(" ",
                    $"The JSON representation of binary data (byte[]) when parsing the server response is not a {Converter5.PseudoTypeKey}:{Converter5.Binary} object.",
                    $"This happens if your JSON document contains binary data (byte[]) in some other format (like a base64 string only) rather than a native RethinkDB pseudo type {Converter5.PseudoTypeKey}:{Converter5.Binary} object.",
                    $"If you are overriding the default Ser/Deserialization process, you need to make sure byte[] is a native {Converter5.PseudoTypeKey}:{Converter5.Binary} objects before using the built-in {nameof(ReqlBinaryConverter)}.",
                    "See https://rethinkdb.com/docs/data-types/ for more information about how binary data is represented in RethinkDB.");
                throw new JsonSerializationException(msg);
            }

            reader.ReadAndAssertProperty(Converter5.PseudoTypeKey);
            var reql_type = reader.ReadAsString();
            if( reql_type != Converter5.Binary )
            {
                throw new JsonSerializationException($"Expected {Converter5.PseudoTypeKey} should be {Converter5.Binary} but got {reql_type}.");
            }

            reader.ReadAndAssertProperty("data");

            return reader.ReadAsBytes();
        }

        public override bool CanConvert(Type objectType)
        {
#if !DNX
            useInternal = base.CanConvert(objectType);
#endif
            return useInternal || objectType == typeof(byte[]);
        }
    }

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