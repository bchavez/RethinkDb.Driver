using System;
using System.IO;
using System.Text;
using System.Threading;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;

namespace RethinkDb.Driver.Net
{
    public class Connection
    {
        // public immutable
        public readonly string hostname;
        public readonly int port;

        private long nextToken = 0;
        private readonly Func<ConnectionInstance> instanceMaker;

        // private mutable
        private string dbname;
        private TimeSpan? connectTimeout;
        private byte[] handshake;
        private ConnectionInstance instance = null;

        internal Connection(ConnectionBuilder builder)
        {
            dbname = builder.dbname;
            var authKey = builder._authKey ?? string.Empty;
            var authKeyBytes = Encoding.ASCII.GetBytes(authKey);

            using( var ms = new MemoryStream() )
            using( var bw = new BinaryWriter(ms) )
            {
                bw.Write((int)RethinkDb.Driver.Proto.Version.V0_4);
                bw.Write(authKeyBytes.Length);
                bw.Write(authKeyBytes);
                bw.Write((int)RethinkDb.Driver.Proto.Protocol.JSON);
                bw.Flush();
                handshake = ms.ToArray();
            }

            hostname = builder._hostmame ?? "localhost";
            port = builder._port ?? 28015;
            connectTimeout = builder._timeout;

            instanceMaker = builder.instanceMaker;
        }

        public static ConnectionBuilder build()
        {
            return new ConnectionBuilder(() => new ConnectionInstance());
        }

        public virtual string db()
        {
            return dbname;
        }

        internal virtual void addToCache<T>(long token, Cursor<T> cursor)
        {
            if( instance == null )
                throw new ReqlDriverError("Can't add to cache when not connected.");

            instance?.AddToCache(token, cursor);
        }

        internal virtual void removeFromCache(long token)
        {
            instance?.RemoveFromCache(token);
        }

        public virtual void use(string db)
        {
            dbname = db;
        }

        public virtual TimeSpan? timeout()
        {
            return connectTimeout;
        }

        public virtual Connection reconnect()
        {
            return reconnect(false, null);
        }

        public virtual Connection reconnect(bool noreplyWait, TimeSpan? timeout)
        {
            if( !timeout.HasValue )
            {
                timeout = connectTimeout;
            }
            close(noreplyWait);
            ConnectionInstance inst = instanceMaker();
            instance = inst;
            inst.Connect(hostname, port, handshake, timeout);
            return this;
        }

        public virtual bool Open => instance?.Open ?? false;

        public virtual ConnectionInstance checkOpen()
        {
            if( !instance?.Open ?? true )
            {
                throw new ReqlDriverError("Connection is closed.");
            }
            return instance;
        }

        public virtual void close(bool shouldNoreplyWait)
        {
            if( instance != null )
            {
                try
                {
                    if( shouldNoreplyWait )
                    {
                        noreplyWait();
                    }
                }
                finally
                {
                    nextToken = 0;
                    instance.Close();
                    instance = null;
                }
            }
        }

        private long newToken()
        {
            return Interlocked.Increment(ref nextToken);
        }

        internal virtual Response readResponse(long token, long? deadline)
        {
            return checkOpen().ReadResponse(token, deadline);
        }

        internal virtual object runQuery<T>(Query query, bool noreply)
        {
            ConnectionInstance inst = checkOpen();
            if( inst.Socket == null )
                throw new ReqlDriverError("No socket open.");

            inst.Socket.WriteQuery( query.token, query.serialize());

            if( noreply )
            {
                return null;
            }

            Response res = inst.ReadResponse(query.token);

            // TODO: This logic needs to move into the Response class
            Console.WriteLine(res.ToString()); //RSI
            if( res.Atom )
            {
                try
                {
                    return Response.convertPseudotypes(res.data, res.profile)[0];
                }
                catch( System.IndexOutOfRangeException ex )
                {
                    throw new ReqlDriverError("Atom response was empty!", ex);
                }
            }
            else if( res.Partial || res.Sequence )
            {
                ICursor cursor = Cursor<T>.empty(this, query);
                cursor.Extend(res);
                return cursor;
            }
            else if( res.WaitComplete )
            {
                return null;
            }
            else
            {
                throw res.makeError(query);
            }
        }

        internal virtual object runQuery<T>(Query query)
        {
            return runQuery<T>(query, false);
        }

        internal virtual void runQueryNoreply(Query query)
        {
            runQuery<object>(query, true);
        }

        public virtual void noreplyWait()
        {
            runQuery<object>(Query.noreplyWait(newToken()));
        }

        public virtual object run<T>(ReqlAst term, GlobalOptions globalOpts)
        {
            if( globalOpts.Db == null )
            {
                globalOpts.Db = dbname;
            }
            Query q = Query.start(newToken(), term, globalOpts);
            return runQuery<T>(q, globalOpts.Noreply.GetValueOrDefault(false));
        }

        internal virtual void continue_(ICursor cursor)
        {
            runQueryNoreply(Query.continue_(cursor.Token));
        }

        internal virtual void stop(ICursor cursor)
        {
            runQueryNoreply(Query.stop(cursor.Token));
        }

    }

    public class ConnectionBuilder
    {
        internal readonly Func<ConnectionInstance> instanceMaker;
        internal string _hostmame = null;
        internal int? _port = null;
        internal string dbname = null;
        internal string _authKey = null;
        internal TimeSpan? _timeout = null;

        public ConnectionBuilder(Func<ConnectionInstance> instanceMaker)
        {
            this.instanceMaker = instanceMaker;
        }

        public virtual ConnectionBuilder hostname(string val)
        {
            this._hostmame = val;
            return this;
        }

        public virtual ConnectionBuilder port(int val)
        {
            this._port = val;
            return this;
        }

        public virtual ConnectionBuilder db(string val)
        {
            this.dbname = val;
            return this;
        }

        public virtual ConnectionBuilder authKey(string val)
        {
            this._authKey = val;
            return this;
        }

        public virtual ConnectionBuilder timeout(int val)
        {
            this._timeout = TimeSpan.FromSeconds(val);
            return this;
        }

        public virtual Connection connect()
        {
            var conn = new Connection(this);
            conn.reconnect();
            return conn;
        }
    }
}