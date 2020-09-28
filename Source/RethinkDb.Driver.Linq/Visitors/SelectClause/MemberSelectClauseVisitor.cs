using System.Collections.Generic;
using System.Linq.Expressions;
using Remotion.Linq;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Utils;

namespace RethinkDb.Driver.Linq.Visitors.SelectClause
{
    internal class MemberSelectClauseVisitor : ISelectClauseVisitor
    {
        public bool IsAppropriate( Remotion.Linq.Clauses.SelectClause selectClause, QueryModel queryModel )
        {
            return selectClause.Selector is MemberExpression;
        }

        public void Visit( Remotion.Linq.Clauses.SelectClause selectClause, QueryModel queryModel,
            Stack<ReqlExpr> stack )
        {
            var selectExpression = (MemberExpression) selectClause.Selector;
            var memberName = QueryHelper.GetJsonMemberName( selectExpression.Member );
            stack.Push( stack.Pop().Map( x => x[memberName] ) );
        }
    }
}