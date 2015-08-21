using System;
using System.IO;
using System.Text;
using System.Threading;
using RethinkDb.Driver;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Net;

namespace com.rethinkdb.net
{

	public class Connection<T> where T : ConnectionInstance
	{
		// public immutable
		public readonly string hostname;
		public readonly int port;

	    private long nextToken = 0;
		private readonly Func<T> instanceMaker;

		// private mutable
		private string dbname;
		private int? connectTimeout;
		private byte[] handshake;
	    private T instance = null;

		private Connection(Builder<T> builder)
		{
			dbname = builder.dbname;
			var authKey = builder.authKey_Renamed ?? string.Empty;
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

			hostname = builder.hostname_Renamed ?? "localhost";
			port = builder.port_Renamed ?? 28015;
			connectTimeout = builder.timeout_Renamed;

			instanceMaker = builder.instanceMaker;
		}

		public static Builder<ConnectionInstance> build()
		{
		    return new Builder<ConnectionInstance>(() => new ConnectionInstance());
		}

		public virtual string db()
		{
			return dbname;
		}

		internal virtual void addToCache(long token, Cursor<T> cursor)
		{
            if( instance == null )
                throw new ReqlDriverError("Can't add to cache when not connected.");

            instance?.addToCache(token, cursor);		    
		}

		internal virtual void removeFromCache(long token)
		{
			instance?.removeFromCache(token);
		}

		public virtual void use(string db)
		{
		    dbname = db;
		}

		public virtual int? timeout()
		{
			return connectTimeout;
		}

		public virtual Connection<T> reconnect()
		{
			return reconnect(false, null);
		}

		public virtual Connection<T> reconnect(bool noreplyWait, int? timeout)
		{
			if (!timeout.HasValue)
			{
				timeout = connectTimeout;
			}
			close(noreplyWait);
		    T inst = instanceMaker();
		    instance = inst;
			inst.connect(hostname, port, handshake, timeout);
			return this;
		}

		public virtual bool Open => instance?.Open ?? false;

	    public virtual T checkOpen()
	    {
	        if (!instance?.Open ?? true )
			{
				throw new ReqlDriverError("Connection is closed.");
			}
	        return instance;
	    }

	    public virtual void close(bool shouldNoreplyWait)
		{
			if( instance != null)
			{
				try
				{
					if (shouldNoreplyWait)
					{
						noreplyWait();
					}
				}
				finally
				{
					nextToken = 0;
				    instance.close<T>();
                    instance = null;
                }
            }
		}

		private long newToken()
		{
			return Interlocked.Increment(ref nextToken);
		}

		internal virtual Response readResponse(long token, int? deadline)
		{
			return checkOpen().readResponse(token, deadline);
		}

		internal virtual object runQuery(Query query, bool noreply)
		{
			ConnectionInstance inst = checkOpen();
		    if( inst.socket == null )
		        throw new ReqlDriverError("No socket open.");

            inst.socket.write(query.serialize());

			if (noreply)
			{
				return null;
			}

			Response res = inst.readResponse(query.token);

			// TODO: This logic needs to move into the Response class
			Console.WriteLine(res.ToString()); //RSI
			if (res.Atom)
			{
				try
				{
					return Response.convertPseudotypes(res.data, res.profile)[0];
				}
				catch (System.IndexOutOfRangeException ex)
				{
					throw new ReqlDriverError("Atom response was empty!", ex);
				}
			}
			else if (res.Partial || res.Sequence)
			{
				Cursor<T> cursor = Cursor<T>.empty(this, query);
				cursor.extend(res);
				return cursor;
			}
			else if (res.WaitComplete)
			{
				return null;
			}
			else
			{
				throw res.makeError(query);
			}
		}

		internal virtual object runQuery(Query query)
		{
			return runQuery(query, false);
		}

		internal virtual void runQueryNoreply(Query query)
		{
			runQuery(query, true);
		}

		public virtual void noreplyWait()
		{
			runQuery(Query.noreplyWait(newToken()));
		}

		public virtual object run(ReqlAst term, GlobalOptions globalOpts)
		{
			if (globalOpts.Db == null)
			{
			    globalOpts.Db = dbname;
			}
			Query q = Query.start(newToken(), term, globalOpts);
			return runQuery(q, globalOpts.Noreply.GetValueOrDefault(false));
		}

		internal virtual void continue_(Cursor<T> cursor)
		{
			runQueryNoreply(Query.continue_(cursor.token));
		}

		internal virtual void stop(Cursor<T> cursor)
		{
			runQueryNoreply(Query.stop(cursor.token));
		}

		public class Builder<T> where T : ConnectionInstance
		{
			internal readonly Func<T> instanceMaker;
		    internal string hostname_Renamed = null;
		    internal int? port_Renamed = null;
		    internal string dbname = null;
		    internal string authKey_Renamed = null;
		    internal int? timeout_Renamed = null;

			public Builder(Func<T> instanceMaker)
			{
				this.instanceMaker = instanceMaker;
			}
			public virtual Builder<T> hostname(string val)
			{
			    hostname_Renamed = val;
			    return this;
			}
			public virtual Builder<T> port(int val)
			{
			    port_Renamed = val;
			    return this;
			}
			public virtual Builder<T> db(string val)
			{
			    dbname = val;
			    return this;
			}
			public virtual Builder<T> authKey(string val)
			{
			    authKey_Renamed = val;
			    return this;
			}
			public virtual Builder<T> timeout(int val)
			{
			    timeout_Renamed = val;
			    return this;
			}

			public virtual Connection<T> connect()
			{
				Connection<T> conn = new Connection<T>(this);
				conn.reconnect();
				return conn;
			}
		}
	}

}