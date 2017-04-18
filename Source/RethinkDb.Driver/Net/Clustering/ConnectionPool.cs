using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Utils;

namespace RethinkDb.Driver.Net.Clustering
{
    /// <summary>
    /// Represents a pooled connection to a RethinkDB cluster.
    /// </summary>
    public class ConnectionPool : IConnection
    {
        private string user;
        private string password;
        private string dbname;
        private Seed[] seeds;
        private bool discover;
        private IPoolingStrategy poolingStrategy;
        private TimeSpan? initialTimeout;
        private SslContext sslContext;

        /// <summary>
        /// The default database used by queries.
        /// </summary>
        public string Db => this.dbname;

        #region REQL AST RUNNERS

        Task<dynamic> IConnection.RunAsync<T>(ReqlAst term, object globalOpts, CancellationToken cancelToken)
        {
            if( this.shutdownSignal.IsCancellationRequested )
            {
                throw new ReqlDriverError("HostPool is shutdown.");
            }
            return poolingStrategy.RunAsync<T>(term, globalOpts, cancelToken);
        }

        Task<Cursor<T>> IConnection.RunCursorAsync<T>(ReqlAst term, object globalOpts, CancellationToken cancelToken)
        {
            if( this.shutdownSignal.IsCancellationRequested )
            {
                throw new ReqlDriverError("HostPool is shutdown.");
            }
            return poolingStrategy.RunCursorAsync<T>(term, globalOpts, cancelToken);
        }

        Task<T> IConnection.RunAtomAsync<T>(ReqlAst term, object globalOpts, CancellationToken cancelToken)
        {
            if( this.shutdownSignal.IsCancellationRequested )
            {
                throw new ReqlDriverError("HostPool is shutdown.");
            }
            return poolingStrategy.RunAtomAsync<T>(term, globalOpts, cancelToken);
        }

        Task<T> IConnection.RunResultAsync<T>(ReqlAst term, object globalOpts, CancellationToken cancelToken)
        {
            if( this.shutdownSignal.IsCancellationRequested )
            {
                throw new ReqlDriverError("HostPool is shutdown.");
            }
            return poolingStrategy.RunResultAsync<T>(term, globalOpts, cancelToken);
        }

        void IConnection.RunNoReply(ReqlAst term, object globalOpts)
        {
            if( this.shutdownSignal.IsCancellationRequested )
            {
                throw new ReqlDriverError("HostPool is shutdown.");
            }
            poolingStrategy.RunNoReply(term, globalOpts);
        }

        #endregion

        internal ConnectionPool(Builder builder)
        {
            user = builder.user ?? "admin";
            password = builder.password ?? builder.authKey ?? "";
            dbname = builder.dbname;
            seeds = builder.seeds;
            discover = builder.discover;
            poolingStrategy = builder.hostpool;
            initialTimeout = builder.initialTimeout;
            sslContext = builder.sslContext;
        }


        /// <summary>
        /// Shutdown the connection pool.
        /// </summary>
        public void Shutdown()
        {
            shutdownSignal?.Cancel();
            poolingStrategy?.Shutdown();

            if( poolingStrategy != null )
            {
                //shutdown all connections.
                foreach( var h in poolingStrategy.HostList )
                {
                    var conn = h.conn as Connection;
                    conn.Close(false);
                    h.MarkFailed();
                }
            }
        }

        /// <summary>
        /// Used to determine if a node is currently available in the host pool
        /// to receive queries. The boolean value should be considered a "rough" estimate
        /// of the availability of a node in the pool. This indicator may lag behind
        /// as it does not check the exact state of every node's TCP socket.
        /// </summary>
        public bool AnyOpen => this.poolingStrategy.HostList.Any(h => !h.Dead);

        private CancellationTokenSource shutdownSignal;
        private TaskCompletionSource<ConnectionPool> poolReady;

