using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Proto;
using RethinkDb.Driver.Utils;

namespace RethinkDb.Driver.Net
{
    /// <summary>
    /// Represents a single connection to a RethinkDB Server.
    /// </summary>
    public class Connection : IConnection
    {
        private long nextToken = 0;

        private string dbname;
        private readonly TimeSpan? connectTimeout;
        private readonly Handshake handshake;

        internal SocketWrapper Socket { get; private set; }

        private readonly ConcurrentDictionary<long, ICursor> cursorCache = new ConcurrentDictionary<long, ICursor>();

        /// <summary>
        /// Raised when the underlying network connection has thrown an exception.
        /// </summary>
        public event EventHandler<Exception> ConnectionError;

        internal Connection(Builder builder)
        {
            dbname = builder.dbname;
            if( builder.authKey.IsNotNullOrEmpty() && builder.user.IsNotNullOrEmpty() )
            {
                throw new ReqlDriverError("Either `authKey` or `user` can be used, but not both.");
            }

            var user = builder.user ?? "admin";
            var password = builder.password ?? builder.authKey ?? "";

            this.handshake = new Handshake(user, password);

            this.Hostname = builder.hostname ?? "localhost";
            this.Port = builder.port ?? RethinkDBConstants.DefaultPort;
            connectTimeout = builder.timeout;
        }

        /// <summary>
        /// Hostname assigned to the connection.
        /// </summary>
        public string Hostname { get; }

        /// <summary>
        /// TCP port number assigned to the connection.
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// Current default database for queries on the connection. To change default database, <see cref="Use"/>
        /// </summary>
        public virtual string Db => dbname;

        /// <summary>
        /// Changes the default database on the connection.
        /// </summary>
        public virtual void Use(string db)
        {
            dbname = db;
        }

        /// <summary>
        /// Returns the connection timeout setting.
        /// </summary>
        public virtual TimeSpan? Timeout => connectTimeout;

        /// <summary>
        /// Reconnects the underlying connection to the server.
        /// </summary>
        /// <param name="noreplyWait"><see cref="NoReplyWait"/></param>
        /// <param name="timeout">The timeout value before throwing exception</param>
        public virtual void Reconnect(bool noreplyWait = false, TimeSpan? timeout = null)
        {
            if( !timeout.HasValue )
            {
                timeout = connectTimeout;
            }
            Close(noreplyWait);
            this.Socket = new SocketWrapper(this.Hostname, this.Port, timeout, OnSocketErrorCallback);
            this.Socket.Connect(handshake);
        }

        /// <summary>
        /// Asynchronously reconnects the underlying connection to the server.
        /// </summary>
        /// <param name="noreplyWait"><see cref="NoReplyWait"/></param>
        public virtual async Task ReconnectAsync(bool noreplyWait = false)
        {
            Close(noreplyWait);
            this.Socket = new SocketWrapper(this.Hostname, this.Port, connectTimeout, OnSocketErrorCallback);
            await this.Socket.ConnectAsync(handshake).ConfigureAwait(false);
        }

        /// <summary>
        /// Flag to check the underlying socket is connected.
        /// </summary>
        public virtual bool Open => this.Socket?.Open ?? false;

        /// <summary>
        /// Retrieves the client-side local endpoint used to connect to the RethinkDB server.
        /// </summary>
        public virtual IPEndPoint ClientEndPoint => this.Socket?.ClientEndPoint;

        /// <summary>
        /// Flag to check if the underlying socket has some kind of error.
        /// </summary>
        public virtual bool HasError => this.Socket?.HasError ?? false;

        /// <summary>
        /// An exception throwing method to check the state of the 
        /// underlying socket.
        /// </summary>
        /// <exception cref="ReqlDriverError">Throws when the underlying socket is closed.</exception>
        public virtual void CheckOpen()
        {
            if( !this.Socket?.Open ?? true )
            {
                throw new ReqlDriverError("Connection is closed.");
            }
        }

