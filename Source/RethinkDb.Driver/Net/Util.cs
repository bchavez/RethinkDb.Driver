using System;
using System.Diagnostics;

namespace com.rethinkdb.net
{


	public class Util
	{
		public static long Timestamp
		{
			get
			{
				return DateTime.UtcNow.Ticks;
			}
		}

		public static long deadline(TimeSpan? timeout)
		{
		    timeout = timeout ?? TimeSpan.FromSeconds(60);

			return Timestamp + timeout.Value.Ticks;
		}

	}

}