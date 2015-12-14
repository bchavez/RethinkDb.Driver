using System;
using System.Linq;
using System.Threading.Tasks;
using RethinkDb.Driver.Ast;

namespace RethinkDb.Driver.Net.Clustering
{
    public class ConnectionPool : IConnection
    {
        private string authKey;
        private string dbname;
        private string[] seeds;
        private bool discover;
        private IPoolingStrategy poolingStrategy;

        private Func<ReqlAst, object, Task> RunAtom;

        #region REQL AST RUNNERS

        Task<dynamic> IConnection.RunAsync<T>(ReqlAst term, object globalOpts)
        {
            return poolingStrategy.RunAsync<T>(term, globalOpts);
        }

        Task<Cursor<T>> IConnection.RunCursorAsync<T>(ReqlAst term, object globalOpts)
        {
            return poolingStrategy.RunCursorAsync<T>(term, globalOpts);
        }

        Task<T> IConnection.RunAtomAsync<T>(ReqlAst term, object globalOpts)
        {
            return poolingStrategy.RunAtomAsync<T>(term, globalOpts);
        }

        void IConnection.RunNoReply(ReqlAst term, object globalOpts)
        {
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

        protected virtual void StartPool()
        {
            var initialSeeds = this.seeds.Select(s =>
                {
                    var parts = s.Split(new[] {':'}, StringSplitOptions.RemoveEmptyEntries);
                    var host = parts[0];

                    var port = parts.Length == 2 ? int.Parse(parts[1]) : RethinkDBConstants.DEFAULT_PORT;

                    var conn = NewConnection(host, port);
                    return new {conn, host = s};
                });

            foreach( var conn in initialSeeds )
            {
                this.poolingStrategy.AddHost(conn.host, conn.conn);
            }
        }

        protected virtual Connection NewConnection(string hostname, int port)
        {
            return new Connection(new Connection.Builder(() => new ConnectionInstance())
                {
                    _authKey = authKey,
                    _dbname = dbname,
                    _hostname = hostname,
                    _port = port
                });
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

            /// <summary>
            /// Should be strings of the form "Host:Port".
            /// </summary>
            public Builder seed(string[] seeds)
            {
                this.seeds = seeds;
                return this;
            }

            /// <summary>
            /// discover() is used to enable host discovery, when true the driver
            /// will attempt to discover any new nodes added to the cluster and then
            /// start sending queries to these new nodes.
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

            /// <summary>
            /// The selection strategy to for selecting a connection. IE: RoundRobin, HeartBeat, or EpsilonGreedy.
            /// </summary>
            public Builder selectionStrategy(IPoolingStrategy hostPool)
            {
                this.hostpool = hostPool;
                return this;
            }

            public virtual ConnectionPool connect()
            {
                var conn = new ConnectionPool(this);
                conn.StartPool();
                return conn;
            }
        }
    }
}