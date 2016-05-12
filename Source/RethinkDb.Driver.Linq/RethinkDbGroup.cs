using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RethinkDb.Driver.Linq
{
    public class RethinkDbGroup<T, TVal> : IGrouping<T, TVal>
    {
        public IEnumerator<TVal> GetEnumerator()
        {
            return Reduction.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public T Key { get; set; }

        public List<TVal> Reduction { get; set; }
    }
}