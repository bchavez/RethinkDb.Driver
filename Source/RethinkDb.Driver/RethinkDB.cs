using System.Runtime.CompilerServices;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Net;
using RethinkDb.Driver.Net.Clustering;

namespace RethinkDb.Driver
{
    public class RethinkDB : TopLevel
    {
        /// <summary>
        /// The Singleton to use to begin interacting with RethinkDB Driver
        /// </summary>
        public static readonly RethinkDB R = new RethinkDB();

        public virtual Connection.Builder Connection()
        {
            return Net.Connection.Build();
        }

        public virtual ConnectionPool.Builder ConnectionPool()
        {
            return Net.Clustering.ConnectionPool.Build();
        }
    }
}