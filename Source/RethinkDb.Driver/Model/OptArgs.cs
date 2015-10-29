using System.Collections.Generic;
using RethinkDb.Driver.Ast;

namespace RethinkDb.Driver.Model
{
	public class OptArgs : Dictionary<string, ReqlAst>
	{
		public virtual OptArgs with(string key, object value)
		{
			if (key != null)
			{
				this[key] = Util.ToReqlAst(value);
			}
			return this;
		}

		public virtual OptArgs with(string key, IList<object> value)
		{
			if (key != null)
			{
				this[key] = Util.ToReqlAst(value);
			}
			return this;
		}

	    public virtual OptArgs with(object anonType)
	    {
	        var anonDict = PropertyHelper.ObjectToDictionary(anonType);
	        foreach( var kvp in anonDict )
	        {
	            this.with(kvp.Key, kvp.Value);
	        }
	        return this;
	    }

		public static OptArgs fromMap(IDictionary<string, ReqlAst> map)
		{
			OptArgs oa = new OptArgs();

		    foreach( var kvp in map )
		        oa.Add(kvp.Key, kvp.Value);

			return oa;
		}

	    public static OptArgs fromAnonType(object anonType)
	    {
	        OptArgs oa = new OptArgs();
	        oa.with(anonType);

	        return oa;
	    }

	    public static OptArgs of(string key, object val)
	    {
	        OptArgs oa = new OptArgs();
	        oa.with(key, val);
	        return oa;
	    }
	}

}
