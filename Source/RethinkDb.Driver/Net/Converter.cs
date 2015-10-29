using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;

namespace RethinkDb.Driver.Net
{
    public class Converter
    {
        public const string PseudoTypeKey = "$reql_type$";
        public const string Time = "TIME";
        public const string GroupedData = "GROUPED_DATA";
        public const string Geometry = "GEOMETRY";
        public const string Binary = "BINARY";

        public static JToken ConvertPesudoTypes(JToken data, FormatOptions fmt)
        {
            var reqlTypes = data.SelectTokens("$..$reql_type$").ToList();

            foreach( var typeToken in reqlTypes )
            {
                var reqlType = typeToken.Value<string>();
                //JObject -> JProerty -> JVaule:$reql_type$, go backup the chain.
                var pesudoObject = typeToken.Parent.Parent as JObject;

                JToken convertedValue = null;
                if( reqlType == Time )
                {
                    if( fmt.RawTime )
                        continue;
                    convertedValue = new JValue(GetTime(pesudoObject));
                }
                else if( reqlType == GroupedData )
                {
                    if( fmt.RawGroups )
                        continue;
                    convertedValue = new JValue(GetGrouped(pesudoObject));
                }
                else if( reqlType == Binary )
                {
                    if( fmt.RawBinary )
                        continue;
                    convertedValue = new JArray(GetBinary(pesudoObject));
                }

                pesudoObject.Replace(convertedValue);
            }

            return data;
        }

        private static DateTimeOffset GetTime(JObject value)
        {
            double epoch_time = value["epoch_time"].ToObject<double>();
            string timezone = value["timezone"].ToString();

            var tz = TimeSpan.Parse(timezone.Substring(1));
            if( !timezone.StartsWith("+") )
                tz = -tz;

            var epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero); // epoch UTC
            var dt = epoch + TimeSpan.FromSeconds(epoch_time);

            return dt.ToOffset(tz);
        }

        private static byte[] GetBinary(JObject value)
        {
            var base64 = value["data"].Value<string>();
            return Convert.FromBase64String(base64);
        }

        private static List<GroupedResult> GetGrouped(JObject value)
        {
            return value["data"].ToObject<List<List<object>>>()
                .Select(g =>
                    {
                        var group = g[0];
                        g.RemoveAt(0);
                        return new GroupedResult(group, g);
                    }).ToList();
        }

        public static object ToBinary(byte[] data)
        {
            var mob = new MapObject();
            mob.with(PseudoTypeKey, Binary);
            mob.with("data", Convert.ToBase64String(data));
            return mob;
        }

        public static Func<object, JObject> PocoConverter = DefaultPocoConverter;

        public static JObject DefaultPocoConverter(object value)
        {
            return JObject.FromObject(value, JsonSerializer.CreateDefault(new JsonSerializerSettings()
                {
                    Converters = new[] {TimeConverter}
                }));
        }

        public static JsonConverter TimeConverter = new PocoIso8601Converter();
    }

    internal class PocoIso8601Converter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var ast = Util.ToReqlAst(value);
            serializer.Serialize(writer, ast.Build());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DateTime) ||
                   objectType == typeof(DateTimeOffset);
        }
    }

    public class FormatOptions
    {
        public bool RawTime { get; }
        public bool RawGroups { get; }
        public bool RawBinary { get; }

        public FormatOptions(OptArgs args)
        {
            // TODO: find a better way to do this.
            ReqlAst datum;
            var value = args.TryGetValue("time_format", out datum) ? ((Datum)datum).datum : new Datum("native").datum;
            this.RawTime = value.Equals("raw");

            value = args.TryGetValue("binary_format", out datum) ? ((Datum)datum).datum : new Datum("native").datum;
            this.RawBinary = value.Equals("raw");

            value = args.TryGetValue("group_format", out datum) ? ((Datum)datum).datum : new Datum("native").datum;
            this.RawGroups = value.Equals("raw");
        }
    }
}