using Remotion.Linq;
using Remotion.Linq.Clauses.ResultOperators;
using RethinkDb.Driver.Ast;

namespace RethinkDb.Driver.Linq.WhereClauseParsers.SubQueryVisitor
{
    public class AnySubQueryVisitor : BaseFilterableSubQueryVisitor<AnyResultOperator>
    {
        protected override ReqlExpr BuildReql( ReqlExpr reqlExpr, QueryModel queryModel ) => reqlExpr.Count().Gt( 0 );
    }
}
