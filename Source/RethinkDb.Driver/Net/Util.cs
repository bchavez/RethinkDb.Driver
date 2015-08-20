using System.Diagnostics;

namespace com.rethinkdb.net
{


	public class Util
	{
		public static int Timestamp
		{
			get
			{
				return (int)(System.nanoTime() / 1000000L);
			}
		}

		public static int deadline(int timeout)
		{
			return Timestamp + timeout;
		}

		public static ByteBuffer leByteBuffer(int capacity)
		{
			// Creating the ByteBuffer over an underlying array makes
			// it easier to turn into a string later.
			sbyte[] underlying = new sbyte[capacity];
			return ByteBuffer.wrap(underlying).order(ByteOrder.LITTLE_ENDIAN);
		}

		public static string bufferToString(ByteBuffer buf)
		{
			// This should only be used on ByteBuffers we've created by
			// wrapping an array
			return new string(buf.array(), StandardCharsets.UTF_8);
		}

	}

}