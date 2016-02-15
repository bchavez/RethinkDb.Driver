using System;
using System.Collections.Generic;
using RethinkDb.Driver.Ast;

namespace RethinkDb.Driver.Model
{
    /// <summary>
    /// Just a dictionary of (object,object)
    /// </summary>
    public class MapObject : Dictionary<object, object>
    {

        public virtual MapObject With(object key, object value)
        {
            this[key] = value;
            return this;
        }

        public virtual MapObject With(object anonType)
        {
            var anonDict = PropertyHelper.ObjectToDictionary(anonType);
            foreach( var kvp in anonDict )
            {
                this.With(kvp.Key, kvp.Value);
            }
            return this;
        }


        internal static MapObject FromAnonType(object anonType)
        {
            var map = new MapObject();
            map.With(anonType);
            return map;
        }
    }
}