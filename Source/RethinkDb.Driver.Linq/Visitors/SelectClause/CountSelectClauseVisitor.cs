using System.Collections.Generic;
using System.Linq;
using Remotion.Linq;
using Remotion.Linq.Clauses.ResultOperators;
using RethinkDb.Driver.Ast;

namespace RethinkDb.Driver.Linq.Visitors.SelectClause
{
    internal class CountSelectClauseVisitor : ISelectClauseVisitor
    {
        public bool IsAppropriate( Remotion.Linq.Clauses.SelectClause selectClause, QueryModel queryModel )
        {
            return queryModel.ResultOperators.FirstOrDefault() is CountResultOperator;
        }

        public void Visit( Remotion.Linq.Clauses.SelectClause selectClause, QueryModel queryModel,
            Stack<ReqlExpr> stack )
        {
            stack.Push( stack.Pop().Count() );
        }
    }
}