        /// <summary>
        /// Starts the connection pool.
        /// </summary>
        protected virtual void StartPool()
        {
            shutdownSignal = new CancellationTokenSource();
            poolReady = new TaskCompletionSource<ConnectionPool>();

            if( poolingStrategy == null )
            {
                throw new ArgumentNullException(nameof(poolingStrategy),
                    $"You must specify a pooling strategy '{nameof(Builder.PoolingStrategy)}' when building the connection pool.");
            }

            var initialSeeds = this.seeds.Select(seed =>
                {
                    var conn = NewPoolConnection(seed.IpAddress, seed.Port);
                    return new {conn, EndPointId = EndpointIdentifier(seed.IpAddress, seed.Port) };
                });

            foreach( var conn in initialSeeds )
            {
                this.poolingStrategy.AddHost(conn.EndPointId, conn.conn);
            }

            Task.Factory.StartNew(Supervisor, TaskCreationOptions.LongRunning);
            if( discover )
            {
                Task.Factory.StartNew(Discoverer, TaskCreationOptions.LongRunning);
            }
        }

        /// <summary>
        /// This thread is in charge of discovering new hosts via rethinkdb system
        /// table change feed. If a new host is found it is added to the host pool.
        /// </summary>
        private void Discoverer()
        {
            var r = RethinkDB.R;

            var changeFeed = r.Db("rethinkdb").Table("server_status").Changes()[new {include_initial = true}];

            while( true )
            {
                if( shutdownSignal.IsCancellationRequested )
                {
                    Log.Debug($"{nameof(Discoverer)}: Shutdown Signal Received");
                    break;
                }

                try
                {
                    poolReady.Task.Wait();
                }
                catch
                {
                    Log.Trace($"{nameof(Discoverer)}: Pool is not ready to discover new hosts.");
                    Thread.Sleep(1000);
                }

                try
                {
                    var cursor = changeFeed.RunChanges<Server>(this);

                    foreach( var change in cursor )
                    {
                        if( change.NewValue != null )
                        {
                            MaybeAddNewHost(change.NewValue);
                        }
                    }
                }
                catch
                {
                    Log.Trace($"{nameof(Discoverer)}: Discover change feed broke.");
                    Thread.Sleep(1000);
                }
            }
        }

        /// <summary>
        /// Called by the discoverer thread when a new server has been added (or comes back
        /// online). This method will determine if AddHost is needed. We only have monotonically
        /// increasing hosts to prevent unnecessary locking on the host list array.
        /// </summary>
        private void MaybeAddNewHost(Server server)
        {
            //Ok, could be initial or new host.
            //either way, see if we need to add
            //the connection if it has not been
            //discovered.
            var port = server.Network.ReqlPort;

            var realAddresses = server.Network.CanonicalAddress
                .Where(s => // no localhost and no ipv6. for now.
                    !s.Host.StartsWith("127.0.0.1") &&
                    !s.Host.Contains(":"))
                .Select(c => c.Host);

            //now do any of the real
            //addresses match the ones already in
            //the host list?
            var hlist = poolingStrategy.HostList;

            //has it been discovered?
            if( !realAddresses.Any(ip => hlist.Any(s => s.Host.Contains(ip))) )
            {
                //the host IP is not found, so, see if we can connect?
                foreach( var ip in realAddresses )
                {
                    //can we connect to this IP?
                    var test = new TcpClient();
                    try
                    {
#if STANDARD
                        test.ConnectAsync(ip, port).RunSynchronously();
#else
                        test.Connect(ip, port);
#endif
                    }
                    catch
                    {
                        try
                        {
                            test.Shutdown();
                        }
                        catch
                        {
                        }
                        continue;
                    }
                    if( test.Connected )
                    {
                        test.Shutdown();
                        //good chance we can connect to it.
                        var conn = NewPoolConnection(ip, port);
                        var endPointId = EndpointIdentifier(ip, port);
                        this.poolingStrategy.AddHost(endPointId, conn);
                        Log.Trace($"{nameof(Discoverer)}: Server '{server.Name}' ({endPointId}) was added to the host pool. The supervisor will bring up the connection later.");
                        break; //stop checking IPs, one is enough.
                    }
                }
            }
            else
            {
                Log.Trace(
                    $"{nameof(Discoverer)}: Server '{server.Name}' is back, but doesn't need to be added to the pool. The supervisor will re-establish the connection later.");
            }
        }

