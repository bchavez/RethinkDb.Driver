using System;

namespace com.rethinkdb.model
{

	using JSONArray = org.json.simple.JSONArray;

	public class Profile
	{

		public static Optional<Profile> fromJSONArray(JSONArray profileObj)
		{
			if (profileObj == null)
			{
				return Optional.empty();
			}
			throw new Exception("fromJSONArray not implemented");
		}
	}

}