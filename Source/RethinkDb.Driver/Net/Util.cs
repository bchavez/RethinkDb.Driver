using System;

namespace RethinkDb.Driver.Net
{
	public class NetUtil
	{
		public static long Timestamp
		{
			get
			{
				return DateTime.UtcNow.Ticks;
			}
		}

		public static long Deadline(TimeSpan? timeout)
		{
		    timeout = timeout ?? TimeSpan.FromSeconds(60);

			return Timestamp + timeout.Value.Ticks;
		}

	}

}