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
    public class ConnectionPool : IConnection
    {
        private string authKey;
        private string dbname;
        private string[] seeds;
        private bool discover;
        private IPoolingStrategy poolingStrategy;

        
        

        #region REQL AST RUNNERS

        Task<dynamic> IConnection.RunAsync<T>(ReqlAst term, object globalOpts)
        {
            if( this.shutdownSignal.IsCancellationRequested )
            {
                throw new ReqlDriverError("HostPool is shutdown.");
            }
            return poolingStrategy.RunAsync<T>(term, globalOpts);
        }

        Task<Cursor<T>> IConnection.RunCursorAsync<T>(ReqlAst term, object globalOpts)
        {
            if (this.shutdownSignal.IsCancellationRequested)
            {
                throw new ReqlDriverError("HostPool is shutdown.");
            }
            return poolingStrategy.RunCursorAsync<T>(term, globalOpts);
        }

        Task<T> IConnection.RunAtomAsync<T>(ReqlAst term, object globalOpts)
        {
            if (this.shutdownSignal.IsCancellationRequested)
            {
                throw new ReqlDriverError("HostPool is shutdown.");
            }
            return poolingStrategy.RunAtomAsync<T>(term, globalOpts);
        }

        Task<T> IConnection.RunResultAsync<T>(ReqlAst term, object globalOpts)
        {
            if (this.shutdownSignal.IsCancellationRequested)
            {
                throw new ReqlDriverError("HostPool is shutdown.");
            }
            return poolingStrategy.RunResultAsync<T>(term, globalOpts);
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
            authKey = builder._authKey;
            dbname = builder._dbname;
            seeds = builder.seeds;
            discover = builder._discover;
            poolingStrategy = builder.hostpool;
        }


        /// <summary>
        /// Shutdown the connection pool.
        /// </summary>
        public void shutdown()
        {
            shutdownSignal?.Cancel();
            poolingStrategy?.Shutdown();

            if ( poolingStrategy != null )
            {
                //shutdown all connections.
                foreach( var h in poolingStrategy.HostList )
                {
                    var conn = h.conn as Connection;
                    conn.close(false);
                }
            }
        }

        private CancellationTokenSource shutdownSignal;
        private TaskCompletionSource<ConnectionPool> poolReady;

        protected virtual void StartPool()
        {
            shutdownSignal = new CancellationTokenSource();
            poolReady = new TaskCompletionSource<ConnectionPool>();

            if( poolingStrategy == null )
            {
                throw new ArgumentNullException(nameof(poolingStrategy),
                    $"You must specify a pooling strategy '{nameof(Builder.poolingStrategy)}' when building the connection pool.");
            }

            var initialSeeds = this.seeds.Select(s =>
                {
                    var parts = s.Split(new[] {':'}, StringSplitOptions.RemoveEmptyEntries);
                    var host = parts[0];

                    IPAddress.Parse(host); //make sure it's an IP address.

                    var port = parts.Length == 2 ? int.Parse(parts[1]) : RethinkDBConstants.DEFAULT_PORT;

                    var conn = NewPoolConnection(host, port);
                    return new {conn, host = s};
                });

            foreach( var conn in initialSeeds )
            {
                this.poolingStrategy.AddHost(conn.host, conn.conn);
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

            var changeFeed = r.Db("rethinkdb").Table("server_status").Changes()[new { include_initial = true }];

            while ( true )
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

                    foreach ( var change in cursor )
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
            if (!realAddresses.Any(ip => hlist.Any(s => s.Host.Contains(ip))))
            {
                //the host IP is not found, so, see if we can connect?
                foreach (var ip in realAddresses)
                {
                    //can we connect to this IP?
                    var test = new TcpClient();
                    try
                    {
                        test.Connect(ip, port);
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
                    if (test.Connected)
                    {
                        test.Shutdown();
                        //good chance we can connect to it.
                        var conn = NewPoolConnection(ip, port);
                        var host = $"{ip}:{port}";
                        this.poolingStrategy.AddHost(host, conn);
                        Log.Trace($"{nameof(Discoverer)}: Server '{server.Name}' ({host}) was added to the host pool. The supervisor will bring up the connection later.");
                        break; //stop checking IPs, one is enough.
                    }
                }
            }
            else
            {
                Log.Trace($"{nameof(Discoverer)}: Server '{server.Name}' is back, but doesn't need to be added to the pool. The supervisor will re-establish the connection later.");
            }
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

            while ( true )
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

                    if ( he.NeedsRetry() )
                    {

                        var worker = Task.Run(() =>
                            {
                                conn.reconnect();

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
                    Task.WaitAll(restartWorkers.ToArray());
                    restartWorkers.Clear();
                }
                else
                {
                    Thread.Sleep(1000);
                }
            }
        }

        protected virtual Connection NewPoolConnection(string hostname, int port)
        {
            var connNew = new Connection(new Connection.Builder()
                {
                    _authKey = authKey,
                    _dbname = dbname,
                    _hostname = hostname,
                    _port = port
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

        public static Builder build()
        {
            return new Builder();
        }

        public class Builder
        {
            internal bool _discover;
            internal string[] seeds;
            internal string _dbname;
            internal string _authKey;
            internal IPoolingStrategy hostpool;
            internal TimeSpan _supervisePeriod;

            /// <summary>
            /// Seed the driver with the following endpoints. Should be strings of the form "Host:Port".
            /// </summary>
            public Builder seed(string[] seeds)
            {
                this.seeds = seeds;
                return this;
            }

            /// <summary>
            /// discover() is used to enable host discovery, when true the driver
            /// will attempt to discover any new nodes added to the cluster and then
            /// start sending queries to the newly added cluster nodes.
            /// </summary>
            public Builder discover(bool discoverNewHosts)
            {
                this._discover = discoverNewHosts;
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

            public virtual Builder superviseEvery(TimeSpan period)
            {
                this._supervisePeriod = period;
                return this;
            }

            /// <summary>
            /// The selection strategy to for selecting a connection. IE: RoundRobin, HeartBeat, or EpsilonGreedy.
            /// </summary>
            public Builder poolingStrategy(IPoolingStrategy hostPool)
            {
                this.hostpool = hostPool;
                return this;
            }

            public virtual ConnectionPool connect()
            {
                var conn = new ConnectionPool(this);
                conn.StartPool();
                conn.poolReady.Task.Wait();
                return conn;
            }

            public virtual Task<ConnectionPool> connectAsync()
            {
                var conn = new ConnectionPool(this);
                conn.StartPool();
                return conn.poolReady.Task;
            }
        }

        public void Dispose()
        {
            this.shutdown();
        }
    }
}