        /// <summary>
        /// Called when the underlying <see cref="SocketWrapper"/> encounters an error.
        /// </summary>
        protected void OnSocketErrorCallback(Exception e)
        {
            CleanUpCursorCache(e.Message);

            //raise event defensively. in case anyone else is subscribed
            //we don't stop processing the rest of the subscribers.
            this.ConnectionError.FireEvent(this, e);
        }

        /// <summary>
        /// Called when cleanup is needed. Usually when the connection was closed
        /// and can no longer be used. The <see cref="Connection"/> is in a state
        /// where it must be "reconnected" before it can be used again.
        /// </summary>
        protected void CleanUpCursorCache(string message)
        {
            foreach( var cursor in this.cursorCache.Values )
            {
                cursor.SetError(message);
            }
            cursorCache.Clear();
        }

        /// <summary>
        /// Closes the connection.
        /// </summary>
        public void Dispose()
        {
            this.Close(false);
        }

        /// <summary>
        /// Closes the connection.
        /// </summary>
        /// <param name="shouldNoReplyWait"><see cref="NoReplyWait"/></param>
        public virtual void Close(bool shouldNoReplyWait = true)
        {
            if( this.Socket != null )
            {
                try
                {
                    if( shouldNoReplyWait )
                    {
                        NoReplyWait();
                    }
                }
                finally
                {
                    nextToken = 0;
                    this.Socket.Close();
                    this.Socket = null;
                }
            }

            CleanUpCursorCache("The connection is closed.");
        }


        /// <summary>
        /// Ensure that previous queries executed with NoReplyWait have been processed
        /// by the server. Note that this guarantee only apples to queries run on the
        /// same connection.
        /// </summary>
        public virtual void NoReplyWait()
        {
            NoReplyWaitAsync().WaitSync();
        }

        /// <summary>
        /// Asynchronously ensures that previous queries executed with NoReplyWait have 
        /// been processed by the server. Note that this guarantee only apples to queries
        /// run on the same connection.
        /// </summary>
        public virtual Task NoReplyWaitAsync(CancellationToken cancelToken = default(CancellationToken))
        {
            return RunQueryWaitAsync(Query.NoReplyWait(NewToken()), cancelToken);
        }


        /// <summary>
        /// Return the server name and server UUID being used by a connection.
        /// </summary>
        public virtual async Task<Server> ServerAsync(CancellationToken cancelToken = default(CancellationToken))
        {
            var response = await SendQuery(Query.ServerInfo(NewToken()), cancelToken, awaitResponse: true).ConfigureAwait(false);
            if( response.Type == ResponseType.SERVER_INFO )
            {
                return response.Data[0].ToObject<Server>(Converter.Serializer);
            }
            throw new ReqlDriverError("Did not receive a SERVER_INFO response.");
        }

        /// <summary>
        /// Return the server name and server UUID being used by a connection.
        /// </summary>
        public virtual Server Server()
        {
            return ServerAsync().WaitSync();
        }


        private long NewToken()
        {
            return Interlocked.Increment(ref nextToken);
        }

        Task<Response> SendQueryReply(Query query)
        {
            return SendQuery(query, CancellationToken.None, awaitResponse: true);
        }

        void SendQueryNoReply(Query query)
        {
            SendQuery(query, CancellationToken.None, awaitResponse: false);
        }

        /// <summary>
        /// Fast SUCCESS_SEQUENCE or SUCCESS_PARTIAL conversion without DLR dynamic.
        /// </summary>
        protected virtual async Task<Cursor<T>> RunQueryCursorAsync<T>(Query query, CancellationToken cancelToken)
        {
            var res = await SendQuery(query, cancelToken, awaitResponse: true).ConfigureAwait(false);
            if( res.IsPartial || res.IsSequence )
            {
                return new Cursor<T>(this, query, res);
            }
            if (res.IsError)
            {
                throw res.MakeError(query);
            }
            throw new ReqlDriverError(
                $"The query response cannot be converted to a Cursor<T>. The run helper works with SUCCESS_SEQUENCE or SUCCESS_PARTIAL results. The server response was {res.Type}. If the server response can be handled by this run method check T. Otherwise, if the server response cannot be handled by this run helper use `.RunAtom<T>` or `.RunResult<T>`.");
        }

