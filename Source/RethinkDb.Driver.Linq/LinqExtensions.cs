using Remotion.Linq;
using Remotion.Linq.Parsing.Structure;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Net;

namespace RethinkDb.Driver.Linq
{
    public static class LinqExtensions
    {
        public static RethinkQueryable<T> AsQueryable<T>( this Table term, IConnection conn )
        {
            var executor = new RethinkQueryExecutor( term, conn );
            return new RethinkQueryable<T>(
                new DefaultQueryProvider(
                    typeof( RethinkQueryable<> ),
                    QueryParser.CreateDefault(),
                    executor )
                );
        }

        public static RethinkQueryable<T> Table<T>( this Db db, string tableName, IConnection conn )
        {
            return db.Table( tableName ).AsQueryable<T>( conn );
        }
    }
}
