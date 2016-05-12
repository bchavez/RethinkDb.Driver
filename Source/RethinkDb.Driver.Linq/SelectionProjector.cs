using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;
using RethinkDb.Driver.Ast;

namespace RethinkDb.Driver.Linq
{
    public class SelectionProjector : ThrowingExpressionVisitor
    {
        private readonly Stack<ReqlExpr> _stack = new Stack<ReqlExpr>();

        public ReqlExpr Current => _stack.Count > 0 ? _stack.Peek() : null;

        protected override Exception CreateUnhandledItemException<T>( T unhandledItem, string visitMethod )
        {
            throw new NotImplementedException();
        }

        protected override Expression VisitQuerySourceReference( QuerySourceReferenceExpression expression )
        {
            return expression;
        }
    }
}