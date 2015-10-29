using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Net;
using RethinkDb.Driver.Proto;

namespace RethinkDb.Driver
{
    public class RethinkDB : TopLevel
    {
        /// <summary>
        /// The Singleton to use to begin interacting with RethinkDB Driver
        /// </summary>
        public static readonly RethinkDB r = new RethinkDB();

        public virtual Connection.Builder connection()
        {
            return Connection.build();
        }
    }
}