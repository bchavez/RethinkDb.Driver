using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Remotion.Linq;
using RethinkDb.Driver.Ast;
using ExpressionVisitor = RethinkDb.Driver.Linq.WhereClauseParsers.ExpressionVisitor;

namespace RethinkDb.Driver.Linq.Visitors.SelectClause
{
    internal class MathematicalSelectClauseVisitor : ISelectClauseVisitor
    {
        public bool IsAppropriate(Remotion.Linq.Clauses.SelectClause selectClause, QueryModel queryModel)
        {
            var supportedTypes = new[]
            {
                ExpressionType.Add,
                ExpressionType.Subtract,
                ExpressionType.Multiply,
                ExpressionType.Divide,
                ExpressionType.Modulo
            };
            return supportedTypes.Contains( selectClause.Selector.NodeType );
        }

        public void Visit( Remotion.Linq.Clauses.SelectClause selectClause, QueryModel queryModel,
            Stack<ReqlExpr> stack )
        {
            var expression = (BinaryExpression)selectClause.Selector;
            stack.Push( stack.Pop().Map( x =>
            {
                var expressionVisitor = new ExpressionVisitor( x, expression.Type );
                expressionVisitor.Visit( expression );
                return expressionVisitor.Current;
            } ) );
        }
    }
}