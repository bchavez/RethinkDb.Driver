#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using Newtonsoft.Json;
using RethinkDb.Driver.Ast;
#if DNX
using RethinkDb.Driver.Utils;
#endif

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
            return objectType.IsSubclassOf(typeof(ReqlExpr));
        }
    }
}