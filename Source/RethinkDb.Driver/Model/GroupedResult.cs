using System.Collections.Generic;

namespace RethinkDb.Driver.Model
{
    public class GroupedResult
    {
        public object Group { get; }
        public List<object> Values { get; }

        public GroupedResult(object @group, List<object> values)
        {
            Group = @group;
            Values = values;
        }
    }
}