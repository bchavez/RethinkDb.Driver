using System;
using Newtonsoft.Json;
using RethinkDb.Driver.Ast;

namespace RethinkDb.Driver.Net.JsonConverters
{
    public class PocoExprConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var ast = value as ReqlExpr;
            serializer.Serialize(writer, ast.Build());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead => false;

        public override bool CanConvert(Type objectType)
        {
            return typeof(ReqlExpr).IsAssignableFrom(objectType);
        }
    }
}