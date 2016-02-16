using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Net;
using RethinkDb.Driver.Net.Clustering;

namespace RethinkDb.Driver
{
    /// <summary>
    /// The RethinkDB API
    /// </summary>
    public class RethinkDB : TopLevel
    {
        /// <summary>
        /// The Singleton to use to begin interacting with RethinkDB Driver
        /// </summary>
        public static readonly RethinkDB R = new RethinkDB();

        /// <summary>
        /// A connection builder that can create a single connection to a RethinkDB.
        /// </summary>
        /// <returns>A connection builder for setting connection properties fluently.</returns>
        public virtual Connection.Builder Connection()
        {
            return Net.Connection.Build();
        }

        /// <summary>
        /// A connection builder that can create a pooled connection to a RethinkDB cluster.
        /// </summary>
        /// <returns>A connection builder for setting connection pool properties fluently.</returns>
        public virtual ConnectionPool.Builder ConnectionPool()
        {
            return Net.Clustering.ConnectionPool.Build();
        }
    }
}