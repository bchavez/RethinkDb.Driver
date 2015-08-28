using System;
using Newtonsoft.Json.Linq;
using RethinkDb.Driver;

namespace RethinkDb.Driver.Model
{
    
	public class Backtrace
	{
		public static Backtrace FromJsonArray(JArray @object)
		{
			if (@object == null || @object.Count == 0)
			{
			    return null;
			}
			throw new Exception("fromJSONArray not implemented");
		}
	}

}