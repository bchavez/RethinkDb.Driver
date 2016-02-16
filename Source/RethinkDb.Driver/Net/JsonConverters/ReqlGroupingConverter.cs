#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Utils;

namespace RethinkDb.Driver.Net.JsonConverters
{
    public class ReqlGroupingConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new JsonSerializationException($"{nameof(ReqlGroupingConverter)} can't be used to serialize grouping results.");
        }

        //EXAMPLE:
        // "r": [
        //    {
        //    "$reql_type$": "GROUPED_DATA",
        //    "data": [
        //        [
        //          "Alice",
        //          [
        //            { "id": 5,  "player": "Alice", "points": 7, "type": "free"},
        //            { "id": 12, "player": "Alice", "points": 2, "type": "free" }
        //          ]
        //        ],
        //        [
        //          "Bob",
        //          [
        //            { "id": 2,  "player": "Bob", "points": 15, "type": "ranked" },
        //            { "id": 11, "player": "Bob", "points": 10, "type": "free" }
        //          ]
        //        ]
        //       ]
        //    }
        // ]
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            reader.ReadAndAssertProperty(Converter.PseudoTypeKey);
            var reql_type = reader.ReadAsString();
            if( reql_type != Converter.GroupedData )
            {
                throw new JsonSerializationException($"Expected {Converter.PseudoTypeKey} should be {Converter.GroupedData} but got {reql_type}.");
            }

            reader.ReadAndAssertProperty("data");

            //move reader to property value
            reader.ReadAndAssert();

            //... probably find a better way to do this.
            var genType = objectType.GetGenericTypeDefinition();
            IList list;
            if( genType == typeof(GroupedResultSet<,>) )
            {
                list = (IList)Activator.CreateInstance(objectType);
                objectType = objectType.BaseType().GenericTypeArguments[0];
            }
            else
            {
                var listType = typeof(List<>).MakeGenericType(objectType);
                list = (IList)Activator.CreateInstance(listType);
            }

            var data = serializer.Deserialize<List<JArray>>(reader);

            foreach( var group in data )
            {
                var key = group[0]; //key, group value in common
                var items = group[1]; //the grouped items
                var grouping = Activator.CreateInstance(objectType, key, items);
                list.Add(grouping);
            }
            //.Select(arr => Activator.CreateInstance(objectType, arr) ).ToList();

            return list;
        }

        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType)
        {
            if( objectType.IsGenericType() )
            {
                var genType = objectType.GetGenericTypeDefinition();
                return genType == typeof(GroupedResult<,>) ||
                       genType == typeof(GroupedResultSet<,>);
            }
            return false;
        }
    }
}