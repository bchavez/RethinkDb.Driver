using System.Collections.Generic;
using System.Linq.Expressions;
using Remotion.Linq;
using RethinkDb.Driver.Ast;

namespace RethinkDb.Driver.Linq.WhereClauseVisitors
{
    internal interface IWhereClauseVisitor
    {
        IEnumerable<Expression> Visit(IEnumerable<Expression> expressions, QueryModel queryModel, Stack<ReqlExpr> stack);
    }
}