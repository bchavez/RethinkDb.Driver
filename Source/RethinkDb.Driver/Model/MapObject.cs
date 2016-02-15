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
        /// <summary>
        /// Fluent helper for setting dictionary[key] = value.
        /// </summary>
        public virtual MapObject With(object key, object value)
        {
            this[key] = value;
            return this;
        }

        internal virtual MapObject with(object key, object value)
        {
            return With(key, value);
        }

        /// <summary>
        /// Fluent helper for setting all key,value pairs of an anonymous type
        /// and loading them into the MapObject.
        /// </summary>
        public virtual MapObject With(object anonType)
        {
            var anonDict = PropertyHelper.ObjectToDictionary(anonType);
            foreach( var kvp in anonDict )
            {
                this.With(kvp.Key, kvp.Value);
            }
            return this;
        }

        /// <summary>
        /// Creates a new MapObject from an anonymous type.
        /// </summary>
        internal static MapObject FromAnonType(object anonType)
        {
            var map = new MapObject();
            map.With(anonType);
            return map;
        }
    }
}