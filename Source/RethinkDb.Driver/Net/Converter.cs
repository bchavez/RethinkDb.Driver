using Newtonsoft.Json;
using RethinkDb.Driver.Net.JsonConverters;

namespace RethinkDb.Driver.Net
{
    public static class Converter
    {
        static Converter()
        {
            DateTimeConverter = new ReqlDateTimeConverter();
            BinaryConverter = new ReqlBinaryConverter();
            GroupingConverter = new ReqlGroupingConverter();
            PocoArrayConverter = new PocoArrayConverter();

            var deserSettings = new JsonSerializerSettings()
                {
                    Converters = new JsonConverter[]
                        {
                            DateTimeConverter, BinaryConverter, GroupingConverter
                        }
                };

            Deserializer = JsonSerializer.CreateDefault(deserSettings);

            var serSettings = new JsonSerializerSettings()
                {
                    Converters = new JsonConverter[]
                        {
                            DateTimeConverter, BinaryConverter, PocoArrayConverter
                        }
                };
            Serializer = JsonSerializer.CreateDefault(serSettings);
        }

        public static JsonSerializer Serializer { get; set; }
        public static JsonSerializer Deserializer { get; set; }

        public static PocoArrayConverter PocoArrayConverter { get; set; }
        public static ReqlDateTimeConverter DateTimeConverter { get; set; }
        public static ReqlBinaryConverter BinaryConverter { get; set; }
        public static ReqlGroupingConverter GroupingConverter { get; set; }

        public const string PseudoTypeKey = "$reql_type$";
        public const string Time = "TIME";
        public const string GroupedData = "GROUPED_DATA";
        public const string Geometry = "GEOMETRY";
        public const string Binary = "BINARY";
    }
}