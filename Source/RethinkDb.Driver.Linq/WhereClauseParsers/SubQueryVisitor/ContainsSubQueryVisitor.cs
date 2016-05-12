using System.Collections.Generic;
using System.Linq.Expressions;
using Remotion.Linq;
using Remotion.Linq.Clauses.ResultOperators;
using RethinkDb.Driver.Ast;

namespace RethinkDb.Driver.Linq.WhereClauseParsers.SubQueryVisitor
{
    public class ContainsSubQueryVisitor : BaseSubQueryVisitor<ContainsResultOperator>
    {
        public override ReqlExpr Visit( ReqlExpr reqlExpr, QueryModel queryModel )
        {
            var fromExpression = queryModel.MainFromClause.FromExpression as ConstantExpression;
            var array = RethinkDB.R.Expr( fromExpression.Value as IEnumerable<object> );

            var resultOperator = queryModel.ResultOperators[0] as ContainsResultOperator;

            var memberNameResolver = new MemberNameResolver( (MemberExpression)resultOperator.Item );
            return array.Contains( memberNameResolver.Resolve( reqlExpr ) );
        }
    }
}