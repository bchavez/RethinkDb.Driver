#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;

namespace RethinkDb.Driver.Net.JsonConverters
{
    public class PocoArrayConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var items = value as IEnumerable;
            var innerValues = new Arguments();
            foreach( var item in items )
            {
                innerValues.Add(Util.ToReqlAst(item));
            }
            var makeArray = new MakeArray(innerValues, null);
            serializer.Serialize(writer, makeArray.Build());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead => false;

        public override bool CanConvert(Type objectType)
        {
            var contract = Converter.Serializer.ContractResolver.ResolveContract(objectType);
            return contract is JsonArrayContract;
        }
    }
}