using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;

namespace RethinkDb.Driver.Net
{
    public class Converter2
    {
        //public static Func<long, string, Response> ResponseBuillder = Response.ParseFrom;

        public const string PseudoTypeKey = "$reql_type$";
        public const string Time = "TIME";
        public const string GROUPED_DATA = "GROUPED_DATA";
        public const string GEOMETRY = "GEOMETRY";
        public const string BINARY = "BINARY";

        public static JToken ConvertPesudoTypes(JToken obj, FormatOptions fmt)
        {
            var jarray = obj as JArray;
            if( jarray != null )
            {
                for( var i = 0; i < jarray.Count; i++ )
                {
                    var value = ConvertPesudoTypes(jarray[i], fmt);
                    jarray[i] = value;
                }
                return jarray;
            }

            var jobject = obj as JObject;
            if( jobject != null )
            {
                var props = jobject.Properties().ToList();
                if( props.Any(prop => prop.Name == PseudoTypeKey) )
                {
                    var value = ConvertPesudo(jobject, fmt);
                    return new JValue(value);
                }
                for( var i = 0; i < props.Count; i++ )
                {
                    var value = ConvertPesudoTypes(props[i], fmt);
                    props[i].Value = value;
                }

                return jobject;
            }

            return (JToken)obj;
        }

        public static object ConvertPesudo(JObject value, FormatOptions fmt)
        {
            if( value == null ) return null;

            var reqlType = value[PseudoTypeKey].ToString();

            switch( reqlType )
            {
                case Time:
                    return fmt.RawTime ? value : (object)GetTime(value);
                case GROUPED_DATA:
                    return fmt.RawGroups ? value : (object)GetGrouped(value);
                case BINARY:
                    return fmt.RawBinary ? value : (object)GetBinary(value);
                case GEOMETRY:
                    return value;

                default:
                    return value;
            }
        }

        private static DateTimeOffset GetTime(JObject value)
        {
            double epoch_time = value["epoch_time"].ToObject<double>();
            string timezone = value["timezone"].ToString();

            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var dt = epoch + TimeSpan.FromSeconds(epoch_time);

            var tz = TimeSpan.Parse(timezone.Substring(1));
            if( !timezone.StartsWith("+") )
                tz = -tz;

            return new DateTimeOffset(dt, tz);
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
            mob.with(PseudoTypeKey, BINARY);
            mob.with("data", Convert.ToBase64String(data));
            return mob;
        }
    }
}