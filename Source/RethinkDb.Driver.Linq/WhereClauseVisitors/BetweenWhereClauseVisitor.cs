using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Remotion.Linq;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Linq.Attributes;
using RethinkDb.Driver.Linq.Helpers;
using RethinkDb.Driver.Utils;

namespace RethinkDb.Driver.Linq.WhereClauseVisitors
{
    internal class BetweenWhereClauseVisitor : IWhereClauseVisitor
    {
        private class WhereCondition
        {
            public BinaryExpression Expression { get; private set; }
            public MemberInfo MemberInfo { get; private set; }
            public ExpressionType NodeType { get; private set; }

            public static WhereCondition Create(Expression expression)
            {
                var binaryExpression = expression as BinaryExpression;
                if ( binaryExpression == null )
                    return null;

                var left = binaryExpression.Left as MemberExpression;
                var memberExpression = left ?? binaryExpression.Right as MemberExpression;
                if (memberExpression == null)
                    return null;

                return new WhereCondition
                {
                    Expression = (BinaryExpression)expression,
                    MemberInfo = memberExpression.Member,
                    NodeType = binaryExpression.NodeType
                };
            }
        }

        public IEnumerable<Expression> Visit(IEnumerable<Expression> expressions, QueryModel queryModel, Stack<ReqlExpr> stack)
        {
            var expressionsResult = expressions.ToList();

            var betweenExpressionTypes = new[]
            {
                ExpressionType.LessThan,
                ExpressionType.LessThanOrEqual,
                ExpressionType.GreaterThan,
                ExpressionType.GreaterThanOrEqual
            };

            var pairs = expressionsResult
                .Select(WhereCondition.Create)
                .Where(x => x != null
                            && betweenExpressionTypes.Contains(x.NodeType)
                            && x.MemberInfo.HasAttribute<SecondaryIndexAttribute>())
                .GroupBy(x => x.MemberInfo)
                .Where(x => x.Count() == 2)
                .Select(x => x.ToList())
                .ToList();

            foreach (var pair in pairs)
            {
                var lessThanTypes = new[]
                {
                    ExpressionType.LessThan,
                    ExpressionType.LessThanOrEqual
                };
                var greaterThanTypes = new[]
                {
                    ExpressionType.GreaterThan,
                    ExpressionType.GreaterThanOrEqual
                };

                var reql = stack.Pop();

                var greaterThan = pair.First(x => greaterThanTypes.Contains(x.NodeType));
                var lessThan = pair.First(x => lessThanTypes.Contains(x.NodeType));

                var greaterThanValue = greaterThan.Expression.GetConstantExpression().Value;
                var lessThanValue = lessThan.Expression.GetConstantExpression().Value;

                var query = reql.Between(greaterThanValue, lessThanValue)
                    .OptArg("index", QueryHelper.GetJsonMemberName(greaterThan.MemberInfo));

                if (greaterThan.NodeType == ExpressionType.GreaterThan)
                    query = query.OptArg("left_bound", "open");

                if (lessThan.NodeType == ExpressionType.LessThanOrEqual)
                    query = query.OptArg("right_bound", "closed");

                stack.Push(query);

                expressionsResult.Remove(pair[0].Expression);
                expressionsResult.Remove(pair[1].Expression);
            }

            return expressionsResult;
        }
    }
}
