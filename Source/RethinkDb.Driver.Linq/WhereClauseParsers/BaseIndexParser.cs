using System;
using System.Linq;
using System.Linq.Expressions;
using Remotion.Linq;
using RethinkDb.Driver.Ast;

namespace RethinkDb.Driver.Linq.WhereClauseParsers
{
    public abstract class BaseIndexParser<T> : IWhereClauseParser
    {
        public bool IsAppropriate( ReqlAst reql, Expression expression, Type resultType )
        {
            var binaryExpression = expression as BinaryExpression;

            if( !( reql is Table ) || binaryExpression?.NodeType != ExpressionType.Equal )
                return false;

            var left = binaryExpression.Left as MemberExpression;
            if( left != null )
                return IsIndex( left );

            var right = binaryExpression.Right as MemberExpression;

            return right != null && IsIndex( right );
        }

        private static bool IsIndex( MemberExpression left )
        {
            return left.Member.CustomAttributes.Any( x => x.AttributeType == typeof( T ) );
        }

        public abstract ReqlExpr Parse( ReqlExpr expression, QueryModel queryModel, Expression predicate );

        protected static object GetValue( BinaryExpression binaryExpression )
        {
            var left = binaryExpression.Left as ConstantExpression;
            return left != null ? left.Value : ( (ConstantExpression)binaryExpression.Right ).Value;
        }
    }
}