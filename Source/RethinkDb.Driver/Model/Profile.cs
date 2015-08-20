using System;
using Newtonsoft.Json.Linq;

namespace com.rethinkdb.model
{
	public class Profile
	{

		public static Profile fromJSONArray(JArray profileObj)
		{
			if (profileObj == null)
			{
				return null;
			}
			throw new Exception("fromJSONArray not implemented");
		}
	}

}