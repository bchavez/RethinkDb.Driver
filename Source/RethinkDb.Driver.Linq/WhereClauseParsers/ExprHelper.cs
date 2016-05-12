using System;
using System.Linq.Expressions;
using RethinkDb.Driver.Ast;

namespace RethinkDb.Driver.Linq.WhereClauseParsers
{
    public static class ExprHelper
    {
        public static ReqlExpr TranslateUnary( ExpressionType type, ReqlExpr term )
        {
            switch( type )
            {
                case ExpressionType.Not:
                    return term.Not();
                default:
                    throw new NotSupportedException( "Unary term not supported." );
            }
        }

        public static ReqlExpr TranslateBinary( ExpressionType type, ReqlExpr left, ReqlExpr right )
        {
            switch( type )
            {
                case ExpressionType.Equal:
                    return left.Eq( right );
                case ExpressionType.NotEqual:
                    return left.Eq( right ).Not();
                case ExpressionType.LessThan:
                    return left.Lt( right );
                case ExpressionType.LessThanOrEqual:
                    return left.Le( right );
                case ExpressionType.GreaterThan:
                    return left.Gt( right );
                case ExpressionType.GreaterThanOrEqual:
                    return left.Ge( right );
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    return left.And( right );
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    return left.Or( right );
                case ExpressionType.Not:
                    throw new InvalidOperationException( "ExpresionType:Not cannot be called on a binary translation." );
                case ExpressionType.Add:
                    return left.Add( right );
                case ExpressionType.Subtract:
                    return left.Sub( right );
                case ExpressionType.Multiply:
                    return left.Mul( right );
                case ExpressionType.Divide:
                    return left.Div( right );
                case ExpressionType.Modulo:
                    return left.Mod( right );
                default:
                    throw new NotSupportedException( "Binary expression not supported." );
            }
        }

    }
}