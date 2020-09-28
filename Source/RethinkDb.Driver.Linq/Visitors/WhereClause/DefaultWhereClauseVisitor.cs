using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Remotion.Linq;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Linq.WhereClauseParsers;

namespace RethinkDb.Driver.Linq.Visitors.WhereClause
{
    internal class DefaultWhereClauseVisitor : IWhereClauseVisitor
    {
        public IEnumerable<Expression> Visit(IEnumerable<Expression> expressions, QueryModel queryModel, Stack<ReqlExpr> stack)
        {
            var expressionsResult = expressions.ToList();

            foreach (var expression in expressionsResult.ToList())
            {
                var whereClauseParsers = new List<IWhereClauseParser>
                {
                    new GroupItemsParser(),
                    new PrimaryIndexParser(),
                    new SecondaryIndexParser(),
                    new DefaultParser()
                };

                var reql = stack.Pop();
                var matchingParser = whereClauseParsers.FirstOrDefault(x =>
                    x.IsAppropriate(reql, expression, queryModel.ResultTypeOverride));
                if (matchingParser != null)
                {
                    stack.Push(matchingParser.Parse(reql, queryModel, expression));
                    expressionsResult.Remove(expression);
                }
            }

            return expressionsResult;
        }
    }
}