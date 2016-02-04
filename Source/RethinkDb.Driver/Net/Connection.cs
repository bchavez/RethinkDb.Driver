using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Proto;
using RethinkDb.Driver.Utils;

namespace RethinkDb.Driver.Net
{
    public interface IConnection
    {
        Task<dynamic> RunAsync<T>(ReqlAst term, object globalOpts);
        Task<Cursor<T>> RunCursorAsync<T>(ReqlAst term, object globalOpts);
        Task<T> RunAtomAsync<T>(ReqlAst term, object globalOpts);
        Task<T> RunResultAsync<T>(ReqlAst term, object globalOpts);
        void RunNoReply(ReqlAst term, object globalOpts);
    }
    public class Connection : IConnection
    {
        // public immutable
        public readonly string hostname;
        public readonly int port;

        private long nextToken = 0;

        // private mutable
        private string dbname;
        private readonly TimeSpan? connectTimeout;
        private readonly byte[] handshake;


        internal SocketWrapper Socket { get; private set; }


        private readonly ConcurrentDictionary<long, ICursor> cursorCache = new ConcurrentDictionary<long, ICursor>();

        internal Connection(Builder builder)
        {
            dbname = builder._dbname;
            var authKey = builder._authKey ?? string.Empty;
            var authKeyBytes = Encoding.ASCII.GetBytes(authKey);

            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write((int)Proto.Version.V0_4);
                bw.Write(authKeyBytes.Length);
                bw.Write(authKeyBytes);
                bw.Write((int)Proto.Protocol.JSON);
                bw.Flush();
                handshake = ms.ToArray();
            }

            hostname = builder._hostname ?? "localhost";
            port = builder._port ?? RethinkDBConstants.DEFAULT_PORT;
            connectTimeout = builder._timeout;
        }

        public virtual string db()
        {
            return dbname;
        }

        public virtual void use(string db)
        {
            dbname = db;
        }

        public virtual TimeSpan? timeout()
        {
            return connectTimeout;
        }

        public virtual Connection reconnect(bool noreplyWait = false, TimeSpan? timeout = null)
        {
            if( !timeout.HasValue )
            {
                timeout = connectTimeout;
            }
            close(noreplyWait);
            this.Socket = new SocketWrapper(hostname, port, timeout);
            this.Socket.Connect(handshake);
            return this;
        }

        public virtual async Task<Connection> reconnectAsync(bool noreplyWait = false)
        {
            close(noreplyWait);
            this.Socket = new SocketWrapper(hostname, port, connectTimeout);
            await this.Socket.ConnectAsync(handshake);
            return this;
        }

        public virtual bool Open => this.Socket?.Open ?? false;
        public virtual bool HasError => this.Socket?.HasError ?? false;

        public virtual void checkOpen()
        {
            if( !this.Socket?.Open ?? true )
            {
                throw new ReqlDriverError("Connection is closed.");
            }
        }

        public virtual void close(bool shouldNoReplyWait = true)
        {
            if ( this.Socket != null )
            {
                try
                {
                    if( shouldNoReplyWait )
                    {
                        var task = noreplyWaitAsync();
                        task.Wait();
                    }
                }
                finally
                {
                    nextToken = 0;
                    this.Socket.Close();
                    this.Socket = null;
                }
            }

            foreach (var cursor in this.cursorCache.Values)
            {
                cursor.SetError("Connection is closed.");
            }
            cursorCache.Clear();
        }

        public virtual void noreplyWait()
        {
            noreplyWaitAsync().WaitSync();
        }

        public virtual Task noreplyWaitAsync()
        {
            return RunQueryWaitAsync(Query.NoReplyWait(NewToken()));
        }

        public async virtual Task<Server> serverAsync()
        {
            var response = await SendQuery(Query.ServerInfo(NewToken()), awaitResponse: true).ConfigureAwait(false);
            if( response.Type == ResponseType.SERVER_INFO )
            {
                return response.Data[0].ToObject<Server>(Converter.Serializer);
            }
            throw new ReqlDriverError("Did not receive a SERVER_INFO response.");
        }

        public virtual Server server()
        {
            return serverAsync().WaitSync();
        }


        private long NewToken()
        {
            return Interlocked.Increment(ref nextToken);
        }

        protected virtual Task<Response> RunQueryReply(Query query)
        {
            return SendQuery(query, awaitResponse: true);
        }

        protected virtual void RunQueryNoReply(Query query)
        {
            SendQuery(query, awaitResponse: false);
        }

