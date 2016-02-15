using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RethinkDb.Driver.Net;

namespace RethinkDb.Driver.Model
{
    /// <summary>
    /// A grouped result helper.
    /// </summary>
    /// <typeparam name="TKey">The KEY type</typeparam>
    /// <typeparam name="TElement">The VALUE type</typeparam>
    public class GroupedResult<TKey, TElement> : IGrouping<TKey, TElement>
    {
        internal GroupedResult(JToken key, JArray items)
        {
            this.Key = key.ToObject<TKey>(Converter.Serializer);
            this.Items = items.ToObject<List<TElement>>(Converter.Serializer);
        }
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
        public IEnumerator<TElement> GetEnumerator()
        {
            return this.Items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member

        /// <summary>
        /// The Key
        /// </summary>
        public TKey Key { get; }
        
        /// <summary>
        /// The list of items grouped by <see cref="Key"/>
        /// </summary>
        public List<TElement> Items { get; set; }
    }

    /// <summary>
    /// A typed grouped result helper.
    /// </summary>
    /// <typeparam name="TKey">The KEY type</typeparam>
    /// <typeparam name="TItem">The VALUE type</typeparam>
    public class GroupedResultSet<TKey, TItem> : List< GroupedResult<TKey, TItem>>
    {
        /// <summary>
        /// Item type
        /// </summary>
        public Type ItemType => typeof(TItem);

        /// <summary>
        /// Key type
        /// </summary>
        public Type KeyType => typeof(TKey);
    }
}