        /// <summary>
        /// Fast SUCCESS_ATOM conversion without the DLR dynamic
        /// </summary>
        protected virtual async Task<T> RunQueryAtomAsync<T>(Query query, CancellationToken cancelToken)
        {
            var res = await SendQuery(query, cancelToken, awaitResponse: true).ConfigureAwait(false);
            if( res.IsAtom )
            {
                try
                {
                    if( typeof(T).IsJToken() )
                    {
                        var fmt = FormatOptions.FromOptArgs(query.GlobalOptions);
                        Converter.ConvertPseudoTypes(res.Data[0], fmt);
                        return (T)(object)res.Data[0]; //ugh ugly. find a better way to do this.
                    }
                    return res.Data[0].ToObject<T>(Converter.Serializer);
                    
                }
                catch( IndexOutOfRangeException ex )
                {
                    throw new ReqlDriverError("Atom response was empty!", ex);
                }
            }
            if( res.IsError )
            {
                throw res.MakeError(query);
            }
            throw new ReqlDriverError(
                $"The query response cannot be converted to an object of T or List<T>. This run helper works with SUCCESS_ATOM results. The server response was {res.Type}. If the server response can be handled by this run method try converting to T or List<T>. Otherwise, if the server response cannot be handled by this run helper use another run helper like `.RunCursor` or `.RunResult<T>`.");
        }


        /// <summary>
        /// Fast SUCCESS_ATOM or SUCCESS_SEQUENCE conversion without the DLR dynamic
        /// </summary>
        private async Task<T> RunQueryResultAsync<T>(Query query, CancellationToken cancelToken)
        {
            var res = await SendQuery(query, cancelToken, awaitResponse: true).ConfigureAwait(false);
            if( res.IsAtom )
            {
                try
                {
                    if( typeof(T).IsJToken() )
                    {
                        var fmt = FormatOptions.FromOptArgs(query.GlobalOptions);
                        Converter.ConvertPseudoTypes(res.Data[0], fmt);
                        return (T)(object)res.Data[0]; //ugh ugly. find a better way to do this.
                    }
                    return res.Data[0].ToObject<T>(Converter.Serializer);
                }
                catch ( IndexOutOfRangeException ex )
                {
                    throw new ReqlDriverError("Atom response was empty!", ex);
                }
            }
            if( res.IsSequence )
            {
                if( typeof(T).IsJToken() )
                {
                    var fmt = FormatOptions.FromOptArgs(query.GlobalOptions);
                    Converter.ConvertPseudoTypes(res.Data, fmt);
                    return (T)(object)res.Data; //ugh ugly. find a better way to do this.
                }
                return res.Data.ToObject<T>(Converter.Serializer);
            }
            if (res.IsError)
            {
                throw res.MakeError(query);
            }
            throw new ReqlDriverError(
                $"The query response cannot be converted to an object of T or List<T>. This run helper works with SUCCESS_ATOM or SUCCESS_SEQUENCE results. The server response was {res.Type}. If the server response can be handled by this run method try converting to T or List<T>. Otherwise, if the server response cannot be handled by this run helper use another run helper like `.RunCursor`.");
        }

