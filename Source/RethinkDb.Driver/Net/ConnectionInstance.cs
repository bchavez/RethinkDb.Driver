using System.Collections.Generic;
using RethinkDb.Driver;
using RethinkDb.Driver.Net;

namespace com.rethinkdb.net
{
	public class ConnectionInstance
	{
		// package private
		internal SocketWrapper socket = null;

		// protected members
		protected internal Dictionary<long?, Cursor> cursorCache = new Dictionary<long?, Cursor>();
		protected internal bool closing = false;
	    protected internal ByteBuffer headerInProgress = null;

		public ConnectionInstance()
		{
		}

		public virtual void connect(string hostname, int port, ByteBuffer handshake, int? timeout)
		{
			SocketWrapper sock = new SocketWrapper(hostname, port, timeout);
			sock.connect(handshake);
		    socket = sock;
		}

		public virtual bool Open
		{
			get { return socket?.Open ?? false; }
		}

		public virtual void close<T>()
		{
			closing = true;
			foreach (Cursor<T> cursor in cursorCache.Values)
			{
				cursor.Error = "Connection is closed.";
			}
			cursorCache.Clear();
		    socket?.close();
		}

		internal virtual void addToCache(long token, Cursor cursor)
		{
			cursorCache[token] = cursor;
		}

		internal virtual void removeFromCache(long token)
		{
			cursorCache.Remove(token);
		}

		internal virtual Response readResponse(long token)
		{
			return readResponse(token, null);
		}

		internal virtual Response readResponse(long token, int? deadline)
		{
		    if( socket == null )
		        throw new ReqlError("Socket not open");

		    var sock = socket;

			while (true)
			{
				if (!headerInProgress.Present)
				{
					headerInProgress = Optional.of(sock.recvall(12, deadline));
				}
				long resToken = headerInProgress.get().Long;
				int resLen = headerInProgress.get().Int;
				ByteBuffer resBuf = sock.recvall(resLen, deadline);
				headerInProgress = Optional.empty();

				Response res = Response.parseFrom(resToken, resBuf);

				Optional<Cursor> cursor = Optional.ofNullable(cursorCache[resToken]);
				cursor.ifPresent(c => c.extend(res));

				if (res.token == token)
				{
					return res;
				}
				else if (closing || cursor.Present)
				{
					close();
					throw new ReqlDriverError("Unexpected response received");
				}
			}
		}
	}

}