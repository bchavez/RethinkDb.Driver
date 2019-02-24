using Newtonsoft.Json.Linq;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Proto;
using RethinkDb.Driver.Utils;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RethinkDb.Driver.Net {
    /// <summary>
    /// Represents a single connection to a RethinkDB Server.
    /// </summary>
    public class Connection : IConnection {
        private long nextToken = 0;

        private string dbname;
        private readonly TimeSpan? connectTimeout;
        private readonly Handshake handshake;

        internal SocketWrapper Socket { get; private set; }

        private readonly ConcurrentDictionary<long, ICursor> cursorCache = new ConcurrentDictionary<long, ICursor>();
        private readonly SslContext sslContext;

        /// <summary>
        /// Raised when the underlying network connection has thrown an exception.
        /// </summary>
        public event EventHandler<Exception> ConnectionError;

        internal Connection(Builder builder) {
            dbname = builder.dbname;
            if (builder.authKey.IsNotNullOrEmpty() && builder.user.IsNotNullOrEmpty()) {
                throw new ReqlDriverError("Either `authKey` or `user` can be used, but not both.");
            }

            string user = builder.user ?? "admin";
            string password = builder.password ?? builder.authKey ?? "";

            handshake = new Handshake(user, password);

            Hostname = builder.hostname ?? "localhost";
            Port = builder.port ?? RethinkDBConstants.DefaultPort;
            connectTimeout = builder.timeout;
            sslContext = builder.sslContext;
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
        public virtual void Use(string db) {
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
        public virtual void Reconnect(bool noreplyWait = false, TimeSpan? timeout = null) {
            ReconnectAsync(noreplyWait, timeout).WaitSync();
        }

        /// <summary>
        /// Asynchronously reconnects the underlying connection to the server.
        /// </summary>
        /// <param name="noreplyWait"><see cref="NoReplyWait"/></param>
        /// <param name="timeout">The timeout value before throwing exception</param>
        public virtual async Task ReconnectAsync(bool noreplyWait = false, TimeSpan? timeout = null) {
            Close(noreplyWait);
            Socket = new SocketWrapper(Hostname, Port, timeout ?? connectTimeout, sslContext, OnSocketErrorCallback);
            await Socket.ConnectAsync(handshake).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the connection state of the Socket. This property will return the latest
        /// known state of the Socket. When it returns false, the Socket was either never connected
        /// or it is not connected anymore. When it returns true, though, there's no guarantee that the Socket
        /// is still connected, but only that it was connected at the time of the last IO operation.
        /// </summary>
        public virtual bool Open => Socket?.Open ?? false;

        /// <summary>
        /// Retrieves the client-side local endpoint used to connect to the RethinkDB server.
        /// </summary>
        public virtual IPEndPoint ClientEndPoint => Socket?.ClientEndPoint;

        /// <summary>
        /// Retrieves the server-side endpoint of the connection to the RethinkDB server.
        /// </summary>
        public virtual IPEndPoint RemoteEndPoint => Socket?.RemoteEndPoint;

        /// <summary>
        /// Flag to check if the underlying socket has some kind of error.
        /// </summary>
        public virtual bool HasError => Socket?.HasError ?? false;

        /// <summary>
        /// An exception throwing method to check the state of the 
        /// underlying socket.
        /// </summary>
        /// <exception cref="ReqlDriverError">Throws when the underlying socket is closed.</exception>
        public virtual void CheckOpen() {
            if (!Socket?.Open ?? true) {
                throw new ReqlDriverError("Connection is closed.");
            }
        }

        /// <summary>
        /// Called when the underlying <see cref="SocketWrapper"/> encounters an error.
        /// </summary>
        protected void OnSocketErrorCallback(Exception e) {
            CleanUpCursorCache(e.Message);

            //raise event defensively. in case anyone else is subscribed
            //we don't stop processing the rest of the subscribers.
            ConnectionError.FireEvent(this, e);
        }

        /// <summary>
        /// Called when cleanup is needed. Usually when the connection was closed
        /// and can no longer be used. The <see cref="Connection"/> is in a state
        /// where it must be "reconnected" before it can be used again.
        /// </summary>
        protected void CleanUpCursorCache(string message) {
            foreach (ICursor cursor in cursorCache.Values) {
                cursor.SetError(message);
            }
            cursorCache.Clear();
        }

        /// <summary>
        /// Closes the connection.
        /// </summary>
        public void Dispose() {
            Close(false);
        }

        /// <summary>
        /// Closes the connection.
        /// </summary>
        /// <param name="shouldNoReplyWait"><see cref="NoReplyWait"/></param>
        public virtual void Close(bool shouldNoReplyWait = true) {
            if (Socket != null) {
                try {
                    if (shouldNoReplyWait) {
                        NoReplyWait();
                    }
                }
                finally {
                    nextToken = 0;
                    Socket.Close();
                    Socket = null;
                }
            }

            CleanUpCursorCache("The connection is closed.");
        }


        /// <summary>
        /// Ensure that previous queries executed with NoReplyWait have been processed
        /// by the server. Note that this guarantee only apples to queries run on the
        /// same connection.
        /// </summary>
        public virtual void NoReplyWait() {
            NoReplyWaitAsync().WaitSync();
        }

        /// <summary>
        /// Asynchronously ensures that previous queries executed with NoReplyWait have 
        /// been processed by the server. Note that this guarantee only apples to queries
        /// run on the same connection.
        /// </summary>
        public virtual Task NoReplyWaitAsync(CancellationToken cancelToken = default) {
            return RunQueryWaitAsync(Query.NoReplyWait(NewToken()), cancelToken);
        }


        /// <summary>
        /// Return the server name and server UUID being used by a connection.
        /// </summary>
        public virtual async Task<Server> ServerAsync(CancellationToken cancelToken = default) {
            Response response = await SendQuery(Query.ServerInfo(NewToken()), cancelToken, awaitResponse: true).ConfigureAwait(false);
            if (response.Type == ResponseType.SERVER_INFO) {
                return response.Data[0].ToObject<Server>(Converter.Serializer);
            }
            throw new ReqlDriverError("Did not receive a SERVER_INFO response.");
        }

        /// <summary>
        /// Return the server name and server UUID being used by a connection.
        /// </summary>
        public virtual Server Server() {
            return ServerAsync().WaitSync();
        }


        private long NewToken() {
            return Interlocked.Increment(ref nextToken);
        }

        Task<Response> SendQueryReply(Query query) {
            return SendQuery(query, CancellationToken.None, awaitResponse: true);
        }

        void SendQueryNoReply(Query query) {
            SendQuery(query, CancellationToken.None, awaitResponse: false);
        }

        /// <summary>
        /// Fast SUCCESS_SEQUENCE or SUCCESS_PARTIAL conversion without DLR dynamic.
        /// </summary>
        protected virtual async Task<Cursor<T>> RunQueryCursorAsync<T>(Query query, CancellationToken cancelToken) {
            Response res = await SendQuery(query, cancelToken, awaitResponse: true).ConfigureAwait(false);
            if (res.IsPartial || res.IsSequence) {
                return new Cursor<T>(this, query, res);
            }
            if (res.IsError) {
                throw res.MakeError(query);
            }
            throw new ReqlDriverError(
                $"The query response cannot be converted to a Cursor<T>. The run helper " +
                $"works with SUCCESS_SEQUENCE or SUCCESS_PARTIAL results. The server " +
                $"response was {res.Type}. If the server response can be handled by " +
                $"this run method check T. Otherwise, if the server response cannot " +
                $"be handled by this run helper use `.RunAtom<T>` or `.RunResult<T>`.");
        }

        /// <summary>
        /// Fast SUCCESS_ATOM conversion without the DLR dynamic
        /// </summary>
        protected virtual async Task<T> RunQueryAtomAsync<T>(Query query, CancellationToken cancelToken) {
            Response res = await SendQuery(query, cancelToken, awaitResponse: true).ConfigureAwait(false);
            if (res.IsAtom) {
                try {
                    if (typeof(T).IsJToken()) {
                        if (res.Data[0].Type == JTokenType.Null) return (T)(object)null;
                        FormatOptions fmt = FormatOptions.FromOptArgs(query.GlobalOptions);
                        Converter.ConvertPseudoTypes(res.Data[0], fmt);
                        return (T)(object)res.Data[0]; //ugh ugly. find a better way to do this.
                        //return res.Data[0].ToObject<T>();
                    }
                    return res.Data[0].ToObject<T>(Converter.Serializer);

                }
                catch (IndexOutOfRangeException ex) {
                    throw new ReqlDriverError("Atom response was empty!", ex);
                }
            }
            if (res.IsError) {
                throw res.MakeError(query);
            }
            throw new ReqlDriverError(
                $"The query response cannot be converted to an object of T or List<T>. This run helper works with SUCCESS_ATOM results. The server response was {res.Type}. If the server response can be handled by this run method try converting to T or List<T>. Otherwise, if the server response cannot be handled by this run helper use another run helper like `.RunCursor` or `.RunResult<T>`.");
        }


        /// <summary>
        /// Fast SUCCESS_ATOM or SUCCESS_SEQUENCE conversion without the DLR dynamic
        /// </summary>
        private async Task<T> RunQueryResultAsync<T>(Query query, CancellationToken cancelToken) {
            Response res = await SendQuery(query, cancelToken, awaitResponse: true).ConfigureAwait(false);
            if (res.IsAtom) {
                try {
                    if (typeof(T).IsJToken()) {
                        if (res.Data[0].Type == JTokenType.Null) return (T)(object)null;
                        FormatOptions fmt = FormatOptions.FromOptArgs(query.GlobalOptions);
                        Converter.ConvertPseudoTypes(res.Data[0], fmt);
                        return (T)(object)res.Data[0]; //ugh ugly. find a better way to do this.
                    }
                    return res.Data[0].ToObject<T>(Converter.Serializer);
                }
                catch (IndexOutOfRangeException ex) {
                    throw new ReqlDriverError("Atom response was empty!", ex);
                }
            }
            if (res.IsSequence) {
                if (typeof(T).IsJToken()) {
                    FormatOptions fmt = FormatOptions.FromOptArgs(query.GlobalOptions);
                    Converter.ConvertPseudoTypes(res.Data, fmt);
                    return (T)(object)res.Data; //ugh ugly. find a better way to do this.
                }
                return res.Data.ToObject<T>(Converter.Serializer);
            }
            if (res.IsError) {
                throw res.MakeError(query);
            }
            throw new ReqlDriverError(
                $"The query response cannot be converted to an object of T or List<T> " +
                $"because the server response was {res.Type}. The `.RunResult<T>` helper " +
                $"only works with SUCCESS_ATOM or SUCCESS_SEQUENCE responses. When the query " +
                $"response grows larger (over 100K), the response type from the server " +
                $"can change from SUCCESS_SEQUENCE to SUCCESS_PARTIAL; in which case, you'll " +
                $"need to use `.RunCursor` that handles both SUCCESS_SEQUENCE and SUCCESS_PARTIAL " +
                $"response types. The `.RunResult` run helper is only meant to be a " +
                $"convenience method for relatively quick and smaller responses.");
        }

        /// <summary>
        /// Asynchronously ensures that previous queries executed with NoReplyWait have 
        /// been processed by the server. Note that this guarantee only apples to queries
        /// run on the same connection.
        /// </summary>
        protected virtual async Task RunQueryWaitAsync(Query query, CancellationToken cancelToken) {
            Response res = await SendQuery(query, cancelToken, awaitResponse: true).ConfigureAwait(false);
            if (res.IsWaitComplete) {
                return;
            }
            if (res.IsError) {
                throw res.MakeError(query);
            }
            throw new ReqlDriverError(
                $"The query response is not WAIT_COMPLETE. The returned query is {res.Type}. You need to call the appropriate run method that handles the response type for your query.");
        }

        /// <summary>
        /// Run the query but it's return type is standard dynamic.
        /// </summary>
        protected async Task<dynamic> RunQueryAsync<T>(Query query, CancellationToken cancelToken) {
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
            Response res = await SendQuery(query, cancelToken, awaitResponse: true).ConfigureAwait(false);

            if (res.IsAtom) {
                try {
                    if (typeof(T).IsJToken()) {
                        if (res.Data[0].Type == JTokenType.Null) return null;
                        FormatOptions fmt = FormatOptions.FromOptArgs(query.GlobalOptions);
                        Converter.ConvertPseudoTypes(res.Data[0], fmt);
                        return res.Data[0];
                    }
                    return res.Data[0].ToObject(typeof(T), Converter.Serializer);
                }
                catch (IndexOutOfRangeException ex) {
                    throw new ReqlDriverError("Atom response was empty!", ex);
                }
            }
            else if (res.IsPartial || res.IsSequence) {
                return new Cursor<T>(this, query, res);
            }
            else if (res.IsWaitComplete) {
                return null;
            }
            else {
                throw res.MakeError(query);
            }
        }

        /// <summary>
        /// Sends the query over to the underlying socket for sending.
        /// </summary>
        protected Task<Response> SendQuery(Query query, CancellationToken cancelToken, bool awaitResponse) {
            if (Socket == null) throw new ReqlDriverError("No socket open.");
            return Socket.SendQuery(query.Token, query.Serialize(), awaitResponse, cancelToken);
        }

        /// <summary>
        /// Prepares the query by setting the default DB if it doesn't exist.
        /// </summary>
        protected Query PrepareQuery(ReqlAst term, OptArgs globalOpts) {
            SetDefaultDb(globalOpts);
            Query q = Query.Start(NewToken(), term, globalOpts);
            if (globalOpts?.ContainsKey("noreply") == true) {
                throw new ReqlDriverError("Don't provide the noreply option as an optarg. Use `.RunNoReply` instead of `.Run`");
            }
            return q;
        }

        /// <summary>
        /// Sets the database if it's not already set.
        /// </summary>
        protected void SetDefaultDb(OptArgs globalOpts) {
            if (globalOpts?.ContainsKey("db") == false && dbname != null) {
                // Only override the db global arg if the user hasn't
                // specified one already and one is specified on the connection
                globalOpts.With("db", dbname);
            }
            if (globalOpts?.ContainsKey("db") == true) {
                // The db arg must be wrapped in a db ast object
                globalOpts.With("db", new Db(Arguments.Make(globalOpts["db"])));
            }
        }

        #region REQL AST RUNNERS

        //Typically called by the surface API of ReqlAst.

        Task<dynamic> IConnection.RunAsync<T>(ReqlAst term, object globalOpts, CancellationToken cancelToken) {
            Query q = PrepareQuery(term, OptArgs.FromAnonType(globalOpts));
            return RunQueryAsync<T>(q, cancelToken);
        }

        Task<Cursor<T>> IConnection.RunCursorAsync<T>(ReqlAst term, object globalOpts, CancellationToken cancelToken) {
            Query q = PrepareQuery(term, OptArgs.FromAnonType(globalOpts));
            return RunQueryCursorAsync<T>(q, cancelToken);
        }

        Task<T> IConnection.RunAtomAsync<T>(ReqlAst term, object globalOpts, CancellationToken cancelToken) {
            Query q = PrepareQuery(term, OptArgs.FromAnonType(globalOpts));
            return RunQueryAtomAsync<T>(q, cancelToken);
        }

        Task<T> IConnection.RunResultAsync<T>(ReqlAst term, object globalOpts, CancellationToken cancelToken) {
            Query q = PrepareQuery(term, OptArgs.FromAnonType(globalOpts));
            return RunQueryResultAsync<T>(q, cancelToken);
        }

        void IConnection.RunNoReply(ReqlAst term, object globalOpts) {
            OptArgs opts = OptArgs.FromAnonType(globalOpts);
            SetDefaultDb(opts);
            opts.With("noreply", true);
            SendQueryNoReply(Query.Start(NewToken(), term, opts));
        }

        async Task<string> IConnection.RunResultAsRawJson(ReqlAst term, object globalOpts, CancellationToken cancelToken) {
            Query q = PrepareQuery(term, OptArgs.FromAnonType(globalOpts));
            Response res = await SendQuery(q, cancelToken, awaitResponse: true).ConfigureAwait(false);
            if (res.IsError)
                throw res.MakeError(q);
            return res.Data.ToString();
        }

        #endregion

        #region CURSOR SUPPORT

        internal virtual Task<Response> Continue(ICursor cursor) {
            return SendQueryReply(Query.Continue(cursor.Token));
        }

        internal virtual void Stop(ICursor cursor) {
            /*
            neumino: The END query itself doesn't come back with a response
            cowboy: ..... a Query[token,STOP], is like sending a very last CONTINUE, r:[] would contain the last bits of the finished seq
            neumino: Yes a STOP is like a very last CONTINUE
            neumino: If you have a pending CONTINUE, and send a STOP, you should get back two SUCCESS_SEQUENCE
            */
            //this.Socket?.CancelAwaiter(cursor.Token);
            SendQueryNoReply(Query.Stop(cursor.Token));
        }


        internal void AddToCache(long token, ICursor cursor) {
            if (Socket == null)
                throw new ReqlDriverError("Can't add to cache when not connected.");
            cursorCache[token] = cursor;
        }

        internal void RemoveFromCache(long token) {
            ICursor removed;
            if (!cursorCache.TryRemove(token, out removed)) {
                Log.Trace($"Could not remove cursor token {token} from cursorCache.");
            }
        }

        #endregion

        internal static Builder Build() {
            return new Builder();
        }





        /// <summary>
        /// The Connection builder.
        /// </summary>
        public class Builder : IConnectionBuilder<Builder> {
            internal string hostname = null;
            internal int? port = null;
            internal string dbname = null;
            internal TimeSpan? timeout = null;
            internal string authKey = null;
            internal string user = null;
            internal string password = null;
            internal SslContext sslContext = null;

            /// <summary>
            /// The hostname or IP address of the server.
            /// </summary>
            /// <param name="hostnameOrIp">Hostname or IP address</param>
            public virtual Builder Hostname(string hostnameOrIp) {
                hostname = hostnameOrIp;
                return this;
            }

            /// <summary>
            /// The TCP port to connect with.
            /// </summary>
            public virtual Builder Port(int driverPort) {
                port = driverPort;
                return this;
            }

            /// <summary>
            /// The default DB for queries.
            /// </summary>
            public virtual Builder Db(string database) {
                dbname = database;
                return this;
            }

            /// <summary>
            /// The authorization key to the server.
            /// </summary>
            public virtual Builder AuthKey(string key) {
                authKey = key;
                return this;
            }

            /// <summary>
            /// The user account and password to connect as (default "admin", "").
            /// </summary>
            public virtual Builder User(string user, string password) {
                this.user = user;
                this.password = password;
                return this;
            }

            /// <summary>
            /// Note: Timeout is not used when using connectAsync();
            /// </summary>
            /// <param name="seconds"></param>
            /// <returns></returns>
            public virtual Builder Timeout(int seconds) {
                timeout = TimeSpan.FromSeconds(seconds);
                return this;
            }

            /// <summary>
            /// Creates and establishes the connection using the specified settings.
            /// </summary>
            public virtual Connection Connect() {
                return ConnectAsync().WaitSync();
            }

            /// <summary>
            /// Asynchronously creates and establishes the connection using the specified settings.
            /// </summary>
            public virtual async Task<Connection> ConnectAsync() {
                Connection conn = new Connection(this);
                await conn.ReconnectAsync().ConfigureAwait(false);
                return conn;
            }

            /// <summary>
            /// Enables SSL over the driver port.
            /// </summary>
            /// <param name="context">Context settings for the SSL stream.</param>
            public virtual Builder EnableSsl(SslContext context, string licenseTo, string licenseKey) {
                sslContext = context;

#if !DEBUG
                if( !LicenseVerifier.VerifyLicense(licenseTo, licenseKey) )
                {
                    throw new ReqlDriverError("The SSL/TLS usage license is invalid. Please check your license that you copied all the characters in your license. If you still have trouble, please contact support@bitarmory.com.");
                }
#endif
                return this;
            }
        }
    }

    internal sealed class LicenseVerifier {
        public static bool VerifyLicense(string licenseTo, string licenseKey) {
            AssertKeyIsNotBanned(licenseKey);

            const string modulusString =
                "tccosXHxEfxzsO1NgsAbGT0X+WuQMUYcj7r2UJCHDqqq3TbR7UoAgzjm3MV1k/eJxRnURypw2wj98eafyajeprk3lK6BnWa5tG8S7p3QU5kkUo7TnmTj8rRkTk9RwArIGGjKfCVZToE06UQudsJmw1UK3mku1qF0/hD909cSiCM=";
            const string exponentString = "AQAB";

            byte[] data = Encoding.UTF8.GetBytes(licenseTo);

            RSAParameters rsaParameters = new RSAParameters {
                Modulus = Convert.FromBase64String(modulusString),
                Exponent = Convert.FromBase64String(exponentString)
            };
#if STANDARD
            using( var rsa = System.Security.Cryptography.RSA.Create() )
#else
            using (RSACryptoServiceProvider rsa = new System.Security.Cryptography.RSACryptoServiceProvider())
#endif
            {
                byte[] licenseData = Convert.FromBase64String(licenseKey);
                rsa.ImportParameters(rsaParameters);

                bool verified = false;

#if STANDARD
                verified = rsa.VerifyData(data, licenseData, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
#else
                verified = rsa.VerifyData(data, CryptoConfig.MapNameToOID("SHA256"), licenseData);
#endif
                return verified;
            }
        }

        private static void AssertKeyIsNotBanned(string licenseKey) {
        }
    }
}