        /// <summary>
        /// Asynchronously ensures that previous queries executed with NoReplyWait have 
        /// been processed by the server. Note that this guarantee only apples to queries
        /// run on the same connection.
        /// </summary>
        protected virtual async Task RunQueryWaitAsync(Query query, CancellationToken cancelToken)
        {
            var res = await SendQuery(query, cancelToken, awaitResponse: true).ConfigureAwait(false);
            if( res.IsWaitComplete )
            {
                return;
            }
            if (res.IsError)
            {
                throw res.MakeError(query);
            }
            throw new ReqlDriverError(
                $"The query response is not WAIT_COMPLETE. The returned query is {res.Type}. You need to call the appropriate run method that handles the response type for your query.");
        }

        /// <summary>
        /// Run the query but it's return type is standard dynamic.
        /// </summary>
        protected async Task<dynamic> RunQueryAsync<T>(Query query, CancellationToken cancelToken)
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
            var res = await SendQuery(query, cancelToken, awaitResponse: true).ConfigureAwait(false);

            if( res.IsAtom )
            {
                try
                {
                    if( typeof(T).IsJToken() )
                    {
                        var fmt = FormatOptions.FromOptArgs(query.GlobalOptions);
                        Converter.ConvertPseudoTypes(res.Data[0], fmt);
                        return res.Data[0];
                    }
                    return res.Data[0].ToObject(typeof(T), Converter.Serializer);
                }
                catch ( IndexOutOfRangeException ex )
                {
                    throw new ReqlDriverError("Atom response was empty!", ex);
                }
            }
            else if( res.IsPartial || res.IsSequence )
            {
                return new Cursor<T>(this, query, res);
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

        /// <summary>
        /// Sends the query over to the underlying socket for sending.
        /// </summary>
        protected Task<Response> SendQuery(Query query, CancellationToken cancelToken, bool awaitResponse)
        {
            if( this.Socket == null ) throw new ReqlDriverError("No socket open.");
            return this.Socket.SendQuery(query.Token, query.Serialize(), awaitResponse, cancelToken);
        }

        /// <summary>
        /// Prepares the query by setting the default DB if it doesn't exist.
        /// </summary>
        protected Query PrepareQuery(ReqlAst term, OptArgs globalOpts)
        {
            SetDefaultDb(globalOpts);
            Query q = Query.Start(NewToken(), term, globalOpts);
            if( globalOpts?.ContainsKey("noreply") == true )
            {
                throw new ReqlDriverError("Don't provide the noreply option as an optarg. Use `.RunNoReply` instead of `.Run`");
            }
            return q;
        }

        /// <summary>
        /// Sets the database if it's not already set.
        /// </summary>
        protected void SetDefaultDb(OptArgs globalOpts)
        {
            if( globalOpts?.ContainsKey("db") == false && this.dbname != null )
            {
                // Only override the db global arg if the user hasn't
                // specified one already and one is specified on the connection
                globalOpts.With("db", this.dbname);
            }
            if( globalOpts?.ContainsKey("db") == true )
            {
                // The db arg must be wrapped in a db ast object
                globalOpts.With("db", new Db(Arguments.Make(globalOpts["db"])));
            }
        }

        #region REQL AST RUNNERS

        //Typically called by the surface API of ReqlAst.

        Task<dynamic> IConnection.RunAsync<T>(ReqlAst term, object globalOpts, CancellationToken cancelToken)
        {
            Query q = PrepareQuery(term, OptArgs.FromAnonType(globalOpts));
            return RunQueryAsync<T>(q, cancelToken);
        }

        Task<Cursor<T>> IConnection.RunCursorAsync<T>(ReqlAst term, object globalOpts, CancellationToken cancelToken)
        {
            Query q = PrepareQuery(term, OptArgs.FromAnonType(globalOpts));
            return RunQueryCursorAsync<T>(q, cancelToken);
        }

        Task<T> IConnection.RunAtomAsync<T>(ReqlAst term, object globalOpts, CancellationToken cancelToken)
        {
            Query q = PrepareQuery(term, OptArgs.FromAnonType(globalOpts));
            return RunQueryAtomAsync<T>(q, cancelToken);
        }

        Task<T> IConnection.RunResultAsync<T>(ReqlAst term, object globalOpts, CancellationToken cancelToken)
        {
            Query q = PrepareQuery(term, OptArgs.FromAnonType(globalOpts));
            return RunQueryResultAsync<T>(q, cancelToken);
        }

        void IConnection.RunNoReply(ReqlAst term, object globalOpts)
        {
            var opts = OptArgs.FromAnonType(globalOpts);
            SetDefaultDb(opts);
            opts.With("noreply", true);
            SendQueryNoReply(Query.Start(NewToken(), term, opts));
        }

        #endregion

        #region CURSOR SUPPORT

        internal virtual Task<Response> Continue(ICursor cursor)
        {
            return SendQueryReply(Query.Continue(cursor.Token));
        }

        internal virtual void Stop(ICursor cursor)
        {
            /*
            neumino: The END query itself doesn't come back with a response
            cowboy: ..... a Query[token,STOP], is like sending a very last CONTINUE, r:[] would contain the last bits of the finished seq
            neumino: Yes a STOP is like a very last CONTINUE
            neumino: If you have a pending CONTINUE, and send a STOP, you should get back two SUCCESS_SEQUENCE
            */
            //this.Socket?.CancelAwaiter(cursor.Token);
            SendQueryNoReply(Query.Stop(cursor.Token));
        }


        internal void AddToCache(long token, ICursor cursor)
        {
            if( this.Socket == null )
                throw new ReqlDriverError("Can't add to cache when not connected.");
            cursorCache[token] = cursor;
        }

        internal void RemoveFromCache(long token)
        {
            ICursor removed;
            if( !cursorCache.TryRemove(token, out removed) )
            {
                Log.Trace($"Could not remove cursor token {token} from cursorCache.");
            }
        }

        #endregion

        internal static Builder Build()
        {
            return new Builder();
        }

        /// <summary>
        /// The Connection builder.
        /// </summary>
        public class Builder
        {
            internal string hostname = null;
            internal int? port = null;
            internal string dbname = null;
            internal TimeSpan? timeout = null;
            internal string authKey = null;
            internal string user = null;
            internal string password = null;

            /// <summary>
            /// The hostname or IP address of the server.
            /// </summary>
            /// <param name="val">Hostname or IP address</param>
            public virtual Builder Hostname(string val)
            {
                this.hostname = val;
                return this;
            }

            /// <summary>
            /// The TCP port to connect with.
            /// </summary>
            public virtual Builder Port(int val)
            {
                this.port = val;
                return this;
            }

            /// <summary>
            /// The default DB for queries.
            /// </summary>
            public virtual Builder Db(string val)
            {
                this.dbname = val;
                return this;
            }

            /// <summary>
            /// The authorization key to the server.
            /// </summary>
            public virtual Builder AuthKey(string key)
            {
                this.authKey = key;
                return this;
            }

            /// <summary>
            /// The user account and password to connect as (default "admin", "").
            /// </summary>
            public virtual Builder User(string user, string password)
            {
                this.user = user;
                this.password = password;
                return this;
            }

            /// <summary>
            /// Note: Timeout is not used when using connectAsync();
            /// </summary>
            /// <param name="val"></param>
            /// <returns></returns>
            public virtual Builder Timeout(int val)
            {
                this.timeout = TimeSpan.FromSeconds(val);
                return this;
            }

            /// <summary>
            /// Creates and establishes the connection using the specified settings.
            /// </summary>
            public virtual Connection Connect()
            {
                var conn = new Connection(this);
                conn.Reconnect();
                return conn;
            }

            /// <summary>
            /// Asynchronously creates and establishes the connection using the specified settings.
            /// </summary>
            public virtual async Task<Connection> ConnectAsync()
            {
                var conn = new Connection(this);
                await conn.ReconnectAsync().ConfigureAwait(false);
                return conn;
            }
        }
    }
}