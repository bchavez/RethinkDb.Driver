using System.Collections.Generic;
using RethinkDb.Driver.Ast;

namespace RethinkDb.Driver.Model
{
    public class OptArgs : Dictionary<string, ReqlAst>
    {
        public virtual OptArgs With(string key, object value)
        {
            return with(key, value);
        }
        internal virtual OptArgs with(string key, object value)
        {
            if( key != null )
            {
                this[key] = Util.ToReqlAst(value);
            }
            return this;
        }

        internal virtual OptArgs With(string key, IList<object> value)
        {
            return with(key, value);
        }
        internal virtual OptArgs with(string key, IList<object> value)
        {
            if( key != null )
            {
                this[key] = Util.ToReqlAst(value);
            }
            return this;
        }


        public virtual OptArgs With(object anonType)
        {
            return with(anonType);
        }
        internal virtual OptArgs with(object anonType)
        {
            var anonDict = PropertyHelper.ObjectToDictionary(anonType);
            foreach( var kvp in anonDict )
            {
                this.with(kvp.Key, kvp.Value);
            }
            return this;
        }


        public virtual OptArgs With(OptArgs args)
        {
            return with(args);
        }
        internal virtual OptArgs with(OptArgs args)
        {
            foreach( var kvp in args )
                this.with(kvp.Key, kvp.Value);

            return this;
        }


        public static OptArgs FromMap(IDictionary<string, ReqlAst> map)
        {
            return fromMap(map);
        }
        internal static OptArgs fromMap(IDictionary<string, ReqlAst> map)
        {
            OptArgs oa = new OptArgs();

            foreach( var kvp in map )
                oa.Add(kvp.Key, kvp.Value);

            return oa;
        }

        public static OptArgs FromAnonType(object anonType)
        {
            OptArgs oa = new OptArgs();
            oa.with(anonType);

            return oa;
        }

        public static OptArgs Of(string key, object val)
        {
            return of(key, val);
        }
        internal static OptArgs of(string key, object val)
        {
            OptArgs oa = new OptArgs();
            oa.with(key, val);
            return oa;
        }
    }
}