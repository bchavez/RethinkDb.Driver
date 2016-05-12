using System;
using System.Linq.Expressions;
using System.Reflection;
using Remotion.Linq;
using RethinkDb.Driver.Ast;

namespace RethinkDb.Driver.Linq.WhereClauseParsers
{
    public class DefaultParser : IWhereClauseParser
    {
        public bool IsAppropriate( ReqlAst reql, Expression expression, Type resultType ) => true;

        public ReqlExpr Parse( ReqlExpr expression, QueryModel queryModel, Expression predicate )
        {
            return expression.Filter( reqlExpr => GetWhereReqlAst( reqlExpr, predicate, queryModel ) );
        }

        private static ReqlExpr GetWhereReqlAst( ReqlExpr reqlExpr, Expression predicate, QueryModel queryModel )
        {
            var visitor = new ExpressionVisitor( reqlExpr, GetResultType( queryModel ) );
            visitor.Visit( predicate );
            return visitor.Current;
        }

        private static Type GetResultType( QueryModel queryModel )
        {
            if( !queryModel.ResultTypeOverride.GetTypeInfo().IsGenericType )
                return queryModel.ResultTypeOverride;
            return queryModel.ResultTypeOverride.GetTypeInfo().GenericTypeArguments[0];
        }
    }
}
