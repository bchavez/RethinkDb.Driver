using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RethinkDb.Driver.Net.JsonConverters;

namespace RethinkDb.Driver.Net
{
    /// <summary>
    /// Configuration for RethinkDB's JSON serializer
    /// </summary>
    public static class Converter
    {
        static Converter()
        {
            DateTimeConverter = new ReqlDateTimeConverter();
            BinaryConverter = new ReqlBinaryConverter();
            GroupingConverter = new ReqlGroupingConverter();
            PocoArrayConverter = new PocoArrayConverter();
            PocoExprConverter = new PocoExprConverter();

            var settings = new JsonSerializerSettings()
                {
                    Converters = new JsonConverter[]
                        {
                            DateTimeConverter,
                            BinaryConverter,
                            GroupingConverter,
                            PocoArrayConverter,
                            PocoExprConverter
                        }
                };
            Serializer = JsonSerializer.CreateDefault(settings);


        }

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
        /// Serializes arrays in POCO into wire specific format
        /// </summary>
        public static PocoArrayConverter PocoArrayConverter { get; set; }
        
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
    }
}