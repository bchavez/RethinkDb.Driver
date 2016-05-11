using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RethinkDb.Driver.Net.JsonConverters;
using RethinkDb.Driver.Utils;

namespace RethinkDb.Driver.Net
{
    /// <summary>
    /// Configuration for RethinkDB's JSON serializer
    /// </summary>
    public static class Converter
    {
        static Converter()
        {
            InitializeDefault();
        }

        /// <summary>
        /// Initializes default serializer settings. There is no need to call this manually
        /// as it is already initialized in the static constructor.
        /// </summary>
        public static void InitializeDefault()
        {
            Converters = new JsonConverter[]
                {
                    DateTimeConverter = new ReqlDateTimeConverter(),
                    BinaryConverter = new ReqlBinaryConverter(),
                    GroupingConverter = new ReqlGroupingConverter(),
                    PocoExprConverter = new PocoExprConverter(),
                };

            var settings = new JsonSerializerSettings()
            {
                Converters = Converters
            };

            Serializer = JsonSerializer.CreateDefault(settings);
        }

        /// <summary>
        /// An array of the JSON converters in this static class.
        /// </summary>
        public static JsonConverter[] Converters { get; set; }

        /// <summary>
        /// The JSON serializer used for ser/deser.
        /// </summary>
        public static JsonSerializer Serializer { get; set; }

     
        /// <summary>
        /// DateTime converter to/from ReQL pseudo types
        /// </summary>
        public static ReqlDateTimeConverter DateTimeConverter { get; set; }

        /// <summary>
        /// Binary converter to/from ReQL pseudo types
        /// </summary>
        public static ReqlBinaryConverter BinaryConverter { get; set; }

        /// <summary>
        /// DateTime converter from ReQL grouping types
        /// </summary>
        public static ReqlGroupingConverter GroupingConverter { get; set; }

        /// <summary>
        /// Allows anonymous types to be composed with ReQL expressions like R.Now()
        /// </summary>
        public static PocoExprConverter PocoExprConverter { get; set; }


        /// <summary>
        /// The pseudo type key
        /// </summary>
        public const string PseudoTypeKey = "$reql_type$";

        /// <summary>
        /// Discriminator for TIME types.
        /// </summary>
        public const string Time = "TIME";

        /// <summary>
        /// Discriminator for GROUPED_DATA types.
        /// </summary>
        public const string GroupedData = "GROUPED_DATA";

        /// <summary>
        /// Discriminator for GEOMETRY types.
        /// </summary>
        public const string Geometry = "GEOMETRY";

        /// <summary>
        /// Discriminator for BINARY types.
        /// </summary>
        public const string Binary = "BINARY";


        /// <summary>
        /// Method for converting pseudo types in JToken (JObjects)
        /// </summary>
        public static void ConvertPseudoTypes(JToken data, FormatOptions fmt)
        {
            var reqlTypes = data.SelectTokens("$..$reql_type$").ToList();

            foreach (var typeToken in reqlTypes)
            {
                var reqlType = typeToken.Value<string>();
                //JObject -> JProerty -> JVaule:$reql_type$, go backup the chain.
                var pesudoObject = typeToken.Parent.Parent as JObject;

                JToken convertedValue = null;
                if (reqlType == Time)
                {
                    if (fmt.RawTime)
                        continue;
                    convertedValue = new JValue(GetTime(pesudoObject));
                }
                else if (reqlType == GroupedData)
                {
                    if (fmt.RawGroups)
                        continue;
                    convertedValue = GetGrouped(pesudoObject);
                }
                else if (reqlType == Binary)
                {
                    if (fmt.RawBinary)
                        continue;
                    convertedValue = new JValue(GetBinary(pesudoObject));
                }
                else if (reqlType == Geometry)
                {
                    // Nothing specific here
                    continue;
                }
                else
                {
                    // Just leave unknown pseudo-types alone
                    continue;
                }

                pesudoObject.Replace(convertedValue);
            }
        }

        private static object GetTime(JObject value)
        {
            double epoch_time = value["epoch_time"].ToObject<double>();
            string timezone = value["timezone"].ToString();

            if (Serializer.DateParseHandling == DateParseHandling.DateTime)
            {
                return ReqlDateTimeConverter.ConvertDateTime(epoch_time, timezone, Serializer.DateTimeZoneHandling);
            }
            else
            {
                return ReqlDateTimeConverter.ConvertDateTimeOffset(epoch_time, timezone);
            }
        }

        private static byte[] GetBinary(JObject value)
        {
            var base64 = value["data"].Value<string>();
            return Convert.FromBase64String(base64);
        }

        private static JToken GetGrouped(JObject value)
        {
            return value["data"];
        }
    }
}