using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace RethinkDb.Driver.Net
{
    public class ConnectionInstance
    {
        internal SocketWrapper Socket { get; private set; }

        private readonly ConcurrentDictionary<long, ICursor> cursorCache = new ConcurrentDictionary<long, ICursor>();
        private bool closing = false;

        public virtual void Connect(string hostname, int port, byte[] handshake, TimeSpan? timeout)
        {
            var sock = new SocketWrapper(hostname, port, timeout);
            sock.Connect(handshake);
            Socket = sock;
        }

        public virtual async Task ConnectAsync(string hostname, int port, byte[] handshake)
        {
            var sock = new SocketWrapper(hostname, port, null);
            await sock.ConnectAsync(handshake).ConfigureAwait(false);
            Socket = sock;
        }

        public virtual bool Open => this.Socket?.Open ?? false;

        public virtual void Close()
        {
            closing = true;
            foreach( var cursor in cursorCache.Values.ToList() )
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
            ICursor removed;
            if( !cursorCache.TryRemove(token, out removed) )
            {
                Log.Trace($"Could not remove cursor token {token} from cursorCache.");
            }
        }

    }
}