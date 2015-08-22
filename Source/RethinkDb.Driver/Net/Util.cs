using System;
using System.Diagnostics;

namespace com.rethinkdb.net
{


	public class Util
	{
		public static int Timestamp
		{
			get
			{
				return (int)(DateTime.UtcNow.Ticks / 1000000L);
			}
		}

		public static int deadline(int timeout)
		{
			return Timestamp + timeout;
		}

	}

}