        private string EndpointIdentifier(string ip, int port)
        {
            return $"{ip}:{port}";
        }

        /// <summary>
        /// This thread is mainly in charge of supervising the connections.
        /// The supervisor will kick off a worker to try to reconnect
        /// connections that are due for reconnecting. It also scans though
        /// the host list looking for connections that are not dead but have errors
        /// and attempts to reset them.
        /// </summary>
        private void Supervisor()
        {
            var restartWorkers = new List<Task>();

            while( true )
            {
                if( shutdownSignal.IsCancellationRequested )
                {
                    Log.Debug($"{nameof(Supervisor)}: Shutdown Signal Received");
                    break;
                }

                var hlist = poolingStrategy.HostList;

                for( int i = 0; i < hlist.Length; i++ )
                {
                    var he = hlist[i];
                    var conn = he.conn as Connection;

                    if( he.NeedsRetry() )
                    {
                        var worker = Task.Run(() =>
                            {
                                try
                                {
                                    conn.Reconnect();
                                }
                                catch( Exception e )
                                {
                                    Log.Debug($"{nameof(Supervisor)}: EXCEPTION: '{he.Host}' -- {e.Message}.");
                                }

                                if( conn.Open )
                                {
                                    Log.Debug($"{nameof(Supervisor)}: RETRY: Server '{he.Host}' is UP.");
                                    he.Dead = false;
                                    poolReady.TrySetResult(this);
                                }
                                else
                                {
                                    Log.Debug($"{nameof(Supervisor)}: RETRY: Server '{he.Host}' is DOWN.");
                                    he.RetryFailed();
                                }
                            });
                        
                        restartWorkers.Add(worker);
                    }
                }

                if( restartWorkers.Any() )
                {
                    try
                    {
                        Task.WaitAll(restartWorkers.ToArray());
                    }
                    catch{}
                    restartWorkers.Clear();
                }
                else
                {
                    Thread.Sleep(1000);
                }
            }
        }

        /// <summary>
        /// Called to create a new connection to a RethinkDB server.
        /// </summary>
        protected virtual Connection NewPoolConnection(string hostname, int port)
        {
            var connNew = new Connection(new Connection.Builder()
                {
                    user = user,
                    password = password,
                    dbname = dbname,
                    hostname = hostname,
                    port = port,
                    sslContext = sslContext,
                });

            connNew.ConnectionError += OnConnectionError;
            return connNew;
        }

        private void OnConnectionError(object sender, Exception e)
        {
            var connError = sender as Connection;
            var hlist = this.poolingStrategy.HostList;
            var he = hlist.FirstOrDefault(h => h.conn == connError);
            he?.MarkFailed();
        }

        internal static Builder Build()
        {
            return new Builder();
        }


        /// <summary>
        /// Disposes / shuts down the connection pool.
        /// </summary>
        public void Dispose()
        {
            this.Shutdown();
        }

        /// <summary>
        /// The connection pool builder.
        /// </summary>
        public class Builder : IConnectionBuilder<Builder>
        {
            internal bool discover;
            internal Seed[] seeds;
            internal string dbname;
            internal string authKey;
            internal string user;
            internal string password;
            internal IPoolingStrategy hostpool;
            internal TimeSpan? initialTimeout;
            internal SslContext sslContext;

