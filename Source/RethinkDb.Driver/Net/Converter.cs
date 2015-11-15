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

            var settings = new JsonSerializerSettings()
                {
                    Converters = new JsonConverter[]
                        {
                            DateTimeConverter, BinaryConverter, GroupingConverter
                        }
                };

            Seralizer = JsonSerializer.CreateDefault(settings);
        }

        public static JsonSerializer Seralizer { get; set; }
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