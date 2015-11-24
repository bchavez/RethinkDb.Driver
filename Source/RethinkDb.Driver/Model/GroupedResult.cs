using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RethinkDb.Driver.Net;

namespace RethinkDb.Driver.Model
{
    public class GroupedResult<TKey, TElement> : IGrouping<TKey, TElement>
    {
        public GroupedResult(JToken key, JArray items)
        {
            this.Key = key.ToObject<TKey>(Converter.Serializer);
            this.Items = items.ToObject<List<TElement>>(Converter.Serializer);
        }

        public IEnumerator<TElement> GetEnumerator()
        {
            return this.Items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public TKey Key { get; }
        public List<TElement> Items { get; set; }
    }

    public class GroupedResultSet<TKey, TItem> : List< GroupedResult<TKey, TItem>>
    {
        public Type ItemType => typeof(TItem);
        public Type KeyType => typeof(TKey);
    }
}