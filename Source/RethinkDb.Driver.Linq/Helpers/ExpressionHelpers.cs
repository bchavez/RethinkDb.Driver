using System.Linq.Expressions;

namespace RethinkDb.Driver.Linq.Helpers
{
    internal static class ExpressionHelpers
    {
        public static ConstantExpression GetConstantExpression(this BinaryExpression expression)
        {
            return expression.Left as ConstantExpression ?? (ConstantExpression)expression.Right;
        }
    }
}