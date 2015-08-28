using System;
using System.Collections.Generic;

namespace RethinkDb.Driver.Net
{
	public class ConnectionInstance
	{
		// package private
		internal SocketWrapper socket = null;

		// protected members
	    internal Dictionary<long, ICursor> cursorCache = new Dictionary<long, ICursor>();
		protected internal bool closing = false;


		public virtual void connect(string hostname, int port, byte[] handshake, TimeSpan? timeout)
		{
			SocketWrapper sock = new SocketWrapper(hostname, port, timeout);
			sock.connect(handshake);
		    socket = sock;
		}

		public virtual bool Open
		{
			get { return socket?.Open ?? false; }
		}

		public virtual void close()
		{
			closing = true;
			foreach (var cursor in cursorCache.Values)
			{
			    cursor.SetError("Connection is closed.");
			}
			cursorCache.Clear();
		    socket?.close();
		}

		internal virtual void addToCache(long token, ICursor cursor)
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

	    internal virtual Response readResponse(long token, long? deadline)
	    {
	        if( socket == null )
	            throw new ReqlError("Socket not open");
            /*
				if (headerInProgress == null)
				{
					headerInProgress = sock.recvall(12, deadline);
				}
				long resToken = headerInProgress.get().Long;
				int resLen = headerInProgress.get().Int;
				ByteBuffer resBuf = sock.recvall(resLen, deadline);
			    headerInProgress = null;

				var res = Response.parseFrom(resToken, resBuf);*/

	        while( true )
	        {
                //may or maynot be the token we're looking for.
	            var res = this.socket.read();

	            var cursor = cursorCache[res.token];

	            cursor?.Extend(res);

	            if( res.token == token )
	            {
	                return res;
	            }
	            else if( closing || cursor != null )
	            {
	                close();
	                throw new ReqlDriverError("Unexpected response received");
	            }
	        }
	    }
	}

}