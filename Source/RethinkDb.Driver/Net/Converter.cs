using Newtonsoft.Json;
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
    }
}