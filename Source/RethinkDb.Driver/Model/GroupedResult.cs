using System.Collections;
using System.Collections.Generic;
using RethinkDb.Driver.Ast;
using System.Linq;
using Newtonsoft.Json.Linq;
using RethinkDb.Driver.Net;

namespace RethinkDb.Driver.Model
{
    public class GroupedResult<TKey, TElement> : IGrouping<TKey, TElement>
    {
        public GroupedResult(JToken key, JArray items)
        {
            this.Key = key.ToObject<TKey>(Converter.Deserializer);
            this.Items = items.ToObject<List<TElement>>(Converter.Deserializer);
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
}