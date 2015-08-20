using System;
using Newtonsoft.Json.Linq;
using RethinkDb.Driver;

namespace RethinkDb.Driver.Model
{
    
	public class Backtrace
	{
		public static Optional<Backtrace> fromJSONArray(JArray @object)
		{
			if (@object == null || @object.size() == 0)
			{
				return Optional.empty();
			}
			throw new Exception("fromJSONArray not implemented");
		}
	}

}