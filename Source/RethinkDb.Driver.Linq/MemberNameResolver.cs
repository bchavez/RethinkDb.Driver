using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Remotion.Linq.Clauses.Expressions;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Linq.WhereClauseParsers.SubQueryVisitor;

namespace RethinkDb.Driver.Linq
{
    public class MemberNameResolver
    {
        private readonly MemberExpression _expression;

        public MemberNameResolver( MemberExpression expression )
        {
            _expression = expression;
        }

        public ReqlExpr Resolve( ReqlExpr reqlExpr ) => ResolveMemberExpression( reqlExpr, _expression );

        private static ReqlExpr ResolveMemberExpression( ReqlExpr reqlExpr, MemberExpression expression )
        {
            if( expression.Expression.NodeType == ExpressionType.MemberAccess )
                reqlExpr = ResolveMemberExpression( reqlExpr, (MemberExpression)expression.Expression );
            if( expression.Expression.NodeType == ExpressionType.Extension && expression.Expression is SubQueryExpression )
                reqlExpr = ResolveExtensionExpression( reqlExpr, (SubQueryExpression)expression.Expression );
            return reqlExpr[expression.Member.Name];
        }

        private static ReqlExpr ResolveExtensionExpression( ReqlExpr reqlExpr, SubQueryExpression expression )
        {
            var subQueryVisitors = new List<ISubQueryVisitor>
            {
                new AnySubQueryVisitor(),
                new AllSubQueryVisitor(),
                new FirstAndLastSubQueryVisitor()
            };

            var subQueryVisitor = subQueryVisitors.FirstOrDefault( x => x.CanVisit( expression.QueryModel ) );

            if( subQueryVisitor == null )
                throw new NotSupportedException( "subqueries not allowed ." );

            return subQueryVisitor.Visit( reqlExpr, expression.QueryModel );
        }
    }
}