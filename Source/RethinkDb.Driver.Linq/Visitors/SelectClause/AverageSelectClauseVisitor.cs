using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Remotion.Linq;
using Remotion.Linq.Clauses.ResultOperators;
using RethinkDb.Driver.Ast;

namespace RethinkDb.Driver.Linq.Visitors.SelectClause
{
    internal class AverageSelectClauseVisitor : ISelectClauseVisitor
    {
        public bool IsAppropriate( Remotion.Linq.Clauses.SelectClause selectClause, QueryModel queryModel )
        {
            return queryModel.ResultOperators.FirstOrDefault() is AverageResultOperator;
        }

        public void Visit( Remotion.Linq.Clauses.SelectClause selectClause, QueryModel queryModel,
            Stack<ReqlExpr> stack )
        {
            var memberExpression = selectClause.Selector as MemberExpression;
            var memberNameResolver = new MemberNameResolver( memberExpression );
            stack.Push( stack.Pop().Avg( memberNameResolver.Resolve ) );
        }
    }
}