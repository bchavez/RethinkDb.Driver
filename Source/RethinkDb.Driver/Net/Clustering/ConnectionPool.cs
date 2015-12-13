using System;
using System.Threading.Tasks;
using RethinkDb.Driver.Ast;

namespace RethinkDb.Driver.Net.Clustering
{
    public class ConnectionPool : IConnection
    {
        #region REQL AST RUNNERS

        Task<dynamic> IConnection.RunAsync<T>(ReqlAst term, object globalOpts)
        {
            throw new NotImplementedException();
        }

        Task<Cursor<T>> IConnection.RunCursorAsync<T>(ReqlAst term, object globalOpts)
        {
            throw new NotImplementedException();
        }

        Task<T> IConnection.RunAtomAsync<T>(ReqlAst term, object globalOpts)
        {
            throw new NotImplementedException();
        }

        void IConnection.RunNoReply(ReqlAst term, object globalOpts)
        {
            throw new NotImplementedException();
        }

        #endregion

        internal ConnectionPool(Builder builder)
        {
        }


        protected virtual void StartPool()
        {
            
        }

        public static Builder build()
        {
            return new Builder();
        }

        public class Builder
        {
            internal bool _discover;
            private string[] seeds;
            private string _dbname;
            private string _authKey;
            private IPoolingStrategy hostpool;

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