            /// <summary>
            /// Seed the driver with the following endpoints. Should be strings of the form "Host:Port".
            /// </summary>
            /// <param name="seeds">Strings of the form "Host:Port"</param>
            public Builder Seed(params string[] seeds)
            {
                var initalSeeds = seeds.Select(s =>
                    {
                        var parts = s.Split(new[] {':'}, StringSplitOptions.RemoveEmptyEntries);
                        var host = parts[0];

                        IPAddress.Parse(host); //make sure it's an IP address.

                        var port = parts.Length == 2 ? int.Parse(parts[1]) : RethinkDBConstants.DefaultPort;

                        return new Seed(host, port);
                    });

                return Seed(initalSeeds);
            }

            /// <summary>
            /// Seed the driver with the following endpoints. Should be strings of the form "Host:Port".
            /// </summary>
            /// <param name="seeds">Strings of the form "Host:Port"</param>
            public Builder Seed(IEnumerable<string> seeds)
            {
                return Seed(seeds.ToArray());
            }

            /// <summary>
            /// Seed the driver with the following endpoints.
            /// </summary>
            public Builder Seed(IEnumerable<Seed> seeds)
            {
                this.seeds = seeds.ToArray();
                return this;
            }

            /// <summary>
            /// Seed the driver with the following endpoints.
            /// </summary>
            public Builder Seed(params Seed[] seeds)
            {
                this.seeds = seeds.ToArray();
                return this;
            }

            /// <summary>
            /// Sets the initial timeout (in seconds) for connecting. 
            /// If a connection cannot be made within the specified time span
            /// to any server, an exception is thrown. If no timeout is set,
            /// the call to Connect() will block until at least one connection
            /// is made.
            /// </summary>
            public Builder InitialTimeout(int timeout)
            {
                this.initialTimeout = TimeSpan.FromSeconds(timeout);
                return this;
            }

            /// <summary>
            /// discover() is used to enable host discovery, when true the driver
            /// will attempt to discover any new nodes added to the cluster and then
            /// start sending queries to the newly added cluster nodes.
            /// </summary>
            public Builder Discover(bool discoverNewHosts)
            {
                this.discover = discoverNewHosts;
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
            /// The authorization key to the cluster.
            /// </summary>
            public virtual Builder AuthKey(string val)
            {
                this.authKey = val;
                return this;
            }

            /// <summary>
            /// The user account and password to connect as (default "admin", "").
            /// </summary>
            public Builder User(string user, string password)
            {
                this.user = user;
                this.password = password;
                return this;
            }

            /// <summary>
            /// The selection strategy to for selecting a connection. IE: RoundRobin, HeartBeat, or EpsilonGreedy.
            /// </summary>
            public Builder PoolingStrategy(IPoolingStrategy hostPool)
            {
                this.hostpool = hostPool;
                return this;
            }

            /// <summary>
            /// Creates and establishes the connection pool using the specified settings.
            /// </summary>
            /// <returns>The returned connect pool is ready to be used. At least one host will be ready to accept a query.</returns>
            public virtual ConnectionPool Connect()
            {
                var conn = new ConnectionPool(this);
                conn.StartPool();
                if( initialTimeout.HasValue )
                {
                    if( !conn.poolReady.Task.Wait(initialTimeout.Value) )
                    {
                        conn.Shutdown();
                        throw new ReqlDriverError("Connection timed out.");
                    }
                }
                else
                {
                    conn.poolReady.Task.Wait();
                }
                return conn;
            }

            /// <summary>
            /// Asynchronously creates and establishes the connection pool using the specified settings.
            /// </summary>
            /// <returns>The returned connect pool is ready to be used. At least one host will be ready to accept a query.</returns>
            public virtual Task<ConnectionPool> ConnectAsync()
            {
                var conn = new ConnectionPool(this);
                conn.StartPool();
                return conn.poolReady.Task;
            }


            /// <summary>
            /// Enables SSL over the driver port
            /// </summary>
            /// <param name="context">Context settings for the SSL stream.</param>
            public virtual Builder EnableSsl(SslContext context, string licenseTo, string licenseKey)
            {
                this.sslContext = context;

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
}