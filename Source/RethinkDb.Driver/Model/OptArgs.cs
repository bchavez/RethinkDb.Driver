using System.Collections.Generic;
using RethinkDb.Driver.Ast;

namespace RethinkDb.Driver.Model
{
    /// <summary>
    /// Dictionary of string,ReqlAst
    /// </summary>
    public class OptArgs : Dictionary<string, ReqlAst>
    {
        /// <summary>
        /// Fluent helper for setting dictionary[key] = value.
        /// </summary>
        public virtual OptArgs With(string key, object value)
        {
            if( key != null )
            {
                this[key] = Util.ToReqlAst(value);
            }
            return this;
        }

        internal OptArgs with(string key, object value)
        {
            return With(key, value);
        }

        /// <summary>
        /// Fluent helper for setting dictionary[key] = List.
        /// </summary>
        public virtual OptArgs With(string key, IList<object> value)
        {
            if( key != null )
            {
                this[key] = Util.ToReqlAst(value);
            }
            return this;
        }

        internal virtual OptArgs with(string key, IList<object> value)
        {
            return With(key, value);
        }

        /// <summary>
        /// Fluent helper for setting dictionary[key] = value multiple times 
        /// for each Property=Value in the anonType.
        /// </summary>
        public virtual OptArgs With(object anonType)
        {
            var anonDict = PropertyHelper.ObjectToDictionary(anonType);
            foreach( var kvp in anonDict )
            {
                this.with(kvp.Key, kvp.Value);
            }
            return this;
        }

        internal virtual OptArgs with(object anonType)
        {
            return With(anonType);
        }


        /// <summary>
        /// Fluent helper to copy all key value pairs from <paramref name="args"/>
        /// </summary>
        public virtual OptArgs With(OptArgs args)
        {
            foreach( var kvp in args )
                this.with(kvp.Key, kvp.Value);

            return this;
        }


        /// <summary>
        /// Creates a new OptArg from all key value pairs in <paramref name="map"/>
        /// </summary>
        public static OptArgs FromMap(IDictionary<string, ReqlAst> map)
        {
            OptArgs oa = new OptArgs();

            foreach( var kvp in map )
                oa.Add(kvp.Key, kvp.Value);

            return oa;
        }

        /// <summary>
        /// Creates a new OptArg from all Property = Value pairs in <paramref name="anonType"/> anonymous type.
        /// </summary>
        public static OptArgs FromAnonType(object anonType)
        {
            OptArgs oa = new OptArgs();
            oa.with(anonType);

            return oa;
        }
    }
}