using System.Linq;
using System.Linq.Expressions;
using Remotion.Linq;
using Remotion.Linq.Parsing.Structure;

namespace RethinkDb.Driver.Linq
{
    public class RethinkQueryable<T> : QueryableBase<T>
    {
        public RethinkQueryable( IQueryParser queryParser, IQueryExecutor executor ) : base( queryParser, executor )
        {
        }

        public RethinkQueryable( IQueryProvider provider ) : base( provider )
        {
        }

        public RethinkQueryable( IQueryProvider provider, Expression expression ) : base( provider, expression )
        {
        }
    }
}