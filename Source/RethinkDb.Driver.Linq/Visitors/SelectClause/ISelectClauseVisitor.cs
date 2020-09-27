using System.Collections.Generic;
using Remotion.Linq;
using RethinkDb.Driver.Ast;

namespace RethinkDb.Driver.Linq.Visitors.SelectClause
{
    internal interface ISelectClauseVisitor
    {
        bool IsAppropriate( Remotion.Linq.Clauses.SelectClause selectClause, QueryModel queryModel );
        void Visit( Remotion.Linq.Clauses.SelectClause selectClause, QueryModel queryModel, Stack<ReqlExpr> stack );
    }
}
