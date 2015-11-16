using System;
using System.Collections.Generic;
using RethinkDb.Driver.Ast;

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

        public virtual MapObject with(object anonType)
        {
            var anonDict = PropertyHelper.ObjectToDictionary(anonType);
            foreach( var kvp in anonDict )
            {
                this.with(kvp.Key, kvp.Value);
            }
            return this;
        }

        public static MapObject fromAnonType(object anonType)
        {
            var map = new MapObject();
            map.with(anonType);
            return map;
        }
    }
}