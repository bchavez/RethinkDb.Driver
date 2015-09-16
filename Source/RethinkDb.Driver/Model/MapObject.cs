using System.Collections.Generic;

namespace RethinkDb.Driver.Model
{

	public class MapObject : Dictionary<string, object>
	{

		public MapObject()
		{
		}

		public virtual MapObject with(string key, object value)
		{
			this[key] = value;
			return this;
		}
	}

}