using System;

namespace RethinkDb.Driver.Net
{
	public class NetUtil
	{
		public static long Deadline(TimeSpan? timeout)
		{
		    return DateTime.UtcNow.Add(timeout.Value).Ticks;
		}

	}

}
