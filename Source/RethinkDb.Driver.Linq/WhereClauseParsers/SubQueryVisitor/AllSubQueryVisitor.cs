using System.Linq.Expressions;
using Remotion.Linq;
using Remotion.Linq.Clauses.ResultOperators;
using RethinkDb.Driver.Ast;

namespace RethinkDb.Driver.Linq.WhereClauseParsers.SubQueryVisitor
{
    public class AllSubQueryVisitor : BaseSubQueryVisitor<AllResultOperator>
    {
        public override ReqlExpr Visit( ReqlExpr reqlExpr, QueryModel queryModel )
        {
            var fromExpression = queryModel.MainFromClause.FromExpression as MemberExpression;
            var memberNameResolver = new MemberNameResolver( fromExpression );
            reqlExpr = memberNameResolver.Resolve( reqlExpr );
            reqlExpr = reqlExpr.Filter( expr => GetWhereReqlAst( expr, ( (AllResultOperator)queryModel.ResultOperators[0] ).Predicate, queryModel ).Not() );
            return reqlExpr.Count().Eq( 0 );
        }
    }
}