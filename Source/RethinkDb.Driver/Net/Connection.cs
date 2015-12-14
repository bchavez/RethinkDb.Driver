using System;
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
        void RunNoReply(ReqlAst term, object globalOpts);
    }
    public class Connection : IConnection
    {
        // public immutable
        public readonly string hostname;
        public readonly int port;

        private long nextToken = 0;
        private readonly Func<ConnectionInstance> instanceMaker;

        // private mutable
        private string dbname;
        private readonly TimeSpan? connectTimeout;
        private readonly byte[] handshake;
        private ConnectionInstance instance = null;

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

            instanceMaker = builder._instanceMaker;
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

        public virtual Connection reconnect()
        {
            try
            {
                return reconnect(false, null);
            }
            catch( Exception e )
            {
                throw e;
            }
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

        public virtual void close(bool shouldNoReplyWait = true)
        {
            if( instance != null )
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
                    instance.Close();
                    instance = null;
                }
            }
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
            if( res.IsPartial || res.IsSequence )
            {
                return Cursor<T>.create(this, query, res);
            }
            throw new ReqlDriverError($"The query response can't be converted to a Cursor<T>. The query response is not a SUCCESS_SEQUENCE or SUCCESS_PARTIAL. The response received was {res.Type}. Use `.run` and inspect the object manually. Ensure your query result is a stream that can be turned into a Cursor<T>. Most likely, your query returns an ATOM of object T and you should be using `.runAtom` instead.");
        }

        /// <summary>
        /// Fast ATOM conversion without the DLR dynamic
        /// </summary>
        protected async virtual Task<T> RunQueryAtomAsync<T>(Query query)
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
            throw new ReqlDriverError($"The query response can't be converted to an object of T. The query response is not SUCCESS_ATOM. The response received was {res.Type}. Use `.run` and inspect the response manually. Ensure that your query result is something that can be converted to an object of T.  Most likely your query returns a STREAM and you should be using `.runCursor`.");
        }

        protected async virtual Task RunQueryWaitAsync(Query query)
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
            var inst = checkOpen();
            if( inst.Socket == null ) throw new ReqlDriverError("No socket open.");
            return inst.Socket.SendQuery(query.Token, query.Serialize(), awaitResponse);
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

        internal virtual void RemoveFromCache(long token)
        {
            instance?.RemoveFromCache(token);
        }

        internal virtual void AddToCache<T>(long token, Cursor<T> cursor)
        {
            if (instance == null)
                throw new ReqlDriverError("Can't add to cache when not connected.");

            instance?.AddToCache(token, cursor);
        }

        #endregion

        public static Builder build()
        {
            return new Builder(() => new ConnectionInstance());
        }

        public class Builder
        {
            internal readonly Func<ConnectionInstance> _instanceMaker;
            internal string _hostname = null;
            internal int? _port = null;
            internal string _dbname = null;
            internal string _authKey = null;
            internal TimeSpan? _timeout = null;

            public Builder(Func<ConnectionInstance> instanceMaker)
            {
                this._instanceMaker = instanceMaker;
            }

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
        }
    }
}