        protected async virtual Task<Cursor<T>> RunQueryCursorAsync<T>(Query query)
        {
            var res = await SendQuery(query, awaitResponse: true).ConfigureAwait(false);
            if( res.IsPartial || res.IsSequence )
            {
                return Cursor<T>.create(this, query, res);
            }
            throw new ReqlDriverError($"The query response cannot be converted to a Cursor<T>. The run helper works with SUCCESS_SEQUENCE or SUCCESS_PARTIAL results. The server response was {res.Type}. If the server response can be handled by this run method check T. Otherwise, if the server response cannot be handled by this run helper use `.runAtom<T>` or `.runResult<T>`.");
        }

        /// <summary>
        /// Fast SUCCESS_ATOM conversion without the DLR dynamic
        /// </summary>
        protected async virtual Task<T> RunQueryAtomAsync<T>(Query query)
        {
            var res = await SendQuery(query, awaitResponse: true).ConfigureAwait(false);
            if (res.IsAtom)
            {
                try
                {
                    return res.Data[0].ToObject<T>(Converter.Serializer);
                }
                catch (IndexOutOfRangeException ex)
                {
                    throw new ReqlDriverError("Atom response was empty!", ex);
                }
            }
            throw new ReqlDriverError($"The query response cannot be converted to an object of T or List<T>. This run helper works with SUCCESS_ATOM results. The server response was {res.Type}. If the server response can be handled by this run method try converting to T or List<T>. Otherwise, if the server response cannot be handled by this run helper use another run helper like `.runCursor` or `.runResult<T>`.");
        }   


        /// <summary>
        /// FAST SUCCESS_ATOM or SUCCESS_SEQUENCE conversion without the DLR dynamic
        /// </summary>
        private async Task<T> RunQueryResultAsync<T>(Query query)
        {
            var res = await SendQuery(query, awaitResponse: true).ConfigureAwait(false);
            if( res.IsAtom )
            {
                try
                {
                    return res.Data[0].ToObject<T>(Converter.Serializer);
                }
                catch( IndexOutOfRangeException ex )
                {
                    throw new ReqlDriverError("Atom response was empty!", ex);
                }
            }
            else if( res.IsSequence )
            {
                return res.Data.ToObject<T>(Converter.Serializer);
            }
            throw new ReqlDriverError($"The query response cannot be converted to an object of T or List<T>. This run helper works with SUCCESS_ATOM or SUCCESS_SEQUENCE results. The server response was {res.Type}. If the server response can be handled by this run method try converting to T or List<T>. Otherwise, if the server response cannot be handled by this run helper use another run helper like `.runCursor`.");
        }

        protected async virtual Task RunQueryWaitAsync(Query query)
        {
            var res = await SendQuery(query, awaitResponse: true).ConfigureAwait(false);
            if( res.IsWaitComplete )
            {
                return;
            }
            throw new ReqlDriverError($"The query response is not WAIT_COMPLETE. The returned query is {res.Type}. You need to call the appropriate run method that handles the response type for your query.");
        }

        protected async Task<dynamic> RunQueryAsync<T>(Query query)
        {
            //If you need to continue after an await, **while inside the driver**, 
            //as a library writer, you must use ConfigureAwait(false) on *your*
            //await to tell the compiler NOT to resume
            //on synchronization context (if one is present).
            //
            //The top most await (your user) will capture the correct synchronization context
            //(if any) when they await on a query's run.
            //
            // https://channel9.msdn.com/Series/Three-Essential-Tips-for-Async/Async-library-methods-should-consider-using-Task-ConfigureAwait-false-
            // http://blogs.msdn.com/b/lucian/archive/2013/11/23/talk-mvp-summit-async-best-practices.aspx
            //
            var res = await SendQuery(query, awaitResponse: true).ConfigureAwait(false);

            if( res.IsAtom )
            {
                try
                {
                    return res.Data[0].ToObject(typeof(T), Converter.Serializer);
                }
                catch( IndexOutOfRangeException ex )
                {
                    throw new ReqlDriverError("Atom response was empty!", ex);
                }
            }
            else if( res.IsPartial || res.IsSequence )
            {
                ICursor cursor = Cursor<T>.create(this, query, res);
                return cursor;
            }
            else if( res.IsWaitComplete )
            {
                return null;
            }
            else
            {
                throw res.MakeError(query);
            }
        }

        protected Task<Response> SendQuery(Query query, bool awaitResponse)
        {
            if( this.Socket == null ) throw new ReqlDriverError("No socket open.");
            return this.Socket.SendQuery(query.Token, query.Serialize(), awaitResponse);
        }

