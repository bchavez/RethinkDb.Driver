using System.Linq;
using System.Linq.Expressions;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using RethinkDb.Driver.Ast;

namespace RethinkDb.Driver.Linq.WhereClauseParsers.SubQueryVisitor
{
    public abstract class BaseFilterableSubQueryVisitor<T> : BaseSubQueryVisitor<T>
    {
        public override ReqlExpr Visit( ReqlExpr reqlExpr, QueryModel queryModel )
        {
            var fromExpression = queryModel.MainFromClause.FromExpression as MemberExpression;
            var memberNameResolver = new MemberNameResolver( fromExpression );
            reqlExpr = memberNameResolver.Resolve( reqlExpr );
            if( queryModel.BodyClauses.Any() )
                reqlExpr = reqlExpr.Filter( expr => GetWhereReqlAst( expr, ( (WhereClause)queryModel.BodyClauses[0] ).Predicate, queryModel ) );
            return BuildReql( reqlExpr, queryModel );
        }

        protected abstract ReqlExpr BuildReql( ReqlExpr reqlExpr, QueryModel queryModel );
    }
}