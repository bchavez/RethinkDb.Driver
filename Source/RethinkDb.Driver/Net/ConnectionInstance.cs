using System;
using System.Collections.Generic;

namespace RethinkDb.Driver.Net
{
	public class ConnectionInstance
	{
		internal SocketWrapper Socket { get; private set; }

	    private readonly Dictionary<long, ICursor> cursorCache = new Dictionary<long, ICursor>();
		private bool closing = false;

		public virtual void Connect(string hostname, int port, byte[] handshake, TimeSpan? timeout)
		{
			var sock = new SocketWrapper(hostname, port, timeout);
			sock.Connect(handshake);
		    Socket = sock;
		}

		public virtual bool Open
		{
			get { return Socket?.Open ?? false; }
		}

		public virtual void Close()
		{
			closing = true;
			foreach (var cursor in cursorCache.Values)
			{
			    cursor.SetError("Connection is closed.");
			}
			cursorCache.Clear();
		    Socket?.Close();
		}

		internal virtual void AddToCache(long token, ICursor cursor)
		{
			cursorCache[token] = cursor;
		}

		internal virtual void RemoveFromCache(long token)
		{
			cursorCache.Remove(token);
		}

		internal virtual Response ReadResponse(long token)
		{
			return ReadResponse(token, null);
		}

	    internal virtual Response ReadResponse(long token, long? deadline)
	    {
	        if( Socket == null )
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
	            var res = this.Socket.Read();

	            ICursor cursor;
	            if( cursorCache.TryGetValue(res.token, out cursor) )
	            {
	                cursor.Extend(res);
	            }

                if( res.token == token )
	            {
	                return res;
	            }
	            else if( closing || cursor != null )
	            {
	                Close();
	                throw new ReqlDriverError("Unexpected response received");
	            }
	        }
	    }
	}

}