using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Linq.WhereClauseParsers.SubQueryVisitor;

namespace RethinkDb.Driver.Linq.WhereClauseParsers
{
    public class ExpressionVisitor : ThrowingExpressionVisitor
    {
        private readonly ReqlExpr _reqlExpr;
        private readonly Type _type;
        private readonly Stack<ReqlExpr> _stack = new Stack<ReqlExpr>();

        public ExpressionVisitor( ReqlExpr reqlExpr, Type type )
        {
            _type = type;
            _reqlExpr = reqlExpr;
        }

        public ReqlExpr Current => _stack.Peek();

        protected override Exception CreateUnhandledItemException<T>( T unhandledItem, string visitMethod )
        {
            string itemText = unhandledItem.ToString();
            var message = $"The expression '{itemText}' (type: {typeof( T )}) is not supported by RethinkDB LINQ provider.";
            return new NotSupportedException( message );
        }

        private ReqlExpr VistSide( Expression expression )
        {
            if( expression.NodeType == ExpressionType.Extension && expression is QuerySourceReferenceExpression )
                return _reqlExpr;
            Visit( expression );
            return _stack.Pop();
        }

        protected override Expression VisitBinary( BinaryExpression expression )
        {
            var left = VistSide( expression.Left );
            var right = VistSide( expression.Right );

            var operation = ExprHelper.TranslateBinary( expression.NodeType, left, right );
            _stack.Push( operation );

            return expression;
        }

        protected override Expression VisitUnary( UnaryExpression expression )
        {
            if( expression.NodeType == ExpressionType.Not )
            {
                Visit( expression.Operand );
                _stack.Push( _stack.Pop().Not() );
                return null;
            }
            return base.VisitUnary( expression );
        }

        protected override Expression VisitConstant( ConstantExpression expression )
        {
            var datum = Util.ToReqlExpr( expression.Value );
            _stack.Push( datum );
            return expression;
        }

        protected override Expression VisitMember( MemberExpression expression )
        {
            Visit( expression.Expression );

            var fieldName = expression.Member.Name;
            if( _type.GetTypeInfo().IsGenericType && _type.GetGenericTypeDefinition() == typeof( IGrouping<,> ) && fieldName == "Key" )
                _stack.Push( _reqlExpr["group"] );
            else
            {
                var memberNameResolver = new MemberNameResolver( expression );
                _stack.Push( memberNameResolver.Resolve( _reqlExpr ) );
            }

            return expression;
        }

        protected override Expression VisitSubQuery( SubQueryExpression expression )
        {
            var subQueryVisitors = new List<ISubQueryVisitor>
            {
                new AnySubQueryVisitor(), 
                new AllSubQueryVisitor(),
                new FirstAndLastSubQueryVisitor(),
                new CountSubQueryVisitor(),
                new ContainsSubQueryVisitor()
            };

            var subQueryVisitor = subQueryVisitors.FirstOrDefault( x => x.CanVisit( expression.QueryModel ) );

            if( subQueryVisitor == null ) throw new NotSupportedException( "subqueries not allowed ." );

            _stack.Push( subQueryVisitor.Visit( _reqlExpr, expression.QueryModel ) );
            return null;
        }

        protected override Expression VisitQuerySourceReference( QuerySourceReferenceExpression expression )
        {
            return expression;
        }
    }
}