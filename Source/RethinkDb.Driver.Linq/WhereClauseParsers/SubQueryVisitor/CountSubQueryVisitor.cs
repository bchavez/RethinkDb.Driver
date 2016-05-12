using Remotion.Linq;
using Remotion.Linq.Clauses.ResultOperators;
using RethinkDb.Driver.Ast;

namespace RethinkDb.Driver.Linq.WhereClauseParsers.SubQueryVisitor
{
    public class CountSubQueryVisitor : BaseFilterableSubQueryVisitor<CountResultOperator>
    {
        protected override ReqlExpr BuildReql( ReqlExpr reqlExpr, QueryModel queryModel ) => reqlExpr.Count();
    }
}