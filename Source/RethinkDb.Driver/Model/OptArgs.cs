using System.Collections.Generic;
using com.rethinkdb.ast;
using RethinkDb.Driver.Ast;

namespace RethinkDb.Driver.Model
{
	public class OptArgs : Dictionary<string, ReqlAst>
	{
		public virtual OptArgs with(string key, object value)
		{
			if (key != null)
			{
				this[key] = Util.toReqlAst(value);
			}
			return this;
		}

		public virtual OptArgs with(string key, IList<object> value)
		{
			if (key != null)
			{
				this[key] = Util.toReqlAst(value);
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

	}

}