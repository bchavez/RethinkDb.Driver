using System.Collections.Generic;

namespace com.rethinkdb.model
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