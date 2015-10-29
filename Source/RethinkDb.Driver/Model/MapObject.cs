using System.Collections.Generic;

namespace RethinkDb.Driver.Model
{
    public class MapObject : Dictionary<object, object>
    {
        public MapObject()
        {
        }

        public virtual MapObject with(object key, object value)
        {
            this[key] = value;
            return this;
        }
    }
}