        protected Query PrepareQuery(ReqlAst term, OptArgs globalOpts)
        {
            SetDefaultDb(globalOpts);
            Query q = Query.Start(NewToken(), term, globalOpts);
            if( globalOpts?.ContainsKey("noreply") == true )
            {
                throw new ReqlDriverError("Don't provide the noreply option as an optarg. Use `.runNoReply` instead of `.run`");
            }
            return q;
        }

        protected void SetDefaultDb(OptArgs globalOpts)
        {
            if (globalOpts?.ContainsKey("db") == false && this.dbname != null)
            {
                // Only override the db global arg if the user hasn't
                // specified one already and one is specified on the connection
                globalOpts.with("db", this.dbname);
            }
            if (globalOpts?.ContainsKey("db") == true)
            {
                // The db arg must be wrapped in a db ast object
                globalOpts.with("db", new Db(Arguments.Make(globalOpts["db"])));
            }
        }

        #region REQL AST RUNNERS

        //Typically called by the surface API of ReqlAst.

        Task<dynamic> IConnection.RunAsync<T>(ReqlAst term, object globalOpts)
        {
            Query q = PrepareQuery(term, OptArgs.fromAnonType(globalOpts));
            return RunQueryAsync<T>(q);
        }

        Task<Cursor<T>> IConnection.RunCursorAsync<T>(ReqlAst term, object globalOpts)
        {
            Query q = PrepareQuery(term, OptArgs.fromAnonType(globalOpts));
            return RunQueryCursorAsync<T>(q);
        }

        Task<T> IConnection.RunAtomAsync<T>(ReqlAst term, object globalOpts)
        {
            Query q = PrepareQuery(term, OptArgs.fromAnonType(globalOpts));
            return RunQueryAtomAsync<T>(q);
        }

        Task<T> IConnection.RunResultAsync<T>(ReqlAst term, object globalOpts)
        {
            Query q = PrepareQuery(term, OptArgs.fromAnonType(globalOpts));
            return RunQueryResultAsync<T>(q);
        }

        void IConnection.RunNoReply(ReqlAst term, object globalOpts)
        {
            var opts = OptArgs.fromAnonType(globalOpts);
            SetDefaultDb(opts);
            opts.with("noreply", true);
            RunQueryNoReply(Query.Start(NewToken(), term, opts));
        }

        #endregion

        #region CURSOR SUPPORT

        internal virtual Task<Response> Continue(ICursor cursor)
        {
            return RunQueryReply(Query.Continue(cursor.Token));
        }

        internal virtual Task<Response> Stop(ICursor cursor)
        {
            /*
            neumino: The END query itself doesn't come back with a response
            cowboy: ..... a Query[token,STOP], is like sending a very last CONTINUE, r:[] would contain the last bits of the finished seq
            neumino: Yes a STOP is like a very last CONTINUE
            neumino: If you have a pending CONTINUE, and send a STOP, you should get back two SUCCESS_SEQUENCE
            */
            return RunQueryReply(Query.Stop(cursor.Token));
        }


        internal virtual void AddToCache(long token, ICursor cursor)
        {
            if( this.Socket == null)
                throw new ReqlDriverError("Can't add to cache when not connected.");
            cursorCache[token] = cursor;
        }

        internal virtual void RemoveFromCache(long token)
        {
            ICursor removed;
            if (!cursorCache.TryRemove(token, out removed))
            {
                Log.Trace($"Could not remove cursor token {token} from cursorCache.");
            }
        }

        #endregion

        public static Builder build()
        {
            return new Builder();
        }

        public class Builder
        {
            internal string _hostname = null;
            internal int? _port = null;
            internal string _dbname = null;
            internal string _authKey = null;
            internal TimeSpan? _timeout = null;

            public virtual Builder hostname(string val)
            {
                this._hostname = val;
                return this;
            }

            public virtual Builder port(int val)
            {
                this._port = val;
                return this;
            }

            public virtual Builder db(string val)
            {
                this._dbname = val;
                return this;
            }

            public virtual Builder authKey(string val)
            {
                this._authKey = val;
                return this;
            }

            /// <summary>
            /// Note: Timeout is not used when using connectAsync();
            /// </summary>
            /// <param name="val"></param>
            /// <returns></returns>
            public virtual Builder timeout(int val)
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

            public virtual Task<Connection> connectAsync()
            {
                var conn = new Connection(this);
                return conn.reconnectAsync();
            }
        }
    }
}
